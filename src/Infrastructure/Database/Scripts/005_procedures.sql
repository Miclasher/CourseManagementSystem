-- 005_procedures.sql: stored procedures (all writes go through these)
-- Business errors use custom SQLSTATEs: CM400 problem, CM404 not found, CM409 conflict.
-- HINT carries the error code (e.g. Enrollments.Full) that the application maps to Error.

CREATE OR REPLACE PROCEDURE sp_register_user(
    p_id uuid, p_email varchar, p_password_hash varchar,
    p_first_name varchar, p_last_name varchar, p_role_id smallint, p_actor_id uuid)
LANGUAGE plpgsql AS $$
BEGIN
    PERFORM set_config('app.actor_id', coalesce(p_actor_id, p_id)::text, true);
    IF EXISTS (SELECT 1 FROM users WHERE lower(email) = lower(p_email) AND NOT is_deleted) THEN
        RAISE EXCEPTION 'Email is already in use' USING ERRCODE = 'CM409', HINT = 'Users.EmailNotUnique';
    END IF;
    INSERT INTO users (id, email, password_hash, first_name, last_name, updated_by)
    VALUES (p_id, p_email, p_password_hash, p_first_name, p_last_name, NULL);
    INSERT INTO user_roles (user_id, role_id) VALUES (p_id, p_role_id);
END $$;

CREATE OR REPLACE PROCEDURE sp_deactivate_user(p_user_id uuid, p_actor_id uuid)
LANGUAGE plpgsql AS $$
BEGIN
    PERFORM set_config('app.actor_id', p_actor_id::text, true);
    UPDATE users SET is_deleted = true, deleted_at = now(), deleted_by = p_actor_id, updated_by = p_actor_id
    WHERE id = p_user_id AND NOT is_deleted;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'User not found' USING ERRCODE = 'CM404', HINT = 'Users.NotFound';
    END IF;
END $$;

CREATE OR REPLACE PROCEDURE sp_create_course(
    p_id uuid, p_category_id uuid, p_title varchar, p_slug varchar,
    p_description text, p_level varchar, p_price numeric, p_max_students integer, p_actor_id uuid)
LANGUAGE plpgsql AS $$
BEGIN
    PERFORM set_config('app.actor_id', p_actor_id::text, true);
    IF NOT EXISTS (SELECT 1 FROM categories WHERE id = p_category_id AND NOT is_deleted) THEN
        RAISE EXCEPTION 'Category not found' USING ERRCODE = 'CM404', HINT = 'Categories.NotFound';
    END IF;
    IF EXISTS (SELECT 1 FROM courses WHERE slug = p_slug AND NOT is_deleted) THEN
        RAISE EXCEPTION 'Slug is already in use' USING ERRCODE = 'CM409', HINT = 'Courses.SlugNotUnique';
    END IF;
    INSERT INTO courses (id, category_id, title, slug, description, level, price, max_students, updated_by)
    VALUES (p_id, p_category_id, p_title, p_slug, p_description, p_level, p_price, p_max_students, p_actor_id);
    INSERT INTO course_instructors (course_id, user_id, is_lead) VALUES (p_id, p_actor_id, true);
END $$;

CREATE OR REPLACE PROCEDURE sp_update_course(
    p_id uuid, p_category_id uuid, p_title varchar, p_description text,
    p_level varchar, p_price numeric, p_max_students integer, p_actor_id uuid)
LANGUAGE plpgsql AS $$
BEGIN
    PERFORM set_config('app.actor_id', p_actor_id::text, true);
    UPDATE courses
    SET category_id = p_category_id, title = p_title, description = p_description,
        level = p_level, price = p_price, max_students = p_max_students, updated_by = p_actor_id
    WHERE id = p_id AND NOT is_deleted;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Course not found' USING ERRCODE = 'CM404', HINT = 'Courses.NotFound';
    END IF;
END $$;

