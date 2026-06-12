using System;

namespace AbilityKit.Game.Flow
{
    internal static class MobaFlowActionIds
    {
        public const string ResetBattleSessionRuntimeState = "battle.reset_session_runtime_state";
        public const string ReturnLobbyAfterBattleEnd = "battle.return_lobby_after_end";
    }

    internal readonly struct MobaFlowActionContext
    {
        public MobaFlowActionContext(GameFlowDomain domain, int installedCount = 0)
        {
            Domain = domain ?? throw new ArgumentNullException(nameof(domain));
            InstalledCount = installedCount;
        }

        public GameFlowDomain Domain { get; }
        public int InstalledCount { get; }
    }

    internal sealed class MobaFlowActionExecutor
    {
        public bool Execute(string actionId, in MobaFlowActionContext ctx)
        {
            if (string.IsNullOrEmpty(actionId))
            {
                return true;
            }

            switch (actionId)
            {
                case MobaFlowActionIds.ResetBattleSessionRuntimeState:
                    ctx.Domain.ResetBattleSessionRuntimeState();
                    return true;
                case MobaFlowActionIds.ReturnLobbyAfterBattleEnd:
                    ctx.Domain.ReturnLobbyAfterBattleEnd();
                    return true;
                default:
                    return false;
            }
        }
    }
}
