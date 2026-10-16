-- 001_tables.sql: schema for the Learning Course Management System

CREATE TABLE roles (
    id          smallint PRIMARY KEY,
    name        varchar(32) NOT NULL UNIQUE,
    description varchar(255)
);

CREATE TABLE users (
    id            uuid PRIMARY KEY,
    email         varchar(256) NOT NULL,
    password_hash varchar(512) NOT NULL,
    first_name    varchar(100) NOT NULL,
    last_name     varchar(100) NOT NULL,
    bio           text,
    -- audit
    created_at    timestamptz NOT NULL DEFAULT now(),
    updated_at    timestamptz NOT NULL DEFAULT now(),
    updated_by    uuid,
    -- soft delete
    is_deleted    boolean NOT NULL DEFAULT false,
    deleted_at    timestamptz,
    deleted_by    uuid,
    CONSTRAINT ck_users_deleted CHECK (is_deleted = (deleted_at IS NOT NULL))
);

ALTER TABLE users
    ADD CONSTRAINT fk_users_updated_by FOREIGN KEY (updated_by) REFERENCES users (id),
    ADD CONSTRAINT fk_users_deleted_by FOREIGN KEY (deleted_by) REFERENCES users (id);

CREATE TABLE user_roles (
    user_id uuid     NOT NULL REFERENCES users (id),
    role_id smallint NOT NULL REFERENCES roles (id),
    PRIMARY KEY (user_id, role_id)
);

CREATE TABLE categories (
    id          uuid PRIMARY KEY,
    parent_id   uuid REFERENCES categories (id),
    name        varchar(100) NOT NULL,
    slug        varchar(120) NOT NULL,
    created_at  timestamptz NOT NULL DEFAULT now(),
    updated_at  timestamptz NOT NULL DEFAULT now(),
    updated_by  uuid REFERENCES users (id),
    is_deleted  boolean NOT NULL DEFAULT false,
    deleted_at  timestamptz,
    deleted_by  uuid REFERENCES users (id),
    CONSTRAINT ck_categories_deleted CHECK (is_deleted = (deleted_at IS NOT NULL))
);

CREATE TABLE courses (
    id             uuid PRIMARY KEY,
    category_id    uuid NOT NULL REFERENCES categories (id),
    title          varchar(200) NOT NULL,
    slug           varchar(220) NOT NULL,
    description    text NOT NULL DEFAULT '',
    level          varchar(16) NOT NULL DEFAULT 'beginner',
    status         varchar(16) NOT NULL DEFAULT 'draft',
    price          numeric(10, 2) NOT NULL DEFAULT 0,
    max_students   integer,
    published_at   timestamptz,
    rating_avg     numeric(3, 2) NOT NULL DEFAULT 0,
    rating_count   integer NOT NULL DEFAULT 0,
    enrolled_count integer NOT NULL DEFAULT 0,
    created_at     timestamptz NOT NULL DEFAULT now(),
    updated_at     timestamptz NOT NULL DEFAULT now(),
    updated_by     uuid REFERENCES users (id),
    is_deleted     boolean NOT NULL DEFAULT false,
    deleted_at     timestamptz,
    deleted_by     uuid REFERENCES users (id),
    CONSTRAINT ck_courses_level  CHECK (level IN ('beginner', 'intermediate', 'advanced')),
    CONSTRAINT ck_courses_status CHECK (status IN ('draft', 'published', 'archived')),
    CONSTRAINT ck_courses_price  CHECK (price >= 0),
    CONSTRAINT ck_courses_max    CHECK (max_students IS NULL OR max_students > 0),
    CONSTRAINT ck_courses_deleted CHECK (is_deleted = (deleted_at IS NOT NULL))
);

CREATE TABLE course_instructors (
    course_id     uuid NOT NULL REFERENCES courses (id),
    user_id       uuid NOT NULL REFERENCES users (id),
    is_lead       boolean NOT NULL DEFAULT false,
    assigned_at   timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (course_id, user_id)
);

CREATE TABLE modules (
    id         uuid PRIMARY KEY,
    course_id  uuid NOT NULL REFERENCES courses (id),
    title      varchar(200) NOT NULL,
    position   integer NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    updated_by uuid REFERENCES users (id),
    CONSTRAINT ck_modules_position CHECK (position > 0)
);

CREATE TABLE lessons (
    id               uuid PRIMARY KEY,
    module_id        uuid NOT NULL REFERENCES modules (id),
    title            varchar(200) NOT NULL,
    content          text NOT NULL DEFAULT '',
    duration_minutes integer NOT NULL DEFAULT 0,
    position         integer NOT NULL,
    created_at       timestamptz NOT NULL DEFAULT now(),
    updated_at       timestamptz NOT NULL DEFAULT now(),
    updated_by       uuid REFERENCES users (id),
    is_deleted       boolean NOT NULL DEFAULT false,
    deleted_at       timestamptz,
    deleted_by       uuid REFERENCES users (id),
    CONSTRAINT ck_lessons_position CHECK (position > 0),
    CONSTRAINT ck_lessons_duration CHECK (duration_minutes >= 0),
    CONSTRAINT ck_lessons_deleted  CHECK (is_deleted = (deleted_at IS NOT NULL))
);

