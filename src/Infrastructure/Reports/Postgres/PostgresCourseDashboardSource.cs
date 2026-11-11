using Application.Reports.CourseDashboard;
using Dapper;
using Npgsql;

namespace Infrastructure.Reports.Postgres;

internal sealed class PostgresCourseDashboardSource(NpgsqlDataSource dataSource) : ICourseDashboardSource
{
    private const int RatingLevels = 5;

    private const string Sql = """
        SELECT c.id AS course_id, c.title, c.slug, c.level, c.status,
               (c.price * 100)::bigint AS price_cents,
               cat.name AS category_name,
               (SELECT u.first_name || ' ' || u.last_name
                FROM course_instructors ci
                JOIN users u ON u.id = ci.user_id
                WHERE ci.course_id = c.id AND ci.is_lead
                ORDER BY ci.assigned_at, ci.user_id::text
                LIMIT 1) AS lead_instructor_name,
               c.published_at,
               en.active_enrollments, en.completed_enrollments, en.dropped_enrollments
        FROM courses c
        JOIN categories cat ON cat.id = c.category_id
        CROSS JOIN LATERAL (
            SELECT count(*) FILTER (WHERE e.status = 'active')::bigint AS active_enrollments,
                   count(*) FILTER (WHERE e.status = 'completed')::bigint AS completed_enrollments,
                   count(*) FILTER (WHERE e.status = 'dropped')::bigint AS dropped_enrollments
            FROM enrollments e
            WHERE e.course_id = c.id) en
        WHERE c.id = @courseId AND NOT c.is_deleted;

        SELECT r.rating::int AS rating, count(*)::bigint AS count
        FROM reviews r
        WHERE r.course_id = @courseId AND NOT r.is_deleted
        GROUP BY r.rating;

        SELECT m.id AS module_id, m.title, m.position
        FROM modules m
        WHERE m.course_id = @courseId
        ORDER BY m.position, m.id::text;

        SELECT l.module_id, l.id AS lesson_id, l.title, l.position, l.duration_minutes
        FROM lessons l
        JOIN modules m ON m.id = l.module_id
        WHERE m.course_id = @courseId AND NOT l.is_deleted
        ORDER BY l.position, l.id::text;

        SELECT r.id AS review_id, u.first_name || ' ' || u.last_name AS author_name,
               r.rating::int AS rating, r.comment, r.created_at
        FROM reviews r
        JOIN users u ON u.id = r.user_id
        WHERE r.course_id = @courseId AND NOT r.is_deleted
        ORDER BY r.created_at DESC, r.id::text ASC
        LIMIT 5;
        """;

    public async Task<CourseDashboardData?> GetAsync(Guid courseId, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

        await using SqlMapper.GridReader grid = await connection.QueryMultipleAsync(
            new CommandDefinition(Sql, new { courseId }, cancellationToken: cancellationToken));

        CourseDashboardData? data = await grid.ReadSingleOrDefaultAsync<CourseDashboardData>();
        if (data is null)
        {
            return null;
        }

        IEnumerable<RatingCountRow> ratings = await grid.ReadAsync<RatingCountRow>();
        IEnumerable<DashboardModuleData> modules = await grid.ReadAsync<DashboardModuleData>();
        IEnumerable<LessonRow> lessons = await grid.ReadAsync<LessonRow>();
        IEnumerable<DashboardReviewData> reviews = await grid.ReadAsync<DashboardReviewData>();

        long[] ratingCounts = new long[RatingLevels];
        foreach (RatingCountRow rating in ratings)
        {
            if (rating.Rating is >= 1 and <= RatingLevels)
            {
                ratingCounts[rating.Rating - 1] = rating.Count;
            }
        }

        ILookup<Guid, LessonRow> lessonsByModule = lessons.ToLookup(l => l.ModuleId);

        data.RatingCounts = [.. ratingCounts];
        data.Modules = modules
            .Select(m =>
            {
                m.Lessons = lessonsByModule[m.ModuleId]
                    .Select(l => new DashboardLessonData
                    {
                        LessonId = l.LessonId,
                        Title = l.Title,
                        Position = l.Position,
                        DurationMinutes = l.DurationMinutes
                    })
                    .ToList();
                return m;
            })
            .ToList();
        data.RecentReviews = reviews.AsList();

        return data;
    }

    internal sealed class RatingCountRow
    {
        public int Rating { get; set; }
        public long Count { get; set; }
    }

    internal sealed class LessonRow
    {
        public Guid ModuleId { get; set; }
        public Guid LessonId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Position { get; set; }
        public int DurationMinutes { get; set; }
    }
}
