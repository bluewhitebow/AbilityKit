using System;

namespace AbilityKit.Demo.Moba.Console.Battle
{
    /// <summary>
    /// ?????????????
    /// </summary>
    public readonly struct BattleStartPlan
    {
        public string WorldId { get; init; }
        public string WorldType { get; init; }
        public string ClientId { get; init; }
        public string PlayerId { get; init; }

        public int TickRate { get; init; }
        public int InputDelayFrames { get; init; }

        /// <summary>
        /// ????
        /// </summary>
        public BattleSyncMode SyncMode { get; init; }

        /// <summary>
        /// ?????Normal / Record / Replay?
        /// </summary>
        public BattleRunMode RunMode { get; init; }

        /// <summary>
        /// ????
        /// </summary>
        public bool EnableDebug { get; init; }

        /// <summary>
        /// ??????
        /// </summary>
        public int MaxPlayerCount { get; init; }

        /// <summary>
        /// ??????
        /// </summary>
        public bool EnableInputRecording { get; init; }

        /// <summary>
        /// ????????
        /// </summary>
        public string InputRecordOutputPath { get; init; }

        /// <summary>
        /// ??????
        /// </summary>
        public bool EnableInputReplay { get; init; }

        /// <summary>
        /// ??????
        /// </summary>
        public string InputReplayPath { get; init; }

        /// <summary>
        /// ???????
        /// </summary>
        public bool EnableClientPrediction { get; init; }

        /// <summary>
        /// ????
        /// </summary>
        public BattleStartPlan(
            string worldId = "room_1",
            string worldType = "MobaBattle",
            string clientId = "console_client",
            string playerId = "player_1",
            int tickRate = 30,
            int inputDelayFrames = 2,
            BattleSyncMode syncMode = BattleSyncMode.Lockstep,
            BattleRunMode runMode = BattleRunMode.Normal,
            bool enableDebug = true,
            int maxPlayerCount = 10,
            bool enableInputRecording = false,
            string inputRecordOutputPath = "",
            bool enableInputReplay = false,
            string inputReplayPath = "",
            bool enableClientPrediction = false)
        {
            WorldId = worldId;
            WorldType = worldType;
            ClientId = clientId;
            PlayerId = playerId;
            TickRate = tickRate;
            InputDelayFrames = inputDelayFrames;
            SyncMode = syncMode;
            RunMode = runMode;
            EnableDebug = enableDebug;
            MaxPlayerCount = maxPlayerCount;
            EnableInputRecording = enableInputRecording;
            InputRecordOutputPath = inputRecordOutputPath;
            EnableInputReplay = enableInputReplay;
            InputReplayPath = inputReplayPath;
            EnableClientPrediction = enableClientPrediction;
        }

        /// <summary>
        /// ??????
        /// </summary>
        public static BattleStartPlan CreateDefault()
        {
            return new BattleStartPlan();
        }

        /// <summary>
        /// ??????
        /// </summary>
        public static BattleStartPlan CreateDebug()
        {
            return new BattleStartPlan(enableDebug: true);
        }
    }

    /// <summary>
    /// ??????
    /// </summary>
    public enum BattleSyncMode
    {
        /// <summary>
        /// ?????
        /// </summary>
        Lockstep = 0,

        /// <summary>
        /// ??????
        /// </summary>
        SnapshotAuthority = 1,

        /// <summary>
        /// ????????
        /// </summary>
        HybridPredictReconcile = 2,
    }

    /// <summary>
    /// ??????
    /// </summary>
    public enum BattleRunMode
    {
        /// <summary>
        /// ??????
        /// </summary>
        Normal = 0,

        /// <summary>
        /// ????
        /// </summary>
        Record = 1,

        /// <summary>
        /// ????
        /// </summary>
        Replay = 2,
    }
}
