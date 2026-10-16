using Application.Abstractions.Messaging;

namespace Application.Assignments.Submit;

public sealed record SubmitAssignmentCommand(Guid AssignmentId, Guid EnrollmentId, string Content) : ICommand<Guid>;
