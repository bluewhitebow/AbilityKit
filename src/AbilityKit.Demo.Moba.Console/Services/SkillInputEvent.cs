using System;

namespace AbilityKit.Demo.Moba.Console.Services
{
    /// <summary>
    /// 技能输入事件
    /// 对应 Unity 项目的 SkillInputEvent
    /// </summary>
    public readonly struct SkillInputEvent
    {
        /// <summary>
        /// 技能槽位 (1/2/3)
        /// </summary>
        public int Slot { get; }

        /// <summary>
        /// 输入阶段
        /// </summary>
        public SkillInputPhase Phase { get; }

        /// <summary>
        /// 目标 Actor ID
        /// </summary>
        public int TargetActorId { get; }

        /// <summary>
        /// 瞄准位置 X
        /// </summary>
        public float AimX { get; }

        /// <summary>
        /// 瞄准位置 Z
        /// </summary>
        public float AimZ { get; }

        /// <summary>
        /// 瞄准方向 X
        /// </summary>
        public float DirX { get; }

        /// <summary>
        /// 瞄准方向 Z
        /// </summary>
        public float DirZ { get; }

        /// <summary>
        /// 操作码
        /// </summary>
        public int OpCode { get; }

        /// <summary>
        /// 额外数据
        /// </summary>
        public byte[] Payload { get; }

        public SkillInputEvent(
            int slot,
            SkillInputPhase phase,
            int targetActorId = 0,
            float aimX = 0,
            float aimZ = 0,
            float dirX = 0,
            float dirZ = 0,
            int opCode = 0,
            byte[] payload = null)
        {
            Slot = slot;
            Phase = phase;
            TargetActorId = targetActorId;
            AimX = aimX;
            AimZ = aimZ;
            DirX = dirX;
            DirZ = dirZ;
            OpCode = opCode;
            Payload = payload;
        }

        /// <summary>
        /// 创建按下事件
        /// </summary>
        public static SkillInputEvent CreatePress(int slot)
        {
            return new SkillInputEvent(slot, SkillInputPhase.Press);
        }

        /// <summary>
        /// 创建按住事件
        /// </summary>
        public static SkillInputEvent CreateHold(int slot)
        {
            return new SkillInputEvent(slot, SkillInputPhase.Hold);
        }

        /// <summary>
        /// 创建释放事件（带瞄准位置）
        /// </summary>
        public static SkillInputEvent CreateRelease(int slot, float aimX, float aimZ, float dirX = 0, float dirZ = 0)
        {
            return new SkillInputEvent(slot, SkillInputPhase.Release, aimX: aimX, aimZ: aimZ, dirX: dirX, dirZ: dirZ);
        }

        /// <summary>
        /// 创建取消事件
        /// </summary>
        public static SkillInputEvent CreateCancel(int slot)
        {
            return new SkillInputEvent(slot, SkillInputPhase.Cancel);
        }

        /// <summary>
        /// 创建瞄准释放事件（带目标）
        /// </summary>
        public static SkillInputEvent CreateTargetRelease(int slot, int targetActorId, float aimX, float aimZ)
        {
            return new SkillInputEvent(slot, SkillInputPhase.Release, targetActorId, aimX, aimZ);
        }

        public override string ToString()
        {
            return $"SkillInput(Slot={Slot}, Phase={Phase}, Target={TargetActorId}, Aim=({AimX:F2},{AimZ:F2}))";
        }
    }
}
