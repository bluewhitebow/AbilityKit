using System;
using System.Collections.Generic;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// 输入组件 - 管理输入缓冲
    /// 对应 Moba.Console �?ConsoleInputFeature
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ETInputComponent: Entity, IAwake
    {
        // 输入缓冲 (帧号 -> 命令列表)
        private Dictionary<int, List<object>> _inputBuffer = new();

        // 当前输入状�?
        public float MoveTargetX { get; set; }
        public float MoveTargetY { get; set; }
        public int CurrentSkillSlot { get; set; } = -1;
        public float SkillTargetX { get; set; }
        public float SkillTargetY { get; set; }

        public void Awake()
        {
        }

        /// <summary>
        /// 添加移动命令到缓�?
        /// </summary>
        public void AddMoveCommand(int frame, long actorId, float x, float y)
        {
            if (!_inputBuffer.TryGetValue(frame, out var commands))
            {
                commands = new List<object>();
                _inputBuffer[frame] = commands;
            }
            commands.Add(new MoveCommand(frame, actorId, x, y));
        }

        /// <summary>
        /// 添加技能命令到缓冲
        /// </summary>
        public void AddSkillCommand(int frame, long actorId, int skillSlot, float targetX, float targetY)
        {
            if (!_inputBuffer.TryGetValue(frame, out var commands))
            {
                commands = new List<object>();
                _inputBuffer[frame] = commands;
            }
            commands.Add(new SkillCommand(frame, actorId, skillSlot, targetX, targetY));
        }

        /// <summary>
        /// 添加停止命令到缓�?
        /// </summary>
        public void AddStopCommand(int frame, long actorId)
        {
            if (!_inputBuffer.TryGetValue(frame, out var commands))
            {
                commands = new List<object>();
                _inputBuffer[frame] = commands;
            }
            commands.Add(new StopCommand(frame, actorId));
        }

        /// <summary>
        /// 获取指定帧的输入
        /// </summary>
        public List<object>? GetInputsForFrame(int frame)
        {
            return _inputBuffer.TryGetValue(frame, out var commands) ? commands : null;
        }

        /// <summary>
        /// 清除已处理的输入
        /// </summary>
        public void ClearProcessedInputs(int upToFrame)
        {
            var framesToRemove = new List<int>();
            foreach (var frame in _inputBuffer.Keys)
            {
                if (frame <= upToFrame)
                    framesToRemove.Add(frame);
            }
            foreach (var frame in framesToRemove)
            {
                _inputBuffer.Remove(frame);
            }
        }
    }
}
