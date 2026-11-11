using Application.Reports.CourseDashboard;
using Application.Reports.StudentTranscript;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Infrastructure.Reports.Mongo;

/// <summary>
/// Read-model store: the raw report shapes from Application are stored as-is, one document per course / student,
/// keyed by the id as a string. Guids are stored as strings everywhere so documents stay readable in mongosh.
/// Shared by the API (reads) and tools/Seeder (projection writes).
/// </summary>
internal sealed class MongoReportsContext
{
    public const string CourseDashboardsCollection = "course_dashboards";
    public const string StudentTranscriptsCollection = "student_transcripts";

    private static readonly Lock RegistrationLock = new();
    private static bool _registered;

    public MongoReportsContext(IMongoClient client, string databaseName)
    {
        RegisterClassMaps();

        Database = client.GetDatabase(databaseName);
        CourseDashboards = Database.GetCollection<CourseDashboardData>(CourseDashboardsCollection);
        StudentTranscripts = Database.GetCollection<StudentTranscriptData>(StudentTranscriptsCollection);
    }

    public IMongoDatabase Database { get; }

    public IMongoCollection<CourseDashboardData> CourseDashboards { get; }

    public IMongoCollection<StudentTranscriptData> StudentTranscripts { get; }

    /// <summary>Database name from the connection string path (mongodb://host:27017/cms), "cms" when absent.</summary>
    public static string DatabaseNameOf(string connectionString) => MongoUrl.Create(connectionString).DatabaseName ?? "cms";

    private static void RegisterClassMaps()
    {
        lock (RegistrationLock)
        {
            if (_registered)
            {
                return;
            }

            BsonSerializer.TryRegisterSerializer(new GuidSerializer(BsonType.String));

            BsonClassMap.TryRegisterClassMap<CourseDashboardData>(map =>
            {
                map.AutoMap();
                map.MapIdMember(c => c.CourseId);
                map.SetIgnoreExtraElements(true);
            });

            BsonClassMap.TryRegisterClassMap<StudentTranscriptData>(map =>
            {
                map.AutoMap();
                map.MapIdMember(s => s.StudentId);
                map.SetIgnoreExtraElements(true);
            });

            _registered = true;
        }
    }
}
