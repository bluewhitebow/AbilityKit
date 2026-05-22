using System;

namespace ET.Logic
{
    /// <summary>
    /// 登录状态
    /// </summary>
    public enum LoginState
    {
        Idle,
        Connecting,
        LoginSuccess,
        LoginFail,
    }

    /// <summary>
    /// 登录组件 - 只定义数据
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class DemoLoginComponent: Entity, IAwake
    {
        public LoginState State { get; set; } = LoginState.Idle;
        public long PlayerId { get; set; }
        public string PlayerName { get; set; }

        public void Awake()
        {
        }
    }

    /// <summary>
    /// 登录完成事件
    /// </summary>
    public struct DemoLoginFinish: IEvent
    {
        public Type Type => typeof(DemoLoginFinish);

        public long PlayerId;
        public string PlayerName;
    }

    /// <summary>
    /// 请求进入战斗事件
    /// </summary>
    public struct DemoRequestEnterBattle: IEvent
    {
        public Type Type => typeof(DemoRequestEnterBattle);

        public long PlayerId;
        public string PlayerName;
    }
}
