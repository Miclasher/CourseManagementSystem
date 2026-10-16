using System.Net.Http.Json;
using Npgsql;

namespace IntegrationTests;

[Collection(nameof(IntegrationTestCollection))]
public abstract class BaseIntegrationTest
{
    protected static readonly Guid AdminId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    protected static readonly Guid InstructorId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    protected static readonly Guid StudentId = Guid.Parse("00000000-0000-0000-0000-000000000003");
    protected static readonly Guid DatabasesCategoryId = Guid.Parse("10000000-0000-0000-0000-000000000002");

    protected BaseIntegrationTest(IntegrationTestWebAppFactory factory)
    {
        Factory = factory;
        HttpClient = factory.CreateClient();
    }

    protected IntegrationTestWebAppFactory Factory { get; }

    protected HttpClient HttpClient { get; }

    /// <summary>Acts as the given user for all following requests (the API reads the X-User-Id header).</summary>
    protected void ActAs(Guid userId)
    {
        HttpClient.DefaultRequestHeaders.Remove("X-User-Id");
        HttpClient.DefaultRequestHeaders.Add("X-User-Id", userId.ToString());
    }

    protected async Task<Guid> CreateCourseAsync(int? maxStudents = null)
    {
        ActAs(InstructorId);

        HttpResponseMessage response = await HttpClient.PostAsJsonAsync("api/courses", new
        {
            categoryId = DatabasesCategoryId,
            title = "Course " + Guid.NewGuid(),
            slug = "course-" + Guid.NewGuid().ToString("N"),
            description = "Integration test course",
            level = 0,
            price = 0,
            maxStudents
        });
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    /// <summary>Creates a published course with the given number of lessons; returns the course and its lesson ids.</summary>
    protected async Task<(Guid CourseId, List<Guid> LessonIds)> CreatePublishedCourseAsync(
        int lessons = 2,
        int? maxStudents = null)
    {
        Guid courseId = await CreateCourseAsync(maxStudents);

        HttpResponseMessage moduleResponse = await HttpClient.PostAsJsonAsync(
            $"api/courses/{courseId}/modules",
            new { title = "Module" });
        moduleResponse.EnsureSuccessStatusCode();
        Guid moduleId = await moduleResponse.Content.ReadFromJsonAsync<Guid>();

        var lessonIds = new List<Guid>();
        for (int i = 0; i < lessons; i++)
        {
            HttpResponseMessage lessonResponse = await HttpClient.PostAsJsonAsync(
                $"api/modules/{moduleId}/lessons",
                new { title = $"Lesson {i}", content = "content", durationMinutes = 10 });
            lessonResponse.EnsureSuccessStatusCode();
            lessonIds.Add(await lessonResponse.Content.ReadFromJsonAsync<Guid>());
        }

        HttpResponseMessage publish = await HttpClient.PutAsync($"api/courses/{courseId}/publish", null);
        publish.EnsureSuccessStatusCode();

        return (courseId, lessonIds);
    }

    // Test-only helpers: the SQL comes from the tests themselves, never from user input.
#pragma warning disable CA2100
    protected async Task<T> ScalarAsync<T>(string sql)
    {
        await using var connection = new NpgsqlConnection(Factory.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);

        return (T)(await command.ExecuteScalarAsync())!;
    }

    protected async Task ExecuteAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(Factory.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
#pragma warning restore CA2100
}
