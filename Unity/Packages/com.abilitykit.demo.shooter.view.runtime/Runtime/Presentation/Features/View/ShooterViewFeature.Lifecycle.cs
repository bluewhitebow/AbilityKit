namespace AbilityKit.Demo.Shooter.View
{
    public sealed partial class ShooterViewFeature
    {
        public void OnAttach()
        {
            Binder?.Clear();
            Binder = new ShooterViewBinder();
        }

        public void OnDetach()
        {
            Binder?.Clear();
            Binder = null;
        }

        public void Tick(float deltaTime)
        {
            Binder?.TickInterpolation(deltaTime);
        }
    }
}