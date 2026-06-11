#nullable enable

using System;

namespace AbilityKit.Demo.Shooter.View
{
    public sealed class ShooterSnapshotViewBinder : IDisposable
    {
        private readonly ShooterPresentationFacade _presentation;
        private readonly IShooterSnapshotViewSink _sink;
        private bool _disposed;

        public ShooterSnapshotViewBinder(ShooterPresentationFacade presentation)
            : this(presentation, null)
        {
        }

        public ShooterSnapshotViewBinder(ShooterPresentationFacade presentation, IShooterSnapshotViewSink? sink)
        {
            _presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
            _sink = sink ?? ShooterNullSnapshotViewSink.Instance;
            _presentation.Snapshots.SnapshotApplied += OnSnapshotApplied;
        }

        public void Sync(in ShooterSnapshotViewBatch batch)
        {
            _sink.ApplySnapshot(in batch);
        }

        public void Clear()
        {
            _sink.Clear();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _presentation.Snapshots.SnapshotApplied -= OnSnapshotApplied;
            Clear();
        }

        private void OnSnapshotApplied(ShooterSnapshotViewBatch batch)
        {
            Sync(in batch);
        }
    }
}
