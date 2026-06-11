#nullable enable

using System;

namespace AbilityKit.Demo.Shooter.View
{
    public sealed class ShooterPresentationSessionContext
    {
        private int _retainCount;

        public ShooterPresentationSessionContext(ShooterPresentationFacade presentation)
            : this(presentation, null)
        {
        }

        public ShooterPresentationSessionContext(ShooterPresentationFacade presentation, IShooterSnapshotViewSink? viewSink)
        {
            Presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
            View = new ShooterSnapshotViewBinder(Presentation, viewSink);
        }

        public ShooterPresentationFacade Presentation { get; }

        public ShooterSnapshotViewBinder View { get; }

        public int RetainCount => _retainCount;

        internal void Retain()
        {
            _retainCount++;
        }

        internal bool Release()
        {
            if (_retainCount > 0)
            {
                _retainCount--;
            }

            if (_retainCount == 0)
            {
                View.Dispose();
                return true;
            }

            return false;
        }

        public static ShooterPresentationSessionContext CreateDefault()
        {
            return CreateDefault(null);
        }

        public static ShooterPresentationSessionContext CreateDefault(IShooterSnapshotViewSink? viewSink)
        {
            return new ShooterPresentationSessionContext(new ShooterPresentationFacade(), viewSink);
        }

        public static ShooterPresentationSessionContext CreateFromFacade(ShooterPresentationFacade presentation)
        {
            return CreateFromFacade(presentation, null);
        }

        public static ShooterPresentationSessionContext CreateFromFacade(ShooterPresentationFacade presentation, IShooterSnapshotViewSink? viewSink)
        {
            return new ShooterPresentationSessionContext(presentation, viewSink);
        }
    }
}
