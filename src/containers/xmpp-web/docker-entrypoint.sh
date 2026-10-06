#!/usr/bin/env bash
# Renders the converse.js index.html with ${XMPP_DOMAIN}, ${XMPP_WS_URL} and ${XMPP_BOSH_URL}
# env vars supplied by the Aspire app stack (chat-services.cs) from .env values.
# Falls back to derived defaults from XMPP_DOMAIN if URLs are unset.
set -euo pipefail

: "${XMPP_DOMAIN:=__PLACEHOLDER__}"
: "${XMPP_WS_URL:=wss://${XMPP_DOMAIN}/ws}"
: "${XMPP_BOSH_URL:=https://${XMPP_DOMAIN}/bosh}"

export XMPP_DOMAIN XMPP_WS_URL XMPP_BOSH_URL
envsubst '${XMPP_DOMAIN} ${XMPP_WS_URL} ${XMPP_BOSH_URL}' \
    < /usr/share/nginx/html/index.html.template \
    > /usr/share/nginx/html/index.html

exec "$@"