#!/usr/bin/env fish
# Throughput/latency benchmark of the SQL vs NoSQL report routes with wrk.
#
#   fish bench/run.fish [--duration 30s] [--warmup 10s] [--threads 4]
#                       [--connections 64] [--only r1,r3] [--pause 3]
#                       [--base-url URL]
#
# Env equivalents: BENCH_DURATION, BENCH_WARMUP, BENCH_THREADS, BENCH_CONNECTIONS,
# BENCH_ONLY, BENCH_PAUSE, BASE_URL, BENCH_TRACK_STATUS (0 = no Lua response hook),
# BENCH_DOCKER_STATS (0 = skip docker stats snapshot).
#
# Per route: warm-up run (discarded) -> measured `wrk --latency`.
# Pairs run back-to-back (pg, then NoSQL). Output:
#   bench/results/raw/<report>_<source>.txt   raw wrk output
#   bench/results/results.md                  summary table

source (status dirname)/lib.fish

function usage
    echo "usage: run.fish [-d|--duration 30s] [-w|--warmup 10s] [-t|--threads 4] [-c|--connections 64]"
    echo "                [-o|--only REPORT[,REPORT]] [-p|--pause SECONDS] [-b|--base-url URL]"
    echo "                [-T|--timeout 10s] [-O|--out results.md]"
    echo "reports: "(bench_reports)
end

argparse 'd/duration=' 'w/warmup=' 't/threads=' 'c/connections=' 'o/only=+' 'p/pause=' 'b/base-url=' 'T/timeout=' 'O/out=' 'h/help' -- $argv
or begin
    usage >&2
    exit 2
end
if set -q _flag_help
    usage
    exit 0
end

# defaults < env < flags
set -l duration 30s
set -l warmup 10s
set -l threads 4
set -l connections 64
set -l pause 3
set -l wrk_timeout 10s
set -l out_name results.md
set -q BENCH_TIMEOUT; and set wrk_timeout $BENCH_TIMEOUT
set -q BENCH_OUT; and set out_name $BENCH_OUT
set -q _flag_timeout; and set wrk_timeout $_flag_timeout
set -q _flag_out; and set out_name $_flag_out
set -l only
set -q BENCH_DURATION; and set duration $BENCH_DURATION
set -q BENCH_WARMUP; and set warmup $BENCH_WARMUP
set -q BENCH_THREADS; and set threads $BENCH_THREADS
set -q BENCH_CONNECTIONS; and set connections $BENCH_CONNECTIONS
set -q BENCH_PAUSE; and set pause $BENCH_PAUSE
set -q BENCH_ONLY; and set only $BENCH_ONLY
set -q _flag_duration; and set duration $_flag_duration
set -q _flag_warmup; and set warmup $_flag_warmup
set -q _flag_threads; and set threads $_flag_threads
set -q _flag_connections; and set connections $_flag_connections
set -q _flag_pause; and set pause $_flag_pause
set -q _flag_only; and set only $_flag_only
set -q _flag_base_url; and set BASE_URL (string trim -r -c / -- $_flag_base_url)

for pair in "duration $duration" "warmup $warmup"
    set -l kv (string split ' ' -- $pair)
    if not string match -qr '^[0-9]+[smh]?$' -- $kv[2]
        bench_err "--$kv[1] must look like 30, 30s, 2m (got '$kv[2]')"
        exit 2
    end
end
for pair in "threads $threads" "connections $connections" "pause $pause"
    set -l kv (string split ' ' -- $pair)
    if not string match -qr '^[0-9]+$' -- $kv[2]
        bench_err "--$kv[1] must be an integer (got '$kv[2]')"
        exit 2
    end
end

if not command -q wrk
    bench_err "wrk not found in PATH"
    exit 2
end

set -l reports (bench_select_reports $only); or exit 2
bench_check_files $reports; or exit 2
bench_check_health; or exit 2

