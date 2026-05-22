using System;
using ET.AbilityKit.Demo.ET.Share;
using AbilityKit.Demo.Moba.Share;

namespace ET.Logic
{
    /// <summary>
    /// ????????- ??????
    /// ??????????????
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ETBattleComponent: Entity, IAwake, IUpdate, IDestroy
    {
        // ????
        public long BattleId { get; set; }
        public long PlayerId { get; set; }
        public long PlayerActorId { get; set; }

        // ?????
        public BattleState State { get; set; } = BattleState.Idle;

        // ????????BattleDriver??
        public int CurrentFrame => BattleDriver?.CurrentFrame ?? 0;
        public double LogicTimeSeconds => BattleDriver?.LogicTimeSeconds ?? 0;
        public int TickRate => BattleDriver?.TickRate ?? 30;

        // ?? Sink (??View ????
        public IETViewEventSink ViewSink { get; set; }

        // ???????? IBattleDriver ????
        public IBattleDriver BattleDriver { get; set; }

        public void Awake()
        {
        }

        public void Update(ETBattleComponent self)
        {
        }

        public void OnDestroy(ETBattleComponent self)
        {
        }
    }
}
