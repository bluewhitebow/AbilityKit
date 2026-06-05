using System;
using System.IO;
using AbilityKit.Samples.Abstractions;

namespace AbilityKit.Samples.Infrastructure
{
    public sealed class TextWriterSampleLogger : ILogger, IDisposable
    {
        private readonly TextWriter _writer;
        private readonly bool _ownsWriter;

        public TextWriterSampleLogger(TextWriter writer, bool ownsWriter = false)
        {
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _ownsWriter = ownsWriter;
        }

        public void Info(string message) => _writer.WriteLine(message);
        public void Warn(string message) => _writer.WriteLine("[WARN] " + message);
        public void Error(string message) => _writer.WriteLine("[ERR] " + message);

        public void Section(string title)
        {
            Divider();
            _writer.WriteLine(title);
            Divider();
        }

        public void Line() => _writer.WriteLine();
        public void Divider() => _writer.WriteLine(new string('-', 72));
        public void Bullet(string text) => _writer.WriteLine("  - " + text);
        public void Numbered(int num, string text) => _writer.WriteLine($"  {num}. {text}");
        public void KeyValue(string key, string value) => _writer.WriteLine($"  {key}: {value}");
        public void Flush() => _writer.Flush();

        public void Dispose()
        {
            Flush();
            if (_ownsWriter)
                _writer.Dispose();
        }
    }
}
