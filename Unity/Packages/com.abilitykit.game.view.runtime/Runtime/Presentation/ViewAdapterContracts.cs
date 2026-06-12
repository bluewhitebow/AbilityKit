namespace AbilityKit.Game.View.Presentation
{
    public interface IViewShellLoader
    {
        object LoadShell(int kindId, int modelId);
        void UnloadShell(object shell);
    }

    public interface IViewHandle
    {
        uint EntityId { get; }
        object Shell { get; }
        void Despawn();
    }

    public interface IViewBinder<TViewBatch> : IViewSink<TViewBatch>
        where TViewBatch : struct, IViewBatch
    {
        bool InterpolationEnabled { get; set; }
        void TickInterpolation(float deltaTime);
        void RebindAll();
    }

    public abstract class ViewFeature<TViewBatch>
        where TViewBatch : struct, IViewBatch
    {
        protected IViewBinder<TViewBatch>? Binder { get; private set; }
        protected IViewShellLoader? ShellLoader { get; private set; }

        public virtual void Initialize(IViewShellLoader shellLoader)
        {
            if (shellLoader == null) throw new System.ArgumentNullException(nameof(shellLoader));

            ShellLoader = shellLoader;
            Binder = CreateBinder(shellLoader);
        }

        protected abstract IViewBinder<TViewBatch> CreateBinder(IViewShellLoader shellLoader);

        public virtual void Tick(float deltaTime)
        {
            Binder?.TickInterpolation(deltaTime);
        }

        public virtual void Shutdown()
        {
            Binder?.Clear();
            Binder = null;
            ShellLoader = null;
        }
    }
}
