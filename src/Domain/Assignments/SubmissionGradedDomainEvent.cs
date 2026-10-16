using SharedKernel;

namespace Domain.Assignments;

public sealed record SubmissionGradedDomainEvent(Guid SubmissionId, int Score) : IDomainEvent;
