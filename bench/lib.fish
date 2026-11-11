# Shared definitions for bench/compare.fish and bench/run.fish (sourced, not executed).

set -g BENCH_ROOT (path resolve (status dirname))
# Locale-independent number formatting (printf would otherwise emit "1,50" in e.g. uk_UA).
set -gx LC_NUMERIC C
set -q BASE_URL; or set -g BASE_URL http://localhost:5000
set -g BASE_URL (string trim -r -c / -- $BASE_URL)

# Each report is a pair: <pg route> <nosql route>.
# Route entry format: "<report> <source> <path template with {id}> <id/param file relative to bench/>"
set -g BENCH_ROUTES \
    "r1_activity pg /api/reports/pg/activity/monthly?{id} params/r1_activity.txt" \
    "r1_activity clickhouse /api/reports/clickhouse/activity/monthly?{id} params/r1_activity.txt" \
    "r2_top_courses pg /api/reports/pg/courses/top?{id} params/r2_top_courses.txt" \
    "r2_top_courses clickhouse /api/reports/clickhouse/courses/top?{id} params/r2_top_courses.txt" \
    "r3_course_dashboard pg /api/reports/pg/courses/{id}/dashboard data/course_ids.txt" \
    "r3_course_dashboard mongo /api/reports/mongo/courses/{id}/dashboard data/course_ids.txt" \
    "r4_student_transcript pg /api/reports/pg/students/{id}/transcript data/student_ids.txt" \
    "r4_student_transcript mongo /api/reports/mongo/students/{id}/transcript data/student_ids.txt"

function bench_err
    set_color red >&2
    echo "error: $argv" >&2
    set_color normal >&2
end

function bench_info
    set_color cyan
    echo "==> $argv"
    set_color normal
end

# All report names, in order.
function bench_reports
    for r in $BENCH_ROUTES
        string split -f1 ' ' -- $r
    end | uniq
end

# Reports matching the --only filters (substring match; empty filter = all).
function bench_select_reports
    set -l all (bench_reports)
    if test (count $argv) -eq 0
        printf '%s\n' $all
        return 0
    end
    set -l filters (string split ',' -- $argv)
    set -l out
    for r in $all
        for f in $filters
            if string match -q -- "*$f*" $r
                set -a out $r
                break
            end
        end
    end
    if test (count $out) -eq 0
        bench_err "--only '$argv' matches no report (have: $all)"
        return 1
    end
    printf '%s\n' $out
end

# Prints the route entries (one per line, pg first) for a report.
function bench_routes_of --argument-names report
    for r in $BENCH_ROUTES
        set -l parts (string split ' ' -- $r)
        test "$parts[1]" = "$report"; and echo $r
    end
end

# Absolute path of a route's id/param file. data/* honours BENCH_DATA_DIR
# (default bench/data, where the seeder writes course_ids.txt / student_ids.txt).
function bench_file --argument-names rel
    if set -q BENCH_DATA_DIR; and string match -q 'data/*' -- $rel
        echo (path resolve $BENCH_DATA_DIR)/(string replace 'data/' '' -- $rel)
    else
        echo $BENCH_ROOT/$rel
    end
end

# Usable lines of an id/param file (no blanks, no comments).
function bench_lines --argument-names file
    string trim <$file | string match -rv '^(#.*)?$'
end

# Fails if the API health endpoint is not 200.
function bench_check_health
    set -l code (curl -s -o /dev/null -w '%{http_code}' --max-time 5 "$BASE_URL/health")
    if test "$code" != 200
        if test "$code" = 000
            bench_err "API not reachable at $BASE_URL (GET /health failed to connect). Start it or set BASE_URL."
        else
            bench_err "GET $BASE_URL/health returned HTTP $code (expected 200)."
        end
        return 1
    end
end

# Fails if any id/param file needed by the given reports is missing or empty.
function bench_check_files
    set -l files
    for report in $argv
        for r in (bench_routes_of $report)
            contains -- (string split ' ' -- $r)[4] $files; or set -a files (string split ' ' -- $r)[4]
        end
    end
    set -l rc 0
    for f in $files
        set -l file (bench_file $f)
        if not test -f $file
            set -l hint
            string match -q 'data/*' -- $f; and set hint " (run the data seeder first; it writes bench/data/*.txt, or set BENCH_DATA_DIR)"
            bench_err "missing $file$hint"
            set rc 1
        else if test (count (bench_lines $file)) -eq 0
            bench_err "$file has no usable ids/params"
            set rc 1
        end
    end
    return $rc
end
