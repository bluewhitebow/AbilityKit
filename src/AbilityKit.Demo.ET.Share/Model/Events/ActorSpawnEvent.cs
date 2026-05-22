using System;

namespace ET.AbilityKit.Demo.ET.Share
{
    /// <summary>
    /// 单位生成事件
    /// </summary>
    public struct ActorSpawnEvent
    {
        public long ActorId;
        public int EntityCode;
        public ActorKind Kind;
        public string Name;
        public float X;
        public float Y;
        public float MaxHp;
        public bool IsLocalPlayer;
    }
}
