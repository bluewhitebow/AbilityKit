using System;
using System.Collections.Generic;

namespace AbilityKit.Demo.Moba.Console.Platform
{
    /// <summary>
    /// ??????
    /// ???????????
    /// </summary>
    public interface ILogSink
    {
        /// <summary>
        /// ????
        /// </summary>
        void Log(OutputChannel channel, string message);

        /// <summary>
        /// ?? sink ??
        /// </summary>
        string Name { get; }
    }

    /// <summary>
    /// ?????
    /// ???? sink ????????????????
    /// </summary>
    public static class Log
    {
        private static IOutput _output = new Console_.ConsoleOutput();
        private static readonly List<ILogSink> _sinks = new();
        private static readonly object _lock = new();

        /// <summary>
        /// ?????
        /// </summary>
        public static void SetOutput(IOutput output)
        {
            _output = output ?? new Console_.ConsoleOutput();
        }

        /// <summary>
        /// ???????
        /// </summary>
        public static IOutput Output => _output;

        /// <summary>
        /// ???? sink
        /// </summary>
        public static void AddSink(ILogSink sink)
        {
            if (sink == null) return;
            lock (_lock)
            {
                if (!_sinks.Contains(sink))
                {
                    _sinks.Add(sink);
                }
            }
        }

        /// <summary>
        /// ???? sink
        /// </summary>
        public static void RemoveSink(ILogSink sink)
        {
            if (sink == null) return;
            lock (_lock)
            {
                _sinks.Remove(sink);
            }
        }

        /// <summary>
        /// ???? sink
        /// </summary>
        public static void ClearSinks()
        {
            lock (_lock)
            {
                _sinks.Clear();
            }
        }

        /// <summary>
        /// ???? sink
        /// </summary>
        public static IReadOnlyList<ILogSink> GetSinks()
        {
            lock (_lock)
            {
                return _sinks.ToArray();
            }
        }

        private static void PublishToSinks(OutputChannel channel, string message)
        {
            ILogSink[] snapshot;
            lock (_lock)
            {
                if (_sinks.Count == 0) return;
                snapshot = _sinks.ToArray();
            }

            foreach (var sink in snapshot)
            {
                try
                {
                    sink.Log(channel, message);
                }
                catch
                {
                    // ?? sink ???????
                }
            }
        }

        private static void WriteToChannel(OutputChannel channel, string message)
        {
            _output.Write(channel, message);
            PublishToSinks(channel, message);
        }

        private static void WriteFormatToChannel(OutputChannel channel, string format, params object[] args)
        {
            var message = string.Format(format, args);
            _output.WriteFormat(channel, format, args);
            PublishToSinks(channel, message);
        }

        /// <summary>
        /// ????
        /// </summary>
        public static void System(string message) => WriteToChannel(OutputChannel.System, message);
        public static void System(string format, params object[] args) => WriteFormatToChannel(OutputChannel.System, format, args);

        /// <summary>
        /// ????
        /// </summary>
        public static void Phase(string message) => WriteToChannel(OutputChannel.Phase, message);
        public static void Phase(string format, params object[] args) => WriteFormatToChannel(OutputChannel.Phase, format, args);

        /// <summary>
        /// ????
        /// </summary>
        public static void View(string message) => WriteToChannel(OutputChannel.View, message);
        public static void View(string format, params object[] args) => WriteFormatToChannel(OutputChannel.View, format, args);

        /// <summary>
        /// ????
        /// </summary>
        public static void Sync(string message) => WriteToChannel(OutputChannel.Sync, message);
        public static void Sync(string format, params object[] args) => WriteFormatToChannel(OutputChannel.Sync, format, args);

        /// <summary>
        /// ????
        /// </summary>
        public static void Input(string message) => WriteToChannel(OutputChannel.Input, message);
        public static void Input(string format, params object[] args) => WriteFormatToChannel(OutputChannel.Input, format, args);

        /// <summary>
        /// ????
        /// </summary>
        public static void Battle(string message) => WriteToChannel(OutputChannel.Battle, message);
        public static void Battle(string format, params object[] args) => WriteFormatToChannel(OutputChannel.Battle, format, args);

        /// <summary>
        /// ????
        /// </summary>
        public static void Skill(string message) => WriteToChannel(OutputChannel.Skill, message);
        public static void Skill(string format, params object[] args) => WriteFormatToChannel(OutputChannel.Skill, format, args);

        /// <summary>
        /// ????
        /// </summary>
        public static void Damage(string message) => WriteToChannel(OutputChannel.Damage, message);
        public static void Damage(string format, params object[] args) => WriteFormatToChannel(OutputChannel.Damage, format, args);

        /// <summary>
        /// Buff ??
        /// </summary>
        public static void Buff(string message) => WriteToChannel(OutputChannel.Buff, message);
        public static void Buff(string format, params object[] args) => WriteFormatToChannel(OutputChannel.Buff, format, args);

        /// <summary>
        /// ????
        /// </summary>
        public static void Projectile(string message) => WriteToChannel(OutputChannel.Projectile, message);
        public static void Projectile(string format, params object[] args) => WriteFormatToChannel(OutputChannel.Projectile, format, args);

        /// <summary>
        /// ????
        /// </summary>
        public static void Area(string message) => WriteToChannel(OutputChannel.Area, message);
        public static void Area(string format, params object[] args) => WriteFormatToChannel(OutputChannel.Area, format, args);

        /// <summary>
        /// ????
        /// </summary>
        public static void Entity(string message) => WriteToChannel(OutputChannel.Entity, message);
        public static void Entity(string format, params object[] args) => WriteFormatToChannel(OutputChannel.Entity, format, args);

        /// <summary>
        /// ????
        /// </summary>
        public static void Config(string message) => WriteToChannel(OutputChannel.Config, message);
        public static void Config(string format, params object[] args) => WriteFormatToChannel(OutputChannel.Config, format, args);

        /// <summary>
        /// ????
        /// </summary>
        public static void Debug(string format, params object[] args) => WriteFormatToChannel(OutputChannel.Debug, format, args);

        /// <summary>
        /// ??
        /// </summary>
        public static void Warn(string message) => WriteToChannel(OutputChannel.Warning, message);
        public static void Warn(string format, params object[] args) => WriteFormatToChannel(OutputChannel.Warning, format, args);

        /// <summary>
        /// ??
        /// </summary>
        public static void Error(string message) => WriteToChannel(OutputChannel.Error, message);
        public static void Error(string format, params object[] args) => WriteFormatToChannel(OutputChannel.Error, format, args);

        /// <summary>
        /// ???
        /// </summary>
        public static void Separator(char c = '=', int length = 60) => _output.WriteSeparator(OutputChannel.System, c, length);

        /// <summary>
        /// ??
        /// </summary>
        public static void Title(string title, char c = '=', int width = 60) => _output.WriteTitle(OutputChannel.System, title, c, width);

        /// <summary>
        /// ??
        /// </summary>
        public static void Clear() => _output.Clear();
    }
}
