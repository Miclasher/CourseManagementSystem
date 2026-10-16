using SharedKernel;

namespace Domain.Courses;

public sealed record CourseDeletedDomainEvent(Guid CourseId) : IDomainEvent;