set -l lua $BENCH_ROOT/lua/random_id.lua
set -l raw_dir $BENCH_ROOT/results/raw
set -l out_md $BENCH_ROOT/results/$out_name
mkdir -p $raw_dir

# ---------- wrk output parsing ----------

# "1.23ms" / "456.00us" / "1.02s" / "1.00m" -> milliseconds (2 decimals)
function to_ms --argument-names value
    set -l m (string match -r '^([0-9.]+)(us|ms|s|m|h)$' -- $value)
    if test (count $m) -ne 3
        echo n/a
        return 1
    end
    set -l v
    switch $m[3]
        case us
            set v (math --scale=6 "$m[2] / 1000")
        case ms
            set v $m[2]
        case s
            set v (math --scale=6 "$m[2] * 1000")
        case m
            set v (math --scale=6 "$m[2] * 60000")
        case h
            set v (math --scale=6 "$m[2] * 3600000")
    end
    printf '%.2f\n' $v
end

# Echoes: rps avg_ms p50_ms p90_ms p99_ms errors requests  (n/a where unknown)
function parse_wrk --argument-names file
    set -l rps (string match -r --groups-only '^Requests/sec:\s+([0-9.]+)' <$file)
    set -l avg (string match -r --groups-only '^\s+Latency\s+(\S+)' <$file)[1]
    set -l p50 (string match -r --groups-only '^\s+50%\s+(\S+)' <$file)
    set -l p90 (string match -r --groups-only '^\s+90%\s+(\S+)' <$file)
    set -l p99 (string match -r --groups-only '^\s+99%\s+(\S+)' <$file)
    set -l requests (string match -r --groups-only '^\s+([0-9]+) requests in' <$file)

    set -l sock (string match -r --groups-only '^\s+Socket errors: connect (\d+), read (\d+), write (\d+), timeout (\d+)' <$file)
    set -l sock_sum 0
    for n in $sock
        set sock_sum (math $sock_sum + $n)
    end
    # Prefer the Lua status counter; fall back to wrk's own "Non-2xx or 3xx" line.
    set -l non2xx (string match -r --groups-only '^BENCH_STATUS tracking=on .*non2xx=(\d+)' <$file)
    if test -z "$non2xx"
        set non2xx (string match -r --groups-only '^\s+Non-2xx or 3xx responses: (\d+)' <$file)
        test -z "$non2xx"; and set non2xx 0
    end

    test -z "$rps"; and set rps n/a
    test -z "$requests"; and set requests n/a
    echo $rps (to_ms "$avg") (to_ms "$p50") (to_ms "$p90") (to_ms "$p99") (math $non2xx + $sock_sum) $requests
end

function ratio --argument-names num den
    if string match -qr '^[0-9.]+$' -- $num; and string match -qr '^[0-9.]+$' -- $den
        and string match -qr '[1-9]' -- $den
        printf '%.2f\n' (math --scale=6 "$num / $den")
    else
        echo n/a
    end
end

# ---------- run ----------

set -l started (date -Iseconds)
set -l docker_snapshot
if test "$BENCH_DOCKER_STATS" != 0; and command -q docker
    set docker_snapshot (timeout 15 docker stats --no-stream --format 'table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.MemPerc}}' 2>/dev/null)
end

