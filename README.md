# Learning Course Management System

Lab 1 (databases): a PostgreSQL model with soft delete, audit columns, stored procedures, functions, triggers, views and indexes, used from C# through **Repository + Unit of Work** where every query is a stored procedure call or a view/function read.

## Database

SQL lives in `src/Infrastructure/Database/Scripts` (embedded, applied once each in name order at startup in Development; tracked in `schema_versions`):

| Script | Content |
|---|---|
| `001_tables.sql` | 19 tables, PK/FK/CHECK/UNIQUE |
| `002_indexes.sql` | B-tree, partial/unique partial, GIN (full text and jsonb), BRIN, hash |
| `003_functions.sql` | `fn_course_progress`, `fn_student_gpa`, `fn_search_courses`, `fn_generate_certificate_code` |
| `004_views.sql` | catalog, curriculum, student enrollments, stats, pending submissions, deleted courses, entity views |
| `005_procedures.sql` | 19 `sp_*` procedures (all writes) |
| `006_triggers.sql` | `updated_at`, audit log, hard delete to soft delete, enrollment counter, rating, certificate |
| `007_seed.sql` | roles, demo admin / instructor / student, categories |

* **Soft delete** (`is_deleted`, `deleted_at`, `deleted_by`): users, categories, courses, lessons, reviews. A plain `DELETE` is converted by `trg_block_hard_delete`; deleting a course cascades to its lessons and restoring undoes exactly that.
* **Audit** (`created_at`, `updated_at`, `updated_by`) on the soft-delete tables plus modules, enrollments, assignments, submissions; `trg_audit_log` records old/new row data in `audit_log`.
* Procedures take `p_actor_id` and report business errors with `RAISE EXCEPTION ... USING ERRCODE = 'CM400' | 'CM404' | 'CM409', HINT = '<Error.Code>'`, which the API maps to 400/404/409 ProblemDetails.

## Repository + Unit of Work

* Repositories (`Domain/*/I*Repository`, Dapper implementations in `Infrastructure/*`) **read** from `v_*` views and **write** by queueing a delegate: `unitOfWork.Enqueue((conn, tx, ct) => conn.ExecuteAsync("CALL sp_...", args, tx), entity)`.
* `IUnitOfWork` is registered **transient**; every instance is a thin facade over a scoped `OperationQueue`, so all repositories (and the committer) of one request share the same pending work.
* `UnitOfWorkEndpointFilter` is attached to the `api` route group. When an endpoint returns a non-error result it calls `CommitAsync`: one connection, one transaction, all delegates in order, commit, then domain events are published. Error results and exceptions leave the queue unexecuted.
* Consequences: handlers validate by reading first, ids are generated client-side (`Guid.CreateVersion7()`), a handler cannot read its own pending writes, and the procedures re-check invariants (a failure there rolls back the whole request).
* Query handlers read through `ISqlConnectionFactory` + Dapper from views/functions only.

## Run

```
docker compose up -d postgres
dotnet run --project src/Web.Api --launch-profile http
```

Swagger UI: <http://localhost:5000/swagger>, OpenAPI JSON: <http://localhost:5000/swagger/v1/swagger.json>. Use the **Authorize** button to set the `X-User-Id` header (identity placeholder until real auth exists). Seeded users:

| Role | Id |
|---|---|
| admin | `00000000-0000-0000-0000-000000000001` |
| instructor | `00000000-0000-0000-0000-000000000002` |
| student | `00000000-0000-0000-0000-000000000003` |

`appsettings.Development.json` points at host `postgres` (docker network); change it to `localhost` to run the API on the host.

## Tests

```
dotnet test
```

Unit tests (handlers with mocked repositories, validators), architecture tests (layer rules, no Dapper/Npgsql in Domain) and integration tests (real API + Testcontainers Postgres with the real scripts; needs Docker).

## Template

To reuse the template: clone the repository, run `dotnet new install .`, then `dotnet new ca-min -n ProjectName`.

## Lab 2: SQL vs NoSQL (MongoDB + ClickHouse)

Four reports exist twice: computed live from the normalized PostgreSQL schema, and read from a denormalized NoSQL read model projected from PostgreSQL (CQRS). Every pair returns **byte-identical JSON**: sources return only raw integer aggregates, and one shared mapper (`Application/Reports/ReportMath.cs`) does all rounding, sorting and date formatting. `bench/compare.fish` checks the sha256 of 244 sampled response pairs, and all of them match.

