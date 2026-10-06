---
title: Architecture Notes
domain: docs
type: info
status: active
tags: [topology, single-arm-vpn, caddy, raspberry-pi]
filed: 2026-07-22
related:
  - decisions.md
---

# Architecture Notes

The long form of [decisions.md](decisions.md): one paragraph per choice, what the constraint buys.

## Single-Arm VPN Gateway

One open port (ZeroTier UDP/9993), one HTTP edge (Caddy), one bridge network (`homelab_net`).
The attack surface of a home lab is the number of weak services the internet can touch; collapsing that number to one hardened VPN endpoint means a vulnerability in Seafile or OneDev is not a vulnerability the internet can reach.
Trade-off: the Pi is a single point of failure for remote access until a second node exists.
WireGuard-based stacks (WG-Easy, `wg-quick`, Pi-WireGuard) were rejected because each new client needs a hand-written peer config shipped out of band, with no central UI to manage membership.
Tailscale and Headscale were rejected because Tailscale needs a hosted control plane and Headscale adds a service to operate in exchange for nothing the lab actually needs.
See [services/zerotier.md](services/zerotier.md) for the per-rejection reason.

## Caddy as the Sole Ingress

One Caddy container terminates TLS (DuckDNS DNS-01), applies headers, and reverse-proxies by host.
DNS-01 issues certificates without an inbound HTTP challenge, which is what keeps the one-open-port rule intact.
The `*.{$DUCKDNS_DOMAIN}` site block is a single wildcard with named host matches; new services get a named match, never their own TLS.
Trade-off: Caddy down means all web access down; `restart: unless-stopped` plus a future second node is the answer.

## Two Layers as the System's Form

`docker-compose.yml` declares the infra layer (edge, VPN, DNS, dashboard, storage) and `apphost.cs` declares the app layer; together they are every container, network and volume.
The split exists so that restarting the Aspire app stack (any change there restarts it) never takes down ingress, DNS or remote access.
Both layers join the external `homelab_net`, so Caddy reaches every app by container name.
Trade-off: two files to read instead of one; `docs/ports.md` (generated) and the homepage groups are the shared map.

## Configuration as Code in the AppHost

Anything a service cannot take from env vars is produced by the AppHost, not by a script: secrets are generated parameters persisted to `.env`, config files are rendered from templates and copied into the container with `WithContainerFiles`.
One-off maintenance actions are dashboard commands (`WithDockerExecCommand`, e.g. "Update yt-dlp" on MeTube) or small scripts in `src/scripts/`.
The Seafile auto-configurator stays an idempotent init container in the infra layer because it has to wait for Seafile to write its own settings first.
