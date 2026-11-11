using System.Globalization;
using System.Text;
using System.Text.Json;
using Bogus;
using Npgsql;
using NpgsqlTypes;

namespace Seeder.Seeding;

internal sealed class DataGenerator
{
    private static readonly (Guid Id, string Email, string First, string Last, short Role)[] DemoUsers =
    [
        (Guid.Parse("00000000-0000-0000-0000-000000000001"), "admin@cms.local", "Ada", "Admin", 1),
        (Guid.Parse("00000000-0000-0000-0000-000000000002"), "teacher@cms.local", "Ivan", "Instructor", 2),
        (Guid.Parse("00000000-0000-0000-0000-000000000003"), "student@cms.local", "Stella", "Student", 3),
    ];

    private static readonly (Guid Id, string Name, string Slug)[] DemoCategories =
    [
        (Guid.Parse("10000000-0000-0000-0000-000000000001"), "Programming", "programming"),
        (Guid.Parse("10000000-0000-0000-0000-000000000002"), "Databases", "databases"),
    ];

    private static readonly (string Name, int Parent)[] ExtraCategories =
    [
        ("Web Development", 0), ("Mobile Development", 0), ("Game Development", 0), ("Programming Languages", 0),
        ("Data Engineering", 1), ("Data Science", -1), ("Machine Learning", -1), ("DevOps", -1),
        ("Cloud Computing", -1), ("Cybersecurity", -1), ("Design", -1), ("Business", -1), ("Marketing", -1),
        ("Finance", -1), ("Mathematics", -1), ("Languages", -1), ("Personal Development", -1),
        ("Project Management", -1),
    ];

    private static readonly string[] Levels = ["beginner", "intermediate", "advanced"];
    private static readonly string[] MaterialTypes = ["link", "video", "pdf", "slides", "code"];

    private static readonly string[] TitleSuffixes =
    [
        "Fundamentals", "in Practice", "Masterclass", "Bootcamp", "Deep Dive", "Essentials", "for Beginners",
        "Patterns", "from Scratch", "Workshop"
    ];

    private const string PasswordHash = "$2a$11$seededseededseededseedeuQ8l1R0eB1yJmZb3g9eYd0b7r5Vw2nKq";

    private readonly NpgsqlConnection _connection;
    private readonly double _scale;
    private readonly Faker _faker;
    private readonly SeedModel _model = new();
    private readonly Dictionary<string, long> _counts = [];

    private readonly string[] _sentences;
    private readonly string[] _paragraphs;
    private readonly string[] _reviewComments;
    private readonly string[] _optionSets;

    public DataGenerator(NpgsqlConnection connection, double scale)
    {
        _connection = connection;
        _scale = scale;
        Randomizer.Seed = new Random(42);
        _faker = new Faker("en");
        _sentences = Enumerable.Range(0, 2000).Select(_ => _faker.Lorem.Sentence(6, 8)).ToArray();
        _paragraphs = Enumerable.Range(0, 500).Select(_ => _faker.Lorem.Paragraph(3)).ToArray();
        _reviewComments = Enumerable.Range(0, 2000).Select(_ => _faker.Rant.Review("course")).ToArray();
        _optionSets = Enumerable.Range(0, 300)
            .Select(_ => JsonSerializer.Serialize(Enumerable.Range(0, 4).Select(_ => _faker.Hacker.Noun()).ToArray()))
            .ToArray();
    }

    public IReadOnlyDictionary<string, long> Counts => _counts;

    private int Scaled(int baseline, int minimum) => Math.Max(minimum, (int)Math.Round(baseline * _scale));

    public async Task RunAsync(CancellationToken ct)
    {
        await Step("users + user_roles", GenerateUsers, ct);
        await Step("categories", GenerateCategories, ct);
        await Step("courses + course_instructors", GenerateCourses, ct);
        await Step("modules + lessons + lesson_materials + quizzes + quiz_questions + assignments", GenerateStructure,
            ct);
        await Step("enrollments", GenerateEnrollments, ct);
        await Step("lesson_progress", GenerateLessonProgress, ct);
        await Step("quiz_attempts", GenerateQuizAttempts, ct);
        await Step("submissions", GenerateSubmissions, ct);
        await Step("certificates", GenerateCertificates, ct);
        await Step("reviews", GenerateReviews, ct);
    }

    private Task Step(string name, Action action, CancellationToken ct) =>
        Log.StepAsync($"  copy {name}", () => Task.Run(action, ct));

    private NpgsqlBinaryImporter Copy(string table, string columns) =>
        _connection.BeginBinaryImport($"COPY {table} ({columns}) FROM STDIN (FORMAT BINARY)");

