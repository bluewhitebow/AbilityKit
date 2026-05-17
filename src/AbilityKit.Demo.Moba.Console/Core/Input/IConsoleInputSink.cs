using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;

namespace AbilityKit.Demo.Moba.Console.Core.Input
{
    /// <summary>
    /// 表现层输入服务接口
    ///
    /// 表现层通过此接口将玩家输入转发到逻辑层
    ///
    /// 架构说明：
    /// - 表现层持有此接口，不关心具体实现
    /// - 逻辑层提供实现（如 MobaLobbyInputSink 或 Console 适配器）
    /// - 表现层只调用 Submit() 方法，不执行任何逻辑
    /// </summary>
    public interface IConsoleInputSink
    {
        /// <summary>
        /// 提交输入命令到逻辑层
        /// </summary>
        /// <param name="frame">帧号</param>
        /// <param name="inputs">输入命令列表</param>
        void Submit(FrameIndex frame, IReadOnlyList<PlayerInputCommand> inputs);
    }
}
