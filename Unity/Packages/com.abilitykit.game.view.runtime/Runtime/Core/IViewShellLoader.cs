namespace AbilityKit.Game.View
{
    public interface IViewShellLoader
    {
        object LoadShell(int modelId);
        void UnloadShell(object shell);
    }
}