CREATE OR REPLACE PROCEDURE sp_publish_course(p_id uuid, p_actor_id uuid)
LANGUAGE plpgsql AS $$
BEGIN
    PERFORM set_config('app.actor_id', p_actor_id::text, true);
    IF NOT EXISTS (SELECT 1 FROM courses WHERE id = p_id AND NOT is_deleted) THEN
        RAISE EXCEPTION 'Course not found' USING ERRCODE = 'CM404', HINT = 'Courses.NotFound';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM modules m JOIN lessons l ON l.module_id = m.id AND NOT l.is_deleted
                   WHERE m.course_id = p_id) THEN
        RAISE EXCEPTION 'A course needs at least one lesson to be published'
            USING ERRCODE = 'CM400', HINT = 'Courses.NoLessons';
    END IF;
    UPDATE courses SET status = 'published', published_at = coalesce(published_at, now()), updated_by = p_actor_id
    WHERE id = p_id;
END $$;

-- Cascades to the lessons of the course; the lessons share the course's deleted_at so restore can undo exactly this.
CREATE OR REPLACE PROCEDURE sp_soft_delete_course(p_id uuid, p_actor_id uuid)
LANGUAGE plpgsql AS $$
DECLARE v_now timestamptz := now();
BEGIN
    PERFORM set_config('app.actor_id', p_actor_id::text, true);
    UPDATE courses SET is_deleted = true, deleted_at = v_now, deleted_by = p_actor_id, updated_by = p_actor_id
    WHERE id = p_id AND NOT is_deleted;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Course not found' USING ERRCODE = 'CM404', HINT = 'Courses.NotFound';
    END IF;
    UPDATE lessons SET is_deleted = true, deleted_at = v_now, deleted_by = p_actor_id, updated_by = p_actor_id
    WHERE NOT is_deleted AND module_id IN (SELECT id FROM modules WHERE course_id = p_id);
END $$;

CREATE OR REPLACE PROCEDURE sp_restore_course(p_id uuid, p_actor_id uuid)
LANGUAGE plpgsql AS $$
DECLARE v_deleted_at timestamptz;
BEGIN
    PERFORM set_config('app.actor_id', p_actor_id::text, true);
    SELECT deleted_at INTO v_deleted_at FROM courses WHERE id = p_id AND is_deleted;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Deleted course not found' USING ERRCODE = 'CM404', HINT = 'Courses.NotFound';
    END IF;
    IF EXISTS (SELECT 1 FROM courses c JOIN courses d ON d.slug = c.slug
               WHERE d.id = p_id AND NOT c.is_deleted) THEN
        RAISE EXCEPTION 'Slug is already in use' USING ERRCODE = 'CM409', HINT = 'Courses.SlugNotUnique';
    END IF;
    UPDATE courses SET is_deleted = false, deleted_at = NULL, deleted_by = NULL, updated_by = p_actor_id
    WHERE id = p_id;
    UPDATE lessons SET is_deleted = false, deleted_at = NULL, deleted_by = NULL, updated_by = p_actor_id
    WHERE deleted_at = v_deleted_at AND module_id IN (SELECT id FROM modules WHERE course_id = p_id);
END $$;

CREATE OR REPLACE PROCEDURE sp_add_module(p_id uuid, p_course_id uuid, p_title varchar, p_actor_id uuid)
LANGUAGE plpgsql AS $$
BEGIN
    PERFORM set_config('app.actor_id', p_actor_id::text, true);
    IF NOT EXISTS (SELECT 1 FROM courses WHERE id = p_course_id AND NOT is_deleted) THEN
        RAISE EXCEPTION 'Course not found' USING ERRCODE = 'CM404', HINT = 'Courses.NotFound';
    END IF;
    INSERT INTO modules (id, course_id, title, position, updated_by)
    VALUES (p_id, p_course_id, p_title,
            coalesce((SELECT max(position) FROM modules WHERE course_id = p_course_id), 0) + 1, p_actor_id);
END $$;

CREATE OR REPLACE PROCEDURE sp_add_lesson(
    p_id uuid, p_module_id uuid, p_title varchar, p_content text, p_duration integer, p_actor_id uuid)
