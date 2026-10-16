using SharedKernel;

namespace Domain.Assignments;

public sealed class Submission : Entity
{
    public Guid Id { get; set; }
    public Guid AssignmentId { get; set; }
    public Guid EnrollmentId { get; set; }
    public string Content { get; set; } = string.Empty;
    public int? Score { get; set; }
    public string? Feedback { get; set; }
    public Guid? GradedBy { get; set; }
    public DateTime? GradedAt { get; set; }
    public DateTime SubmittedAt { get; set; }

    public static Submission Create(Guid assignmentId, Guid enrollmentId, string content)
    {
        var submission = new Submission
        {
            Id = Guid.CreateVersion7(),
            AssignmentId = assignmentId,
            EnrollmentId = enrollmentId,
            Content = content
        };

        submission.Raise(new AssignmentSubmittedDomainEvent(submission.Id, assignmentId));

        return submission;
    }

    public void Grade(int score, string? feedback, Guid gradedBy)
    {
        Score = score;
        Feedback = feedback;
        GradedBy = gradedBy;

        Raise(new SubmissionGradedDomainEvent(Id, score));
    }
}
