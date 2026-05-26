using System;
using AbilityKit.Ability.Host.Framework;
using AbilityKit.Demo.Moba.Share;

namespace ET.Logic
{
    /// <summary>
    /// 生命周期处理器阶段
    /// </summary>
    public enum LifecyclePhase
    {
        Initialize,
        Start,
        Tick,
        Stop,
        Destroy
    }

    /// <summary>
    /// 生命周期处理器标记特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class LifecycleHandlerAttribute : Attribute
    {
        public LifecyclePhase Phase { get; }

        public LifecycleHandlerAttribute(LifecyclePhase phase)
        {
            Phase = phase;
        }
    }

    /// <summary>
    /// 生命周期处理器接口
    /// </summary>
    public interface ILifecycleHandler
    {
        LifecyclePhase Phase { get; }
    }

    /// <summary>
    /// 初始化阶段处理器接口
    /// </summary>
    public interface IInitializeHandler : ILifecycleHandler
    {
        void Handle(ETMobaBattleDriver driver, in BattleStartPlan plan, IBattleViewEventSink viewSink);
    }

    /// <summary>
    /// 启动阶段处理器接口
    /// </summary>
    public interface IStartHandler : ILifecycleHandler
    {
        void Handle(ETMobaBattleDriver driver);
    }

    /// <summary>
    /// 帧推进处理器接口
    /// </summary>
    public interface ITickHandler : ILifecycleHandler
    {
        void Handle(ETMobaBattleDriver driver, float deltaTime);
    }

    /// <summary>
    /// 停止阶段处理器接口
    /// </summary>
    public interface IStopHandler : ILifecycleHandler
    {
        void Handle(ETMobaBattleDriver driver);
    }

    /// <summary>
    /// 销毁阶段处理器接口
    /// </summary>
    public interface IDestroyHandler : ILifecycleHandler
    {
        void Handle(ETMobaBattleDriver driver);
    }
}
