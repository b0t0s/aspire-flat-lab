# Converse.js XMPP web client.
# All endpoints (websocket, bosh, domain) are templated with ${XMPP_*_URL} / ${XMPP_DOMAIN}
# and rendered at container start by the entrypoint from the XMPP_WS_URL / XMPP_BOSH_URL /
# XMPP_DOMAIN env vars set by the Aspire app stack. No domain or host is baked into the image.

FROM nginx:alpine

ARG CONVERSE_VERSION=14.0.0

RUN apk add --no-cache bash curl tar gettext \
    && cd /tmp \
    && curl -sL -o converse.tgz https://github.com/conversejs/converse.js/releases/download/v${CONVERSE_VERSION}/converse.js-${CONVERSE_VERSION}.tgz \
    && mkdir -p /tmp/cv-extract \
    && tar -xzf converse.tgz -C /tmp/cv-extract \
    && cp -r /tmp/cv-extract/package/dist/. /usr/share/nginx/html/ \
    && rm -rf /tmp/converse.tgz /tmp/cv-extract

COPY index.html /usr/share/nginx/html/index.html.template
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY docker-entrypoint.sh /docker-entrypoint.sh
RUN chmod +x /docker-entrypoint.sh

ENTRYPOINT ["/docker-entrypoint.sh"]
CMD ["nginx", "-g", "daemon off;"]

EXPOSE 80
