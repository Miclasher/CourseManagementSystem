using SharedKernel;

namespace Domain.Curriculum;

public sealed record ModuleAddedDomainEvent(Guid ModuleId, Guid CourseId) : IDomainEvent;
