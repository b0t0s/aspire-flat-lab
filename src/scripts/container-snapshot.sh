#!/usr/bin/env bash
# container-snapshot.sh — checkpoint running containers as portable image tarballs.
#
# Saves ONLY the container's filesystem + config (via `docker commit` + `docker save`).
# Volumes, bind mounts, and host data are NOT touched and are NOT included in the archive.
# After restoring, reattach the same volumes and the container resumes with its data intact.
#
# Usage:
#   ./src/scripts/container-snapshot.sh save <name> [<name> ...]   # freeze one or more running containers
#   ./src/scripts/container-snapshot.sh save-all                   # freeze every running container
#   ./src/scripts/container-snapshot.sh load <tar> [<tar> ...]     # re-import a saved tarball as an image
#   ./src/scripts/container-snapshot.sh list                       # show available snapshots in ./container-snapshots/
#   ./src/scripts/container-snapshot.sh rm <tar> [<tar> ...]       # delete a snapshot tarball
#
# Output: ./container-snapshots/<name>-<UTC-timestamp>.tar
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SNAP_DIR="${SNAP_DIR:-${SCRIPT_DIR}/../../container-snapshots}"
mkdir -p "$SNAP_DIR"

usage() {
    # Print the leading comment block (line 2 up to but not including the first non-comment, non-blank line), with markers stripped.
    awk 'NR>1{ if (/^# ?/) { sub(/^# ?/, ""); print; next } exit }' "${BASH_SOURCE[0]}"
    exit "${1:-0}"
}

cmd_save() {
    [[ $# -ge 1 ]] || { echo "save: need at least one container name" >&2; exit 1; }
    local stamp ts name img existing
    stamp="$(date -u +%Y%m%dT%H%M%SZ)"
    for name in "$@"; do
        if ! docker inspect "$name" >/dev/null 2>&1; then
            echo "save: container '$name' not found" >&2; continue
        fi
        # Remove any prior commit image with the same tag so save always produces a clean archive.
        img="flat-lab-snapshot:${name}"
        docker image rm "$img" >/dev/null 2>&1 || true
        docker commit "$name" "$img" >/dev/null
        ts="${SNAP_DIR}/${name}-${stamp}.tar"
        docker save -o "$ts" "$img"
        docker image rm "$img" >/dev/null 2>&1 || true
        echo "saved $name -> $ts"
    done
}

cmd_save_all() {
    local names=()
    while IFS= read -r n; do names+=("$n"); done < <(docker ps --format '{{.Names}}')
    if [[ ${#names[@]} -eq 0 ]]; then
        echo "save-all: no running containers"; exit 0
    fi
    cmd_save "${names[@]}"
}

cmd_load() {
    [[ $# -ge 1 ]] || { echo "load: need at least one tarball path" >&2; exit 1; }
    local tar
    for tar in "$@"; do
        [[ -f "$tar" ]] || { echo "load: not a file: $tar" >&2; continue; }
        docker load -i "$tar"
    done
    echo "load complete. Start a container from the imported image with e.g.:"
    echo "  docker run -d --name <name> --volumes-from <original-name-or-data-container> <repo>:<tag>"
}

cmd_list() {
    ls -lh "$SNAP_DIR" 2>/dev/null | tail -n +2 || echo "(no snapshots yet)"
}

cmd_rm() {
    [[ $# -ge 1 ]] || { echo "rm: need at least one snapshot file" >&2; exit 1; }
    local f
    for f in "$@"; do
        [[ -f "$f" ]] || { echo "rm: not a file: $f" >&2; continue; }
        rm -v -- "$f"
    done
}

case "${1:-}" in
    save)      shift; cmd_save "$@" ;;
    save-all)  shift; cmd_save_all "$@" ;;
    load)      shift; cmd_load "$@" ;;
    list)      shift; cmd_list "$@" ;;
    rm)        shift; cmd_rm "$@" ;;
    -h|--help|"") usage 0 ;;
    *)         echo "unknown subcommand: $1" >&2; usage 1 ;;
esac