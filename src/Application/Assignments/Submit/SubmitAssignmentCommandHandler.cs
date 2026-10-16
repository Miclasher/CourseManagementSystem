using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using Domain.Assignments;
using Domain.Enrollments;
using SharedKernel;

namespace Application.Assignments.Submit;

internal sealed class SubmitAssignmentCommandHandler(
    IAssignmentRepository assignments,
    IEnrollmentRepository enrollments,
    IUserContext userContext)
    : ICommandHandler<SubmitAssignmentCommand, Guid>
{
    public async Task<Result<Guid>> Handle(SubmitAssignmentCommand command, CancellationToken cancellationToken)
    {
        Assignment? assignment = await assignments.GetByIdAsync(command.AssignmentId, cancellationToken);

        if (assignment is null)
        {
            return Result.Failure<Guid>(AssignmentErrors.NotFound(command.AssignmentId));
        }

        Enrollment? enrollment = await enrollments.GetByIdAsync(command.EnrollmentId, cancellationToken);

        if (enrollment is null || enrollment.Status == EnrollmentStatus.Dropped)
        {
            return Result.Failure<Guid>(EnrollmentErrors.NotFound(command.EnrollmentId));
        }

        if (enrollment.UserId != userContext.UserId)
        {
            return Result.Failure<Guid>(EnrollmentErrors.NotOwner(enrollment.Id));
        }

        var submission = Submission.Create(assignment.Id, enrollment.Id, command.Content);

        assignments.Submit(submission);

        return submission.Id;
    }
}
