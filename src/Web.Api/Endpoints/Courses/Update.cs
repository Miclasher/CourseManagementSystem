using Application.Abstractions.Messaging;
using Application.Courses.Update;
using Domain.Courses;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Courses;

internal sealed class Update : IEndpoint
{
    public sealed class Request
    {
        public Guid CategoryId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Level { get; set; }
        public decimal Price { get; set; }
        public int? MaxStudents { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("courses/{id:guid}", async (
            Guid id,
            Request request,
            ICommandHandler<UpdateCourseCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateCourseCommand
            {
                CourseId = id,
                CategoryId = request.CategoryId,
                Title = request.Title,
                Description = request.Description,
                Level = (CourseLevel)request.Level,
                Price = request.Price,
                MaxStudents = request.MaxStudents
            };

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Courses);
    }
}
