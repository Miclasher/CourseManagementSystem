using SharedKernel;

namespace Domain.Assignments;

public static class AssignmentErrors
{
    public static Error NotFound(Guid assignmentId) => Error.NotFound(
        "Assignments.NotFound",
        $"The assignment with the Id = '{assignmentId}' was not found");

    public static Error SubmissionNotFound(Guid submissionId) => Error.NotFound(
        "Submissions.NotFound",
        $"The submission with the Id = '{submissionId}' was not found");

    public static Error ScoreOutOfRange(int maxScore) => Error.Problem(
        "Submissions.ScoreOutOfRange",
        $"The score must be between 0 and {maxScore}");
}