| Report | PostgreSQL | NoSQL |
|---|---|---|
| R1 monthly activity per category (`/api/reports/{pg\|clickhouse}/activity/monthly?year=`) | join of ~8M events with enrollments/courses/categories + `count(distinct)` | ClickHouse columnar scan of `activity_events` |
| R2 top courses for a period (`/api/reports/{pg\|clickhouse}/courses/top?from=&to=&limit=`) | same joins, group by course | ClickHouse |
| R3 course dashboard (`/api/reports/{pg\|mongo}/courses/{id}/dashboard`) | 5 queries in one round trip (curriculum, enrollment counts, rating histogram, latest reviews) | MongoDB, one document by `_id` |
| R4 student transcript (`/api/reports/{pg\|mongo}/students/{id}/transcript`) | enrollments + per-course progress, quiz best score, certificates | MongoDB, one document by `_id` |

PostgreSQL gets proper B-tree indexes for these reports (`008_report_indexes.sql`) but no materialized views, because those would be the same denormalization.

### Run

```
docker compose up -d --build                                   # postgres, mongo, clickhouse, seq, web-api
dotnet run -c Release --project tools/Seeder -- all            # ~70 s: fake data via COPY, then projection to ClickHouse/Mongo
fish bench/compare.fish                                        # byte-equality check
fish bench/run.fish -o r3,r4 -O results_lookup.md              # -t4 -c64 -d30s
fish bench/run.fish -o r1,r2 -t 1 -c 1 -d 60s -T 120s -O results_analytics_single.md
fish bench/run.fish -o r1,r2 -t 2 -c 8 -d 60s -T 120s -O results_analytics.md
```

The data is deterministic (Bogus, seed 42, scale 1.0): 100k users, 2k courses, 36k lessons, 987k enrollments, 6.0M lesson_progress, 2.1M quiz_attempts, 300k reviews. That is 1.9 GB in PostgreSQL, 158 MB for `activity_events` in ClickHouse (7.9M rows), and 141 MB in MongoDB (1,935 dashboards, 97.5k transcripts). Each DB container is limited to 4 CPU / 3 GB. The API runs in Docker with Release build and Warning-level logs. The host has 12 cores. Full tables are in `bench/results/*.md`.

### Results

| Report | wrk | PG RPS | NoSQL RPS | PG p50 | NoSQL p50 | PG p99 | NoSQL p99 |
|---|---|---:|---:|---:|---:|---:|---:|
| R3 dashboard (Mongo) | -c64 | 1,741 | 21,739 | 24.1 ms | 2.6 ms | 110.8 ms | 10.5 ms |
| R4 transcript (Mongo) | -c64 | 2,161 | 23,714 | 19.9 ms | 2.4 ms | 92.1 ms | 10.2 ms |
| R1 monthly activity (ClickHouse) | -c1 | 0.38 | 13.3 | 2,710 ms | 89 ms | 5,630 ms | 181 ms |
| R2 top courses (ClickHouse) | -c1 | 0.80 | 21.7 | 1,210 ms | 43 ms | 4,810 ms | 177 ms |
| R1 monthly activity (ClickHouse) | -c8 | 0.12 | 7.9 | 14,080 ms | 610 ms | 30,360 ms | 2,090 ms |
| R2 top courses (ClickHouse) | -c8 | 0.73 | 15.0 | 8,200 ms | 498 ms | 39,800 ms | 1,600 ms |

* **MongoDB is ~11–12× faster in throughput and ~8–9× faster at p50 on point reads.** One indexed `_id` lookup of a ready document replaces several joins and aggregations per request.
* **ClickHouse is ~20–35× faster on analytics.** It reads only the needed columns of one wide, compressed, month-partitioned table (~10× smaller than the PG tables it replaces). PostgreSQL has to join and hash-aggregate millions of rows, and `count(distinct)` is especially expensive for it. Under concurrency PG degrades much faster: p50 rises from 2.7 s to 14 s at 8 connections.
* With Npgsql's default 30 s command timeout, PG failed some R1/R2 requests at `-c8`. The connection string sets `Command Timeout=120`, so all runs above have 0 errors.
* R1 PG at `-c8` completed only 7 requests in 60 s, so its RPS figure is noisy. The `-c1` row is the cleaner comparison.
* The price is duplicated data, a projection step (`tools/Seeder project`), and eventual consistency: the read models are only as fresh as the last projection. Writes, constraints and transactions stay in PostgreSQL.
