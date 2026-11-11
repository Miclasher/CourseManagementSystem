#!/usr/bin/env fish
# Correctness check: every SQL/NoSQL route pair must return HTTP 200 and
# byte-identical bodies for the same parameters.
#
#   fish bench/compare.fish [--samples N] [--only r3,r4] [--base-url URL]
#
# R1/R2: every line of bench/params/<report>.txt is checked.
# R3/R4: N random ids (default 50) from bench/data/*_ids.txt.
# Exit status: 0 all matched, 1 mismatch/non-200, 2 setup error.

source (status dirname)/lib.fish

function usage
    echo "usage: compare.fish [-n|--samples N] [-o|--only REPORT[,REPORT]] [-b|--base-url URL]"
    echo "reports: "(bench_reports)
end

argparse 'n/samples=' 'o/only=+' 'b/base-url=' 'h/help' -- $argv
or begin
    usage >&2
    exit 2
end
if set -q _flag_help
    usage
    exit 0
end

set -l samples 50
set -q BENCH_SAMPLES; and set samples $BENCH_SAMPLES
set -q _flag_samples; and set samples $_flag_samples
if not string match -qr '^[1-9][0-9]*$' -- $samples
    bench_err "--samples must be a positive integer"
    exit 2
end
set -q _flag_base_url; and set BASE_URL (string trim -r -c / -- $_flag_base_url)

set -l reports (bench_select_reports $_flag_only); or exit 2
bench_check_files $reports; or exit 2
bench_check_health; or exit 2

set -l mismatch_dir $BENCH_ROOT/results/mismatch
rm -rf $mismatch_dir
set -l tmp (mktemp -d)

set -l total_bad 0
set -l summary
set -l diff_shown 0

for report in $reports
    set -l routes (bench_routes_of $report)
    set -l a (string split ' ' -- $routes[1])
    set -l b (string split ' ' -- $routes[2])
    set -l values (bench_lines (bench_file $a[4]))
    if string match -q 'data/*' $a[4]
        set values (printf '%s\n' $values | shuf -n $samples)
    end

    bench_info "$report: $a[2] vs $b[2], "(count $values)" samples"
    set -l matched 0
    set -l mismatched 0
    set -l errors 0
    set -l i 0
    for v in $values
        set i (math $i + 1)
        set -l pa (string replace -a '{id}' -- $v $a[3])
        set -l pb (string replace -a '{id}' -- $v $b[3])
        set -l ca (curl -s -o $tmp/a -w '%{http_code}' --max-time 60 "$BASE_URL$pa")
        set -l cb (curl -s -o $tmp/b -w '%{http_code}' --max-time 60 "$BASE_URL$pb")
        set -l ha (sha256sum $tmp/a | string split -f1 ' ')
        set -l hb (sha256sum $tmp/b | string split -f1 ' ')

        if test "$ca" = 200 -a "$cb" = 200 -a "$ha" = "$hb"
            set matched (math $matched + 1)
            continue
        end

        if test "$ca" != 200 -o "$cb" != 200
            set errors (math $errors + 1)
            echo "  [$i] HTTP $ca ($a[2]) / $cb ($b[2]) for $v"
        else
            set mismatched (math $mismatched + 1)
            echo "  [$i] body mismatch for $v"
        end

        mkdir -p $mismatch_dir
        set -l stem $mismatch_dir/{$report}_{$i}
        cp $tmp/a {$stem}_$a[2].json
        cp $tmp/b {$stem}_$b[2].json
        printf '%s %s\n%s %s\n' $ca "$BASE_URL$pa" $cb "$BASE_URL$pb" >{$stem}_urls.txt

        if test $diff_shown -eq 0 -a "$ca" = 200 -a "$cb" = 200
            set diff_shown 1
            echo "  first mismatch diff (pretty-printed, first 60 lines):"
            python3 -m json.tool {$stem}_$a[2].json >$tmp/pa.json 2>&1
            python3 -m json.tool {$stem}_$b[2].json >$tmp/pb.json 2>&1
            diff -u --label "$a[2] $pa" --label "$b[2] $pb" $tmp/pa.json $tmp/pb.json | head -n 60 | string replace -r '^' '    '
            if diff -q $tmp/pa.json $tmp/pb.json >/dev/null
                echo "    (pretty-printed JSON is identical: difference is whitespace/formatting only)"
            end
        end
    end

    set -l bad (math $mismatched + $errors)
    set total_bad (math $total_bad + $bad)
    set -l verdict OK
    test $bad -gt 0; and set verdict FAIL
    set -a summary (printf '%-22s %-10s %-11s matched=%-4s mismatched=%-4s non200=%-4s %s' \
        $report $a[2] $b[2] $matched $mismatched $errors $verdict)
end

rm -rf $tmp

echo
bench_info Summary
printf '  %s\n' $summary
if test $total_bad -gt 0
    bench_err "$total_bad failing sample(s); bodies saved in $mismatch_dir"
    exit 1
end
set_color green
echo "All pairs byte-identical."
set_color normal
