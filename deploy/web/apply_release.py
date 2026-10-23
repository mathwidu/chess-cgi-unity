#!/usr/bin/env python3
"""Remote half of an explicitly authorized release; JSON arrives through stdin."""
import json
import fcntl
import os
from pathlib import Path
import pwd
import re
import socket
import subprocess
import sys
import time

SERVICE = 'chess-web_web'
STACK = 'chess-web'


def run(*args, timeout=45):
    return subprocess.check_output(args, text=True, stderr=subprocess.STDOUT, timeout=timeout).strip()


def service_exists():
    return SERVICE in run('docker', 'service', 'ls', '--format', '{{.Name}}').splitlines()


def wait_healthy(image):
    for _ in range(36):
        ids = run('docker', 'ps', '--filter', 'label=com.docker.swarm.service.name=' + SERVICE,
                  '--filter', 'status=running', '--format', '{{.ID}}').splitlines()
        for container in ids:
            state = run('docker', 'inspect', container, '--format', '{{.Config.Image}} {{.State.Health.Status}}')
            if state == image + ' healthy':
                return True
        time.sleep(5)
    return False


def wait_removed():
    for _ in range(12):
        if not service_exists():
            return True
        time.sleep(5)
    return False


def main():
    payload = json.load(sys.stdin)
    image, revision = payload['image'], payload['revision']
    if not re.fullmatch(r'ghcr\.io/mathwidu/chess-cgi-unity-web@sha256:[0-9a-f]{64}', image):
        raise ValueError('Only the pinned Chess Web image is accepted')
    if not re.fullmatch('[0-9a-f]{40}', revision):
        raise ValueError('Invalid revision')
    if pwd.getpwuid(os.getuid()).pw_name != 'mathwidu' or socket.gethostname() != 'srv1397065':
        raise ValueError('SSH destination identity mismatch')
    if run('docker', 'info', '--format', '{{.Swarm.ControlAvailable}}') != 'true':
        raise ValueError('Expected a Swarm manager')
    if run('docker', 'network', 'inspect', 'network_swarm_public', '--format', '{{.Driver}} {{.Scope}}') != 'overlay swarm':
        raise ValueError('Public overlay differs from the reviewed deployment')
    root = Path.home() / '.local/share/chess-web'
    root.mkdir(parents=True, exist_ok=True, mode=0o700)
    with (root / '.deploy.lock').open('a') as lock:
        fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        apply_locked(payload, image, revision, root)


def apply_locked(payload, image, revision, root):
    # Ownership, collision checks and the rollback baseline must share this lock.
    existing = service_exists()
    for service in run('docker', 'service', 'ls', '--format', '{{.Name}}').splitlines():
        labels = json.loads(run('docker', 'service', 'inspect', service, '--format', '{{json .Spec.Labels}}')) or {}
        if service != SERVICE and any('chess.matheusduarte.dev.br' in value for value in labels.values()):
            raise ValueError('Another service already claims the domain: ' + service)
        if service.startswith(STACK + '_') and service != SERVICE:
            raise ValueError('Unexpected service in the target stack')
        if service == SERVICE and labels.get('com.chess-cgi.managed-by') != 'chess-web-release':
            raise ValueError('Target service is not owned by this release tool')

    previous = root / 'current-stack.json'
    if existing and not previous.is_file():
        raise ValueError('Existing service has no saved rollback stack; inspect before updating')
    # Pull before changing the service. Existing Docker login may be used, no credential copying.
    run('docker', 'pull', '--platform', 'linux/amd64', image, timeout=180)
    if run('docker', 'image', 'inspect', image, '--format', '{{.Os}}/{{.Architecture}}') != 'linux/amd64':
        raise ValueError('Wrong image architecture')
    image_revision = run('docker', 'image', 'inspect', image, '--format', '{{index .Config.Labels "org.opencontainers.image.revision"}}')
    if image_revision != revision:
        raise ValueError('Image revision does not match the approved commit')

    directory = root / 'releases' / (revision[:12] + '-' + image.rsplit(':', 1)[1][:12])
    directory.mkdir(parents=True, exist_ok=False, mode=0o700)
    stack_file = directory / 'stack.json'
    stack_file.write_text(payload['stack'])
    (directory / 'activate_after_dns.py').write_text(payload['activator'])
    if existing:
        (directory / 'previous-stack.json').write_bytes(previous.read_bytes())
    (directory / 'release.json').write_text(json.dumps({
        'image': image, 'source_revision': revision, 'previous_service_exists': existing,
        'domain': 'chess.matheusduarte.dev.br'}, indent=2) + '\n')
    try:
        run('docker', 'stack', 'deploy', '--resolve-image', 'never', '--with-registry-auth',
            '--compose-file', str(stack_file), STACK, timeout=90)
        if not wait_healthy(image):
            raise RuntimeError('The new image did not become healthy within three minutes')
    except Exception as failure:
        if existing:
            run('docker', 'stack', 'deploy', '--resolve-image', 'never', '--with-registry-auth',
                '--compose-file', str(directory / 'previous-stack.json'), STACK, timeout=90)
            prior_image = json.loads(previous.read_text())['services']['web']['image']
            recovered = wait_healthy(prior_image)
        else:
            run('docker', 'stack', 'rm', STACK, timeout=45)
            recovered = wait_removed()
        if not recovered:
            raise RuntimeError('Deployment failed and rollback did not converge; inspect chess-web only') from failure
        raise RuntimeError('Deployment failed; rollback verified: ' + str(failure)) from failure
    previous_temporary = previous.with_suffix('.tmp')
    previous_temporary.write_bytes(stack_file.read_bytes())
    previous_temporary.replace(previous)
    log = (directory / 'activation.log').open('a')
    process = subprocess.Popen([
        sys.executable, str(directory / 'activate_after_dns.py'), '--image', image,
        '--revision', revision, '--directory', str(directory)],
        stdin=subprocess.DEVNULL, stdout=log, stderr=subprocess.STDOUT,
        start_new_session=True, close_fds=True)
    (directory / 'activation.pid').write_text(str(process.pid) + '\n')
    print(json.dumps({'state': 'healthy_waiting_dns', 'service': SERVICE, 'image': image,
                      'source_revision': revision, 'directory': str(directory),
                      'activation_pid': process.pid, 'dns_wait_hours': 72}))


if __name__ == '__main__':
    try:
        main()
    except Exception as error:
        print(json.dumps({'state': 'failed', 'detail': str(error)[:800]}))
        raise SystemExit(1)
