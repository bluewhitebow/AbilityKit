namespace AbilityKit.Demo.Shooter.View
{
    public interface IShooterViewShellLoader
    {
        object LoadShell(ShooterViewEntityKind kind, int modelId);
    }
}