#!/usr/bin/env bash
#
#   ./deploy.sh                 deploy / update both layers
#   ./deploy.sh --pull          git pull + submodules first
#   ./deploy.sh --bootstrap     fresh host: also install Docker, .NET 10 and log2ram
#   ./deploy.sh --infra         only docker-compose.yml (caddy, dns, homepage, ...)
#   ./deploy.sh --apps          only the Aspire app stack (systemd unit)
#   ./deploy.sh --adopt-live    a live config differs from configs/: copy the live one into configs/
#   ./deploy.sh --use-repo      a live config differs from configs/: keep the repo one (live copy is shadowed)
#
# Order: prerequisites -> .env -> flat_lab_net -> config import -> infra (compose) -> apps (systemd) -> health check.
# See docs/deployment.md.
set -euo pipefail

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$REPO"

PULL=0 BOOTSTRAP=0 INFRA=1 APPS=1 CONFLICT_POLICY=stop
for arg in "$@"; do
  case "$arg" in
    --pull) PULL=1 ;;
    --bootstrap) BOOTSTRAP=1 ;;
    --infra) APPS=0 ;;
    --apps) INFRA=0 ;;
    --adopt-live) CONFLICT_POLICY=adopt ;;
    --use-repo) CONFLICT_POLICY=repo ;;
    -h|--help) sed -n '2,13p' "$0"; exit 0 ;;
    *) echo "unknown option: $arg (see --help)" >&2; exit 2 ;;
  esac
done

step() { printf '\n\033[1;36m==> %s\033[0m\n' "$*"; }
warn() { printf '\033[1;33m!! %s\033[0m\n' "$*" >&2; }
die()  { printf '\033[1;31mxx %s\033[0m\n' "$*" >&2; exit 1; }
have() { command -v "$1" >/dev/null 2>&1; }

ENV_FILE="$REPO/.env"
env_get() {
  local line
  line="$(grep -E "^$1=" "$ENV_FILE" 2>/dev/null | tail -n1 || true)"
  line="${line#*=}"
  line="${line%\"}"; line="${line#\"}"; line="${line%\'}"; line="${line#\'}"
  [[ "$line" == __PLACEHOLDER* ]] && line=""
  printf '%s' "$line"
}
env_set() {
  if grep -qE "^$1=(__PLACEHOLDER[^[:space:]]*)?[[:space:]]*$" "$ENV_FILE"; then
    sed -i -E "s|^$1=(__PLACEHOLDER[^[:space:]]*)?[[:space:]]*$|$1=$2|" "$ENV_FILE"
  else
    printf '%s=%s\n' "$1" "$2" >>"$ENV_FILE"
  fi
}
random_secret() { tr -dc 'A-Za-z0-9' </dev/urandom | head -c "${1:-32}"; }

if (( PULL )); then
  step "git pull"
  git checkout -- docs/ports.md configs/ports.json 2>/dev/null || true
  git pull --ff-only
fi
git submodule update --init --recursive
git config core.hooksPath .githooks

DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
if (( BOOTSTRAP )); then
  step "bootstrap: Docker, .NET 10, log2ram"
  if ! have docker; then
    sudo sh src/scripts/get-docker.sh
    sudo usermod -aG docker "$USER"
    warn "added $USER to the docker group; if docker commands fail below, log out/in and re-run"
  fi
  if [[ ! -x "$DOTNET_ROOT/dotnet" ]]; then
    bash src/scripts/dotnet_install.sh --channel 10.0 --install-dir "$DOTNET_ROOT"
  fi
  if [[ ! -e /etc/log2ram.conf ]]; then
    (cd src/additional-software/log2ram && sudo bash install.sh)
  fi
fi

step "prerequisites"
have docker || die "docker not found (run with --bootstrap)"
docker compose version >/dev/null 2>&1 || die "docker compose plugin not found (run with --bootstrap)"
if (( APPS )); then
  [[ -x "$DOTNET_ROOT/dotnet" ]] || die "dotnet not found in $DOTNET_ROOT (run with --bootstrap or set DOTNET_ROOT)"
  "$DOTNET_ROOT/dotnet" --list-sdks | grep -q '^10\.' || die ".NET 10 SDK required (run with --bootstrap)"
fi
echo "docker $(docker version --format '{{.Server.Version}}'), compose $(docker compose version --short)"

step ".env"
if [[ ! -f "$ENV_FILE" ]]; then
  cp .env.example "$ENV_FILE"
  chmod 600 "$ENV_FILE"
  echo "created .env from .env.example"
fi
for key in SEARXNG_SECRET SEAFILE_DB_ROOT_PASS SEAFILE_ADMIN_PASS DASHBOARD__FRONTEND__BROWSERTOKEN; do
  if [[ -z "$(env_get "$key")" ]]; then
    env_set "$key" "$(random_secret 32)"
    echo "generated $key"
  fi
done
missing=()
for key in DUCKDNS_DOMAIN DUCKDNS_TOKEN ZEROTIER_NETWORK_ID SEAFILE_ADMIN_EMAIL; do
  [[ -n "$(env_get "$key")" ]] || missing+=("$key")
