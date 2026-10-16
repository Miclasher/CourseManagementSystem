using Application.Abstractions.Authentication;
using Application.Enrollments.Enroll;
using Domain.Courses;
using Domain.Enrollments;

namespace Application.UnitTests.Enrollments;

public sealed class EnrollCommandHandlerTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private readonly ICourseRepository _courses = Substitute.For<ICourseRepository>();
    private readonly IEnrollmentRepository _enrollments = Substitute.For<IEnrollmentRepository>();
    private readonly EnrollCommandHandler _handler;

    public EnrollCommandHandlerTests()
    {
        IUserContext userContext = Substitute.For<IUserContext>();
        userContext.UserId.Returns(UserId);

        _handler = new EnrollCommandHandler(_courses, _enrollments, userContext);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenCourseDoesNotExist()
    {
        // Arrange
        var command = new EnrollCommand(Guid.NewGuid());

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(CourseErrors.NotFound(command.CourseId));
        _enrollments.DidNotReceive().Add(Arg.Any<Enrollment>());
    }

    [Fact]
    public async Task Handle_Should_ReturnProblem_WhenCourseIsNotPublished()
    {
        // Arrange
        Course course = ArrangeCourse(CourseStatus.Draft);
        var command = new EnrollCommand(course.Id);

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Error.ShouldBe(EnrollmentErrors.CourseNotPublished(course.Id));
        _enrollments.DidNotReceive().Add(Arg.Any<Enrollment>());
    }

    [Fact]
    public async Task Handle_Should_ReturnConflict_WhenAlreadyEnrolled()
    {
        // Arrange
        Course course = ArrangeCourse(CourseStatus.Published);
        _enrollments.GetCurrentAsync(course.Id, UserId, Arg.Any<CancellationToken>())
            .Returns(new Enrollment { Id = Guid.NewGuid(), CourseId = course.Id, UserId = UserId });

        // Act
        Result<Guid> result = await _handler.Handle(new EnrollCommand(course.Id), CancellationToken.None);

        // Assert
        result.Error.ShouldBe(EnrollmentErrors.AlreadyEnrolled(course.Id));
    }

    [Fact]
    public async Task Handle_Should_ReturnConflict_WhenCourseIsFull()
    {
        // Arrange
        Course course = ArrangeCourse(CourseStatus.Published, maxStudents: 1, enrolledCount: 1);

        // Act
        Result<Guid> result = await _handler.Handle(new EnrollCommand(course.Id), CancellationToken.None);

        // Assert
        result.Error.ShouldBe(EnrollmentErrors.CourseFull(course.Id));
    }

    [Fact]
    public async Task Handle_Should_QueueEnrollmentAndRaiseEvent_WhenValid()
    {
        // Arrange
        Course course = ArrangeCourse(CourseStatus.Published);
        Enrollment? queued = null;
        _enrollments.When(r => r.Add(Arg.Any<Enrollment>())).Do(call => queued = call.Arg<Enrollment>());

        // Act
        Result<Guid> result = await _handler.Handle(new EnrollCommand(course.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        queued.ShouldNotBeNull();
        queued.Id.ShouldBe(result.Value);
        queued.UserId.ShouldBe(UserId);
        queued.DomainEvents.ShouldContain(e => e is StudentEnrolledDomainEvent);
    }

    private Course ArrangeCourse(CourseStatus status, int? maxStudents = null, int enrolledCount = 0)
    {
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Status = status,
            MaxStudents = maxStudents,
            EnrolledCount = enrolledCount
        };
        _courses.GetByIdAsync(course.Id, Arg.Any<CancellationToken>()).Returns(course);

        return course;
    }
}
