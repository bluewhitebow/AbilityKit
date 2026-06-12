namespace AbilityKit.Demo.Shooter.View
{
    public class ShooterViewResourceProvider
    {
        private static ShooterViewResourceProvider _default;

        public static ShooterViewResourceProvider Default => 
            _default ??= new ShooterViewResourceProvider();

        public static ShooterViewResourceProvider OrDefault(ShooterViewResourceProvider provider)
        {
            return provider ?? Default;
        }

        public object LoadModelPrefab(int modelId)
        {
            return null;
        }

        public object LoadProjectilePrefab(int projectileId)
        {
            return null;
        }
    }
}