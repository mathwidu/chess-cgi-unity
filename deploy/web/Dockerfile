# Build only from a verified context; runtime URLs retain their complete prefix.
# Official NGINX unprivileged multi-platform index, resolved 2026-10-23.
# https://github.com/nginx/docker-nginx-unprivileged
FROM nginxinc/nginx-unprivileged:stable-alpine@sha256:f5116ed4606f3eb015dd4a8e28cb9b351476dc0ad4bc6e9f93b19cec130cdf03

# A separate document root excludes the base image's welcome/error pages.
COPY deploy/web/nginx.conf /etc/nginx/nginx.conf
COPY deploy/web/runtime-assets.conf /etc/nginx/runtime-assets.conf
COPY web/ /srv/chess-web/

USER 101:101
EXPOSE 8080

# Process endpoint and the mutable runtime pointer must both be reachable.
HEALTHCHECK --interval=30s --timeout=10s --start-period=10s --retries=3 \
    CMD wget -q -T 4 -O - http://127.0.0.1:8080/healthz | grep -qx ok \
        && wget -q -T 4 -O /dev/null http://127.0.0.1:8080/jogar/runtime/manifest.json

# No entrypoint template writes: root filesystem may be read-only with /tmp tmpfs.
ENTRYPOINT ["nginx"]
CMD ["-c", "/etc/nginx/nginx.conf", "-g", "daemon off;"]
