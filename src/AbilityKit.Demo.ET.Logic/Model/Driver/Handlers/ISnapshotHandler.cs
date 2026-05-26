using AbilityKit.Ability.Host.Framework;
using AbilityKit.Demo.Moba.Share;

namespace ET.Logic
{
    /// <summary>
    /// 快照处理器标记特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class SnapshotHandlerAttribute : Attribute
    {
        /// <summary>
        /// 快照类型
        /// </summary>
        public SnapshotType Type { get; }

        public SnapshotHandlerAttribute(SnapshotType type)
        {
            Type = type;
        }
    }

    /// <summary>
    /// 快照处理器接口
    /// </summary>
    public interface ISnapshotHandler
    {
        SnapshotType SnapshotType { get; }
        bool CanHandle(in FrameSnapshotData snapshot);
        void Handle(ETMobaBattleDriver driver, in FrameSnapshotData snapshot);
    }
}
