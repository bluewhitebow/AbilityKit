using AbilityKit.Samples.Abstractions;

namespace AbilityKit.Samples.Infrastructure
{
    public sealed class NullSampleLogger : ILogger
    {
        public static NullSampleLogger Instance { get; } = new();

        private NullSampleLogger()
        {
        }

        public void Info(string message) { }
        public void Warn(string message) { }
        public void Error(string message) { }
        public void Section(string title) { }
        public void Line() { }
        public void Divider() { }
        public void Bullet(string text) { }
        public void Numbered(int num, string text) { }
        public void KeyValue(string key, string value) { }
        public void Flush() { }
    }
}
