-- 008_report_indexes.sql: B-tree indexes backing the analytical reports (lab 2), no materialized views

-- monthly activity / top courses: range scans over the event timestamps
CREATE INDEX IF NOT EXISTS ix_lesson_progress_completed_at ON lesson_progress (completed_at) INCLUDE (enrollment_id);
CREATE INDEX IF NOT EXISTS ix_quiz_attempts_attempted_at   ON quiz_attempts (attempted_at) INCLUDE (enrollment_id, score_percent, passed);

-- transcript: enrollments of a student, progress per enrollment
CREATE INDEX IF NOT EXISTS ix_enrollments_user             ON enrollments (user_id);
CREATE INDEX IF NOT EXISTS ix_lesson_progress_enrollment   ON lesson_progress (enrollment_id);

-- dashboard: latest reviews, lead instructor, curriculum
CREATE INDEX IF NOT EXISTS ix_reviews_course_created       ON reviews (course_id, created_at DESC) WHERE NOT is_deleted;
CREATE INDEX IF NOT EXISTS ix_course_instructors_lead      ON course_instructors (course_id) WHERE is_lead;
CREATE INDEX IF NOT EXISTS ix_modules_course               ON modules (course_id);
