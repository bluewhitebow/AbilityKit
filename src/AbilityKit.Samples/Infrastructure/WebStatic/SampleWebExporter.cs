using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using AbilityKit.Samples.Abstractions;
using AbilityKit.Samples.Logic;

namespace AbilityKit.Samples.Infrastructure.WebStatic
{
    internal static class SampleWebExporter
    {
        public static string Export(string outputDirectory, SampleRunOptions options)
        {
            if (string.IsNullOrWhiteSpace(outputDirectory))
                outputDirectory = "sample-web";

            Directory.CreateDirectory(outputDirectory);

            var catalog = SampleCatalogProvider.CreateCatalog();
            var executor = new SampleExecutionService(catalog, SampleEnvironmentFactory.Create);
            var exported = new List<WebSampleEntry>();

            var exportEntries = catalog.Entries
                .Where(entry => entry.Tags.Any(tag =>
                    string.Equals(tag, "web", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(tag, "web-export", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            foreach (var entry in exportEntries)
            {
                var logger = new BufferedSampleLogger();
                var runOptions = new SampleRunOptions
                {
                    ExecutionMode = options.ExecutionMode,
                    HostKind = SampleHostKind.Web,
                    WriteConsole = false,
                    WriteFile = false,
                    OutputDirectory = outputDirectory
                };

                var result = executor.Run(entry, logger, runOptions);
                exported.Add(WebSampleEntry.From(entry, logger.Entries, result));
            }

            var page = RenderPage(new WebSampleDocument
            {
                GeneratedAt = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz"),
                Samples = exported
            });

            var path = Path.GetFullPath(Path.Combine(outputDirectory, "index.html"));
            File.WriteAllText(path, page, Encoding.UTF8);
            return path;
        }

        private static string RenderPage(WebSampleDocument document)
        {
            var json = JsonSerializer.Serialize(document, new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            return @"<!doctype html>
<html lang=""zh-CN"">
<head>
  <meta charset=""utf-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
  <title>AbilityKit Samples</title>
  <style>
    :root { color-scheme: light; --bg:#f6f7f9; --panel:#fff; --line:#d8dee8; --text:#1f2937; --muted:#64748b; --accent:#0f766e; --bad:#b91c1c; }
    * { box-sizing: border-box; }
    body { margin:0; font-family: Segoe UI, Microsoft YaHei, Arial, sans-serif; background:var(--bg); color:var(--text); }
    .app { display:grid; grid-template-columns: 360px 1fr; min-height:100vh; }
    aside { background:var(--panel); border-right:1px solid var(--line); padding:16px; overflow:auto; }
    main { padding:20px; overflow:auto; }
    h1 { font-size:20px; margin:0 0 4px; }
    h2 { font-size:18px; margin:0 0 8px; }
    .meta { color:var(--muted); font-size:13px; margin-bottom:14px; }
    .search { width:100%; padding:10px 12px; border:1px solid var(--line); border-radius:6px; margin:10px 0 14px; font-size:14px; }
    .category { margin:16px 0 8px; color:var(--muted); font-weight:600; font-size:13px; text-transform:uppercase; }
    button.sample { width:100%; display:block; text-align:left; background:#fff; border:1px solid var(--line); border-radius:6px; padding:10px; margin:6px 0; cursor:pointer; }
    button.sample:hover, button.sample.active { border-color:var(--accent); background:#ecfdf5; }
    .title { font-weight:650; font-size:14px; }
    .id { color:var(--muted); font-size:12px; margin-top:3px; overflow-wrap:anywhere; }
    .toolbar { display:flex; gap:8px; align-items:center; justify-content:space-between; margin-bottom:12px; }
    .badge { display:inline-block; border:1px solid var(--line); border-radius:999px; padding:3px 8px; font-size:12px; color:var(--muted); margin-right:6px; }
    .ok { color:var(--accent); }
    .fail { color:var(--bad); }
    .log { background:var(--panel); border:1px solid var(--line); border-radius:8px; padding:14px; }
    .row { padding:3px 0; white-space:pre-wrap; overflow-wrap:anywhere; font-family: Consolas, Microsoft YaHei, monospace; font-size:13px; }
    .section { font-weight:700; margin-top:8px; }
    .warn { color:#a16207; }
    .error { color:var(--bad); }
    .kv .key { color:var(--muted); }
    @media (max-width: 800px) { .app { grid-template-columns:1fr; } aside { border-right:0; border-bottom:1px solid var(--line); max-height:45vh; } }
  </style>
</head>
<body>
<div class=""app"">
  <aside>
    <h1>AbilityKit Samples</h1>
    <div class=""meta"" id=""generated""></div>
    <input class=""search"" id=""search"" placeholder=""Search title, id, category..."">
    <div id=""list""></div>
  </aside>
  <main>
    <div class=""toolbar"">
      <div>
        <h2 id=""sampleTitle"">Select a sample</h2>
        <div class=""meta"" id=""sampleMeta""></div>
      </div>
      <div id=""sampleState""></div>
    </div>
    <div class=""log"" id=""log""></div>
  </main>
</div>
<script>
const DATA = " + json + @";
const list = document.getElementById('list');
const log = document.getElementById('log');
const search = document.getElementById('search');
const generated = document.getElementById('generated');
const sampleTitle = document.getElementById('sampleTitle');
const sampleMeta = document.getElementById('sampleMeta');
const sampleState = document.getElementById('sampleState');
generated.textContent = '导出时间：' + DATA.generatedAt + '。重新导出后刷新本文件即可。';
let activeId = DATA.samples[0]?.id || '';

function renderList() {
  const q = search.value.trim().toLowerCase();
  const items = DATA.samples.filter(s => !q || (s.title + ' ' + s.id + ' ' + s.category + ' ' + s.tags.join(' ')).toLowerCase().includes(q));
  const groups = new Map();
  for (const item of items) {
    if (!groups.has(item.category)) groups.set(item.category, []);
    groups.get(item.category).push(item);
  }
  list.innerHTML = '';
  for (const [category, samples] of groups) {
    const label = document.createElement('div');
    label.className = 'category';
    label.textContent = category;
    list.appendChild(label);
    for (const sample of samples) {
      const button = document.createElement('button');
      button.className = 'sample' + (sample.id === activeId ? ' active' : '');
      button.innerHTML = '<div class=""title"">' + escapeHtml(sample.title) + '</div><div class=""id"">' + escapeHtml(sample.id) + '</div>';
      button.onclick = () => { activeId = sample.id; renderList(); renderSample(); };
      list.appendChild(button);
    }
  }
}

function renderSample() {
  const sample = DATA.samples.find(s => s.id === activeId) || DATA.samples[0];
  if (!sample) return;
  sampleTitle.textContent = sample.title;
  sampleMeta.innerHTML = '<span class=""badge"">' + escapeHtml(sample.id) + '</span><span class=""badge"">' + escapeHtml(sample.category) + '</span>';
  sampleState.innerHTML = sample.succeeded ? '<span class=""ok"">Succeeded</span>' : '<span class=""fail"">Failed</span>';
  log.innerHTML = '';
  for (const entry of sample.logs) {
    const row = document.createElement('div');
    row.className = 'row ' + kindClass(entry.kind);
    row.innerHTML = formatEntry(entry);
    log.appendChild(row);
  }
}

function formatEntry(entry) {
  if (entry.kind === 'Line') return '&nbsp;';
  if (entry.kind === 'Divider') return '------------------------------------------------------------------------';
  if (entry.kind === 'Bullet') return '  - ' + escapeHtml(entry.text);
  if (entry.kind === 'Numbered') return '  ' + entry.number + '. ' + escapeHtml(entry.text);
  if (entry.kind === 'KeyValue') return '<span class=""key"">' + escapeHtml(entry.key) + ':</span> ' + escapeHtml(entry.text);
  return escapeHtml(entry.text);
}

function kindClass(kind) {
  if (kind === 'Section') return 'section';
  if (kind === 'Warn') return 'warn';
  if (kind === 'Error') return 'error';
  if (kind === 'KeyValue') return 'kv';
  return '';
}

function escapeHtml(value) {
  return String(value ?? '')
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/""/g, '&quot;')
    .replace(/'/g, '&#39;');
}

search.oninput = renderList;
renderList();
renderSample();
</script>
</body>
</html>";
        }

        private sealed class WebSampleDocument
        {
            public string GeneratedAt { get; set; } = string.Empty;
            public List<WebSampleEntry> Samples { get; set; } = new();
        }

        private sealed class WebSampleEntry
        {
            public string Id { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public string[] Tags { get; set; } = Array.Empty<string>();
            public bool Succeeded { get; set; }
            public string ErrorMessage { get; set; } = string.Empty;
            public List<WebLogEntry> Logs { get; set; } = new();

            public static WebSampleEntry From(SampleCatalogEntry entry, IReadOnlyList<SampleLogEntry> logs, SampleExecutionResult result)
            {
                return new WebSampleEntry
                {
                    Id = entry.Id,
                    Title = entry.Title,
                    Description = entry.Description,
                    Category = entry.Category.GetDisplayName(),
                    Tags = entry.Tags.ToArray(),
                    Succeeded = result.Succeeded,
                    ErrorMessage = result.ErrorMessage,
                    Logs = logs.Select(WebLogEntry.From).ToList()
                };
            }
        }

        private sealed class WebLogEntry
        {
            public string Kind { get; set; } = string.Empty;
            public string Text { get; set; } = string.Empty;
            public string Key { get; set; } = string.Empty;
            public int? Number { get; set; }

            public static WebLogEntry From(SampleLogEntry entry)
            {
                return new WebLogEntry
                {
                    Kind = entry.Kind.ToString(),
                    Text = entry.Text,
                    Key = entry.Key,
                    Number = entry.Number
                };
            }
        }
    }
}
