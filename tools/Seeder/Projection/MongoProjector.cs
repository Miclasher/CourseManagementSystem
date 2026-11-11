using System.Globalization;
using Dapper;
using Infrastructure.Reports.Mongo;
using Infrastructure.Reports.Postgres;
using MongoDB.Bson;
using MongoDB.Driver;
using Npgsql;
using Seeder.Seeding;

namespace Seeder.Projection;

/// <summary>
/// Rebuilds the MongoDB read models. Every document is produced by the PostgreSQL report source itself
/// (single source of truth), so the API returns byte-identical responses from either store.
/// </summary>
internal sealed class MongoProjector(SeederOptions options)
{
    private const int Parallelism = 16;
    private const int MaxBatch = 1000;

    public async Task RunAsync(CancellationToken ct)
    {
        Log.Info("== project (MongoDB)");
        DefaultTypeMap.MatchNamesWithUnderscores = true; // same Dapper convention as Infrastructure.DependencyInjection

        var pgBuilder = new NpgsqlConnectionStringBuilder(options.Postgres) { MaxPoolSize = Parallelism * 2 };
        await using var dataSource = NpgsqlDataSource.Create(pgBuilder.ConnectionString);
        var context = new MongoReportsContext(new MongoClient(options.Mongo),
            MongoReportsContext.DatabaseNameOf(options.Mongo));

        await Log.StepAsync("drop + create collections", async () =>
        {
            foreach (string name in new[]
                     {
                         MongoReportsContext.CourseDashboardsCollection,
                         MongoReportsContext.StudentTranscriptsCollection
                     })
            {
                await context.Database.DropCollectionAsync(name, ct);
                await context.Database.CreateCollectionAsync(name, cancellationToken: ct);
            }
        });

        List<Guid> courseIds;
        List<Guid> studentIds;
        await using (NpgsqlConnection connection = await dataSource.OpenConnectionAsync(ct))
        {
            courseIds = await PostgresSeeder.ReadIdsAsync(connection,
                "SELECT id FROM courses WHERE NOT is_deleted ORDER BY id", ct);
            studentIds = await PostgresSeeder.ReadIdsAsync(connection, """
                                                                       SELECT u.id FROM users u
                                                                       WHERE NOT u.is_deleted
                                                                         AND EXISTS (SELECT 1 FROM user_roles ur JOIN roles r ON r.id = ur.role_id WHERE ur.user_id = u.id AND r.name = 'student')
                                                                       ORDER BY u.id
                                                                       """, ct);
        }

        var dashboards = new PostgresCourseDashboardSource(dataSource);
        long dashboardCount = await Log.StepAsync("course_dashboards", () =>
            ProjectAsync(courseIds, dashboards.GetAsync, context.CourseDashboards, ct));

        var transcripts = new PostgresStudentTranscriptSource(dataSource);
        long transcriptCount = await Log.StepAsync("student_transcripts", () =>
            ProjectAsync(studentIds, transcripts.GetAsync, context.StudentTranscripts, ct));

        long mongoDashboards = await context.CourseDashboards.EstimatedDocumentCountAsync(cancellationToken: ct);
        long mongoTranscripts = await context.StudentTranscripts.EstimatedDocumentCountAsync(cancellationToken: ct);
        Report("course_dashboards", courseIds.Count, dashboardCount, mongoDashboards);
        Report("student_transcripts", studentIds.Count, transcriptCount, mongoTranscripts);

        await PrintStatsAsync(context.Database, MongoReportsContext.CourseDashboardsCollection, ct);
        await PrintStatsAsync(context.Database, MongoReportsContext.StudentTranscriptsCollection, ct);

        if (mongoDashboards != courseIds.Count || mongoTranscripts != studentIds.Count)
        {
            throw new InvalidOperationException("MongoDB projection does not match PostgreSQL");
        }
    }

    private static async Task<long> ProjectAsync<T>(
        List<Guid> ids,
        Func<Guid, CancellationToken, Task<T?>> load,
        IMongoCollection<T> collection,
        CancellationToken ct)
        where T : class
    {
        long written = 0;
        int batchSize = Math.Clamp((int)Math.Ceiling(ids.Count / (Parallelism * 4.0)), 1, MaxBatch);
        var insertOptions = new InsertManyOptions { IsOrdered = false };

        await Parallel.ForEachAsync(
            ids.Chunk(batchSize),
            new ParallelOptions { MaxDegreeOfParallelism = Parallelism, CancellationToken = ct },
            async (chunk, token) =>
            {
                var documents = new List<T>(chunk.Length);
                foreach (Guid id in chunk)
                {
                    if (await load(id, token) is { } document)
                    {
                        documents.Add(document);
                    }
                }

                if (documents.Count > 0)
                {
                    await collection.InsertManyAsync(documents, insertOptions, token);
                    Interlocked.Add(ref written, documents.Count);
                }
            });

        return written;
    }

    private static void Report(string collection, long pg, long produced, long mongo) =>
        Log.Info(string.Create(CultureInfo.InvariantCulture,
            $"  {collection,-20} postgres={pg,9:N0} produced={produced,9:N0} mongo={mongo,9:N0} {(pg == mongo && pg == produced ? "OK" : "MISMATCH")}"));

    private static async Task PrintStatsAsync(IMongoDatabase database, string name, CancellationToken ct)
    {
        IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>(name);
        BsonDocument stats = await collection
            .Aggregate()
            .AppendStage<BsonDocument>(new BsonDocument("$collStats",
                new BsonDocument("storageStats", new BsonDocument())))
            .FirstAsync(ct);
        BsonDocument storage = stats["storageStats"].AsBsonDocument;
        Log.Info(string.Create(CultureInfo.InvariantCulture,
            $"  {name,-20} count={storage["count"].ToInt64(),9:N0} data={storage["size"].ToInt64() / 1048576.0,8:F1} MB " +
            $"storage={storage["storageSize"].ToInt64() / 1048576.0,8:F1} MB indexes={storage["totalIndexSize"].ToInt64() / 1048576.0,6:F1} MB " +
            $"avgObj={storage.GetValue("avgObjSize", 0).ToInt64():N0} B"));
    }
}
