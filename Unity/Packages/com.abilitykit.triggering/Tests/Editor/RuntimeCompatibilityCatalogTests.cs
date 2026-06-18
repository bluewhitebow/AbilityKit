using System.Collections.Generic;
using AbilityKit.Triggering.Validation;
using NUnit.Framework;

namespace AbilityKit.Triggering.Tests
{
    public sealed class RuntimeCompatibilityCatalogTests
    {
        [Test]
        public void TryGetEntry_ReturnsReplacementForRootCompatibilityEntry()
        {
            var found = RuntimeCompatibilityCatalog.TryGetEntry("TriggerRunner.cs", out var entry);

            Assert.That(found, Is.True);
            Assert.That(entry.ReplacementPath, Is.EqualTo("Runtime/Runtime/TriggerRunner.cs"));
            Assert.That(entry.Status, Is.EqualTo(ECompatibilityEntryStatus.Compatibility));
            Assert.That(entry.IsRemovalCandidate, Is.False);
        }

        [Test]
        public void TryGetEntry_MarksLegacyDispatcherAsRemovalCandidate()
        {
            var found = RuntimeCompatibilityCatalog.TryGetEntry("TriggerDispatcherHub.cs", out var entry);

            Assert.That(found, Is.True);
            Assert.That(entry.Status, Is.EqualTo(ECompatibilityEntryStatus.Legacy));
            Assert.That(entry.IsRemovalCandidate, Is.True);
            Assert.That(entry.RemovalGate, Does.Contain("Unity meta GUID"));
        }

        [Test]
        public void Entries_ContainDeprecatedRootHubNewEntry()
        {
            var found = RuntimeCompatibilityCatalog.TryGetEntry("TriggerDispatcherHub_new.cs", out var entry);

            Assert.That(found, Is.True);
            Assert.That(entry.ReplacementPath, Is.Empty);
            Assert.That(entry.Status, Is.EqualTo(ECompatibilityEntryStatus.Deprecated));
            Assert.That(entry.IsRemovalCandidate, Is.True);
        }

        [Test]
        public void ScanRootEntries_ReturnsValidWhenActualFilesMatchCatalog()
        {
            var fileNames = new List<string>();
            foreach (var entry in RuntimeCompatibilityCatalog.Entries)
            {
                fileNames.Add(entry.EntryPath);
            }

            var result = RuntimeCompatibilityCatalog.ScanRootEntries(fileNames);

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Issues, Is.Empty);
        }

        [Test]
        public void ScanRootEntries_ReportsMissingCatalogEntry()
        {
            var fileNames = new List<string>();
            foreach (var entry in RuntimeCompatibilityCatalog.Entries)
            {
                fileNames.Add(entry.EntryPath);
            }
            fileNames.Add("NewRootCompatibilityEntry.cs");

            var result = RuntimeCompatibilityCatalog.ScanRootEntries(fileNames);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues, Has.Some.Matches<RuntimeCompatibilityScanIssue>(issue =>
                issue.Code == ValidationErrorCodes.RUNTIME_COMPATIBILITY_ENTRY_MISSING &&
                issue.EntryPath == "NewRootCompatibilityEntry.cs"));
        }

        [Test]
        public void ScanRootEntries_ReportsStaleCatalogEntry()
        {
            var fileNames = new List<string>();
            foreach (var entry in RuntimeCompatibilityCatalog.Entries)
            {
                if (entry.EntryPath != "TriggerRunner.cs")
                {
                    fileNames.Add(entry.EntryPath);
                }
            }

            var result = RuntimeCompatibilityCatalog.ScanRootEntries(fileNames);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues, Has.Some.Matches<RuntimeCompatibilityScanIssue>(issue =>
                issue.Code == ValidationErrorCodes.RUNTIME_COMPATIBILITY_ENTRY_STALE &&
                issue.EntryPath == "TriggerRunner.cs"));
        }

        [Test]
        public void ScanRootEntries_ConvertsIssuesToValidationWarnings()
        {
            var result = RuntimeCompatibilityCatalog.ScanRootEntries(new[] { "UnknownRootEntry.cs" });

            var validation = result.ToValidationResult();

            Assert.That(validation.IsValid, Is.True);
            Assert.That(validation.Warnings, Has.Some.Matches<ValidationIssue>(issue =>
                issue.Code == ValidationErrorCodes.RUNTIME_COMPATIBILITY_ENTRY_MISSING));
        }
    }
}
