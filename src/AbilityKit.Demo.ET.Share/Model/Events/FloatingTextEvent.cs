using System;

namespace ET.AbilityKit.Demo.ET.Share
{
    /// <summary>
    /// 飘字事件
    /// </summary>
    public struct FloatingTextEvent
    {
        public long ActorId;
        public string Text;
        public float X;
        public float Y;
        public string TextType;  // "damage", "heal", "miss"
    }
}
