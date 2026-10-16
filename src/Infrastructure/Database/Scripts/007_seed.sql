-- 007_seed.sql: reference data and a demo admin / instructor / student

INSERT INTO roles (id, name, description) VALUES
    (1, 'admin', 'Platform administrator'),
    (2, 'instructor', 'Creates and teaches courses'),
    (3, 'student', 'Enrolls in courses')
ON CONFLICT DO NOTHING;

INSERT INTO users (id, email, password_hash, first_name, last_name) VALUES
    ('00000000-0000-0000-0000-000000000001', 'admin@cms.local', 'seed', 'Ada', 'Admin'),
    ('00000000-0000-0000-0000-000000000002', 'teacher@cms.local', 'seed', 'Ivan', 'Instructor'),
    ('00000000-0000-0000-0000-000000000003', 'student@cms.local', 'seed', 'Stella', 'Student')
ON CONFLICT DO NOTHING;

INSERT INTO user_roles (user_id, role_id) VALUES
    ('00000000-0000-0000-0000-000000000001', 1),
    ('00000000-0000-0000-0000-000000000002', 2),
    ('00000000-0000-0000-0000-000000000003', 3)
ON CONFLICT DO NOTHING;

INSERT INTO categories (id, name, slug) VALUES
    ('10000000-0000-0000-0000-000000000001', 'Programming', 'programming'),
    ('10000000-0000-0000-0000-000000000002', 'Databases', 'databases')
ON CONFLICT DO NOTHING;
