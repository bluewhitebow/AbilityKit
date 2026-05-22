using System;

namespace ET.Logic
{
    /// <summary>
    /// 流程阶段枚举
    /// </summary>
    public enum FlowPhase
    {
        None,
        Prepare,
        Connect,
        CreateWorld,
        LoadAssets,
        InMatch,
        End,
    }

    /// <summary>
    /// 流程步骤枚举
    /// </summary>
    public enum FlowStep
    {
        None,

        // Prepare 阶段步骤
        Prepare_Initialize,

        // Connect 阶段步骤
        Connect_Connect,
        Connect_WaitPlayers,

        // CreateWorld 阶段步骤
        CreateWorld_CreateEntities,
        CreateWorld_RegisterPlayers,

        // LoadAssets 阶段步骤
        LoadAssets_LoadResources,
        LoadAssets_NotifyReady,

        // InMatch 阶段步骤
        InMatch_StartBattle,
        InMatch_BattleLoop,
        InMatch_CheckEnd,

        // End 阶段步骤
        End_Cleanup,
        End_Finished,
    }
}
