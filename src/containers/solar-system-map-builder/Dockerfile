FROM alpine:3.21 AS source

RUN apk add --no-cache git
ARG SOURCE_COMMIT=dc80693063064724cfd7d5bee3e072a9faf6a645
RUN git init /source \
    && git -C /source remote add origin https://codeberg.org/OrbitalCapybara/solar-system-map-builder.git \
    && git -C /source fetch --depth 1 origin "$SOURCE_COMMIT" \
    && git -C /source checkout --detach FETCH_HEAD

FROM python:3-slim

WORKDIR /app
COPY --from=source /source/pyproject.toml ./pyproject.toml
COPY --from=source /source/source/ ./
RUN pip install --no-cache-dir . \
    && mkdir -p /cache

EXPOSE 80
ENTRYPOINT ["python", "server.py"]
CMD ["--cache-folder", "/cache", "--config", "/config/config.yaml", "--port", "80"]