#!/usr/bin/env python3
"""Finish this one release after user-owned DNS exists; never change DNS/Traefik itself."""
import argparse
from contextlib import contextmanager
import fcntl
import ipaddress
import json
from pathlib import Path
import re
import socket
import subprocess
import time
import urllib.request

DOMAIN = 'chess.matheusduarte.dev.br'
EXPECTED_IPV4 = '187.77.47.96'
SERVICE = 'chess-web_web'
RESOLVER_LABEL = 'traefik.http.routers.chess-web.tls.certresolver'
IMAGE_RE = re.compile(r'ghcr\.io/mathwidu/chess-cgi-unity-web@sha256:[0-9a-f]{64}\Z')


def dns_ready(addresses):
    # An unexpected AAAA can send ACME/browser traffic to another machine.
    return bool(addresses) and {str(ipaddress.ip_address(x)) for x in addresses} == {EXPECTED_IPV4}


def read_service():
    result = subprocess.check_output([
        'docker', 'service', 'inspect', SERVICE, '--format',
        '{{json .Spec.TaskTemplate.ContainerSpec.Image}} {{json .Spec.Labels}}'], text=True)
    image, labels = result.strip().split(' ', 1)
    return json.loads(image), json.loads(labels)


def assert_release(image, revision):
    current, labels = read_service()
    if current != image or labels.get('com.chess-cgi.source-revision') != revision:
        raise ValueError('Release changed; refusing to activate another deployment')
    if labels.get('com.chess-cgi.managed-by') != 'chess-web-release':
        raise ValueError('Service ownership mismatch')


@contextmanager
def release_guard(root, image, revision):
    # Updates may temporarily point at an unhealthy candidate before rollback.
    # Wait for that decision before judging whether this activator is obsolete.
    with (root / '.deploy.lock').open('a') as lock:
        fcntl.flock(lock, fcntl.LOCK_EX)
        assert_release(image, revision)
        yield


def check_https(revision):
    # Normal system trust and hostname verification; never use an insecure context.
    base = 'https://' + DOMAIN
    with urllib.request.urlopen(base + '/healthz', timeout=15) as response:
        if response.read(32).strip() != b'ok':
            raise ValueError('Unexpected HTTPS health response')
    with urllib.request.urlopen(base + '/release.json', timeout=15) as response:
        if json.load(response).get('source_revision') != revision:
            raise ValueError('HTTPS is serving another release')
    for path, mime, encoding in (
        ('/Build/Web.wasm.gz', 'application/wasm', 'gzip'),
        ('/Build/Web.data.gz', 'application/gzip', 'gzip'),
        ('/stockfish/stockfish-18-lite-single.wasm', 'application/wasm', None),
    ):
        with urllib.request.urlopen(urllib.request.Request(base + path, method='HEAD'), timeout=15) as response:
            if response.headers.get_content_type() != mime or response.headers.get('Content-Encoding') != encoding:
                raise ValueError('HTTPS runtime headers mismatch: ' + path)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--image', required=True)
    parser.add_argument('--revision', required=True)
    parser.add_argument('--directory', type=Path, required=True)
    parser.add_argument('--hours', type=float, default=72)
    args = parser.parse_args()
    if not IMAGE_RE.fullmatch(args.image) or not re.fullmatch('[0-9a-f]{40}', args.revision):
        parser.error('Invalid immutable image or source revision')
    if socket.gethostname() != 'srv1397065':
        parser.error('Unexpected host')
    state_file = args.directory / 'activation.json'

    def record(state, **fields):
        payload = dict(state=state, domain=DOMAIN, source_revision=args.revision,
                       image=args.image, updated_at=int(time.time()), **fields)
        temporary = state_file.with_suffix('.tmp')
        temporary.write_text(json.dumps(payload, indent=2) + '\n')
        temporary.replace(state_file)

    root = args.directory.parent.parent
    deadline = time.monotonic() + args.hours * 3600
    try:
        while time.monotonic() < deadline:
            with release_guard(root, args.image, args.revision):
                pass
            try:
                addresses = sorted({row[4][0] for row in socket.getaddrinfo(DOMAIN, 443)})
            except socket.gaierror:
                addresses = []
            if dns_ready(addresses):
                break
            record('waiting_dns', observed_addresses=addresses, expected_ipv4=EXPECTED_IPV4)
            time.sleep(60)
        else:
            record('dns_timeout', expected_ipv4=EXPECTED_IPV4)
            return 2

        with release_guard(root, args.image, args.revision):
            _, labels = read_service()
            if labels.get(RESOLVER_LABEL) != 'letsencryptresolver':
                subprocess.run(['docker', 'service', 'update', '--detach=true', '--label-add',
                                RESOLVER_LABEL + '=letsencryptresolver', SERVICE], check=True,
                               stdout=subprocess.DEVNULL, timeout=45)
            current_file = root / 'current-stack.json'
            current = json.loads(current_file.read_text())
            if current['services']['web']['image'] != args.image:
                raise ValueError('Saved rollback stack belongs to a different release')
            current['services']['web']['deploy']['labels'][RESOLVER_LABEL] = 'letsencryptresolver'
            current_temporary = current_file.with_suffix('.tmp')
            current_temporary.write_text(json.dumps(current, indent=2) + '\n')
            current_temporary.replace(current_file)
        record('waiting_https')
        # A single ACME activation; avoid repeated failed certificate orders.
        for _ in range(24):
            with release_guard(root, args.image, args.revision):
                pass
            try:
                check_https(args.revision)
                record('https_verified', url='https://' + DOMAIN + '/')
                return 0
            except (OSError, ValueError):
                time.sleep(15)
        record('https_failed', detail='DNS matched, but trusted HTTPS smoke did not pass within six minutes')
        return 1
    except (OSError, ValueError, subprocess.SubprocessError) as error:
        record('stopped', detail=str(error)[:300])
        return 1


if __name__ == '__main__':
    raise SystemExit(main())
