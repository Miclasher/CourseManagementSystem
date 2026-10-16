-- 006_triggers.sql

-- keeps updated_at current; updated_by is set by the procedures, fall back to the session actor
CREATE OR REPLACE FUNCTION trg_fn_set_updated_at() RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
    NEW.updated_at := now();
    IF NEW.updated_by IS NOT DISTINCT FROM OLD.updated_by THEN
        NEW.updated_by := coalesce(nullif(current_setting('app.actor_id', true), '')::uuid, NEW.updated_by);
    END IF;
    RETURN NEW;
END $$;

-- DELETE on soft-delete tables becomes UPDATE ... SET is_deleted
CREATE OR REPLACE FUNCTION trg_fn_block_hard_delete() RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
    EXECUTE format(
        'UPDATE %I.%I SET is_deleted = true, deleted_at = now(), deleted_by = $2 WHERE id = $1 AND NOT is_deleted',
        TG_TABLE_SCHEMA, TG_TABLE_NAME)
    USING OLD.id, nullif(current_setting('app.actor_id', true), '')::uuid;
    RETURN NULL;
END $$;

CREATE OR REPLACE FUNCTION trg_fn_audit_log() RETURNS trigger
LANGUAGE plpgsql AS $$
DECLARE
    v_row jsonb := to_jsonb(CASE WHEN TG_OP = 'DELETE' THEN OLD ELSE NEW END);
BEGIN
    INSERT INTO audit_log (table_name, record_id, operation, old_data, new_data, changed_by)
    VALUES (TG_TABLE_NAME, (v_row ->> 'id')::uuid, left(TG_OP, 1),
            CASE WHEN TG_OP IN ('UPDATE', 'DELETE') THEN to_jsonb(OLD) END,
            CASE WHEN TG_OP IN ('INSERT', 'UPDATE') THEN to_jsonb(NEW) END,
            coalesce((v_row ->> 'updated_by')::uuid, nullif(current_setting('app.actor_id', true), '')::uuid));
    RETURN NULL;
END $$;

CREATE OR REPLACE FUNCTION trg_fn_enrollment_counter() RETURNS trigger
LANGUAGE plpgsql AS $$
DECLARE v_course uuid := coalesce(NEW.course_id, OLD.course_id);
BEGIN
    UPDATE courses
    SET enrolled_count = (SELECT count(*) FROM enrollments WHERE course_id = v_course AND status <> 'dropped')
    WHERE id = v_course;
    RETURN NULL;
END $$;

CREATE OR REPLACE FUNCTION trg_fn_review_rating() RETURNS trigger
LANGUAGE plpgsql AS $$
DECLARE v_course uuid := coalesce(NEW.course_id, OLD.course_id);
BEGIN
    UPDATE courses c
    SET rating_avg = coalesce(r.avg_rating, 0), rating_count = r.cnt
    FROM (SELECT round(avg(rating), 2) AS avg_rating, count(*)::int AS cnt
          FROM reviews WHERE course_id = v_course AND NOT is_deleted) r
    WHERE c.id = v_course;
    RETURN NULL;
END $$;

-- completes the enrollment and issues a certificate when every lesson is done
CREATE OR REPLACE FUNCTION trg_fn_issue_certificate() RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
    IF fn_course_progress(NEW.enrollment_id) >= 100 THEN
        UPDATE enrollments SET status = 'completed', completed_at = now()
        WHERE id = NEW.enrollment_id AND status = 'active';
        INSERT INTO certificates (id, enrollment_id, code)
        VALUES (gen_random_uuid(), NEW.enrollment_id, fn_generate_certificate_code())
        ON CONFLICT (enrollment_id) DO NOTHING;
    END IF;
    RETURN NULL;
END $$;

DO $$
DECLARE t text;
BEGIN
    FOREACH t IN ARRAY ARRAY['users','categories','courses','modules','lessons','enrollments','assignments','submissions','reviews']
    LOOP
        EXECUTE format('DROP TRIGGER IF EXISTS trg_set_updated_at ON %I', t);
        EXECUTE format('CREATE TRIGGER trg_set_updated_at BEFORE UPDATE ON %I FOR EACH ROW EXECUTE FUNCTION trg_fn_set_updated_at()', t);
        EXECUTE format('DROP TRIGGER IF EXISTS trg_audit_log ON %I', t);
        EXECUTE format('CREATE TRIGGER trg_audit_log AFTER INSERT OR UPDATE OR DELETE ON %I FOR EACH ROW EXECUTE FUNCTION trg_fn_audit_log()', t);
    END LOOP;

    FOREACH t IN ARRAY ARRAY['users','categories','courses','lessons','reviews']
    LOOP
        EXECUTE format('DROP TRIGGER IF EXISTS trg_block_hard_delete ON %I', t);
        EXECUTE format('CREATE TRIGGER trg_block_hard_delete BEFORE DELETE ON %I FOR EACH ROW EXECUTE FUNCTION trg_fn_block_hard_delete()', t);
    END LOOP;
END $$;

DROP TRIGGER IF EXISTS trg_enrollment_counter ON enrollments;
CREATE TRIGGER trg_enrollment_counter
    AFTER INSERT OR UPDATE OF status OR DELETE ON enrollments
    FOR EACH ROW EXECUTE FUNCTION trg_fn_enrollment_counter();

DROP TRIGGER IF EXISTS trg_review_rating ON reviews;
CREATE TRIGGER trg_review_rating
    AFTER INSERT OR UPDATE OF rating, is_deleted OR DELETE ON reviews
    FOR EACH ROW EXECUTE FUNCTION trg_fn_review_rating();

DROP TRIGGER IF EXISTS trg_issue_certificate ON lesson_progress;
CREATE TRIGGER trg_issue_certificate
    AFTER INSERT ON lesson_progress
    FOR EACH ROW EXECUTE FUNCTION trg_fn_issue_certificate();
