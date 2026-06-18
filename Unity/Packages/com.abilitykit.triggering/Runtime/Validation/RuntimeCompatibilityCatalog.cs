using System;
using System.Collections.Generic;

namespace AbilityKit.Triggering.Validation
{
    public enum ECompatibilityEntryStatus : byte
    {
        Formal = 0,
        Compatibility = 1,
        Legacy = 2,
        Deprecated = 3,
    }

    public readonly struct CompatibilityEntry
    {
        public readonly string EntryPath;
        public readonly string ReplacementPath;
        public readonly ECompatibilityEntryStatus Status;
        public readonly string RemovalGate;

        public CompatibilityEntry(string entryPath, string replacementPath, ECompatibilityEntryStatus status, string removalGate)
        {
            EntryPath = entryPath;
            ReplacementPath = replacementPath;
            Status = status;
            RemovalGate = removalGate;
        }

        public bool IsRemovalCandidate => Status == ECompatibilityEntryStatus.Deprecated || Status == ECompatibilityEntryStatus.Legacy;
    }

    public readonly struct RuntimeCompatibilityScanIssue
    {
        public readonly string EntryPath;
        public readonly string Code;
        public readonly string Message;

        public RuntimeCompatibilityScanIssue(string entryPath, string code, string message)
        {
            EntryPath = entryPath;
            Code = code;
            Message = message;
        }
    }

    public readonly struct RuntimeCompatibilityScanResult
    {
        private readonly RuntimeCompatibilityScanIssue[] _issues;

        public RuntimeCompatibilityScanResult(RuntimeCompatibilityScanIssue[] issues)
        {
            _issues = issues ?? Array.Empty<RuntimeCompatibilityScanIssue>();
        }

        public IReadOnlyList<RuntimeCompatibilityScanIssue> Issues => _issues ?? Array.Empty<RuntimeCompatibilityScanIssue>();
        public bool IsValid => Issues.Count == 0;

        public ValidationResult ToValidationResult(string path = "$.runtimeCompatibility")
        {
            var result = ValidationResult.Success;
            for (int i = 0; i < Issues.Count; i++)
            {
                var issue = Issues[i];
                result.AddWarning(issue.Code, issue.Message, $"{path}.{issue.EntryPath}");
            }

            return result;
        }
    }

    public static class RuntimeCompatibilityCatalog
    {
        public const string DefaultRemovalGate = "No package references, external samples migrated, Unity meta GUID no longer referenced, and a major compatibility cleanup batch is active.";

        private static readonly CompatibilityEntry[] _entries =
        {
            Entry("ActionContext.cs", "Runtime/Context/ActionContext.cs", ECompatibilityEntryStatus.Compatibility),
            Entry("ActionDelegateAdapter.cs", "Runtime/ActionScheduler/ActionDelegateAdapter.cs", ECompatibilityEntryStatus.Deprecated),
            Entry("ActionExecutor.cs", "Runtime/ActionScheduler/ActionExecutor.cs", ECompatibilityEntryStatus.Compatibility),
            Entry("ActionInstance.cs", "Runtime/ActionScheduler/ActionInstance.cs", ECompatibilityEntryStatus.Compatibility),
            Entry("ActionScheduler.cs", "Runtime/ActionScheduler/ActionScheduler.cs", ECompatibilityEntryStatus.Compatibility),
            Entry("ContextAdapter.cs", "Runtime/Context/ContextAdapter.cs", ECompatibilityEntryStatus.Compatibility),
            Entry("EventBusDispatcher.cs", "Runtime/Dispatcher/EventBusDispatcher.cs", ECompatibilityEntryStatus.Legacy),
            Entry("ExecCtxAdapter.cs", "Runtime/Context/ExecCtxAdapter.cs", ECompatibilityEntryStatus.Compatibility),
            Entry("ITriggerDispatcher.cs", "Runtime/Dispatcher/ITriggerDispatcher.cs", ECompatibilityEntryStatus.Legacy),
            Entry("NumericValueRefContextExtensions.cs", "Runtime/Variables/Numeric/NumericValueRefContextExtensions.cs", ECompatibilityEntryStatus.Compatibility),
            Entry("PlannedTrigger.cs", "Runtime/Plan/PlannedTrigger.cs", ECompatibilityEntryStatus.Compatibility),
            Entry("TimedDispatcher.cs", "Runtime/Dispatcher/TimedDispatcher.cs", ECompatibilityEntryStatus.Legacy),
            Entry("TriggerDispatcherHub.cs", "Runtime/Dispatcher/TriggerDispatcherHub.cs", ECompatibilityEntryStatus.Legacy),
            Entry("TriggerDispatcherHub_new.cs", "", ECompatibilityEntryStatus.Deprecated),
            Entry("TriggerExecutor.cs", "Runtime/Legacy/TriggerScheduler/TriggerExecutor.cs", ECompatibilityEntryStatus.Legacy),
            Entry("TriggerRunner.cs", "Runtime/Runtime/TriggerRunner.cs", ECompatibilityEntryStatus.Compatibility),
        };

        public static IReadOnlyList<CompatibilityEntry> Entries => _entries;

        public static bool TryGetEntry(string entryPath, out CompatibilityEntry entry)
        {
            if (!string.IsNullOrEmpty(entryPath))
            {
                for (int i = 0; i < _entries.Length; i++)
                {
                    if (string.Equals(_entries[i].EntryPath, entryPath, StringComparison.OrdinalIgnoreCase))
                    {
                        entry = _entries[i];
                        return true;
                    }
                }
            }

            entry = default;
            return false;
        }

        public static RuntimeCompatibilityScanResult ScanRootEntries(IEnumerable<string> rootRuntimeFileNames)
        {
            var issues = new List<RuntimeCompatibilityScanIssue>();
            var actual = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (rootRuntimeFileNames != null)
            {
                foreach (var fileName in rootRuntimeFileNames)
                {
                    var normalized = NormalizeRootFileName(fileName);
                    if (!string.IsNullOrEmpty(normalized) && normalized.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    {
                        actual.Add(normalized);
                    }
                }
            }

            foreach (var fileName in actual)
            {
                if (!TryGetEntry(fileName, out _))
                {
                    issues.Add(new RuntimeCompatibilityScanIssue(
                        fileName,
                        ValidationErrorCodes.RUNTIME_COMPATIBILITY_ENTRY_MISSING,
                        $"Root Runtime compatibility entry is not registered: {fileName}"));
                }
            }

            for (int i = 0; i < _entries.Length; i++)
            {
                var entry = _entries[i];
                if (!actual.Contains(entry.EntryPath))
                {
                    issues.Add(new RuntimeCompatibilityScanIssue(
                        entry.EntryPath,
                        ValidationErrorCodes.RUNTIME_COMPATIBILITY_ENTRY_STALE,
                        $"Registered root Runtime compatibility entry is not present on disk: {entry.EntryPath}"));
                }
            }

            return new RuntimeCompatibilityScanResult(issues.ToArray());
        }

        private static string NormalizeRootFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;
            var normalized = fileName.Replace('\\', '/');
            var index = normalized.LastIndexOf('/');
            return index >= 0 ? normalized.Substring(index + 1) : normalized;
        }

        private static CompatibilityEntry Entry(string entryPath, string replacementPath, ECompatibilityEntryStatus status)
            => new CompatibilityEntry(entryPath, replacementPath, status, DefaultRemovalGate);
    }
}
