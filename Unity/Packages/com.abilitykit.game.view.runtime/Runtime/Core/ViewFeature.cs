namespace AbilityKit.Game.View
{
    public abstract class ViewFeature
    {
        protected IViewBinder _binder;
        protected IViewShellLoader _shellLoader;

        public virtual void Initialize(IViewShellLoader shellLoader)
        {
            _shellLoader = shellLoader;
            _binder = CreateBinder(shellLoader);
        }

        protected abstract IViewBinder CreateBinder(IViewShellLoader shellLoader);

        public virtual void Tick(float deltaTime)
        {
            _binder?.TickInterpolation(deltaTime);
        }

        public virtual void Shutdown()
        {
            _binder?.Clear();
            _binder = null;
            _shellLoader = null;
        }

        public IViewBinder Binder => _binder;
    }
}