using Application.Abstractions.Messaging;
using Application.Courses.Create;
using Domain.Courses;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Courses;

internal sealed class Create : IEndpoint
{
    public sealed class Request
    {
        public Guid CategoryId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Level { get; set; }
        public decimal Price { get; set; }
        public int? MaxStudents { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("courses", async (
            Request request,
            ICommandHandler<CreateCourseCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateCourseCommand
            {
                CategoryId = request.CategoryId,
                Title = request.Title,
                Slug = request.Slug,
                Description = request.Description,
                Level = (CourseLevel)request.Level,
                Price = request.Price,
                MaxStudents = request.MaxStudents
            };

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Courses);
    }
}
