FROM ghcr.io/linuxserver/baseimage-selkies:debiantrixie

ENV TITLE=Stellarium
RUN apt-get update \
    && apt-get install -y --no-install-recommends stellarium libgl1-mesa-dri \
    && rm -rf /var/lib/apt/lists/*
COPY root/ /

EXPOSE 3000
