using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using Domain.Enrollments;
using Domain.Quizzes;
using SharedKernel;

namespace Application.Quizzes.SubmitAttempt;

internal sealed class SubmitQuizAttemptCommandHandler(
    IQuizRepository quizzes,
    IEnrollmentRepository enrollments,
    IUserContext userContext)
    : ICommandHandler<SubmitQuizAttemptCommand, Guid>
{
    public async Task<Result<Guid>> Handle(SubmitQuizAttemptCommand command, CancellationToken cancellationToken)
    {
        Quiz? quiz = await quizzes.GetByIdAsync(command.QuizId, cancellationToken);

        if (quiz is null)
        {
            return Result.Failure<Guid>(QuizErrors.NotFound(command.QuizId));
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

        var attempt = QuizAttempt.Create(quiz.Id, enrollment.Id, command.Answers);

        quizzes.AddAttempt(attempt);

        return attempt.Id;
    }
}
