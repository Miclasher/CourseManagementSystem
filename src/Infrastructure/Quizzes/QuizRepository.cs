using System.Data.Common;
using System.Text.Json;
using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Dapper;
using Domain.Quizzes;

namespace Infrastructure.Quizzes;

internal sealed class QuizRepository(
    ISqlConnectionFactory connectionFactory,
    IUnitOfWork unitOfWork,
    IUserContext userContext) : IQuizRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<Quiz?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using DbConnection connection = await connectionFactory.OpenAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<Quiz>(new CommandDefinition(
            "SELECT id, lesson_id, title, pass_percent, max_attempts FROM v_quizzes WHERE id = @id",
            new { id },
            cancellationToken: cancellationToken));
    }

    public void Add(Quiz quiz) =>
        unitOfWork.Enqueue(
            (connection, transaction, ct) => connection.ExecuteAsync(new CommandDefinition(
                "CALL sp_create_quiz(@Id::uuid, @LessonId::uuid, @Title::varchar, @PassPercent::integer, " +
                "@MaxAttempts::integer, @Questions::jsonb, @actor::uuid)",
                new
                {
                    quiz.Id,
                    quiz.LessonId,
                    quiz.Title,
                    quiz.PassPercent,
                    quiz.MaxAttempts,
                    Questions = JsonSerializer.Serialize(quiz.Questions, JsonOptions),
                    actor = userContext.UserId
                },
                transaction,
                cancellationToken: ct)),
            quiz);

    public void AddAttempt(QuizAttempt attempt) =>
        unitOfWork.Enqueue(
            (connection, transaction, ct) => connection.ExecuteAsync(new CommandDefinition(
                "CALL sp_submit_quiz_attempt(@Id::uuid, @QuizId::uuid, @EnrollmentId::uuid, @Answers::jsonb, @actor::uuid)",
                new
                {
                    attempt.Id,
                    attempt.QuizId,
                    attempt.EnrollmentId,
                    Answers = JsonSerializer.Serialize(attempt.Answers, JsonOptions),
                    actor = userContext.UserId
                },
                transaction,
                cancellationToken: ct)),
            attempt);
}
