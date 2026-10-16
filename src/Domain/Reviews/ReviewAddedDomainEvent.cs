using SharedKernel;

namespace Domain.Reviews;

public sealed record ReviewAddedDomainEvent(Guid ReviewId, Guid CourseId) : IDomainEvent;
