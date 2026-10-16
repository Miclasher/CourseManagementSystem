using SharedKernel;

namespace Domain.Courses;

public sealed record CoursePublishedDomainEvent(Guid CourseId) : IDomainEvent;
