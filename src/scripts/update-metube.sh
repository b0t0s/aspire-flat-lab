#!/usr/bin/env bash
# update-metube.sh — exec into the MeTube container (container_name: ytdl) and refresh pip + yt-dlp
set -euo pipefail

docker exec ytdl \
  sh -c 'pip install --upgrade pip && pip install --upgrade yt-dlp && yt-dlp --version'
