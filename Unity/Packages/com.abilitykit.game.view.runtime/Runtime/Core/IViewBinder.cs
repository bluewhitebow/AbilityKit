using System.Numerics;

namespace AbilityKit.Game.View
{
    public interface IViewBinder
    {
        void Sync(object entity);
        void TickInterpolation(float deltaTime);
        void Clear();
        void RebindAll();
    }
}