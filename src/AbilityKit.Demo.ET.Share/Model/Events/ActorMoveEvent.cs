using System;
using ET;

namespace ET.AbilityKit.Demo.ET.Share
{
    /// <summary>
    /// 单位移动事件
    /// </summary>
    public struct ActorMoveEvent : IEvent
    {
        public Type Type => typeof(ActorMoveEvent);

        /// <summary>
        /// 运行时自增的 ActorId（唯一标识）
        /// </summary>
        public int ActorId;

        /// <summary>
        /// X 坐标
        /// </summary>
        public float X;

        /// <summary>
        /// Y 坐标
        /// </summary>
        public float Y;

        /// <summary>
        /// Z 坐标（高度）
        /// </summary>
        public float Z;

        /// <summary>
        /// 旋转角度（Y 轴旋转）
        /// </summary>
        public float Rotation;
    }
}
