FROM node:25-alpine AS source

RUN apk add --no-cache git
ARG SOURCE_COMMIT=80efdf06e2e2c0e52e32a2c9cdeced229915f064
RUN git init /source \
    && git -C /source remote add origin https://github.com/allxm4/LANtern.git \
    && git -C /source fetch --depth 1 origin "$SOURCE_COMMIT" \
    && git -C /source checkout --detach FETCH_HEAD

FROM node:25-alpine

WORKDIR /app
COPY --from=source /source/ ./
RUN npm install --omit=dev && npm cache clean --force

EXPOSE 3000
CMD ["node", "server.js"]