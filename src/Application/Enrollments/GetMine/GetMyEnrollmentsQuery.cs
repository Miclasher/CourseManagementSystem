using Application.Abstractions.Messaging;

namespace Application.Enrollments.GetMine;

public sealed record GetMyEnrollmentsQuery : IQuery<List<EnrollmentResponse>>;
