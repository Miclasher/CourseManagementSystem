using SharedKernel;

namespace Domain.Assignments;

public sealed record AssignmentCreatedDomainEvent(Guid AssignmentId) : IDomainEvent;
