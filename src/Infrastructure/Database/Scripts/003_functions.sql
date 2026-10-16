-- 003_functions.sql: user-defined functions (views depend on them, so they come first)

CREATE OR REPLACE FUNCTION fn_course_progress(p_enrollment_id uuid)
RETURNS numeric
LANGUAGE sql STABLE AS $$
    WITH course_lessons AS (
        SELECT l.id
        FROM enrollments e
        JOIN modules m ON m.course_id = e.course_id
        JOIN lessons l ON l.module_id = m.id AND NOT l.is_deleted
        WHERE e.id = p_enrollment_id
    )
    SELECT CASE WHEN count(cl.id) = 0 THEN 0
                ELSE round(100.0 * count(lp.lesson_id) / count(cl.id), 2) END
    FROM course_lessons cl
    LEFT JOIN lesson_progress lp
           ON lp.lesson_id = cl.id AND lp.enrollment_id = p_enrollment_id;
$$;

CREATE OR REPLACE FUNCTION fn_student_gpa(p_user_id uuid)
RETURNS numeric
LANGUAGE sql STABLE AS $$
    SELECT coalesce(round(avg(100.0 * s.score / a.max_score), 2), 0)
    FROM submissions s
    JOIN assignments a ON a.id = s.assignment_id
    JOIN enrollments e ON e.id = s.enrollment_id
    WHERE e.user_id = p_user_id AND s.score IS NOT NULL;
$$;

CREATE OR REPLACE FUNCTION fn_search_courses(
    p_query       text,
    p_category_id uuid,
    p_limit       integer,
    p_offset      integer)
RETURNS TABLE (
    id             uuid,
    title          varchar,
    slug           varchar,
    level          varchar,
    price          numeric,
    rating_avg     numeric,
    enrolled_count integer,
    category_name  varchar,
    rank           real)
LANGUAGE sql STABLE AS $$
    SELECT c.id, c.title, c.slug, c.level, c.price, c.rating_avg, c.enrolled_count,
           cat.name,
           CASE WHEN coalesce(p_query, '') = '' THEN 0::real
                ELSE ts_rank(to_tsvector('simple', c.title || ' ' || c.description),
                             plainto_tsquery('simple', p_query)) END
    FROM courses c
    JOIN categories cat ON cat.id = c.category_id
    WHERE NOT c.is_deleted
      AND c.status = 'published'
      AND (p_category_id IS NULL OR c.category_id = p_category_id)
      AND (coalesce(p_query, '') = ''
           OR to_tsvector('simple', c.title || ' ' || c.description) @@ plainto_tsquery('simple', p_query))
    ORDER BY 9 DESC, c.enrolled_count DESC, c.title
    LIMIT p_limit OFFSET p_offset;
$$;

CREATE OR REPLACE FUNCTION fn_generate_certificate_code()
RETURNS varchar
LANGUAGE sql VOLATILE AS $$
    SELECT 'CERT-' || upper(substr(md5(random()::text || clock_timestamp()::text), 1, 12));
$$;
