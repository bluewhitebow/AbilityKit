#nullable enable

using System;

namespace AbilityKit.Demo.Shooter.View
{
    public static class ShooterPresentationSessionHost
    {
        private static readonly ShooterPresentationSessionResolver Resolver = new ShooterPresentationSessionResolver();
        private static ShooterPresentationSessionContext? _current;

        public static event Action<ShooterPresentationSessionContext?>? SessionChanged;

        public static ShooterPresentationSessionContext? Current => _current;

        public static bool HasSession => _current != null;

        public static ShooterPresentationSessionContext Start(ShooterPresentationSessionContext? context = null)
        {
            Stop();
            _current = Resolver.Resolve(context);
            SessionChanged?.Invoke(_current);
            return _current;
        }

        public static ShooterPresentationSessionContext Start(ShooterPresentationFacade presentation)
        {
            if (presentation == null)
            {
                throw new ArgumentNullException(nameof(presentation));
            }

            return Start(ShooterPresentationSessionContext.CreateFromFacade(presentation));
        }

        public static void Stop()
        {
            if (_current == null)
            {
                return;
            }

            var stopped = _current;
            _current = null;
            Resolver.Release(stopped);
            SessionChanged?.Invoke(null);
        }
    }
}
