using SharedKernel;

namespace Domain.Enrollments;

public sealed record LessonCompletedDomainEvent(Guid EnrollmentId, Guid LessonId) : IDomainEvent;
