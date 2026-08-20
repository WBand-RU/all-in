#!/bin/sh
set -eu

envsubst '${VITE_KEYCLOAK_URL} ${VITE_KEYCLOAK_REALM} ${VITE_KEYCLOAK_CLIENT_ID}' \
    < /etc/wband/runtime-config.js.template \
    > /usr/share/nginx/html/runtime-config.js
