using SharedKernel;

namespace Domain.Quizzes;

public static class QuizErrors
{
    public static Error NotFound(Guid quizId) => Error.NotFound(
        "Quizzes.NotFound",
        $"The quiz with the Id = '{quizId}' was not found");
}
