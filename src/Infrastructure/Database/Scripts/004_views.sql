-- 004_views.sql: read models used by the application

CREATE OR REPLACE VIEW v_active_users AS
SELECT u.id, u.email, u.first_name, u.last_name, u.bio, u.created_at,
       coalesce((SELECT array_agg(r.name ORDER BY r.name)
                 FROM user_roles ur JOIN roles r ON r.id = ur.role_id
                 WHERE ur.user_id = u.id), '{}') AS roles
FROM users u
WHERE NOT u.is_deleted;

CREATE OR REPLACE VIEW v_active_courses AS
SELECT c.*
FROM courses c
WHERE NOT c.is_deleted;

CREATE OR REPLACE VIEW v_course_catalog AS
SELECT c.id, c.title, c.slug, c.description, c.level, c.price, c.max_students,
       c.rating_avg, c.rating_count, c.enrolled_count, c.published_at,
       cat.id   AS category_id,
       cat.name AS category_name,
       lead.id  AS lead_instructor_id,
       lead.first_name || ' ' || lead.last_name AS lead_instructor_name,
       coalesce(ls.lesson_count, 0)     AS lesson_count,
       coalesce(ls.total_minutes, 0)    AS total_minutes
FROM courses c
JOIN categories cat ON cat.id = c.category_id
LEFT JOIN course_instructors ci ON ci.course_id = c.id AND ci.is_lead
LEFT JOIN users lead ON lead.id = ci.user_id
LEFT JOIN LATERAL (
    SELECT count(l.id) AS lesson_count, sum(l.duration_minutes) AS total_minutes
    FROM modules m JOIN lessons l ON l.module_id = m.id AND NOT l.is_deleted
    WHERE m.course_id = c.id) ls ON true
WHERE NOT c.is_deleted AND c.status = 'published';

CREATE OR REPLACE VIEW v_course_curriculum AS
SELECT c.id AS course_id,
       m.id AS module_id, m.title AS module_title, m.position AS module_position,
       l.id AS lesson_id, l.title AS lesson_title, l.position AS lesson_position,
       l.duration_minutes
FROM courses c
JOIN modules m ON m.course_id = c.id
LEFT JOIN lessons l ON l.module_id = m.id AND NOT l.is_deleted
WHERE NOT c.is_deleted;

CREATE OR REPLACE VIEW v_student_enrollments AS
SELECT e.id AS enrollment_id, e.user_id, e.status, e.enrolled_at, e.completed_at,
       c.id AS course_id, c.title AS course_title, c.slug AS course_slug,
       fn_course_progress(e.id) AS progress_percent,
       cert.code AS certificate_code
FROM enrollments e
JOIN courses c ON c.id = e.course_id
LEFT JOIN certificates cert ON cert.enrollment_id = e.id
WHERE NOT c.is_deleted;

CREATE OR REPLACE VIEW v_course_stats AS
SELECT c.id AS course_id, c.title,
       count(e.id) FILTER (WHERE e.status <> 'dropped')  AS enrolled,
       count(e.id) FILTER (WHERE e.status = 'completed') AS completed,
       CASE WHEN count(e.id) FILTER (WHERE e.status <> 'dropped') = 0 THEN 0
            ELSE round(100.0 * count(e.id) FILTER (WHERE e.status = 'completed')
                       / count(e.id) FILTER (WHERE e.status <> 'dropped'), 2) END AS completion_rate,
       c.rating_avg
FROM courses c
LEFT JOIN enrollments e ON e.course_id = c.id
WHERE NOT c.is_deleted
GROUP BY c.id;

CREATE OR REPLACE VIEW v_pending_submissions AS
SELECT s.id AS submission_id, s.submitted_at, s.content,
       a.id AS assignment_id, a.title AS assignment_title, a.max_score,
       c.id AS course_id, c.title AS course_title,
       u.id AS student_id, u.first_name || ' ' || u.last_name AS student_name
FROM submissions s
JOIN assignments a ON a.id = s.assignment_id
JOIN lessons l ON l.id = a.lesson_id
JOIN modules m ON m.id = l.module_id
JOIN courses c ON c.id = m.course_id
JOIN enrollments e ON e.id = s.enrollment_id
JOIN users u ON u.id = e.user_id
WHERE s.graded_at IS NULL;

CREATE OR REPLACE VIEW v_deleted_courses AS
SELECT c.id, c.category_id, c.title, c.slug, c.description, c.level, c.status, c.price,
       c.max_students, c.enrolled_count, c.published_at, c.deleted_at, c.deleted_by,
       u.first_name || ' ' || u.last_name AS deleted_by_name
FROM courses c
LEFT JOIN users u ON u.id = c.deleted_by
WHERE c.is_deleted;

-- Entity views: write-side repositories load aggregates through these (never from the tables directly)
CREATE OR REPLACE VIEW v_course_instructors AS
SELECT ci.course_id, ci.user_id, ci.is_lead
FROM course_instructors ci
JOIN users u ON u.id = ci.user_id AND NOT u.is_deleted;

CREATE OR REPLACE VIEW v_modules AS
SELECT m.id, m.course_id, m.title, m.position FROM modules m;

CREATE OR REPLACE VIEW v_active_lessons AS
SELECT l.id, l.module_id, m.course_id, l.title, l.content, l.duration_minutes, l.position
FROM lessons l JOIN modules m ON m.id = l.module_id
WHERE NOT l.is_deleted;

CREATE OR REPLACE VIEW v_enrollments AS
SELECT e.id, e.course_id, e.user_id, e.status, e.enrolled_at, e.completed_at FROM enrollments e;

CREATE OR REPLACE VIEW v_assignments AS
SELECT a.id, a.lesson_id, a.title, a.description, a.max_score, a.due_at FROM assignments a;

CREATE OR REPLACE VIEW v_submissions AS
SELECT s.id, s.assignment_id, s.enrollment_id, s.content, s.score, s.feedback,
       s.graded_by, s.graded_at, s.submitted_at
FROM submissions s;

CREATE OR REPLACE VIEW v_quizzes AS
SELECT q.id, q.lesson_id, q.title, q.pass_percent, q.max_attempts FROM quizzes q;

CREATE OR REPLACE VIEW v_quiz_attempts AS
SELECT qa.id, qa.quiz_id, qa.enrollment_id, qa.score_percent, qa.passed, qa.attempted_at FROM quiz_attempts qa;

CREATE OR REPLACE VIEW v_course_reviews AS
SELECT r.id, r.course_id, r.user_id, r.rating, r.comment, r.created_at,
       u.first_name || ' ' || u.last_name AS author_name
FROM reviews r JOIN users u ON u.id = r.user_id
WHERE NOT r.is_deleted;

CREATE OR REPLACE VIEW v_instructor_pending_submissions AS
SELECT ci.user_id AS instructor_id, p.*
FROM v_pending_submissions p
JOIN v_course_instructors ci ON ci.course_id = p.course_id;
