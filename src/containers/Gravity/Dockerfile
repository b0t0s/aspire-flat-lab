FROM alpine:3.21 AS source

RUN apk add --no-cache git
ARG SOURCE_COMMIT=28e912b8f6808a4bf892fa0f1ffacf857b0faa3a
RUN git init /source \
    && git -C /source remote add origin https://github.com/qunabu/Gravity.git \
    && git -C /source fetch --depth 1 origin "$SOURCE_COMMIT" \
    && git -C /source checkout --detach FETCH_HEAD

FROM node:20-alpine AS builder

WORKDIR /app
COPY --from=source /source/ ./
RUN npm ci \
    && npx tsc \
    && npx vite build --base=/

FROM nginx:alpine AS runner

COPY --from=builder /app/dist /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]