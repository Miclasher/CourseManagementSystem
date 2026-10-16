using SharedKernel;

namespace Domain.Enrollments;

public sealed record EnrollmentDroppedDomainEvent(Guid EnrollmentId) : IDomainEvent;
