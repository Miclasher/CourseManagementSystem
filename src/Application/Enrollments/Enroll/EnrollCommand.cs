using Application.Abstractions.Messaging;

namespace Application.Enrollments.Enroll;

public sealed record EnrollCommand(Guid CourseId) : ICommand<Guid>;
