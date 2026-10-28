#!/usr/bin/env python3
"""Describe the pinned runtime for the coordinated site release. Never deploy."""
import argparse
import json
import re


def render(image, revision, build_id):
    if (not re.fullmatch(r"ghcr\.io/mathwidu/chess-cgi-unity-web@sha256:[0-9a-f]{64}", image)
            or not re.fullmatch(r"[0-9a-f]{40}", revision)
            or not re.fullmatch(r"[0-9a-f]{64}", build_id)):
        raise ValueError("Expected immutable runtime digest, source SHA and build_id")
    return {
        "source_revision": revision, "runtime_image": image, "build_id": build_id,
        "domain": "cicochess.dev", "route": "/jogar/runtime/", "dns_changes": False,
        "deployment_owner": "mathwidu/cicochess-site", "stack": "cicochess-site",
        "service": "cicochess-site_game", "site_release_required": True,
        "publication": "Publish the verified runtime image, then approve the coordinated site + game release.",
        "rollback": "Restore the recorded site/runtime pair using cicochess-site/deploy/release.py.",
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("operation", choices=["plan"])
    parser.add_argument("--image", required=True)
    parser.add_argument("--revision", required=True)
    parser.add_argument("--build-id", required=True)
    args = parser.parse_args()
    print(json.dumps(render(args.image, args.revision, args.build_id), indent=2))


if __name__ == "__main__":
    main()
