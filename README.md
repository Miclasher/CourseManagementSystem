# Learning Course Management System

Lab 1 (databases): a PostgreSQL model with soft delete, audit columns, stored procedures, functions, triggers, views and indexes, used from C# through **Repository + Unit of Work** where every query is a stored procedure call or a view/function read.

Built on a trimmed Clean Architecture template (Domain / Application / Infrastructure / Web.Api / SharedKernel, custom CQRS handlers, `Result`, Scrutor, `.claude/skills`). Persistence is **Dapper + Npgsql**, not EF Core.

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
