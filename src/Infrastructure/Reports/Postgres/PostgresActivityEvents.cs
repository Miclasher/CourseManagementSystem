namespace Infrastructure.Reports.Postgres;

/// <summary>
/// Shared event definition of the activity reports: lesson completions and quiz attempts of non-deleted courses
/// in [@from, @to), each row carrying the user, course and quiz outcome.
/// </summary>
internal static class PostgresActivityEvents
{
    public const string Cte = """
        events AS (
            SELECT e.user_id, e.course_id, c.category_id,
                   lp.completed_at AS event_at,
                   true AS is_lesson, false AS passed, 0::bigint AS score_hundredths
            FROM lesson_progress lp
            JOIN enrollments e ON e.id = lp.enrollment_id
            JOIN courses c ON c.id = e.course_id AND NOT c.is_deleted
            WHERE lp.completed_at >= @from AND lp.completed_at < @to
            UNION ALL
            SELECT e.user_id, e.course_id, c.category_id,
                   qa.attempted_at AS event_at,
                   false AS is_lesson, qa.passed, round(qa.score_percent * 100)::bigint AS score_hundredths
            FROM quiz_attempts qa
            JOIN enrollments e ON e.id = qa.enrollment_id
            JOIN courses c ON c.id = e.course_id AND NOT c.is_deleted
            WHERE qa.attempted_at >= @from AND qa.attempted_at < @to
        )
        """;

    public const string Aggregates = """
        count(*) FILTER (WHERE ev.is_lesson)::bigint AS lessons_completed,
        count(*) FILTER (WHERE NOT ev.is_lesson)::bigint AS quiz_attempts,
        count(*) FILTER (WHERE NOT ev.is_lesson AND ev.passed)::bigint AS quiz_passed,
        coalesce(sum(ev.score_hundredths), 0)::bigint AS score_sum_hundredths,
        count(DISTINCT ev.user_id)::bigint AS active_students
        """;
}
