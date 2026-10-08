# Flat Lab

Single-node home lab on a Raspberry Pi 5: one ZeroTier UDP port in, Caddy in front of everything else, self-hosted services on a bridge network.

## Topology

```mermaid
flowchart LR
    Clients[Remote clients] <-->  ZT <--> Caddy
    LAN[LAN devices] <--> Router --> Caddy[caddy]

    Caddy -->|"https"| Bridge
    Caddy -->|"https"| HostNet

    subgraph Bridge["network_mode: bridge"]
        B["Services<br/>adguard, seafile, onedev, homepage, etc..."]
    end

    subgraph HostNet["network_mode: host"]
        H["netalertx<br/>home-assistant"]
    end

    classDef edge fill:#e8f5e9,stroke:#1b5e20,color:#000
    classDef host fill:#fff8e1,stroke:#ff6f00,color:#000
    class Caddy edge
    class H,HostNet host
```

## Docs

- [`docs/architecture-notes.md`](docs/architecture-notes.md) — topology, long form