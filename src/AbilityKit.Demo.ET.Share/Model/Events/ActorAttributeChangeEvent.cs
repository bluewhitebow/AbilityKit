using System;

namespace ET.AbilityKit.Demo.ET.Share
{
    /// <summary>
    /// 单位属性变化事件
    /// </summary>
    public struct ActorAttributeChangeEvent
    {
        public long ActorId;
        public string AttributeName;
        public float OldValue;
        public float NewValue;
    }
}
