using Application.Abstractions.Authentication;
using Application.Courses.Create;
using Application.Courses.Delete;
using Application.Courses.Publish;
using Domain.Courses;

namespace Application.UnitTests.Courses;

public sealed class CourseHandlersTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private readonly ICourseRepository _courses = Substitute.For<ICourseRepository>();
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();

    public CourseHandlersTests() => _userContext.UserId.Returns(UserId);

    [Fact]
    public async Task Create_Should_QueueCourseAndRaiseEvent()
    {
        // Arrange
        var handler = new CreateCourseCommandHandler(_courses);
        var command = new CreateCourseCommand { CategoryId = Guid.NewGuid(), Title = "T", Slug = "t", Price = 5 };

        // Act
        Result<Guid> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        _courses.Received(1).Add(Arg.Is<Course>(c =>
            c.Id == result.Value &&
            c.Status == CourseStatus.Draft &&
            c.DomainEvents.Any(e => e is CourseCreatedDomainEvent)));
    }

    [Fact]
    public async Task Publish_Should_ReturnNotFound_WhenCourseDoesNotExist()
    {
        // Arrange
        var handler = new PublishCourseCommandHandler(_courses, _userContext);
        var command = new PublishCourseCommand(Guid.NewGuid());

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Error.ShouldBe(CourseErrors.NotFound(command.CourseId));
    }

    [Fact]
    public async Task Publish_Should_ReturnProblem_WhenUserIsNotInstructor()
    {
        // Arrange
        var course = new Course { Id = Guid.NewGuid() };
        _courses.GetByIdAsync(course.Id, Arg.Any<CancellationToken>()).Returns(course);
        _courses.IsInstructorAsync(course.Id, UserId, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new PublishCourseCommandHandler(_courses, _userContext);

        // Act
        Result result = await handler.Handle(new PublishCourseCommand(course.Id), CancellationToken.None);

        // Assert
        result.Error.ShouldBe(CourseErrors.NotInstructor(course.Id));
        _courses.DidNotReceive().Publish(Arg.Any<Course>());
    }

    [Fact]
    public async Task Publish_Should_ReturnProblem_WhenAlreadyPublished()
    {
        // Arrange
        var course = new Course { Id = Guid.NewGuid(), Status = CourseStatus.Published };
        _courses.GetByIdAsync(course.Id, Arg.Any<CancellationToken>()).Returns(course);
        _courses.IsInstructorAsync(course.Id, UserId, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new PublishCourseCommandHandler(_courses, _userContext);

        // Act
        Result result = await handler.Handle(new PublishCourseCommand(course.Id), CancellationToken.None);

        // Assert
        result.Error.ShouldBe(CourseErrors.AlreadyPublished(course.Id));
    }

    [Fact]
    public async Task Delete_Should_QueueSoftDeleteAndRaiseEvent_WhenInstructor()
    {
        // Arrange
        var course = new Course { Id = Guid.NewGuid() };
        _courses.GetByIdAsync(course.Id, Arg.Any<CancellationToken>()).Returns(course);
        _courses.IsInstructorAsync(course.Id, UserId, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new DeleteCourseCommandHandler(_courses, _userContext);

        // Act
        Result result = await handler.Handle(new DeleteCourseCommand(course.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        _courses.Received(1).Delete(course);
        course.DomainEvents.ShouldContain(e => e is CourseDeletedDomainEvent);
    }
}
