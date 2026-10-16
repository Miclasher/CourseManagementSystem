namespace Application.Assignments.GetPending;

public sealed class PendingSubmissionResponse
{
    public Guid SubmissionId { get; set; }
    public DateTime SubmittedAt { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid AssignmentId { get; set; }
    public string AssignmentTitle { get; set; } = string.Empty;
    public int MaxScore { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
}
