-- wrk script: substitute a random line of a file into a path template.
--
-- Usage:
--   wrk -t4 -c64 -d30s --latency -s bench/lua/random_id.lua http://localhost:5000 \
--       -- '/api/reports/mongo/courses/{id}/dashboard' bench/data/course_ids.txt
--
-- Every occurrence of "{id}" in the template is replaced with a random
-- non-empty, non-comment ('#') line of the file. The same mechanism is used for
-- parameter lists: e.g. template '/api/reports/pg/activity/monthly?{id}' with a
-- file of query strings ("year=2024", ...). Because pg and the NoSQL route of a
-- pair are driven by the same file, they get the same parameter distribution.
--
-- The file is read once per thread in init(); math.random is seeded per thread.
-- Set BENCH_TRACK_STATUS=0 to skip the response() hook (wrk then calls no Lua
-- per response; its own "Non-2xx or 3xx responses" line is still printed).

local track_status = os.getenv("BENCH_TRACK_STATUS") ~= "0"

local threads = {}
local thread_counter = 0

function setup(thread)
   thread_counter = thread_counter + 1
   thread:set("tid", thread_counter)
   table.insert(threads, thread)
end

local function fail(msg)
   io.stderr:write("random_id.lua: " .. msg .. "\n")
   os.exit(2)
end

local function load_lines(path)
   local f, err = io.open(path, "r")
   if not f then
      fail("cannot open id/param file '" .. tostring(path) .. "': " .. tostring(err))
   end
   local lines = {}
   for line in f:lines() do
      line = line:gsub("^%s+", ""):gsub("%s+$", "")
      if line ~= "" and line:sub(1, 1) ~= "#" then
         lines[#lines + 1] = line
      end
   end
   f:close()
   if #lines == 0 then
      fail("id/param file '" .. path .. "' has no usable lines")
   end
   return lines
end

local template, values, n_values

function init(args)
   template = args[1]
   local file = args[2]
   if not template or not file then
      fail("expected script args: <path-template-with-{id}> <file>")
   end
   if not template:find("{id}", 1, true) then
      fail("template '" .. template .. "' does not contain {id}")
   end
   values = load_lines(file)
   n_values = #values

   -- Per-thread seed: wall clock + thread index + a bit of CPU clock jitter.
   local t = tid or 0
   math.randomseed(os.time() * 1000 + t * 7919 + math.floor(os.clock() * 1e6))
   for _ = 1, 10 do math.random() end -- discard first values

   -- Pre-build the paths: request() then only picks an index.
   paths = {}
   for i = 1, n_values do
      local v = values[i]:gsub("%%", "%%%%")
      paths[i] = template:gsub("{id}", v)
   end

   non2xx = 0
   total = 0
   codes = {}
end

function request()
   return wrk.format(nil, paths[math.random(n_values)])
end

if track_status then
   function response(status, headers, body)
      total = total + 1
      if status < 200 or status > 299 then
         non2xx = non2xx + 1
      end
      codes[status] = (codes[status] or 0) + 1
   end
end

function done(summary, latency, requests)
   if not track_status then
      io.write("BENCH_STATUS tracking=off\n")
      return
   end
   local t_total, t_non2xx, agg = 0, 0, {}
   for _, thread in ipairs(threads) do
      t_total = t_total + (thread:get("total") or 0)
      t_non2xx = t_non2xx + (thread:get("non2xx") or 0)
      local c = thread:get("codes") or {}
      for code, n in pairs(c) do
         agg[code] = (agg[code] or 0) + n
      end
   end
   local keys = {}
   for code in pairs(agg) do keys[#keys + 1] = code end
   table.sort(keys)
   local parts = {}
   for _, code in ipairs(keys) do parts[#parts + 1] = code .. ":" .. agg[code] end
   io.write(string.format("BENCH_STATUS tracking=on total=%d non2xx=%d codes=%s\n",
      t_total, t_non2xx, table.concat(parts, ",")))
end