CREATE TABLE lesson_materials (
    id         uuid PRIMARY KEY,
    lesson_id  uuid NOT NULL REFERENCES lessons (id),
    title      varchar(200) NOT NULL,
    url        varchar(1024) NOT NULL,
    type       varchar(16) NOT NULL DEFAULT 'link',
    created_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_materials_type CHECK (type IN ('link', 'video', 'pdf', 'slides', 'code'))
);

CREATE TABLE enrollments (
    id           uuid PRIMARY KEY,
    course_id    uuid NOT NULL REFERENCES courses (id),
    user_id      uuid NOT NULL REFERENCES users (id),
    status       varchar(16) NOT NULL DEFAULT 'active',
    enrolled_at  timestamptz NOT NULL DEFAULT now(),
    completed_at timestamptz,
    created_at   timestamptz NOT NULL DEFAULT now(),
    updated_at   timestamptz NOT NULL DEFAULT now(),
    updated_by   uuid REFERENCES users (id),
    CONSTRAINT ck_enrollments_status CHECK (status IN ('active', 'completed', 'dropped'))
);

CREATE TABLE lesson_progress (
    enrollment_id uuid NOT NULL REFERENCES enrollments (id),
    lesson_id     uuid NOT NULL REFERENCES lessons (id),
    completed_at  timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (enrollment_id, lesson_id)
);

CREATE TABLE assignments (
    id          uuid PRIMARY KEY,
    lesson_id   uuid NOT NULL REFERENCES lessons (id),
    title       varchar(200) NOT NULL,
    description text NOT NULL DEFAULT '',
    max_score   integer NOT NULL DEFAULT 100,
    due_at      timestamptz,
    created_at  timestamptz NOT NULL DEFAULT now(),
    updated_at  timestamptz NOT NULL DEFAULT now(),
    updated_by  uuid REFERENCES users (id),
    CONSTRAINT ck_assignments_score CHECK (max_score > 0)
);

CREATE TABLE submissions (
    id            uuid PRIMARY KEY,
    assignment_id uuid NOT NULL REFERENCES assignments (id),
    enrollment_id uuid NOT NULL REFERENCES enrollments (id),
    content       text NOT NULL,
    score         integer,
    feedback      text,
    graded_by     uuid REFERENCES users (id),
    graded_at     timestamptz,
    submitted_at  timestamptz NOT NULL DEFAULT now(),
    created_at    timestamptz NOT NULL DEFAULT now(),
    updated_at    timestamptz NOT NULL DEFAULT now(),
    updated_by    uuid REFERENCES users (id),
    CONSTRAINT ck_submissions_score CHECK (score IS NULL OR score >= 0),
    CONSTRAINT uq_submissions UNIQUE (assignment_id, enrollment_id)
);

CREATE TABLE quizzes (
    id            uuid PRIMARY KEY,
    lesson_id     uuid NOT NULL REFERENCES lessons (id),
    title         varchar(200) NOT NULL,
    pass_percent  integer NOT NULL DEFAULT 60,
    max_attempts  integer NOT NULL DEFAULT 3,
    CONSTRAINT ck_quizzes_pass     CHECK (pass_percent BETWEEN 1 AND 100),
    CONSTRAINT ck_quizzes_attempts CHECK (max_attempts > 0)
);

CREATE TABLE quiz_questions (
    id             uuid PRIMARY KEY,
    quiz_id        uuid NOT NULL REFERENCES quizzes (id),
    text           text NOT NULL,
    options        jsonb NOT NULL,
    correct_option integer NOT NULL,
    points         integer NOT NULL DEFAULT 1,
    CONSTRAINT ck_questions_points CHECK (points > 0)
);

CREATE TABLE quiz_attempts (
    id            uuid PRIMARY KEY,
    quiz_id       uuid NOT NULL REFERENCES quizzes (id),
    enrollment_id uuid NOT NULL REFERENCES enrollments (id),
    score_percent numeric(5, 2) NOT NULL,
    passed        boolean NOT NULL,
    attempted_at  timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_attempts_score CHECK (score_percent BETWEEN 0 AND 100)
);

CREATE TABLE certificates (
    id            uuid PRIMARY KEY,
    enrollment_id uuid NOT NULL UNIQUE REFERENCES enrollments (id),
    code          varchar(32) NOT NULL UNIQUE,
    issued_at     timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE reviews (
    id         uuid PRIMARY KEY,
    course_id  uuid NOT NULL REFERENCES courses (id),
    user_id    uuid NOT NULL REFERENCES users (id),
    rating     smallint NOT NULL,
    comment    text,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    updated_by uuid REFERENCES users (id),
    is_deleted boolean NOT NULL DEFAULT false,
    deleted_at timestamptz,
    deleted_by uuid REFERENCES users (id),
    CONSTRAINT ck_reviews_rating  CHECK (rating BETWEEN 1 AND 5),
    CONSTRAINT ck_reviews_deleted CHECK (is_deleted = (deleted_at IS NOT NULL))
);

CREATE TABLE audit_log (
    id         bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    table_name varchar(64) NOT NULL,
    record_id  uuid,
    operation  char(1) NOT NULL,
    old_data   jsonb,
    new_data   jsonb,
    changed_by uuid,
    created_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_audit_op CHECK (operation IN ('I', 'U', 'D'))
);
