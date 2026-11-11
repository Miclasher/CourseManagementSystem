-- Denormalized fact table: one row per completed lesson or quiz attempt (projected from PostgreSQL).
CREATE TABLE IF NOT EXISTS cms.activity_events
(
    event_type       Enum8('lesson_completed' = 1, 'quiz_attempt' = 2),
    event_at         DateTime('UTC'),
    user_id          UUID,
    enrollment_id    UUID,
    course_id        UUID,
    course_title     String,
    category_id      UUID,
    category_name    LowCardinality(String),
    score_hundredths UInt32,  -- quiz score_percent * 100, 0 for lessons
    passed           UInt8    -- quiz passed flag, 0 for lessons
)
ENGINE = MergeTree
PARTITION BY toYYYYMM(event_at)
ORDER BY (category_id, course_id, event_at);
