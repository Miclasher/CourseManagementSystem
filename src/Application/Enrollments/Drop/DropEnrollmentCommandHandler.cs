using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using Domain.Enrollments;
using SharedKernel;

namespace Application.Enrollments.Drop;

internal sealed class DropEnrollmentCommandHandler(IEnrollmentRepository enrollments, IUserContext userContext)
    : ICommandHandler<DropEnrollmentCommand>
{
    public async Task<Result> Handle(DropEnrollmentCommand command, CancellationToken cancellationToken)
    {
        Enrollment? enrollment = await enrollments.GetByIdAsync(command.EnrollmentId, cancellationToken);

        if (enrollment is null || enrollment.Status != EnrollmentStatus.Active)
        {
            return Result.Failure(EnrollmentErrors.NotFound(command.EnrollmentId));
        }

        if (enrollment.UserId != userContext.UserId)
        {
            return Result.Failure(EnrollmentErrors.NotOwner(enrollment.Id));
        }

        enrollment.Drop();
        enrollments.Drop(enrollment);

        return Result.Success();
    }
}
