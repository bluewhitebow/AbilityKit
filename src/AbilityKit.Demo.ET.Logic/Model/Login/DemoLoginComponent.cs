using System;

namespace ET.AbilityKit.Demo.ET.Logic
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
    /// 登录组件
    /// 负责处理登录流程
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class DemoLoginComponent: Entity, IAwake
    {
        /// <summary>
        /// 当前登录状态
        /// </summary>
        public LoginState State { get; set; } = LoginState.Idle;
        
        /// <summary>
        /// 当前玩家 ID
        /// </summary>
        public long PlayerId { get; set; }
        
        /// <summary>
        /// 当前玩家名称
        /// </summary>
        public string PlayerName { get; set; }
        
        public void Awake()
        {
            Log.Info($"[DemoLogin] DemoLoginComponent awake");
        }
        
        /// <summary>
        /// 开始登录
        /// </summary>
        public void StartLogin(string playerName)
        {
            if (State != LoginState.Idle)
            {
                Log.Info($"[DemoLogin] Already logging in, current state: {State}");
                return;
            }
            
            State = LoginState.Connecting;
            Log.Info($"[DemoLogin] Connecting to server... player: {playerName}");
            
            // 模拟网络延迟后登录成功
            OnLoginSuccess(playerName);
        }
        
        /// <summary>
        /// 登录成功回调
        /// </summary>
        private void OnLoginSuccess(string playerName)
        {
            State = LoginState.LoginSuccess;
            PlayerId = IdGenerater.Instance.GenerateId();
            PlayerName = playerName;
            
            Log.Info($"[DemoLogin] Login success! PlayerId: {PlayerId}, Name: {PlayerName}");
            
            // 发布登录完成事件
            EventSystem.Instance.Publish<Scene, DemoLoginFinish>(this.Scene(), new DemoLoginFinish()
            {
                PlayerId = PlayerId,
                PlayerName = PlayerName
            });
        }
        
        /// <summary>
        /// 请求进入战斗
        /// </summary>
        public void RequestEnterBattle()
        {
            if (State != LoginState.LoginSuccess)
            {
                Log.Info($"[DemoLogin] Cannot enter battle, not logged in");
                return;
            }
            
            Log.Info($"[DemoLogin] Requesting to enter battle...");
            
            // 发布进入战斗请求事件
            EventSystem.Instance.Publish<Scene, DemoRequestEnterBattle>(this.Scene(), new DemoRequestEnterBattle()
            {
                PlayerId = PlayerId,
                PlayerName = PlayerName
            });
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