LANGUAGE plpgsql AS $$
BEGIN
    PERFORM set_config('app.actor_id', p_actor_id::text, true);
    IF NOT EXISTS (SELECT 1 FROM modules WHERE id = p_module_id) THEN
        RAISE EXCEPTION 'Module not found' USING ERRCODE = 'CM404', HINT = 'Modules.NotFound';
    END IF;
    INSERT INTO lessons (id, module_id, title, content, duration_minutes, position, updated_by)
    VALUES (p_id, p_module_id, p_title, p_content, p_duration,
            coalesce((SELECT max(position) FROM lessons WHERE module_id = p_module_id AND NOT is_deleted), 0) + 1,
            p_actor_id);
END $$;

CREATE OR REPLACE PROCEDURE sp_move_lesson(p_lesson_id uuid, p_new_position integer, p_actor_id uuid)
LANGUAGE plpgsql AS $$
DECLARE v_module uuid; v_old integer;
BEGIN
    PERFORM set_config('app.actor_id', p_actor_id::text, true);
    SELECT module_id, position INTO v_module, v_old FROM lessons WHERE id = p_lesson_id AND NOT is_deleted;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Lesson not found' USING ERRCODE = 'CM404', HINT = 'Lessons.NotFound';
    END IF;
    IF p_new_position < v_old THEN
        UPDATE lessons SET position = position + 1, updated_by = p_actor_id
        WHERE module_id = v_module AND NOT is_deleted AND position >= p_new_position AND position < v_old;
    ELSIF p_new_position > v_old THEN
        UPDATE lessons SET position = position - 1, updated_by = p_actor_id
        WHERE module_id = v_module AND NOT is_deleted AND position <= p_new_position AND position > v_old;
    END IF;
    UPDATE lessons SET position = p_new_position, updated_by = p_actor_id WHERE id = p_lesson_id;
END $$;

CREATE OR REPLACE PROCEDURE sp_enroll_student(p_id uuid, p_course_id uuid, p_user_id uuid, p_actor_id uuid)
LANGUAGE plpgsql AS $$
DECLARE v_status varchar; v_max integer; v_enrolled integer;
BEGIN
    PERFORM set_config('app.actor_id', p_actor_id::text, true);
    SELECT status, max_students, enrolled_count INTO v_status, v_max, v_enrolled
    FROM courses WHERE id = p_course_id AND NOT is_deleted FOR UPDATE;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Course not found' USING ERRCODE = 'CM404', HINT = 'Courses.NotFound';
    END IF;
    IF v_status <> 'published' THEN
        RAISE EXCEPTION 'Course is not open for enrollment' USING ERRCODE = 'CM400', HINT = 'Enrollments.CourseNotPublished';
    END IF;
    IF v_max IS NOT NULL AND v_enrolled >= v_max THEN
        RAISE EXCEPTION 'Course is full' USING ERRCODE = 'CM409', HINT = 'Enrollments.CourseFull';
    END IF;
    IF EXISTS (SELECT 1 FROM enrollments WHERE course_id = p_course_id AND user_id = p_user_id AND status <> 'dropped') THEN
        RAISE EXCEPTION 'Already enrolled' USING ERRCODE = 'CM409', HINT = 'Enrollments.AlreadyEnrolled';
    END IF;
    INSERT INTO enrollments (id, course_id, user_id, updated_by) VALUES (p_id, p_course_id, p_user_id, p_actor_id);
END $$;

CREATE OR REPLACE PROCEDURE sp_drop_enrollment(p_id uuid, p_actor_id uuid)
LANGUAGE plpgsql AS $$
BEGIN
    PERFORM set_config('app.actor_id', p_actor_id::text, true);
    UPDATE enrollments SET status = 'dropped', updated_by = p_actor_id WHERE id = p_id AND status = 'active';
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Active enrollment not found' USING ERRCODE = 'CM404', HINT = 'Enrollments.NotFound';
    END IF;
END $$;

CREATE OR REPLACE PROCEDURE sp_complete_lesson(p_enrollment_id uuid, p_lesson_id uuid, p_actor_id uuid)
LANGUAGE plpgsql AS $$
BEGIN
    PERFORM set_config('app.actor_id', p_actor_id::text, true);
    IF NOT EXISTS (
        SELECT 1 FROM enrollments e
        JOIN modules m ON m.course_id = e.course_id
        JOIN lessons l ON l.module_id = m.id AND NOT l.is_deleted
        WHERE e.id = p_enrollment_id AND e.status = 'active' AND l.id = p_lesson_id) THEN
        RAISE EXCEPTION 'Lesson is not part of an active enrollment' USING ERRCODE = 'CM404', HINT = 'Enrollments.LessonNotAvailable';
    END IF;
    INSERT INTO lesson_progress (enrollment_id, lesson_id) VALUES (p_enrollment_id, p_lesson_id)
    ON CONFLICT DO NOTHING;
