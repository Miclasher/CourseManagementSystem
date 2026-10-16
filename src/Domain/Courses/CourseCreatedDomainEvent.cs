using SharedKernel;

namespace Domain.Courses;

public sealed record CourseCreatedDomainEvent(Guid CourseId) : IDomainEvent;
