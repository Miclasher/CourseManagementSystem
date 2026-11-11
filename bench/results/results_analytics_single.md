# Lab 2: SQL vs NoSQL benchmark results

## Environment

- Date: 2026-11-11T19:30:54+02:00 (finished 2026-11-11T19:36:00+02:00)
- Host: Linux 7.3.1-1-cachyos x86_64, nproc: 12, 15 GiB RAM
- Target: `http://localhost:5000`
- wrk: `-t1 -c1 -d60s --latency --timeout 120s`, warm-up 10s per route (discarded), 3 s pause between runs
- Load script: `bench/lua/random_id.lua` (random id/param per request, per-thread seed); response status tracking: on
- Id/param pools: r1_activity=2 r2_top_courses=42 

Docker stats before the run:

```
NAME         CPU %     MEM USAGE / LIMIT     MEM %
web-api      0.01%     95.87MiB / 15.53GiB   0.60%
seq          0.43%     87.75MiB / 15.53GiB   0.55%
postgres     0.33%     950.1MiB / 3GiB       30.93%
clickhouse   2.79%     1.393GiB / 3GiB       46.44%
mongo        0.34%     504.8MiB / 3GiB       16.43%
```

## Results

Latencies in ms. Errors = non-2xx responses + socket errors (connect/read/write/timeout).

| Report | Source | RPS | Avg | p50 | p90 | p99 | Errors |
|---|---|---:|---:|---:|---:|---:|---:|
| r1_activity | pg | 0.38 | 2500.00 | 2710.00 | 5320.00 | 5630.00 | 0 |
| r1_activity | clickhouse | 13.32 | 75.70 | 89.39 | 134.83 | 180.88 | 0 |
| r2_top_courses | pg | 0.80 | 1390.00 | 1210.00 | 3490.00 | 4810.00 | 0 |
| r2_top_courses | clickhouse | 21.66 | 52.23 | 42.62 | 113.26 | 176.98 | 0 |

## Speedup (NoSQL vs PostgreSQL)

RPS speedup = NoSQL RPS / PG RPS; p50 speedup = PG p50 / NoSQL p50 (> 1 means NoSQL is faster).

| Report | NoSQL | PG RPS | NoSQL RPS | RPS speedup | PG p50 | NoSQL p50 | p50 speedup |
|---|---|---:|---:|---:|---:|---:|---:|
| r1_activity | clickhouse | 0.38 | 13.32 | 35.05x | 2710.00 | 89.39 | 30.32x |
| r2_top_courses | clickhouse | 0.80 | 21.66 | 27.07x | 1210.00 | 42.62 | 28.39x |

Raw wrk output: `bench/results/raw/<report>_<source>.txt`.
