namespace AbilityKit.Demo.Shooter.View
{
    public interface IShooterViewFeature
    {
        IShooterViewBinder Binder { get; set; }
        void OnAttach();
        void OnDetach();
        void Tick(float deltaTime);
    }
}