END $$;

CREATE OR REPLACE PROCEDURE sp_create_assignment(
    p_id uuid, p_lesson_id uuid, p_title varchar, p_description text, p_max_score integer,
    p_due_at timestamptz, p_actor_id uuid)
LANGUAGE plpgsql AS $$
BEGIN
    PERFORM set_config('app.actor_id', p_actor_id::text, true);
    IF NOT EXISTS (SELECT 1 FROM lessons WHERE id = p_lesson_id AND NOT is_deleted) THEN
        RAISE EXCEPTION 'Lesson not found' USING ERRCODE = 'CM404', HINT = 'Lessons.NotFound';
    END IF;
    INSERT INTO assignments (id, lesson_id, title, description, max_score, due_at, updated_by)
    VALUES (p_id, p_lesson_id, p_title, p_description, p_max_score, p_due_at, p_actor_id);
END $$;

CREATE OR REPLACE PROCEDURE sp_submit_assignment(
    p_id uuid, p_assignment_id uuid, p_enrollment_id uuid, p_content text, p_actor_id uuid)
LANGUAGE plpgsql AS $$
DECLARE v_due timestamptz;
BEGIN
    PERFORM set_config('app.actor_id', p_actor_id::text, true);
    SELECT due_at INTO v_due FROM assignments WHERE id = p_assignment_id;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Assignment not found' USING ERRCODE = 'CM404', HINT = 'Assignments.NotFound';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM enrollments WHERE id = p_enrollment_id AND status <> 'dropped') THEN
        RAISE EXCEPTION 'Enrollment not found' USING ERRCODE = 'CM404', HINT = 'Enrollments.NotFound';
    END IF;
    IF v_due IS NOT NULL AND v_due < now() THEN
        RAISE EXCEPTION 'The deadline has passed' USING ERRCODE = 'CM400', HINT = 'Assignments.DeadlinePassed';
    END IF;
    INSERT INTO submissions (id, assignment_id, enrollment_id, content, updated_by)
    VALUES (p_id, p_assignment_id, p_enrollment_id, p_content, p_actor_id)
    ON CONFLICT (assignment_id, enrollment_id) DO UPDATE
        SET content = EXCLUDED.content, submitted_at = now(), score = NULL, feedback = NULL,
            graded_by = NULL, graded_at = NULL, updated_by = p_actor_id;
END $$;

CREATE OR REPLACE PROCEDURE sp_grade_submission(
    p_submission_id uuid, p_score integer, p_feedback text, p_actor_id uuid)
LANGUAGE plpgsql AS $$
DECLARE v_max integer;
BEGIN
    PERFORM set_config('app.actor_id', p_actor_id::text, true);
    SELECT a.max_score INTO v_max FROM submissions s JOIN assignments a ON a.id = s.assignment_id
    WHERE s.id = p_submission_id;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Submission not found' USING ERRCODE = 'CM404', HINT = 'Submissions.NotFound';
    END IF;
    IF p_score < 0 OR p_score > v_max THEN
        RAISE EXCEPTION 'Score must be between 0 and %', v_max USING ERRCODE = 'CM400', HINT = 'Submissions.ScoreOutOfRange';
    END IF;
    UPDATE submissions
    SET score = p_score, feedback = p_feedback, graded_by = p_actor_id, graded_at = now(), updated_by = p_actor_id
    WHERE id = p_submission_id;
END $$;

-- p_answers: jsonb array of selected option indexes, in question order of insertion (by id)
CREATE OR REPLACE PROCEDURE sp_submit_quiz_attempt(
    p_id uuid, p_quiz_id uuid, p_enrollment_id uuid, p_answers jsonb, p_actor_id uuid)
