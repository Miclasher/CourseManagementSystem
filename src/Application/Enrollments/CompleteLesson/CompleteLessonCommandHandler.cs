using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using Domain.Enrollments;
using SharedKernel;

namespace Application.Enrollments.CompleteLesson;

internal sealed class CompleteLessonCommandHandler(IEnrollmentRepository enrollments, IUserContext userContext)
    : ICommandHandler<CompleteLessonCommand>
{
    public async Task<Result> Handle(CompleteLessonCommand command, CancellationToken cancellationToken)
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

        enrollment.CompleteLesson(command.LessonId);
        enrollments.CompleteLesson(enrollment, command.LessonId);

        return Result.Success();
    }
}
