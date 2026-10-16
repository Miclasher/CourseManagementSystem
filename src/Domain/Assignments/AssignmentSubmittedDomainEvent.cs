using SharedKernel;

namespace Domain.Assignments;

public sealed record AssignmentSubmittedDomainEvent(Guid SubmissionId, Guid AssignmentId) : IDomainEvent;
