using Application.Courses.Create;
using Domain.Courses;
using FluentValidation.TestHelper;

namespace Application.UnitTests.Courses;

public sealed class CourseValidatorsTests
{
    private readonly CreateCourseCommandValidator _validator = new();

    private static CreateCourseCommand Valid() => new()
    {
        CategoryId = Guid.NewGuid(),
        Title = "SQL",
        Slug = "sql-101",
        Level = CourseLevel.Beginner,
        Price = 0
    };

    [Fact]
    public void Create_Should_Pass_WhenCommandIsValid() =>
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Theory]
    [InlineData("")]
    [InlineData("Has Spaces")]
    [InlineData("UPPER")]
    public void Create_Should_HaveError_WhenSlugIsInvalid(string slug)
    {
        CreateCourseCommand command = Valid();
        command.Slug = slug;

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.Slug);
    }

    [Fact]
    public void Create_Should_HaveError_WhenPriceIsNegative()
    {
        CreateCourseCommand command = Valid();
        command.Price = -1;

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.Price);
    }

    [Fact]
    public void Create_Should_HaveError_WhenMaxStudentsIsZero()
    {
        CreateCourseCommand command = Valid();
        command.MaxStudents = 0;

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.MaxStudents);
    }
}
