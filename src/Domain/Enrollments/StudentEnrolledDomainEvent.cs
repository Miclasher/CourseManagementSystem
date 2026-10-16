using SharedKernel;

namespace Domain.Enrollments;

public sealed record StudentEnrolledDomainEvent(Guid EnrollmentId, Guid CourseId, Guid UserId) : IDomainEvent;
