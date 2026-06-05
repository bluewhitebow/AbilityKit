using System;
using System.Collections.Generic;
using AbilityKit.Samples.Abstractions;

namespace AbilityKit.Samples.Infrastructure
{
    public sealed class CompositeSampleLogger : ILogger, IDisposable
    {
        private readonly IReadOnlyList<ILogger> _loggers;

        public CompositeSampleLogger(IReadOnlyList<ILogger> loggers)
        {
            _loggers = loggers ?? throw new ArgumentNullException(nameof(loggers));
        }

        public void Info(string message) => ForEach(x => x.Info(message));
        public void Warn(string message) => ForEach(x => x.Warn(message));
        public void Error(string message) => ForEach(x => x.Error(message));
        public void Section(string title) => ForEach(x => x.Section(title));
        public void Line() => ForEach(x => x.Line());
        public void Divider() => ForEach(x => x.Divider());
        public void Bullet(string text) => ForEach(x => x.Bullet(text));
        public void Numbered(int num, string text) => ForEach(x => x.Numbered(num, text));
        public void KeyValue(string key, string value) => ForEach(x => x.KeyValue(key, value));
        public void Flush() => ForEach(x => x.Flush());

        public void Dispose()
        {
            Flush();
            foreach (var logger in _loggers)
            {
                if (logger is IDisposable disposable)
                    disposable.Dispose();
            }
        }

        private void ForEach(Action<ILogger> action)
        {
            foreach (var logger in _loggers)
                action(logger);
        }
    }
}
