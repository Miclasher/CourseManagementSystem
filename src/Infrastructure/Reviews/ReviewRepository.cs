using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Dapper;
using Domain.Reviews;

namespace Infrastructure.Reviews;

internal sealed class ReviewRepository(IUnitOfWork unitOfWork, IUserContext userContext) : IReviewRepository
{
    public void Add(Review review) =>
        unitOfWork.Enqueue(
            (connection, transaction, ct) => connection.ExecuteAsync(new CommandDefinition(
                "CALL sp_add_review(@Id::uuid, @CourseId::uuid, @UserId::uuid, @Rating::smallint, @Comment::text, @actor::uuid)",
                new { review.Id, review.CourseId, review.UserId, review.Rating, review.Comment, actor = userContext.UserId },
                transaction,
                cancellationToken: ct)),
            review);
}
