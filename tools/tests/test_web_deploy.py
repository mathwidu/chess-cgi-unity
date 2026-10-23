from contextlib import ExitStack
import fcntl
import importlib.util
import io
import json
from pathlib import Path
import queue
import subprocess
import sys
import tempfile
import threading
from types import SimpleNamespace
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[2]


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, ROOT / path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


deploy = load('web_deploy', 'tools/deploy_web.py')
activate = load('web_activate', 'deploy/web/activate_after_dns.py')
apply = load('web_apply', 'deploy/web/apply_release.py')
REAL_FLOCK = fcntl.flock
NEW_IMAGE = 'ghcr.io/mathwidu/chess-cgi-unity-web@sha256:' + '1' * 64
OLD_IMAGE = 'ghcr.io/mathwidu/chess-cgi-unity-web@sha256:' + '2' * 64
NEW_REVISION = '3' * 40
OLD_REVISION = '4' * 40


class DeployGuards(unittest.TestCase):
    def test_wrong_registry_or_mutable_tag_rejected_before_docker(self):
        with patch.object(deploy.subprocess, 'run') as run:
            for image in ('nginx:latest', 'ghcr.io/mathwidu/chess-cgi-unity-web:latest',
                          'ghcr.io/other/game@sha256:' + '1' * 64):
                with self.assertRaises(ValueError):
                    deploy.render(image, '2' * 40)
            run.assert_not_called()

    def test_render_has_no_host_ports_or_data_volumes_and_no_early_acme(self):
        with patch.object(deploy.subprocess, 'run'):
            result = json.loads(deploy.render('ghcr.io/mathwidu/chess-cgi-unity-web@sha256:' + '1' * 64, '2' * 40))
        service = result['services']['web']
        self.assertNotIn('ports', service)
        self.assertNotIn('volumes', service)
        self.assertEqual(service['deploy']['resources']['limits']['memory'], '128M')
        self.assertNotIn(activate.RESOLVER_LABEL, service['deploy']['labels'])
        self.assertEqual(service['deploy']['labels']['com.chess-cgi.source-revision'], '2' * 40)

    def test_dns_requires_only_the_reviewed_ipv4(self):
        self.assertTrue(activate.dns_ready(['187.77.47.96']))
        self.assertFalse(activate.dns_ready([]))
        self.assertFalse(activate.dns_ready(['127.0.0.1']))
        self.assertFalse(activate.dns_ready(['187.77.47.96', '2001:db8::1']))
        self.assertFalse(activate.dns_ready(['187.77.47.96', '192.0.2.1']))

    def test_old_activator_cannot_change_another_release(self):
        labels = {'com.chess-cgi.source-revision':'2' * 40, 'com.chess-cgi.managed-by':'chess-web-release'}
        with patch.object(activate, 'read_service', return_value=('other-image', labels)):
            with self.assertRaises(ValueError):
                activate.assert_release('expected-image', '2' * 40)
        with patch.object(activate, 'read_service', return_value=('expected-image', {})):
            with self.assertRaises(ValueError):
                activate.assert_release('expected-image', '2' * 40)


class ApplyRollbackTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory(prefix='chess-deploy-rollback-')
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name)
        self.existing = False
        self.commands = []
        self.stack = {'services': {'web': {'image': NEW_IMAGE, 'deploy': {'labels': {
            'com.chess-cgi.managed-by': 'chess-web-release',
            'com.chess-cgi.source-revision': NEW_REVISION,
        }}}}}
        self.payload = {'image': NEW_IMAGE, 'revision': NEW_REVISION,
                        'stack': json.dumps(self.stack), 'activator': '# Never executed by these tests\n'}

    def save_previous(self):
        self.existing = True
        previous = {'services': {'web': {'image': OLD_IMAGE, 'deploy': {'labels': {
            'com.chess-cgi.managed-by': 'chess-web-release',
            'com.chess-cgi.source-revision': OLD_REVISION,
            activate.RESOLVER_LABEL: 'letsencryptresolver',
        }}}}}
        content = (json.dumps(previous, indent=2) + '\n').encode()
        (self.root / 'current-stack.json').write_bytes(content)
        return content

    def docker(self, *args, **kwargs):
        self.commands.append(args)
        if args[:3] == ('docker', 'service', 'ls'):
            return '\n'.join(([apply.SERVICE] if self.existing else []) + ['unrelated_web'])
        if args[:3] == ('docker', 'service', 'inspect'):
            return json.dumps({'com.chess-cgi.managed-by': 'chess-web-release'} if args[3] == apply.SERVICE else {})
        if args[:2] == ('docker', 'pull'):
            return ''
        if args[:3] == ('docker', 'image', 'inspect'):
            return 'linux/amd64' if args[-1] == '{{.Os}}/{{.Architecture}}' else NEW_REVISION
        if args[:3] in (('docker', 'stack', 'deploy'), ('docker', 'stack', 'rm')):
            return ''
        raise AssertionError('Unexpected Docker/external command: ' + repr(args))

    def rollback_patches(self, healthy, removed=True, docker=None):
        stack = ExitStack()
        self.addCleanup(stack.close)
        stack.enter_context(patch.object(apply, 'run', side_effect=docker or self.docker))
        health = stack.enter_context(patch.object(apply, 'wait_healthy', side_effect=healthy))
        removal = stack.enter_context(patch.object(apply, 'wait_removed', return_value=removed))
        activation = stack.enter_context(patch.object(apply.subprocess, 'Popen'))
        return health, removal, activation

    def invoke(self):
        apply.apply_locked(self.payload, NEW_IMAGE, NEW_REVISION, self.root)

    def test_first_install_health_failure_removes_only_chess_stack_and_waits(self):
        health, removal, activation = self.rollback_patches([False])
        with self.assertRaisesRegex(RuntimeError, 'rollback verified'):
            self.invoke()
        health.assert_called_once_with(NEW_IMAGE)
        removal.assert_called_once_with()
        mutations = [command for command in self.commands if command[:2] == ('docker', 'stack')]
        self.assertEqual(len(mutations), 2)
        self.assertEqual(mutations[0][:3], ('docker', 'stack', 'deploy'))
        self.assertEqual(mutations[0][-1], 'chess-web')
        self.assertEqual(mutations[1], ('docker', 'stack', 'rm', 'chess-web'))
        self.assertFalse((self.root / 'current-stack.json').exists())
        activation.assert_not_called()

    def test_update_failure_restores_previous_stack_and_requires_its_health(self):
        previous = self.save_previous()
        health, removal, activation = self.rollback_patches([False, True])
        with self.assertRaisesRegex(RuntimeError, 'rollback verified'):
            self.invoke()
        self.assertEqual([call.args for call in health.call_args_list], [(NEW_IMAGE,), (OLD_IMAGE,)])
        removal.assert_not_called()
        commands = [command for command in self.commands if command[:3] == ('docker', 'stack', 'deploy')]
        self.assertEqual(len(commands), 2)
        self.assertEqual(commands[0][-1], 'chess-web')
        self.assertEqual(commands[1][-1], 'chess-web')
        rollback_path = Path(commands[1][commands[1].index('--compose-file') + 1])
        self.assertEqual(rollback_path.name, 'previous-stack.json')
        self.assertEqual(rollback_path.read_bytes(), previous)
        self.assertEqual((self.root / 'current-stack.json').read_bytes(), previous)
        self.assertNotIn(('docker', 'stack', 'rm', 'chess-web'), self.commands)
        activation.assert_not_called()

    def test_rollback_without_healthy_previous_release_reports_nonconvergence(self):
        previous = self.save_previous()
        health, removal, activation = self.rollback_patches([False, False])
        with self.assertRaisesRegex(RuntimeError, 'rollback did not converge') as failure:
            self.invoke()
        self.assertIn('new image did not become healthy', str(failure.exception.__cause__))
        self.assertEqual(health.call_count, 2)
        self.assertEqual((self.root / 'current-stack.json').read_bytes(), previous)
        removal.assert_not_called()
        activation.assert_not_called()

    def test_first_install_removal_must_converge_before_reporting_rollback_verified(self):
        _, removal, activation = self.rollback_patches([False], removed=False)
        with self.assertRaisesRegex(RuntimeError, 'rollback did not converge'):
            self.invoke()
        removal.assert_called_once_with()
        activation.assert_not_called()

    def test_candidate_docker_failure_still_restores_previous_release(self):
        self.save_previous()
        deployments = []

        def failing_candidate(*args, **kwargs):
            result = self.docker(*args, **kwargs)
            if args[:3] == ('docker', 'stack', 'deploy'):
                deployments.append(args)
                if len(deployments) == 1:
                    raise subprocess.CalledProcessError(1, args, output='Candidate deploy failed')
            return result

        health, removal, activation = self.rollback_patches([True], docker=failing_candidate)
        with self.assertRaisesRegex(RuntimeError, 'rollback verified') as failure:
            self.invoke()
        self.assertIsInstance(failure.exception.__cause__, subprocess.CalledProcessError)
        self.assertEqual(len(deployments), 2)
        health.assert_called_once_with(OLD_IMAGE)
        removal.assert_not_called()
        activation.assert_not_called()

    def test_rollback_docker_failure_is_propagated_without_false_success(self):
        previous = self.save_previous()

        def failing_rollback(*args, **kwargs):
            result = self.docker(*args, **kwargs)
            if args[:3] == ('docker', 'stack', 'deploy') and any('previous-stack.json' in part for part in args):
                raise subprocess.CalledProcessError(7, args, output='Rollback deploy failed')
            return result

        health, removal, activation = self.rollback_patches([False], docker=failing_rollback)
        with self.assertRaises(subprocess.CalledProcessError) as failure:
            self.invoke()
        self.assertEqual(failure.exception.returncode, 7)
        self.assertIn('previous-stack.json', ' '.join(failure.exception.cmd))
        self.assertEqual((self.root / 'current-stack.json').read_bytes(), previous)
        health.assert_called_once_with(NEW_IMAGE)
        removal.assert_not_called()
        activation.assert_not_called()


