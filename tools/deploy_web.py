#!/usr/bin/env python3
"""Render a release plan, or apply it after explicit human approval."""
import argparse
import json
from pathlib import Path
import re
import shlex
import subprocess

ROOT = Path(__file__).resolve().parents[1]
IMAGE_RE = re.compile(r'ghcr\.io/mathwidu/chess-cgi-unity-web@sha256:[0-9a-f]{64}\Z')


def render(image, revision):
    if not IMAGE_RE.fullmatch(image) or not re.fullmatch('[0-9a-f]{40}', revision):
        raise ValueError('Expected a full GHCR digest and 40-character source revision')
    stack = json.loads((ROOT / 'deploy/web/stack.json').read_text())
    stack['services']['web']['image'] = image
    stack['services']['web']['deploy']['labels']['com.chess-cgi.source-revision'] = revision
    rendered = json.dumps(stack, indent=2) + '\n'
    subprocess.run(['docker', 'stack', 'config', '--compose-file', '-'], input=rendered,
                   text=True, check=True, stdout=subprocess.DEVNULL)
    return rendered


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('operation', choices=['plan', 'apply'])
    parser.add_argument('--image', required=True)
    parser.add_argument('--revision', required=True)
    args = parser.parse_args()
    stack = render(args.image, args.revision)
    if args.operation == 'plan':
        print(stack)
        print(json.dumps({'ssh_alias': 'vps-pessoal-nova', 'expected_host': 'srv1397065',
                          'domain': 'chess.matheusduarte.dev.br', 'dns_record': {'type': 'A', 'name': 'chess', 'value': '187.77.47.96'},
                          'changes': 'chess-web_web only; existing overlay; no volumes or database',
                          'activation': 'one process waits at most 72 hours for DNS, then enables this router certificate',
                          'rollback': 'restore saved stack, or remove the new stack if initial health fails'}, indent=2))
        return
    # An invocation is not an approval mechanism: authorization is recorded in the release conversation.
    head = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=ROOT, text=True).strip()
    dirty = subprocess.check_output(['git', 'status', '--porcelain'], cwd=ROOT, text=True).strip()
    if head != args.revision or dirty:
        parser.error('Apply requires the approved revision checked out with no uncommitted changes')
    payload = {'image': args.image, 'revision': args.revision, 'stack': stack,
               'activator': (ROOT / 'deploy/web/activate_after_dns.py').read_text()}
    program = (ROOT / 'deploy/web/apply_release.py').read_text()
    subprocess.run(['ssh', '-o', 'BatchMode=yes', '-o', 'StrictHostKeyChecking=yes',
                    '-o', 'ConnectTimeout=10', 'vps-pessoal-nova',
                    'python3 -c ' + shlex.quote(program)], input=json.dumps(payload), text=True, check=True)


if __name__ == '__main__':
    main()
