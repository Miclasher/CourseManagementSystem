using Application.Abstractions.Messaging;

namespace Application.Enrollments.Drop;

public sealed record DropEnrollmentCommand(Guid EnrollmentId) : ICommand;
