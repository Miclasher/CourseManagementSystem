namespace Application.Reports.StudentTranscript;

/// <summary>
/// Raw transcript of one non-deleted user holding the 'student' role. This exact shape is also the MongoDB document
/// (collection <c>student_transcripts</c>, <c>_id</c> = StudentId as string), produced by running the PostgreSQL source.
/// </summary>
public sealed class StudentTranscriptData
{
    public Guid StudentId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    /// <summary>Every enrollment (any status) in a non-deleted course.</summary>
    public List<TranscriptEnrollmentData> Enrollments { get; set; } = [];
}

public sealed class TranscriptEnrollmentData
{
    public Guid EnrollmentId { get; set; }
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime EnrolledAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    /// <summary>Non-deleted lessons of the course.</summary>
    public long TotalLessons { get; set; }
    /// <summary>lesson_progress rows of this enrollment for non-deleted lessons.</summary>
    public long CompletedLessons { get; set; }
    public long QuizAttempts { get; set; }
    /// <summary>max(score_percent) * 100 over this enrollment's attempts, null when there are none.</summary>
    public long? BestQuizScoreHundredths { get; set; }
    public string? CertificateCode { get; set; }
}