done
(( ${#missing[@]} == 0 )) || die "fill these in .env first: ${missing[*]}"
preset="$(env_get HOMEPAGE_THEME_PRESET)"
[[ -f "configs/homepage/themes/${preset:-default}.css" ]] || die "HOMEPAGE_THEME_PRESET=$preset: no configs/homepage/themes/$preset.css (have: $(cd configs/homepage/themes && ls *.css | sed "s/.css//" | tr "\n" " "))"
DATA="$(env_get FLAT_LAB_DATA)"
DATA="$(cd "$REPO" && realpath -m "${DATA:-../services-data}")"
echo "data root: $DATA"

step "flat_lab_net"
docker network inspect flat_lab_net >/dev/null 2>&1 || docker network create flat_lab_net
echo "ok"

step "configs"
data_path() { local value; value="$(env_get "$1")"; realpath -m "${value:-$DATA/$2}"; }
imports=(
  "$(data_path SEARXNG_DATA system/searxng/data)/settings.yml|configs/searxng/settings.yml"
  "$(data_path SEARXNG_DATA system/searxng/data)/limiter.toml|configs/searxng/limiter.toml"
  "$(data_path NTFY_CONFIG system/ntfy/config)/server.yml|configs/ntfy/server.yml"
  "$(data_path HOME_ASSISTANT_DATA system/home-assistant)/configuration.yaml|configs/home-assistant/configuration.yaml"
  "$(data_path FILEBROWSER_DATA system/filebrowser-quantum)/config.yaml|configs/filebrowser-quantum/config.yaml"
)
scrub_secrets_at() {
  case "$2" in
    *searxng/settings.yml) sed -i -E 's|^([[:space:]]*secret_key:).*|\1 "set-by-SEARXNG_SECRET-env"|' "$1" ;;
  esac
}
conflicts=()
for pair in "${imports[@]}"; do
  live="${pair%%|*}" repo="${pair##*|}"
  [[ -f "$live" ]] || continue
  scrubbed="$(mktemp)"; cp "$live" "$scrubbed"; scrub_secrets_at "$scrubbed" "$repo"
  if [[ ! -f "$repo" ]]; then
    mkdir -p "$(dirname "$repo")"; cp "$scrubbed" "$repo"; echo "imported $live -> $repo"
  elif ! cmp -s "$scrubbed" "$repo"; then
    case "$CONFLICT_POLICY" in
      adopt) cp "$scrubbed" "$repo"; echo "adopted live $live -> $repo" ;;
      repo) echo "keeping $repo (live $live is now shadowed)" ;;
      *) conflicts+=("$repo  <->  $live") ;;
    esac
  fi
  rm -f "$scrubbed"
done
if (( ${#conflicts[@]} )); then
  printf '  %s\n' "${conflicts[@]}" >&2
  die "these repo configs differ from the live ones; compare them (diff), then re-run with --adopt-live or --use-repo"
fi
git status --short -- configs | sed 's/^/  /' || true

if (( INFRA )); then
  step "infra layer (docker-compose.yml)"
  docker compose config -q
  project="$(docker compose config --format json | sed -n 's/^  "name": "\(.*\)",$/\1/p' | head -n1)"
  for name in $(docker compose config --format json | grep -o '"container_name": "[^"]*"' | cut -d'"' -f4); do
    owner="$(docker inspect -f '{{index .Config.Labels "com.docker.compose.project"}}' "$name" 2>/dev/null || true)"
    if docker inspect "$name" >/dev/null 2>&1 && [[ "$owner" != "$project" ]]; then
      warn "removing stale container '$name' (not owned by compose project '$project'); its data is in bind mounts"
      docker rm -f "$name" >/dev/null
    fi
  done
  docker compose up -d --build
fi

if (( APPS )); then
  step "app layer (Aspire AppHost via systemd)"
  # Build first: a broken change fails here and the running stack stays up.
  "$DOTNET_ROOT/dotnet" build apphost.cs -nologo -v quiet
  unit="$(sed -e "s|{{USER}}|$USER|g" -e "s|{{REPO}}|$REPO|g" -e "s|{{DOTNET_ROOT}}|$DOTNET_ROOT|g" configs/systemd/flat-lab.service)"
  if ! cmp -s <(printf '%s\n' "$unit") /etc/systemd/system/flat-lab.service; then
    printf '%s\n' "$unit" | sudo tee /etc/systemd/system/flat-lab.service >/dev/null
    sudo systemctl daemon-reload
    echo "installed /etc/systemd/system/flat-lab.service"
  fi
  sudo systemctl enable flat-lab >/dev/null 2>&1
  sudo systemctl restart flat-lab

  step "health"
  for _ in $(seq 1 90); do
    if curl -fsS -o /dev/null http://localhost:19000; then
      echo "dashboard: https://dashboard.$(env_get DUCKDNS_DOMAIN)  (login token: DASHBOARD__FRONTEND__BROWSERTOKEN in .env)"
      break
    fi
    sleep 2
  done
  curl -fsS -o /dev/null http://localhost:19000 || warn "dashboard not answering yet; follow: journalctl -u flat-lab -f"
  git status --short -- configs docs/ports.md | sed 's/^/  changed: /' || true
fi

step "done"
echo "homepage: https://homepage.$(env_get DUCKDNS_DOMAIN)   ports: docs/ports.md"