class ConvergenceTests(unittest.TestCase):
    def test_health_requires_exact_new_image_and_healthy_state(self):
        observations = iter(('prior\ncandidate-starting', 'candidate-ready'))

        def docker(*args, **kwargs):
            if args[:2] == ('docker', 'ps'):
                return next(observations)
            if args[:2] == ('docker', 'inspect'):
                return {'prior': OLD_IMAGE + ' healthy',
                        'candidate-starting': NEW_IMAGE + ' starting',
                        'candidate-ready': NEW_IMAGE + ' healthy'}[args[2]]
            raise AssertionError('Unexpected command: ' + repr(args))

        with patch.object(apply, 'run', side_effect=docker), patch.object(apply.time, 'sleep') as sleep:
            self.assertTrue(apply.wait_healthy(NEW_IMAGE))
        sleep.assert_called_once_with(5)

    def test_removed_waits_until_service_is_absent_and_reports_timeout(self):
        with patch.object(apply, 'service_exists', side_effect=[True, True, False]), patch.object(apply.time, 'sleep') as sleep:
            self.assertTrue(apply.wait_removed())
        self.assertEqual(sleep.call_count, 2)
        with patch.object(apply, 'service_exists', return_value=True) as exists, patch.object(apply.time, 'sleep'):
            self.assertFalse(apply.wait_removed())
        self.assertEqual(exists.call_count, 12)


class RealReleaseLockTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory(prefix='chess-deploy-lock-')
        self.addCleanup(temporary.cleanup)
        self.home = Path(temporary.name)
        self.root = self.home / '.local/share/chess-web'
        self.root.mkdir(parents=True)
        self.payload = {'image': NEW_IMAGE, 'revision': NEW_REVISION, 'stack': '{}', 'activator': ''}

    @staticmethod
    def thread(function):
        outcome = queue.Queue()

        def run():
            try:
                outcome.put(('return', function()))
            except BaseException as error:
                outcome.put(('error', error))

        worker = threading.Thread(target=run, daemon=True)
        worker.start()
        return worker, outcome

    def preflight(self, *args, **kwargs):
        if args[:2] == ('docker', 'info'):
            return 'true'
        if args[:3] == ('docker', 'network', 'inspect'):
            return 'overlay swarm'
        raise AssertionError('Reached inventory or an unexpected external command: ' + repr(args))

    def main_patches(self):
        stack = ExitStack()
        self.addCleanup(stack.close)
        stack.enter_context(patch.object(apply.sys, 'stdin', io.StringIO(json.dumps(self.payload))))
        stack.enter_context(patch.object(apply.Path, 'home', return_value=self.home))
        stack.enter_context(patch.object(apply.pwd, 'getpwuid', return_value=SimpleNamespace(pw_name='mathwidu')))
        stack.enter_context(patch.object(apply.socket, 'gethostname', return_value='srv1397065'))
        stack.enter_context(patch.object(apply, 'run', side_effect=self.preflight))
        return stack

    def test_second_apply_cannot_reach_inventory_while_first_descriptor_holds_lock(self):
        self.main_patches()
        with patch.object(apply, 'service_exists') as inventory:
            with (self.root / '.deploy.lock').open('a') as first:
                REAL_FLOCK(first, fcntl.LOCK_EX)
                worker, outcome = self.thread(apply.main)
                try:
                    kind, result = outcome.get(timeout=5)
                    self.assertEqual(kind, 'error', 'A concurrent apply must fail before mutating/inventorying services')
                    self.assertIsInstance(result, BlockingIOError)
                    inventory.assert_not_called()
                finally:
                    REAL_FLOCK(first, fcntl.LOCK_UN)
                    worker.join(timeout=5)
                self.assertFalse(worker.is_alive())

    def test_apply_exception_releases_lock_for_next_operation(self):
        self.main_patches()
        with patch.object(apply, 'apply_locked', side_effect=RuntimeError('Simulated apply failure')):
            with self.assertRaisesRegex(RuntimeError, 'Simulated apply failure'):
                apply.main()
        with (self.root / '.deploy.lock').open('a') as independent:
            REAL_FLOCK(independent, fcntl.LOCK_EX | fcntl.LOCK_NB)
            REAL_FLOCK(independent, fcntl.LOCK_UN)

    def test_activator_waits_for_update_then_survives_restored_previous_release(self):
        directory = self.root / 'releases/previous-release'
        directory.mkdir(parents=True)
        current = {'services': {'web': {'image': OLD_IMAGE, 'deploy': {'labels': {
            'com.chess-cgi.managed-by': 'chess-web-release',
            'com.chess-cgi.source-revision': OLD_REVISION,
        }}}}}
        (self.root / 'current-stack.json').write_text(json.dumps(current))
        # The updater currently points at a candidate; its decision is pending.
        service = {'image': NEW_IMAGE, 'revision': NEW_REVISION, 'resolver': None}
        events = queue.Queue()
        reads = []

        def read_service():
            events.put('read_service')
            reads.append(service['image'])
            labels = {'com.chess-cgi.managed-by': 'chess-web-release',
                      'com.chess-cgi.source-revision': service['revision']}
            if service['resolver']:
                labels[activate.RESOLVER_LABEL] = service['resolver']
            return service['image'], labels

        def waiting_flock(descriptor, operation):
            events.put('lock_attempt')
            return REAL_FLOCK(descriptor, operation)

        def update_service(command, **kwargs):
            self.assertEqual(command, ['docker', 'service', 'update', '--detach=true', '--label-add',
                                       activate.RESOLVER_LABEL + '=letsencryptresolver', activate.SERVICE])
            service['resolver'] = 'letsencryptresolver'
            return SimpleNamespace(returncode=0)

        argv = ['activate_after_dns.py', '--image', OLD_IMAGE, '--revision', OLD_REVISION,
                '--directory', str(directory)]
        with (self.root / '.deploy.lock').open('a') as updater:
            REAL_FLOCK(updater, fcntl.LOCK_EX)
            with ExitStack() as stack:
                stack.enter_context(patch.object(sys, 'argv', argv))
                stack.enter_context(patch.object(activate.socket, 'gethostname', return_value='srv1397065'))
                stack.enter_context(patch.object(activate.socket, 'getaddrinfo', return_value=[
                    (2, 1, 6, '', (activate.EXPECTED_IPV4, 443))]))
                stack.enter_context(patch.object(activate, 'read_service', side_effect=read_service))
                stack.enter_context(patch.object(activate.fcntl, 'flock', side_effect=waiting_flock))
                update = stack.enter_context(patch.object(activate.subprocess, 'run', side_effect=update_service))
                https = stack.enter_context(patch.object(activate, 'check_https'))
                stack.enter_context(patch.object(activate.time, 'sleep', side_effect=AssertionError('Unexpected retry/sleep')))
                worker, outcome = self.thread(activate.main)
                try:
                    self.assertEqual(events.get(timeout=5), 'lock_attempt')
                    self.assertEqual(reads, [], 'Activator must wait before judging the temporary candidate')
                    self.assertTrue(outcome.empty(), 'Activator must remain pending while update holds the lock')
                    # The failed update restores the previous release before releasing its lock.
                    service.update(image=OLD_IMAGE, revision=OLD_REVISION)
                finally:
                    REAL_FLOCK(updater, fcntl.LOCK_UN)
                    worker.join(timeout=5)
                self.assertFalse(worker.is_alive())
                self.assertEqual(outcome.get(timeout=1), ('return', 0))
                update.assert_called_once()
                https.assert_called_once_with(OLD_REVISION)
        self.assertTrue(reads)
        self.assertEqual(set(reads), {OLD_IMAGE})
        state = json.loads((directory / 'activation.json').read_text())
        self.assertEqual(state['state'], 'https_verified')
        self.assertEqual(state['source_revision'], OLD_REVISION)
        saved = json.loads((self.root / 'current-stack.json').read_text())
        self.assertEqual(saved['services']['web']['image'], OLD_IMAGE)
        self.assertEqual(saved['services']['web']['deploy']['labels'][activate.RESOLVER_LABEL], 'letsencryptresolver')

    def test_guard_releases_real_lock_when_release_ownership_check_fails(self):
        with patch.object(activate, 'read_service', return_value=(NEW_IMAGE, {})):
            with self.assertRaises(ValueError):
                with activate.release_guard(self.root, OLD_IMAGE, OLD_REVISION):
                    self.fail('An obsolete activator must not enter the guarded operation')
        with (self.root / '.deploy.lock').open('a') as independent:
            REAL_FLOCK(independent, fcntl.LOCK_EX | fcntl.LOCK_NB)
            REAL_FLOCK(independent, fcntl.LOCK_UN)


if __name__ == '__main__':
    unittest.main()
