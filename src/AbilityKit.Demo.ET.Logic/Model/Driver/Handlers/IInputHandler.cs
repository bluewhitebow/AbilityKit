using System;
using System.Collections.Generic;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Framework;

namespace ET.Logic
{
    /// <summary>
    /// 输入处理器标记特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class InputHandlerAttribute : Attribute
    {
        public int OpCode { get; }

        public InputHandlerAttribute(int opCode)
        {
            OpCode = opCode;
        }
    }

    /// <summary>
    /// 输入处理器接口
    /// </summary>
    public interface IInputHandler
    {
        int OpCode { get; }
        bool CanHandle(int opCode);
        void Handle(ETMobaBattleDriver driver, int frame, PlayerInputCommand input);
    }

    /// <summary>
    /// 可提交输入的处理器接口
    /// </summary>
    public interface ISubmittableInputHandler : IInputHandler
    {
        void Submit(ETMobaBattleDriver driver, int actorId, float targetX, float targetZ);
    }

    /// <summary>
    /// 可提交技能输入的处理器接口
    /// </summary>
    public interface ISkillInputHandler : IInputHandler
    {
        bool Submit(ETMobaBattleDriver driver, int actorId, int slot, float targetX, float targetZ);
    }

    /// <summary>
    /// 可提交停止输入的处理器接口
    /// </summary>
    public interface IStopInputHandler : IInputHandler
    {
        void Submit(ETMobaBattleDriver driver, int actorId);
    }
}
