using System;
using System.Collections.Generic;
using System.Text;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;

namespace AbilityKit.Demo.Moba.Services
{
    public enum MobaRuntimeValidationSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2,
    }

    public readonly struct MobaRuntimeValidationEntry
    {
        public readonly MobaRuntimeValidationSeverity Severity;
        public readonly string Source;
        public readonly string Path;
        public readonly string Message;
        public readonly string BusinessId;
        public readonly bool BlocksStartup;

        public MobaRuntimeValidationEntry(
            MobaRuntimeValidationSeverity severity,
            string source,
            string path,
            string message,
            string businessId = null,
            bool blocksStartup = true)
        {
            Severity = severity;
            Source = string.IsNullOrEmpty(source) ? "unknown" : source;
            Path = string.IsNullOrEmpty(path) ? "runtime" : path;
            Message = message ?? string.Empty;
            BusinessId = businessId;
            BlocksStartup = blocksStartup;
        }
    }

    public sealed class MobaRuntimeValidationReport
    {
        private readonly List<MobaRuntimeValidationEntry> _entries = new List<MobaRuntimeValidationEntry>(64);

        public IReadOnlyList<MobaRuntimeValidationEntry> Entries => _entries;
        public int ErrorCount { get; private set; }
        public int WarningCount { get; private set; }
        public int InfoCount { get; private set; }
        public bool HasErrors => ErrorCount > 0;
        public bool ShouldBlockStartup { get; private set; }

        public void Info(string source, string path, string message, string businessId = null)
        {
            Add(new MobaRuntimeValidationEntry(MobaRuntimeValidationSeverity.Info, source, path, message, businessId, blocksStartup: false));
        }

        public void Warning(string source, string path, string message, string businessId = null)
        {
            Add(new MobaRuntimeValidationEntry(MobaRuntimeValidationSeverity.Warning, source, path, message, businessId, blocksStartup: false));
        }

        public void Error(string source, string path, string message, string businessId = null, bool blocksStartup = true)
        {
            Add(new MobaRuntimeValidationEntry(MobaRuntimeValidationSeverity.Error, source, path, message, businessId, blocksStartup));
        }

        public void Add(in MobaRuntimeValidationEntry entry)
        {
            _entries.Add(entry);
            switch (entry.Severity)
            {
                case MobaRuntimeValidationSeverity.Error:
                    ErrorCount++;
                    if (entry.BlocksStartup) ShouldBlockStartup = true;
                    break;
                case MobaRuntimeValidationSeverity.Warning:
                    WarningCount++;
                    break;
                default:
                    InfoCount++;
                    break;
            }
        }

        public string FormatSummary()
        {
            return $"errors={ErrorCount}, warnings={WarningCount}, infos={InfoCount}, blockStartup={ShouldBlockStartup}";
        }

        public string FormatEntry(in MobaRuntimeValidationEntry entry)
        {
            var business = string.IsNullOrEmpty(entry.BusinessId) ? string.Empty : $" businessId={entry.BusinessId}";
            return $"[MobaValidation] {entry.Severity} source={entry.Source} path={entry.Path}{business} {entry.Message}";
        }

        public string FormatAllEntries(int maxEntries = 32)
        {
            if (_entries.Count == 0) return string.Empty;

            var limit = maxEntries <= 0 ? _entries.Count : Math.Min(maxEntries, _entries.Count);
            var sb = new StringBuilder(limit * 96);
            for (int i = 0; i < limit; i++)
            {
                if (i > 0) sb.AppendLine();
                sb.Append(FormatEntry(_entries[i]));
            }

            if (limit < _entries.Count)
            {
                sb.AppendLine();
                sb.Append("[MobaValidation] further entries suppressed. remaining=").Append(_entries.Count - limit);
            }

            return sb.ToString();
        }
    }

    public readonly struct MobaRuntimeValidationContext
    {
        public readonly IWorldResolver Services;
        public readonly string StageName;

        public MobaRuntimeValidationContext(IWorldResolver services, string stageName)
        {
            Services = services;
            StageName = string.IsNullOrEmpty(stageName) ? "runtime" : stageName;
        }

        public bool TryResolve<T>(out T service) where T : class
        {
            service = null;
            return Services != null && Services.TryResolve(out service) && service != null;
        }
    }

    public interface IMobaRuntimeValidator
    {
        string Name { get; }
        void Validate(in MobaRuntimeValidationContext context, MobaRuntimeValidationReport report);
    }

    public interface IMobaRuntimeValidationRegistry
    {
        void Register(IMobaRuntimeValidator validator);
        IReadOnlyList<IMobaRuntimeValidator> Validators { get; }
    }

    public interface IMobaRuntimeValidationRunner
    {
        MobaRuntimeValidationReport ValidateAll(in MobaRuntimeValidationContext context);
    }

    [WorldService(typeof(IMobaRuntimeValidationRegistry), WorldLifetime.Scoped)]
    [WorldService(typeof(IMobaRuntimeValidationRunner), WorldLifetime.Scoped)]
    [WorldService(typeof(MobaRuntimeValidationService), WorldLifetime.Scoped)]
    public sealed class MobaRuntimeValidationService : IMobaRuntimeValidationRegistry, IMobaRuntimeValidationRunner, IService
    {
        private readonly List<IMobaRuntimeValidator> _validators = new List<IMobaRuntimeValidator>(16);
        private readonly HashSet<string> _validatorNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<IMobaRuntimeValidator> Validators => _validators;

        public void Register(IMobaRuntimeValidator validator)
        {
            if (validator == null) return;

            var name = string.IsNullOrEmpty(validator.Name) ? validator.GetType().Name : validator.Name;
            if (!_validatorNames.Add(name)) return;
            _validators.Add(validator);
        }

        public MobaRuntimeValidationReport ValidateAll(in MobaRuntimeValidationContext context)
        {
            var report = new MobaRuntimeValidationReport();
            for (int i = 0; i < _validators.Count; i++)
            {
                var validator = _validators[i];
                try
                {
                    validator.Validate(in context, report);
                }
                catch (Exception ex)
                {
                    report.Error(validator.Name, context.StageName, "validator exception: " + ex.Message);
                    MobaRuntimeLog.Exception(ex, MobaRuntimeLogModule.Diagnostics, MobaRuntimeLogPurpose.Exception, nameof(MobaRuntimeValidationService), $"Validator failed. name={validator.Name}");
                }
            }

            WriteReport(report);
            return report;
        }

        public void Dispose()
        {
            _validators.Clear();
            _validatorNames.Clear();
        }

        private static void WriteReport(MobaRuntimeValidationReport report)
        {
            if (report == null) return;

            if (report.Entries.Count == 0)
            {
                MobaRuntimeLog.Info(MobaRuntimeLogModule.Diagnostics, MobaRuntimeLogPurpose.Validation, nameof(MobaRuntimeValidationService), "Runtime validation completed. no issues.");
                return;
            }

            for (int i = 0; i < report.Entries.Count; i++)
            {
                var entry = report.Entries[i];
                var text = report.FormatEntry(in entry);
                switch (entry.Severity)
                {
                    case MobaRuntimeValidationSeverity.Error:
                        MobaRuntimeLog.Error(MobaRuntimeLogModule.Diagnostics, MobaRuntimeLogPurpose.Validation, nameof(MobaRuntimeValidationService), text);
                        break;
                    case MobaRuntimeValidationSeverity.Warning:
                        MobaRuntimeLog.Warning(MobaRuntimeLogModule.Diagnostics, MobaRuntimeLogPurpose.Validation, nameof(MobaRuntimeValidationService), text);
                        break;
                    default:
                        MobaRuntimeLog.Info(MobaRuntimeLogModule.Diagnostics, MobaRuntimeLogPurpose.Validation, nameof(MobaRuntimeValidationService), text);
                        break;
                }
            }

            MobaRuntimeLog.Warning(MobaRuntimeLogModule.Diagnostics, MobaRuntimeLogPurpose.Validation, nameof(MobaRuntimeValidationService), "Runtime validation completed. " + report.FormatSummary());
        }
    }
}
