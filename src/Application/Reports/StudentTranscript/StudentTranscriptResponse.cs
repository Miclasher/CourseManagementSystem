namespace Application.Reports.StudentTranscript;

public sealed record StudentTranscriptResponse(
    Guid StudentId,
    string FullName,
    string Email,
    int Courses,
    int CompletedCourses,
    int Certificates,
    decimal AverageProgress,
    IReadOnlyList<TranscriptEnrollment> Enrollments);

public sealed record TranscriptEnrollment(
    Guid EnrollmentId,
    Guid CourseId,
    string CourseTitle,
    string Status,
    string EnrolledAt,
    string? CompletedAt,
    long CompletedLessons,
    long TotalLessons,
    decimal ProgressPercent,
    long QuizAttempts,
    decimal? BestQuizScore,
    string? CertificateCode);
