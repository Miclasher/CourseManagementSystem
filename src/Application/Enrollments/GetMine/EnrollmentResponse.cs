namespace Application.Enrollments.GetMine;

public sealed class EnrollmentResponse
{
    public Guid EnrollmentId { get; set; }
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal ProgressPercent { get; set; }
    public DateTime EnrolledAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? CertificateCode { get; set; }
}
