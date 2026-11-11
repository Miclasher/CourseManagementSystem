# Lab 2 benchmark harness (SQL vs NoSQL)

Fish scripts + a wrk Lua script that check and load-test the report route pairs:

| Report | PostgreSQL route | NoSQL route | Inputs |
|---|---|---|---|
| `r1_activity` | `/api/reports/pg/activity/monthly?year=…` | `/api/reports/clickhouse/activity/monthly?…` | `params/r1_activity.txt` |
| `r2_top_courses` | `/api/reports/pg/courses/top?from=…&to=…&limit=…` | `/api/reports/clickhouse/courses/top?…` | `params/r2_top_courses.txt` |
| `r3_course_dashboard` | `/api/reports/pg/courses/{id}/dashboard` | `/api/reports/mongo/courses/{id}/dashboard` | `data/course_ids.txt` |
| `r4_student_transcript` | `/api/reports/pg/students/{id}/transcript` | `/api/reports/mongo/students/{id}/transcript` | `data/student_ids.txt` |

`data/*.txt` (one GUID per line) are written by the data seeder and are git-ignored.
`params/*.txt` are committed parameter lists (R1: years 2024/2025; R2: year, half and
quarter windows of 2024/2025 x limit 10/20/50). Both routes of a pair always use the same file,
so they get the same parameter distribution.

Prerequisites: API running (`GET $BASE_URL/health` must return 200), the seeder run, `wrk`, `curl`, `python3`.

## 1. Correctness: `compare.fish`

```fish
fish bench/compare.fish                 # all reports, 50 random ids for R3/R4
fish bench/compare.fish -n 200 -o r3    # 200 ids, only the course dashboard
```

For every sample both routes are fetched, the HTTP status must be 200 on both, and the bodies
must be byte-identical (sha256). R1/R2 check every parameter line. Prints matched / mismatched /
non-200 per report, shows a `diff -u` of the pretty-printed JSON for the first mismatch, and saves
both bodies plus URLs to `bench/results/mismatch/`. Exits 1 on any failure, 2 on setup errors.

## 2. Load test: `run.fish`

```fish
fish bench/run.fish                                   # defaults: 30s, 10s warm-up, 4 threads, 64 connections
fish bench/run.fish -d 60s -w 15s -t 8 -c 128
fish bench/run.fish --only r3,r4 --pause 5
BENCH_DURATION=10s BASE_URL=http://localhost:8080 fish bench/run.fish
```

| Flag | Env | Default | Meaning |
|---|---|---|---|
| `-d/--duration` | `BENCH_DURATION` | `30s` | measured run per route |
| `-w/--warmup` | `BENCH_WARMUP` | `10s` | discarded warm-up per route (`0` = none) |
| `-t/--threads` | `BENCH_THREADS` | `4` | wrk threads |
| `-c/--connections` | `BENCH_CONNECTIONS` | `64` | wrk connections |
| `-o/--only` | `BENCH_ONLY` | all | comma list, substring match (`r1`, `dashboard`, ...) |
| `-p/--pause` | `BENCH_PAUSE` | `3` | seconds of sleep between runs |
| `-b/--base-url` | `BASE_URL` | `http://localhost:5000` | API base URL |
| | `BENCH_DATA_DIR` | `bench/data` | where the id files are |
| | `BENCH_TRACK_STATUS` | `1` | `0` disables the Lua `response()` status counter |
| | `BENCH_DOCKER_STATS` | `1` | `0` skips the `docker stats` snapshot |

For each report: pg warm-up, pg measured, NoSQL warm-up, NoSQL measured (pairs back-to-back).
Outputs:

- `bench/results/raw/<report>_<source>.txt`: raw `wrk --latency` output (git-ignored)
- `bench/results/results.md`: environment, table (RPS, Avg/p50/p90/p99 in ms, errors = non-2xx +
  socket errors) and per-pair speedups (NoSQL RPS / PG RPS, PG p50 / NoSQL p50). Commit this one.
  It is overwritten on every run, so a `--only` run contains only the selected reports.

## wrk Lua script

`lua/random_id.lua` can be used directly:

```fish
wrk -t4 -c64 -d30s --latency -s bench/lua/random_id.lua http://localhost:5000 \
    -- '/api/reports/mongo/courses/{id}/dashboard' bench/data/course_ids.txt
```

Each thread loads the file once in `init()` (blank and `#` lines skipped), seeds `math.random` with
its own seed, and every request replaces `{id}` with a random line. `done()` prints
`BENCH_STATUS ... non2xx=N codes=200:…` from a per-thread status counter.