set -l rows
set -l pairs
for report in $reports
    set -l pair_metrics
    for r in (bench_routes_of $report)
        set -l parts (string split ' ' -- $r)
        set -l source $parts[2]
        set -l template $parts[3]
        set -l file (bench_file $parts[4])
        set -l raw $raw_dir/{$report}_{$source}.txt
        set -l wrk_args -t$threads -c$connections --timeout $wrk_timeout -s $lua $BASE_URL -- $template $file

        if test (string trim -r -c smh -- $warmup) -gt 0
            bench_info "$report/$source: warm-up $warmup"
            wrk -d$warmup $wrk_args >/dev/null 2>$raw_dir/.warmup_err
            or begin
                bench_err "warm-up failed for $report/$source:"
                cat $raw_dir/.warmup_err >&2
                exit 1
            end
            sleep $pause
        end

        bench_info "$report/$source: measuring $duration ($threads threads, $connections connections)"
        begin
            echo "# $report/$source  $template  ($parts[4])"
            echo "# wrk -d$duration --latency $wrk_args"
            wrk -d$duration --latency $wrk_args 2>&1
        end >$raw
        or begin
            bench_err "wrk failed for $report/$source, see $raw"
            cat $raw >&2
            exit 1
        end
        set -l m (string split ' ' -- (parse_wrk $raw))
        printf '    rps=%s avg=%sms p50=%sms p90=%sms p99=%sms errors=%s requests=%s\n' $m
        if test "$m[6]" != 0
            bench_err "$report/$source had $m[6] errors (non-2xx/socket); numbers are suspect, see $raw"
        end
        set -a rows "| $report | $source | $m[1] | $m[2] | $m[3] | $m[4] | $m[5] | $m[6] |"
        set -a pair_metrics $source $m[1] $m[3]
        sleep $pause
    end
    # pair_metrics: pg rps p50 nosql rps p50
    set -a pairs "| $report | $pair_metrics[4] | $pair_metrics[2] | $pair_metrics[5] | "(ratio $pair_metrics[5] $pair_metrics[2])"x | $pair_metrics[3] | $pair_metrics[6] | "(ratio $pair_metrics[3] $pair_metrics[6])"x |"
end
rm -f $raw_dir/.warmup_err

# ---------- results.md ----------
begin
    echo "# Lab 2: SQL vs NoSQL benchmark results"
    echo
    echo "## Environment"
    echo
    echo "- Date: $started (finished "(date -Iseconds)")"
    echo "- Host: "(uname -srm)", nproc: "(nproc)", "(free -g 2>/dev/null | string match -r '^Mem:\s+\d+' | string replace -r 'Mem:\s+' '')" GiB RAM"
    echo "- Target: `$BASE_URL`"
    echo "- wrk: `-t$threads -c$connections -d$duration --latency --timeout $wrk_timeout`, warm-up $warmup per route (discarded), $pause s pause between runs"
    echo "- Load script: `bench/lua/random_id.lua` (random id/param per request, per-thread seed); response status tracking: "(test "$BENCH_TRACK_STATUS" = 0; and echo off; or echo on)
    echo "- Id/param pools: "(for rep in $reports
        set -l f (string split ' ' -- (bench_routes_of $rep)[1])[4]
        echo -n "$rep="(count (bench_lines (bench_file $f)))" "
    end)
    if test (count $docker_snapshot) -gt 0
        echo
        echo "Docker stats before the run:"
        echo
        echo '```'
        printf '%s\n' $docker_snapshot
        echo '```'
    end
    echo
    echo "## Results"
    echo
    echo "Latencies in ms. Errors = non-2xx responses + socket errors (connect/read/write/timeout)."
    echo
    echo "| Report | Source | RPS | Avg | p50 | p90 | p99 | Errors |"
    echo "|---|---|---:|---:|---:|---:|---:|---:|"
    printf '%s\n' $rows
    echo
    echo "## Speedup (NoSQL vs PostgreSQL)"
    echo
    echo "RPS speedup = NoSQL RPS / PG RPS; p50 speedup = PG p50 / NoSQL p50 (> 1 means NoSQL is faster)."
    echo
    echo "| Report | NoSQL | PG RPS | NoSQL RPS | RPS speedup | PG p50 | NoSQL p50 | p50 speedup |"
    echo "|---|---|---:|---:|---:|---:|---:|---:|"
    printf '%s\n' $pairs
    echo
    echo "Raw wrk output: `bench/results/raw/<report>_<source>.txt`."
end >$out_md

echo
bench_info "Wrote $out_md"
cat $out_md
