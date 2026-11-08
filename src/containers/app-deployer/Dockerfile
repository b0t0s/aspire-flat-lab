FROM docker:27-cli

RUN apk add --no-cache curl
COPY deploy-app.sh /usr/local/bin/deploy-app
RUN chmod +x /usr/local/bin/deploy-app

ENTRYPOINT []
CMD ["sleep", "infinity"]
