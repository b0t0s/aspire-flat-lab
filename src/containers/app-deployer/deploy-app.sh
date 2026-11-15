#!/bin/sh
# deploy-app <name> <tag> <health-url>, with a tar of the app's devops/lab folder on stdin.
# See docs/ignored/app-delivery.md.
set -eu

name=${1:?name}
tag=${2:?tag}
health=${3:?health url}

case "$name" in
  *[!a-z0-9-]* | "") echo "bad app name: $name" >&2; exit 2 ;;
esac

apps=/apps
routes="$apps/routes"
image="flat-lab/$name"
env_file="$apps/$name/app.env"
route_file="$routes/$name.caddy"
caddy_config="--config /etc/caddy/Caddyfile --adapter caddyfile"

notify() {
  curl -fsS -m 5 -H "Title: $name" -d "$1" "${NTFY_URL:?}" >/dev/null || echo "notification failed" >&2
}

fail() {
  echo "deploy-app: $1" >&2
  notify "Deploy failed: $1"
  exit 1
}

mkdir -p "$routes" "$apps/$name"
exec 9>"$apps/.lock"
flock 9

work=$(mktemp -d)
trap 'rm -rf "$work"' EXIT
tar -xf - -C "$work"

[ -f "$work/compose.yaml" ] || fail "compose.yaml is missing from the deploy folder"
[ -f "$env_file" ] || fail "create $name/app.env under services-data/system/apps first"
docker image inspect "$image:$tag" >/dev/null 2>&1 || fail "image $image:$tag was not built"

export APP_IMAGE="$image:current" APP_DATA="$APP_DATA_ROOT/$name" LAB_DOMAIN

up() {
  docker compose -p "app-$name" --env-file "$env_file" -f "$work/compose.yaml" up -d --remove-orphans
}

healthy() {
  attempts=0
  while [ "$attempts" -lt 60 ]; do
    curl -fsS -m 3 -o /dev/null "$health" && return 0
    attempts=$((attempts + 1))
    sleep 3
  done
  return 1
}

has_previous=0
if docker image inspect "$image:current" >/dev/null 2>&1; then
  docker tag "$image:current" "$image:previous"
  has_previous=1
fi

rollback() {
  if [ "$has_previous" -eq 0 ]; then
    fail "$1; no earlier version to return to"
  fi
  docker tag "$image:previous" "$image:current"
  up || true
  fail "$1; returned to the previous version"
}

docker tag "$image:$tag" "$image:current"
up || rollback "containers did not start"
healthy || rollback "$health did not answer"

if [ -f "$work/route.caddy" ]; then
  had_route=0
  if [ -f "$route_file" ]; then
    cp "$route_file" "$work/route.previous"
    had_route=1
  fi
  cp "$work/route.caddy" "$route_file"
  # shellcheck disable=SC2086
  if ! docker exec caddy caddy validate $caddy_config >/dev/null 2>&1; then
    if [ "$had_route" -eq 1 ]; then cp "$work/route.previous" "$route_file"; else rm -f "$route_file"; fi
    rollback "Caddy rejected route.caddy"
  fi
  # shellcheck disable=SC2086
  docker exec caddy caddy reload $caddy_config || fail "Caddy did not reload"
fi

for old in $(docker image ls "$image" --format '{{.Tag}}'); do
  case "$old" in
    current | previous | "$tag") ;;
    *) docker image rm "$image:$old" >/dev/null 2>&1 || true ;;
  esac
done
docker builder prune -f --keep-storage 20GB >/dev/null 2>&1 || true

notify "Published $(printf '%.8s' "$tag")"
echo "deployed $name $tag"
