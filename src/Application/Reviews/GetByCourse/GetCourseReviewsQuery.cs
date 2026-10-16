using Application.Abstractions.Messaging;

namespace Application.Reviews.GetByCourse;

public sealed record GetCourseReviewsQuery(Guid CourseId) : IQuery<List<ReviewResponse>>;
