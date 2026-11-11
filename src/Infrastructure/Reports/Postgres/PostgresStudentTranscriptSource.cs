using Application.Reports.StudentTranscript;
using Dapper;
using Npgsql;

namespace Infrastructure.Reports.Postgres;

internal sealed class PostgresStudentTranscriptSource(NpgsqlDataSource dataSource) : IStudentTranscriptSource
{
    private const string Sql = """
        SELECT u.id AS student_id, u.first_name, u.last_name, u.email
        FROM users u
        WHERE u.id = @studentId
          AND NOT u.is_deleted
          AND EXISTS (SELECT 1
                      FROM user_roles ur
                      JOIN roles r ON r.id = ur.role_id
                      WHERE ur.user_id = u.id AND r.name = 'student');

        SELECT e.id AS enrollment_id, e.course_id, c.title AS course_title, e.status,
               e.enrolled_at, e.completed_at,
               (SELECT count(*)
                FROM lessons l
                JOIN modules m ON m.id = l.module_id
                WHERE m.course_id = c.id AND NOT l.is_deleted)::bigint AS total_lessons,
               (SELECT count(*)
                FROM lesson_progress lp
                JOIN lessons l ON l.id = lp.lesson_id
                WHERE lp.enrollment_id = e.id AND NOT l.is_deleted)::bigint AS completed_lessons,
               qa.quiz_attempts,
               qa.best_quiz_score_hundredths,
               cert.code AS certificate_code
        FROM enrollments e
        JOIN courses c ON c.id = e.course_id AND NOT c.is_deleted
        LEFT JOIN certificates cert ON cert.enrollment_id = e.id
        CROSS JOIN LATERAL (
            SELECT count(*)::bigint AS quiz_attempts,
                   round(max(a.score_percent) * 100)::bigint AS best_quiz_score_hundredths
            FROM quiz_attempts a
            WHERE a.enrollment_id = e.id) qa
        WHERE e.user_id = @studentId
        ORDER BY e.enrolled_at DESC, e.id::text ASC;
        """;

    public async Task<StudentTranscriptData?> GetAsync(Guid studentId, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

        await using SqlMapper.GridReader grid = await connection.QueryMultipleAsync(
            new CommandDefinition(Sql, new { studentId }, cancellationToken: cancellationToken));

        StudentTranscriptData? data = await grid.ReadSingleOrDefaultAsync<StudentTranscriptData>();
        if (data is null)
        {
            return null;
        }

        IEnumerable<TranscriptEnrollmentData> enrollments = await grid.ReadAsync<TranscriptEnrollmentData>();
        data.Enrollments = enrollments.AsList();

        return data;
    }
}
