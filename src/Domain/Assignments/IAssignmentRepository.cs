namespace Domain.Assignments;

public interface IAssignmentRepository
{
    Task<Assignment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Submission?> GetSubmissionByIdAsync(Guid id, CancellationToken cancellationToken = default);

    void Add(Assignment assignment);

    void Submit(Submission submission);

    void Grade(Submission submission);
}
