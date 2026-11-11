# Lab 2: SQL vs NoSQL benchmark results

## Environment

- Date: 2026-11-11T19:15:51+02:00 (finished 2026-11-11T19:18:58+02:00)
- Host: Linux 7.3.1-1-cachyos x86_64, nproc: 12, 15 GiB RAM
- Target: `http://localhost:5000`
- wrk: `-t4 -c64 -d30s --latency --timeout 10s`, warm-up 10s per route (discarded), 3 s pause between runs
- Load script: `bench/lua/random_id.lua` (random id/param per request, per-thread seed); response status tracking: on
- Id/param pools: r3_course_dashboard=1935 r4_student_transcript=20000 

Docker stats before the run:

```
NAME         CPU %     MEM USAGE / LIMIT     MEM %
web-api      0.00%     175MiB / 15.53GiB     1.10%
seq          0.29%     150.7MiB / 15.53GiB   0.95%
postgres     0.24%     1.339GiB / 3GiB       44.64%
clickhouse   3.29%     1.271GiB / 3GiB       42.35%
mongo        0.27%     479MiB / 3GiB         15.59%
```

## Results

Latencies in ms. Errors = non-2xx responses + socket errors (connect/read/write/timeout).

| Report | Source | RPS | Avg | p50 | p90 | p99 | Errors |
|---|---|---:|---:|---:|---:|---:|---:|
| r3_course_dashboard | pg | 1740.83 | 38.90 | 24.05 | 78.99 | 110.79 | 0 |
| r3_course_dashboard | mongo | 21739.26 | 3.07 | 2.61 | 5.45 | 10.45 | 0 |
| r4_student_transcript | pg | 2161.44 | 32.60 | 19.89 | 71.61 | 92.13 | 0 |
| r4_student_transcript | mongo | 23714.06 | 2.83 | 2.40 | 4.96 | 10.15 | 0 |

## Speedup (NoSQL vs PostgreSQL)

RPS speedup = NoSQL RPS / PG RPS; p50 speedup = PG p50 / NoSQL p50 (> 1 means NoSQL is faster).

| Report | NoSQL | PG RPS | NoSQL RPS | RPS speedup | PG p50 | NoSQL p50 | p50 speedup |
|---|---|---:|---:|---:|---:|---:|---:|
| r3_course_dashboard | mongo | 1740.83 | 21739.26 | 12.49x | 24.05 | 2.61 | 9.21x |
| r4_student_transcript | mongo | 2161.44 | 23714.06 | 10.97x | 19.89 | 2.40 | 8.29x |

Raw wrk output: `bench/results/raw/<report>_<source>.txt`.