LANGUAGE plpgsql AS $$
DECLARE v_pass integer; v_max integer; v_used integer; v_total integer; v_earned integer; v_pct numeric;
BEGIN
    PERFORM set_config('app.actor_id', p_actor_id::text, true);
    SELECT pass_percent, max_attempts INTO v_pass, v_max FROM quizzes WHERE id = p_quiz_id;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Quiz not found' USING ERRCODE = 'CM404', HINT = 'Quizzes.NotFound';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM enrollments WHERE id = p_enrollment_id AND status <> 'dropped') THEN
        RAISE EXCEPTION 'Active enrollment not found' USING ERRCODE = 'CM404', HINT = 'Enrollments.NotFound';
    END IF;
    SELECT count(*) INTO v_used FROM quiz_attempts WHERE quiz_id = p_quiz_id AND enrollment_id = p_enrollment_id;
    IF v_used >= v_max THEN
        RAISE EXCEPTION 'No attempts left' USING ERRCODE = 'CM409', HINT = 'Quizzes.NoAttemptsLeft';
    END IF;
    SELECT coalesce(sum(q.points), 0),
           coalesce(sum(q.points) FILTER (WHERE (p_answers ->> (q.rn - 1)::int)::int = q.correct_option), 0)
    INTO v_total, v_earned
    FROM (SELECT points, correct_option, row_number() OVER (ORDER BY id) AS rn
          FROM quiz_questions WHERE quiz_id = p_quiz_id) q;
    v_pct := CASE WHEN v_total = 0 THEN 0 ELSE round(100.0 * v_earned / v_total, 2) END;
    INSERT INTO quiz_attempts (id, quiz_id, enrollment_id, score_percent, passed)
    VALUES (p_id, p_quiz_id, p_enrollment_id, v_pct, v_pct >= v_pass);
END $$;

CREATE OR REPLACE PROCEDURE sp_add_review(
    p_id uuid, p_course_id uuid, p_user_id uuid, p_rating smallint, p_comment text, p_actor_id uuid)
LANGUAGE plpgsql AS $$
BEGIN
    PERFORM set_config('app.actor_id', p_actor_id::text, true);
    IF NOT EXISTS (SELECT 1 FROM enrollments WHERE course_id = p_course_id AND user_id = p_user_id AND status <> 'dropped') THEN
        RAISE EXCEPTION 'Only enrolled students can review a course' USING ERRCODE = 'CM400', HINT = 'Reviews.NotEnrolled';
    END IF;
    IF EXISTS (SELECT 1 FROM reviews WHERE course_id = p_course_id AND user_id = p_user_id AND NOT is_deleted) THEN
        RAISE EXCEPTION 'Course already reviewed' USING ERRCODE = 'CM409', HINT = 'Reviews.AlreadyReviewed';
    END IF;
    INSERT INTO reviews (id, course_id, user_id, rating, comment, updated_by)
    VALUES (p_id, p_course_id, p_user_id, p_rating, p_comment, p_actor_id);
END $$;

-- p_questions: jsonb array of {"text": "...", "options": ["..."], "correctOption": 0, "points": 1}
CREATE OR REPLACE PROCEDURE sp_create_quiz(
    p_id uuid, p_lesson_id uuid, p_title varchar, p_pass_percent integer, p_max_attempts integer,
    p_questions jsonb, p_actor_id uuid)
LANGUAGE plpgsql AS $$
BEGIN
    PERFORM set_config('app.actor_id', p_actor_id::text, true);
    IF NOT EXISTS (SELECT 1 FROM lessons WHERE id = p_lesson_id AND NOT is_deleted) THEN
        RAISE EXCEPTION 'Lesson not found' USING ERRCODE = 'CM404', HINT = 'Lessons.NotFound';
    END IF;
    INSERT INTO quizzes (id, lesson_id, title, pass_percent, max_attempts)
    VALUES (p_id, p_lesson_id, p_title, p_pass_percent, p_max_attempts);
    INSERT INTO quiz_questions (id, quiz_id, text, options, correct_option, points)
    SELECT gen_random_uuid(), p_id, q ->> 'text', q -> 'options',
           (q ->> 'correctOption')::int, coalesce((q ->> 'points')::int, 1)
    FROM jsonb_array_elements(p_questions) AS q;
END $$;
