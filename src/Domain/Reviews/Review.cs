using SharedKernel;

namespace Domain.Reviews;

public sealed class Review : Entity
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public Guid UserId { get; set; }
    public short Rating { get; set; }
    public string? Comment { get; set; }

    public static Review Create(Guid courseId, Guid userId, short rating, string? comment)
    {
        var review = new Review
        {
            Id = Guid.CreateVersion7(),
            CourseId = courseId,
            UserId = userId,
            Rating = rating,
            Comment = comment
        };

        review.Raise(new ReviewAddedDomainEvent(review.Id, courseId));

        return review;
    }
}
