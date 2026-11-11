# Lab 2: SQL vs NoSQL benchmark results

## Environment

- Date: 2026-11-11T19:25:14+02:00 (finished 2026-11-11T19:30:41+02:00)
- Host: Linux 7.3.1-1-cachyos x86_64, nproc: 12, 15 GiB RAM
- Target: `http://localhost:5000`
- wrk: `-t2 -c8 -d60s --latency --timeout 120s`, warm-up 15s per route (discarded), 3 s pause between runs
- Load script: `bench/lua/random_id.lua` (random id/param per request, per-thread seed); response status tracking: on
- Id/param pools: r1_activity=2 r2_top_courses=42 

Docker stats before the run:

```
NAME         CPU %     MEM USAGE / LIMIT    MEM %
web-api      0.14%     54.2MiB / 15.53GiB   0.34%
seq          0.39%     130MiB / 15.53GiB    0.82%
postgres     0.00%     941.4MiB / 3GiB      30.64%
clickhouse   2.03%     1.329GiB / 3GiB      44.31%
mongo        0.29%     503.1MiB / 3GiB      16.38%
```

## Results

Latencies in ms. Errors = non-2xx responses + socket errors (connect/read/write/timeout).

| Report | Source | RPS | Avg | p50 | p90 | p99 | Errors |
|---|---|---:|---:|---:|---:|---:|---:|
| r1_activity | pg | 0.12 | 17300.00 | 14080.00 | 30360.00 | 30360.00 | 0 |
| r1_activity | clickhouse | 7.92 | 1000.00 | 610.45 | 1830.00 | 2090.00 | 0 |
| r2_top_courses | pg | 0.73 | 10300.00 | 8200.00 | 21040.00 | 39800.00 | 0 |
| r2_top_courses | clickhouse | 15.03 | 552.18 | 498.42 | 1080.00 | 1600.00 | 0 |

## Speedup (NoSQL vs PostgreSQL)

RPS speedup = NoSQL RPS / PG RPS; p50 speedup = PG p50 / NoSQL p50 (> 1 means NoSQL is faster).

| Report | NoSQL | PG RPS | NoSQL RPS | RPS speedup | PG p50 | NoSQL p50 | p50 speedup |
|---|---|---:|---:|---:|---:|---:|---:|
| r1_activity | clickhouse | 0.12 | 7.92 | 66.00x | 14080.00 | 610.45 | 23.06x |
| r2_top_courses | clickhouse | 0.73 | 15.03 | 20.59x | 8200.00 | 498.42 | 16.45x |

Raw wrk output: `bench/results/raw/<report>_<source>.txt`.
