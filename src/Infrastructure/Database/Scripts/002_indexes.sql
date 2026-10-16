-- 002_indexes.sql: B-tree, partial/unique partial, GIN, BRIN, hash

-- unique partial B-tree: soft-deleted rows do not block reuse of keys
CREATE UNIQUE INDEX ux_users_email_active      ON users (lower(email))       WHERE NOT is_deleted;
CREATE UNIQUE INDEX ux_courses_slug_active     ON courses (slug)             WHERE NOT is_deleted;
CREATE UNIQUE INDEX ux_categories_slug_active  ON categories (slug)          WHERE NOT is_deleted;
CREATE UNIQUE INDEX ux_reviews_user_course     ON reviews (user_id, course_id) WHERE NOT is_deleted;
CREATE UNIQUE INDEX ux_enrollments_active      ON enrollments (user_id, course_id) WHERE status <> 'dropped';
CREATE INDEX ix_modules_position            ON modules (course_id, position);

-- composite B-tree
CREATE INDEX ix_courses_category_status ON courses (category_id, status) WHERE NOT is_deleted;
CREATE INDEX ix_lessons_module_position ON lessons (module_id, position) WHERE NOT is_deleted;
CREATE INDEX ix_enrollments_course      ON enrollments (course_id, status);
CREATE INDEX ix_submissions_ungraded    ON submissions (assignment_id) WHERE graded_at IS NULL;
CREATE INDEX ix_quiz_attempts_lookup    ON quiz_attempts (enrollment_id, quiz_id);
CREATE INDEX ix_audit_log_record        ON audit_log (table_name, record_id);

-- GIN: full text search over courses
CREATE INDEX ix_courses_fts ON courses
    USING gin (to_tsvector('simple', title || ' ' || description));

-- GIN: jsonb quiz options
CREATE INDEX ix_questions_options ON quiz_questions USING gin (options);

-- BRIN: append-only time series
CREATE INDEX ix_audit_log_created_brin ON audit_log USING brin (created_at);

-- hash: equality-only lookup of certificate codes
CREATE INDEX ix_certificates_code_hash ON certificates USING hash (code);