    private void Done(string table, NpgsqlBinaryImporter importer)
    {
        ulong rows = importer.Complete();
        _counts[table] = (long)rows;
    }

    private static Guid NewGuid(Random rng)
    {
        Span<byte> bytes = stackalloc byte[16];
        rng.NextBytes(bytes);
        bytes[7] = (byte)((bytes[7] & 0x0F) | 0x40); // version 4
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80); // RFC 4122 variant
        return new Guid(bytes);
    }

    private static void Ts(NpgsqlBinaryImporter w, long seconds) =>
        w.Write(SeedTime.ToUtc(seconds), NpgsqlDbType.TimestampTz);

    private static void TsOrNull(NpgsqlBinaryImporter w, long seconds)
    {
        if (seconds > 0)
        {
            Ts(w, seconds);
        }
        else
        {
            w.WriteNull();
        }
    }

    private static void Id(NpgsqlBinaryImporter w, Guid id) => w.Write(id, NpgsqlDbType.Uuid);

    private static void Text(NpgsqlBinaryImporter w, string? value)
    {
        if (value is null)
        {
            w.WriteNull();
        }
        else
        {
            w.Write(value, NpgsqlDbType.Text);
        }
    }

    private static void NullUuid(NpgsqlBinaryImporter w) => w.WriteNull();

    private static string Slugify(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (char c in text.ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(c))
            {
                sb.Append(c);
            }
            else if (sb.Length > 0 && sb[^1] != '-')
            {
                sb.Append('-');
            }
        }

        return sb.ToString().Trim('-');
    }

    private static string Letters(string text) =>
        new(text.ToLowerInvariant().Where(char.IsAsciiLetter).ToArray());

    private static string TitleCase(string text) =>
        CultureInfo.InvariantCulture.TextInfo.ToTitleCase(text);

    private void GenerateUsers()
    {
        var rng = new Random(1);
        int total = Scaled(100_000, 10);
        int instructors = Scaled(1_500, 2);
        int generated = total - DemoUsers.Length;

        using (NpgsqlBinaryImporter w = Copy("users",
                   "id, email, password_hash, first_name, last_name, bio, created_at, updated_at, is_deleted, deleted_at, deleted_by"))
        {
            foreach ((Guid Id, string Email, string First, string Last, short Role) demo in DemoUsers)
            {
                int index = _model.UserIds.Count;
                _model.UserIds.Add(demo.Id);
                _model.UserCreatedAt.Add(SeedTime.Start);
                if (demo.Role == 2)
                {
                    _model.Instructors.Add(index);
                }
                else if (demo.Role == 3)
                {
                    _model.Students.Add(index);
                }

                w.StartRow();
                Id(w, demo.Id);
                Text(w, demo.Email);
                Text(w, "seed");
                Text(w, demo.First);
                Text(w, demo.Last);
                w.WriteNull();
                Ts(w, SeedTime.Start);
                Ts(w, SeedTime.Start);
                w.Write(false);
                w.WriteNull();
                w.WriteNull();
            }

            for (int i = 0; i < generated; i++)
            {
                bool instructor = i < instructors;
                Guid id = NewGuid(rng);
                string first = _faker.Name.FirstName();
                string last = _faker.Name.LastName();
                long created = instructor
                    ? SeedTime.Between(rng, SeedTime.Start, SeedTime.Start + 90 * SeedTime.Day)
                    : SeedTime.Between(rng, SeedTime.Start, SeedTime.Start + 600 * SeedTime.Day);
                bool deleted = !instructor && rng.NextDouble() < 0.01;
                long deletedAt = deleted ? SeedTime.Between(rng, created + SeedTime.Day, SeedTime.End) : 0;

                int index = _model.UserIds.Count;
                _model.UserIds.Add(id);
                _model.UserCreatedAt.Add(created);
                (instructor ? _model.Instructors : _model.Students).Add(index);

                w.StartRow();
                Id(w, id);
                Text(w,
                    string.Create(CultureInfo.InvariantCulture,
                        $"{Letters(first)}.{Letters(last)}.{i + 1}@example.com"));
                Text(w, PasswordHash);
                Text(w, first);
                Text(w, last);
                Text(w, instructor || rng.NextDouble() < 0.2 ? _sentences[rng.Next(_sentences.Length)] : null);
                Ts(w, created);
                Ts(w, deleted ? deletedAt : created);
                w.Write(deleted);
                TsOrNull(w, deletedAt);
                if (deleted)
                {
                    Id(w, id);
                }
                else
                {
                    NullUuid(w);
                }
            }

            Done("users", w);
        }

        using (NpgsqlBinaryImporter w = Copy("user_roles", "user_id, role_id"))
        {
            WriteRole(w, DemoUsers[0].Id, 1);
            foreach (int u in _model.Instructors)
            {
                WriteRole(w, _model.UserIds[u], 2);
            }

            foreach (int u in _model.Students)
            {
                WriteRole(w, _model.UserIds[u], 3);
            }

            Done("user_roles", w);
        }

        static void WriteRole(NpgsqlBinaryImporter w, Guid user, short role)
        {
            w.StartRow();
            Id(w, user);
            w.Write(role, NpgsqlDbType.Smallint);
        }
    }

    // ------------------------------------------------------------------ categories

    private void GenerateCategories()
    {
        var rng = new Random(2);
        using NpgsqlBinaryImporter w = Copy("categories",
            "id, parent_id, name, slug, created_at, updated_at, is_deleted");

        foreach ((Guid Id, string Name, string Slug) demo in DemoCategories)
        {
            _model.CategoryIds.Add(demo.Id);
            Write(demo.Id, null, demo.Name, demo.Slug);
        }

        foreach (var (name, parent) in ExtraCategories)
        {
            Guid id = NewGuid(rng);
            _model.CategoryIds.Add(id);
            Write(id, parent >= 0 ? DemoCategories[parent].Id : null, name, Slugify(name));
        }

        Done("categories", w);

        void Write(Guid id, Guid? parent, string name, string slug)
        {
            w.StartRow();
            Id(w, id);
            if (parent is { } p)
            {
                Id(w, p);
            }
            else
            {
                w.WriteNull();
            }

            Text(w, name);
            Text(w, slug);
            Ts(w, SeedTime.Start);
            Ts(w, SeedTime.Start);
            w.Write(false);
        }
    }

    private void GenerateCourses()
    {
        var rng = new Random(3);
        int count = Scaled(2_000, 5);
        // category popularity: a few big categories, a long tail
        double[] categoryWeights = _model.CategoryIds.Select((_, i) => 1.0 / Math.Sqrt(i + 1)).ToArray();
        double[] categoryCdf = Cdf(categoryWeights);

        using (NpgsqlBinaryImporter w = Copy("courses",
                   "id, category_id, title, slug, description, level, status, price, max_students, published_at, " +
                   "rating_avg, rating_count, enrolled_count, created_at, updated_at, is_deleted, deleted_at, deleted_by"))
        {
            for (int i = 0; i < count; i++)
            {
                Guid id = NewGuid(rng);
                int category = Sample(rng, categoryCdf);
                string title = string.Create(CultureInfo.InvariantCulture,
                    $"{TitleCase(_faker.Hacker.Adjective())} {TitleCase(_faker.Hacker.Noun())} {TitleSuffixes[rng.Next(TitleSuffixes.Length)]}");
                double s = rng.NextDouble();
                string status = s < 0.82 ? "published" : s < 0.92 ? "draft" : "archived";
                long created = SeedTime.Between(rng, SeedTime.Start, SeedTime.Start + 365 * SeedTime.Day);
                long published = status == "draft" ? 0 : SeedTime.Between(rng, created, created + 30 * SeedTime.Day);
                bool deleted = rng.NextDouble() < 0.03;
                long deletedAt = deleted
                    ? SeedTime.Between(rng, Math.Max(created, published) + 30 * SeedTime.Day, SeedTime.End)
                    : 0;
                decimal price = rng.NextDouble() < 0.3 ? 0m : rng.Next(10, 200) - 0.01m;
                int lead = _model.Instructors[rng.Next(_model.Instructors.Count)];

                _model.Courses.Add(new CourseInfo
                    { Id = id, CreatedAt = created, PublishedAt = published, LeadInstructor = lead });

                w.StartRow();
                Id(w, id);
                Id(w, _model.CategoryIds[category]);
                Text(w, title);
                Text(w, string.Create(CultureInfo.InvariantCulture, $"{Slugify(title)}-{i + 1}"));
                Text(w, _paragraphs[rng.Next(_paragraphs.Length)]);
                Text(w, Levels[rng.Next(Levels.Length)]);
                Text(w, status);
                w.Write(price, NpgsqlDbType.Numeric);
                if (rng.NextDouble() < 0.15)
                {
                    w.Write(rng.Next(1, 11) * 5_000, NpgsqlDbType.Integer);
                }
                else
                {
                    w.WriteNull();
                }

                TsOrNull(w, published);
                w.Write(0m, NpgsqlDbType.Numeric);
                w.Write(0, NpgsqlDbType.Integer);
                w.Write(0, NpgsqlDbType.Integer);
                Ts(w, created);
                Ts(w, Math.Max(published, Math.Max(created, deletedAt)));
                w.Write(deleted);
                TsOrNull(w, deletedAt);
                if (deleted)
                {
                    Id(w, _model.UserIds[lead]);
                }
                else
                {
                    w.WriteNull();
                }
            }

            Done("courses", w);
        }

        using (NpgsqlBinaryImporter w = Copy("course_instructors", "course_id, user_id, is_lead, assigned_at"))
        {
            Span<int> chosen = stackalloc int[3];
            foreach (CourseInfo course in _model.Courses)
            {
                int n = 1 + rng.Next(3);
                int picked = 0;
                chosen[picked++] = course.LeadInstructor;
                for (int attempt = 0; picked < n && attempt < 20; attempt++)
                {
                    int candidate = _model.Instructors[rng.Next(_model.Instructors.Count)];
                    if (!chosen[..picked].Contains(candidate))
                    {
                        chosen[picked++] = candidate;
                    }
                }

                for (int k = 0; k < picked; k++)
                {
                    w.StartRow();
                    Id(w, course.Id);
                    Id(w, _model.UserIds[chosen[k]]);
                    w.Write(k == 0);
                    Ts(w, course.CreatedAt);
                }
            }

            Done("course_instructors", w);
        }
    }


    private void GenerateStructure()
    {
        var rng = new Random(4);
        var modules = new List<(Guid Id, Guid Course, int Position, long Created, int LessonFrom, int LessonTo)>();

        // lessons first into memory-light lists, then COPY each table
        foreach (CourseInfo course in _model.Courses)
        {
            course.LessonStart = _model.LessonIds.Count;
            int moduleCount = 4 + rng.Next(5); // 4..8, ~6
            for (int m = 1; m <= moduleCount; m++)
            {
                int from = _model.LessonIds.Count;
                int lessonCount = 2 + rng.Next(3); // 2..4, ~3
                for (int l = 0; l < lessonCount; l++)
                {
                    _model.LessonIds.Add(NewGuid(rng));
                    _model.LessonDeleted.Add(rng.NextDouble() < 0.03);
                }

                modules.Add((NewGuid(rng), course.Id, m, course.CreatedAt, from, _model.LessonIds.Count));
            }

            course.LessonCount = _model.LessonIds.Count - course.LessonStart;
        }

        using (NpgsqlBinaryImporter w = Copy("modules", "id, course_id, title, position, created_at, updated_at"))
        {
            foreach ((Guid Id, Guid Course, int Position, long Created, int LessonFrom, int LessonTo) module in modules)
            {
                w.StartRow();
                Id(w, module.Id);
                Id(w, module.Course);
                Text(w,
                    string.Create(CultureInfo.InvariantCulture,
                        $"Module {module.Position}: {TitleCase(_faker.Hacker.IngVerb())} {_faker.Hacker.Noun()}"));
                w.Write(module.Position, NpgsqlDbType.Integer);
                Ts(w, module.Created);
                Ts(w, module.Created);
            }

            Done("modules", w);
        }

        using (NpgsqlBinaryImporter w = Copy("lessons",
                   "id, module_id, title, content, duration_minutes, position, created_at, updated_at, is_deleted, deleted_at, deleted_by"))
        {
            foreach ((Guid Id, Guid Course, int Position, long Created, int LessonFrom, int LessonTo) module in modules)
            {
                for (int l = module.LessonFrom; l < module.LessonTo; l++)
                {
                    bool deleted = _model.LessonDeleted[l];
                    long deletedAt =
                        deleted ? SeedTime.Between(rng, module.Created + 60 * SeedTime.Day, SeedTime.End) : 0;
                    w.StartRow();
                    Id(w, _model.LessonIds[l]);
                    Id(w, module.Id);
                    Text(w, TitleCase(_sentences[rng.Next(_sentences.Length)].TrimEnd('.')));
                    Text(w, _paragraphs[rng.Next(_paragraphs.Length)]);
                    w.Write(5 + rng.Next(56), NpgsqlDbType.Integer);
                    w.Write(l - module.LessonFrom + 1, NpgsqlDbType.Integer);
                    Ts(w, module.Created);
                    Ts(w, deleted ? deletedAt : module.Created);
                    w.Write(deleted);
                    TsOrNull(w, deletedAt);
                    w.WriteNull();
                }
            }

            Done("lessons", w);
        }

        using (NpgsqlBinaryImporter w = Copy("lesson_materials", "id, lesson_id, title, url, type, created_at"))
        {
            foreach ((Guid Id, Guid Course, int Position, long Created, int LessonFrom, int LessonTo) module in modules)
            {
                for (int l = module.LessonFrom; l < module.LessonTo; l++)
                {
                    if (rng.NextDouble() >= 0.3)
                    {
                        continue;
                    }

                    Guid id = NewGuid(rng);
                    string type = MaterialTypes[rng.Next(MaterialTypes.Length)];
                    w.StartRow();
                    Id(w, id);
                    Id(w, _model.LessonIds[l]);
                    Text(w, string.Create(CultureInfo.InvariantCulture, $"{TitleCase(type)}: {_faker.Hacker.Noun()}"));
                    Text(w,
                        string.Create(CultureInfo.InvariantCulture, $"https://cdn.cms.local/materials/{id:N}.{type}"));
                    Text(w, type);
                    Ts(w, module.Created);
                }
            }

            Done("lesson_materials", w);
        }

        // quizzes: ~0.8 per module, at least one per course; attached to a lesson of the module
        var quizzes = new List<(QuizInfo Quiz, Guid Lesson)>();
        var assignments = new List<(AssignmentInfo Assignment, Guid Lesson, long Created)>();
        int moduleIndex = 0;
        foreach (CourseInfo course in _model.Courses)
        {
            int firstModule = moduleIndex;
            while (moduleIndex < modules.Count && modules[moduleIndex].Course == course.Id)
            {
                (Guid Id, Guid Course, int Position, long Created, int LessonFrom, int LessonTo) module =
                    modules[moduleIndex];
                int quizCount = (rng.NextDouble() < 0.6 ? 1 : 0) + (rng.NextDouble() < 0.2 ? 1 : 0);
                if (quizCount == 0 && moduleIndex == firstModule && course.Quizzes.Count == 0)
                {
                    quizCount = 1;
                }

                for (int q = 0; q < quizCount; q++)
                {
                    var quiz = new QuizInfo(NewGuid(rng), 50 + 5 * rng.Next(7), 1 + rng.Next(3));
                    course.Quizzes.Add(quiz);
                    quizzes.Add((quiz,
                        _model.LessonIds[module.LessonFrom + rng.Next(module.LessonTo - module.LessonFrom)]));
                }

                if (rng.NextDouble() < 0.5)
                {
                    var assignment = new AssignmentInfo(NewGuid(rng),
                        rng.NextDouble() < 0.7 ? 100 : 10 * (1 + rng.Next(10)));
                    course.Assignments.Add(assignment);
                    assignments.Add((assignment, _model.LessonIds[module.LessonTo - 1], module.Created));
                }

                moduleIndex++;
            }
        }

        using (NpgsqlBinaryImporter w = Copy("quizzes", "id, lesson_id, title, pass_percent, max_attempts"))
        {
            foreach (var (quiz, lesson) in quizzes)
            {
                w.StartRow();
                Id(w, quiz.Id);
                Id(w, lesson);
                Text(w, string.Create(CultureInfo.InvariantCulture, $"Quiz: {TitleCase(_faker.Hacker.Noun())}"));
                w.Write(quiz.PassPercent, NpgsqlDbType.Integer);
                w.Write(quiz.MaxAttempts, NpgsqlDbType.Integer);
            }

            Done("quizzes", w);
        }

        using (NpgsqlBinaryImporter w = Copy("quiz_questions", "id, quiz_id, text, options, correct_option, points"))
        {
            foreach (var (quiz, _) in quizzes)
            {
                int questions = 3 + rng.Next(4);
                for (int q = 0; q < questions; q++)
                {
                    w.StartRow();
                    Id(w, NewGuid(rng));
                    Id(w, quiz.Id);
                    Text(w, _sentences[rng.Next(_sentences.Length)].TrimEnd('.') + "?");
                    w.Write(_optionSets[rng.Next(_optionSets.Length)], NpgsqlDbType.Jsonb);
                    w.Write(rng.Next(4), NpgsqlDbType.Integer);
                    w.Write(1 + rng.Next(3), NpgsqlDbType.Integer);
                }
            }

            Done("quiz_questions", w);
        }

        using (NpgsqlBinaryImporter w = Copy("assignments",
                   "id, lesson_id, title, description, max_score, due_at, created_at, updated_at"))
        {
            foreach (var (assignment, lesson, created) in assignments)
            {
                w.StartRow();
                Id(w, assignment.Id);
                Id(w, lesson);
                Text(w,
                    string.Create(CultureInfo.InvariantCulture,
                        $"Assignment: {TitleCase(_faker.Hacker.Verb())} the {_faker.Hacker.Noun()}"));
                Text(w, _paragraphs[rng.Next(_paragraphs.Length)]);
                w.Write(assignment.MaxScore, NpgsqlDbType.Integer);
                TsOrNull(w,
                    rng.NextDouble() < 0.7
                        ? SeedTime.Between(rng, created + 30 * SeedTime.Day, created + 400 * SeedTime.Day)
                        : 0);
                Ts(w, created);
                Ts(w, created);
            }

            Done("assignments", w);
        }
    }


    private void GenerateEnrollments()
    {
        var rng = new Random(5);
        long target = (long)Math.Round(1_000_000 * _scale);
        int[] enrollable = Enumerable.Range(0, _model.Courses.Count).Where(i => _model.Courses[i].Enrollable).ToArray();
        int[] order = enrollable.ToArray();
        rng.Shuffle(order);
        double[] cdf = Cdf(order.Select((_, rank) => 1.0 / Math.Pow(rank + 1, 0.7)).ToArray());

        double average = (double)target / _model.Students.Count;
        int maxPerStudent = Math.Max(1, Math.Min(order.Length / 2, (int)Math.Round(2 * average - 1)));
        var list = new List<EnrollmentRec>((int)(target * 1.05));
        Span<int> picked = stackalloc int[maxPerStudent];

        foreach (int student in _model.Students)
        {
            int k = 1 + rng.Next(maxPerStudent);
            int n = 0;
            for (int attempt = 0; n < k && attempt < k * 20; attempt++)
            {
                int course = order[Sample(rng, cdf)];
                if (!picked[..n].Contains(course))
                {
                    picked[n++] = course;
                }
            }

            for (int j = 0; j < n; j++)
            {
                CourseInfo course = _model.Courses[picked[j]];
                long lo = Math.Max(_model.UserCreatedAt[student], course.PublishedAt);
                long hi = SeedTime.End - SeedTime.Day;
                if (lo > hi)
                {
                    lo = hi - SeedTime.Day;
                }

                long enrolled = SeedTime.Between(rng, lo, hi);
                double s = rng.NextDouble();
                EnrollmentStatus status = s < 0.65 ? EnrollmentStatus.Active :
                    s < 0.90 ? EnrollmentStatus.Completed : EnrollmentStatus.Dropped;
                long completed = status == EnrollmentStatus.Completed
                    ? Math.Min(SeedTime.End, enrolled + SeedTime.Day + rng.NextInt64(150 * SeedTime.Day))
                    : 0;
                list.Add(new EnrollmentRec
                {
                    Id = NewGuid(rng),
                    User = student,
                    Course = picked[j],
                    Status = status,
                    EnrolledAt = enrolled,
                    CompletedAt = completed,
                });
            }
        }

        // rows arrive in time order, like a real system would have written them
        EnrollmentRec[] enrollments = list.ToArray();
        Array.Sort(enrollments,
            static (a, b) =>
                a.EnrolledAt != b.EnrolledAt ? a.EnrolledAt.CompareTo(b.EnrolledAt) : a.Id.CompareTo(b.Id));
        _model.Enrollments = enrollments;

        using NpgsqlBinaryImporter w = Copy("enrollments",
            "id, course_id, user_id, status, enrolled_at, completed_at, created_at, updated_at");
        foreach (ref readonly EnrollmentRec e in enrollments.AsSpan())
        {
            w.StartRow();
            Id(w, e.Id);
            Id(w, _model.Courses[e.Course].Id);
            Id(w, _model.UserIds[e.User]);
            Text(w, e.Status switch
            {
                EnrollmentStatus.Completed => "completed",
                EnrollmentStatus.Dropped => "dropped",
                _ => "active",
            });
            Ts(w, e.EnrolledAt);
            TsOrNull(w, e.CompletedAt);
            Ts(w, e.EnrolledAt);
            Ts(w, e.CompletedAt > 0 ? e.CompletedAt : e.EnrolledAt);
        }

        Done("enrollments", w);
    }

    /// <summary>Time window in which an enrollment produces activity.</summary>
    private static (long From, long To) Window(in EnrollmentRec e) =>
        e.Status == EnrollmentStatus.Completed
            ? (e.EnrolledAt, e.CompletedAt)
            : (e.EnrolledAt,
                Math.Min(SeedTime.End,
                    e.EnrolledAt + (e.Status == EnrollmentStatus.Dropped ? 45 : 200) * SeedTime.Day));

    // ------------------------------------------------------------------ lesson_progress

    private void GenerateLessonProgress()
    {
        var rng = new Random(6);
        int maxLessons = _model.Courses.Max(c => c.LessonCount);
        Span<long> times = stackalloc long[maxLessons];
        Span<int> lessons = stackalloc int[maxLessons];
        using NpgsqlBinaryImporter w = Copy("lesson_progress", "enrollment_id, lesson_id, completed_at");

        foreach (ref readonly EnrollmentRec e in _model.Enrollments.AsSpan())
        {
            CourseInfo course = _model.Courses[e.Course];
            (long from, long to) = Window(e);

            if (e.Status == EnrollmentStatus.Completed)
            {
                // every live lesson; lessons deleted later were completed by about half of the students
                int n = 0;
                for (int l = course.LessonStart; l < course.LessonStart + course.LessonCount; l++)
                {
                    if (!_model.LessonDeleted[l] || rng.NextDouble() < 0.5)
                    {
                        lessons[n++] = l;
                    }
                }

                if (n == 0)
                {
                    continue;
                }

                Span<long> t = times[..n];
                SeedTime.SortedBetween(rng, from, to, t);
                t[^1] = to; // finishing the last lesson completes the course
                for (int i = 0; i < n; i++)
                {
                    Write(w, e.Id, lessons[i], t[i]);
                }
            }
            else
            {
                double fraction = rng.NextDouble() * (e.Status == EnrollmentStatus.Active ? 0.25 : 0.2);
                int n = Math.Min(course.LessonCount - 1, (int)Math.Round(fraction * course.LessonCount));
                if (n <= 0)
                {
                    continue;
                }

                Span<long> t = times[..n];
                SeedTime.SortedBetween(rng, from, to, t);
                for (int i = 0; i < n; i++)
                {
                    Write(w, e.Id, course.LessonStart + i, t[i]);
                }
            }
        }

        Done("lesson_progress", w);

        void Write(NpgsqlBinaryImporter importer, Guid enrollment, int lesson, long at)
        {
            importer.StartRow();
            Id(importer, enrollment);
            Id(importer, _model.LessonIds[lesson]);
            Ts(importer, at);
        }
    }

    // ------------------------------------------------------------------ quiz_attempts

    private void GenerateQuizAttempts()
    {
        var rng = new Random(7);
        Span<long> times = stackalloc long[8];
        using NpgsqlBinaryImporter w = Copy("quiz_attempts",
            "id, quiz_id, enrollment_id, score_percent, passed, attempted_at");

        foreach (ref readonly EnrollmentRec e in _model.Enrollments.AsSpan())
        {
            CourseInfo course = _model.Courses[e.Course];
            (long from, long to) = Window(e);
            double probability = e.Status switch
            {
                EnrollmentStatus.Completed => 0.75,
                EnrollmentStatus.Active => 0.15,
                _ => 0.08,
            };
            double ability = 70 + 15 * Gaussian(rng);

            foreach (QuizInfo quiz in course.Quizzes)
            {
                if (rng.NextDouble() >= probability)
                {
                    continue;
                }

                Span<long> t = times[..quiz.MaxAttempts];
                SeedTime.SortedBetween(rng, from, to, t);
                for (int attempt = 0; attempt < quiz.MaxAttempts; attempt++)
                {
                    int hundredths = (int)Math.Clamp(Math.Round((ability + 12 * Gaussian(rng) + 4 * attempt) * 100), 0,
                        10_000);
                    decimal score = hundredths / 100m;
                    bool passed = score >= quiz.PassPercent;

                    w.StartRow();
                    Id(w, NewGuid(rng));
                    Id(w, quiz.Id);
                    Id(w, e.Id);
                    w.Write(score, NpgsqlDbType.Numeric);
                    w.Write(passed);
                    Ts(w, t[attempt]);
                    if (passed)
                    {
                        break;
                    }
                }
            }
        }

        Done("quiz_attempts", w);
    }

    private void GenerateSubmissions()
    {
        var rng = new Random(8);
        using NpgsqlBinaryImporter w = Copy("submissions",
            "id, assignment_id, enrollment_id, content, score, feedback, graded_by, graded_at, submitted_at, created_at, updated_at");

        foreach (ref readonly EnrollmentRec e in _model.Enrollments.AsSpan())
        {
            CourseInfo course = _model.Courses[e.Course];
            double probability = e.Status switch
            {
                EnrollmentStatus.Completed => 0.10,
                EnrollmentStatus.Active => 0.013,
                _ => 0.01,
            };
            (long from, long to) = Window(e);

            foreach (AssignmentInfo assignment in course.Assignments)
            {
                if (rng.NextDouble() >= probability)
                {
                    continue;
                }

                long submitted = SeedTime.Between(rng, from, to);
                bool graded = rng.NextDouble() < 0.8;
                long gradedAt = graded ? SeedTime.Between(rng, submitted + 3600, submitted + 7 * SeedTime.Day) : 0;

                w.StartRow();
                Id(w, NewGuid(rng));
                Id(w, assignment.Id);
                Id(w, e.Id);
                Text(w, _paragraphs[rng.Next(_paragraphs.Length)]);
                if (graded)
                {
                    w.Write(rng.Next(assignment.MaxScore * 4 / 10, assignment.MaxScore + 1), NpgsqlDbType.Integer);
                    Text(w, rng.NextDouble() < 0.5 ? _sentences[rng.Next(_sentences.Length)] : null);
                    Id(w, _model.UserIds[course.LeadInstructor]);
                    Ts(w, gradedAt);
                }
                else
                {
                    w.WriteNull();
                    w.WriteNull();
                    w.WriteNull();
                    w.WriteNull();
                }

                Ts(w, submitted);
                Ts(w, submitted);
                Ts(w, graded ? gradedAt : submitted);
            }
        }

        Done("submissions", w);
    }

    private void GenerateCertificates()
    {
        var rng = new Random(9);
        ulong counter = 0;
        using NpgsqlBinaryImporter w = Copy("certificates", "id, enrollment_id, code, issued_at");

        foreach (ref readonly EnrollmentRec e in _model.Enrollments.AsSpan())
        {
            if (e.Status != EnrollmentStatus.Completed)
            {
                continue;
            }

            counter++;
            ulong code = (counter * 0x9E3779B97F4A7C15UL) & 0xFFFF_FFFF_FFFFUL;

            w.StartRow();
            Id(w, NewGuid(rng));
            Id(w, e.Id);
            Text(w, string.Create(CultureInfo.InvariantCulture, $"CERT-{code:X12}"));
            Ts(w, e.CompletedAt);
        }

        Done("certificates", w);
    }

    private void GenerateReviews()
    {
        var rng = new Random(10);
        double[] happy = Cdf([3, 5, 12, 35, 45]);
        double[] neutral = Cdf([8, 10, 20, 32, 30]);
        using NpgsqlBinaryImporter w = Copy("reviews",
            "id, course_id, user_id, rating, comment, created_at, updated_at, is_deleted, deleted_at, deleted_by");

        foreach (ref readonly EnrollmentRec e in _model.Enrollments.AsSpan())
        {
            double probability = e.Status switch
            {
                EnrollmentStatus.Completed => 0.6,
                EnrollmentStatus.Active => 0.2,
                _ => 0.25,
            };
            if (rng.NextDouble() >= probability)
            {
                continue;
            }

            long created = e.Status == EnrollmentStatus.Completed
                ? SeedTime.Between(rng, e.CompletedAt, e.CompletedAt + 30 * SeedTime.Day)
                : SeedTime.Between(rng, e.EnrolledAt + 3600, SeedTime.End);
            int rating = 1 + Sample(rng, e.Status == EnrollmentStatus.Completed ? happy : neutral);
            bool deleted = rng.NextDouble() < 0.05;
            long deletedAt = deleted ? SeedTime.Between(rng, created + 3600, SeedTime.End) : 0;
            Guid user = _model.UserIds[e.User];

            w.StartRow();
            Id(w, NewGuid(rng));
            Id(w, _model.Courses[e.Course].Id);
            Id(w, user);
            w.Write((short)rating, NpgsqlDbType.Smallint);
            Text(w, rng.NextDouble() < 0.75 ? _reviewComments[rng.Next(_reviewComments.Length)] : null);
            Ts(w, created);
            Ts(w, deleted ? deletedAt : created);
            w.Write(deleted);
            TsOrNull(w, deletedAt);
            if (deleted)
            {
                Id(w, user);
            }
            else
            {
                w.WriteNull();
            }
        }

        Done("reviews", w);
    }

    private static double[] Cdf(double[] weights)
    {
        double[] cdf = new double[weights.Length];
        double sum = 0;
        for (int i = 0; i < weights.Length; i++)
        {
            sum += weights[i];
            cdf[i] = sum;
        }

        for (int i = 0; i < cdf.Length; i++)
        {
            cdf[i] /= sum;
        }

        return cdf;
    }

    private static int Sample(Random rng, double[] cdf)
    {
        int index = Array.BinarySearch(cdf, rng.NextDouble());
        return Math.Min(index < 0 ? ~index : index, cdf.Length - 1);
    }

    private static double Gaussian(Random rng)
    {
        double u1 = 1.0 - rng.NextDouble();
        double u2 = rng.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }
}
