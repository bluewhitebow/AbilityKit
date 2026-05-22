using System.Collections.Generic;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Demo.Moba.Share;
using AbilityKit.Protocol.Moba.StateSync;

namespace ET.Logic
{
    /// <summary>
    /// ?? SubFeature Component
    /// ??????????
    /// </summary>
    [ComponentOf(typeof(ETBattleComponent))]
    public class ETBattleSnapshotComponent : Entity, IAwake, IDestroy
    {
        // ????
        public List<FrameSnapshotData> SnapshotHistory { get; set; } = new List<FrameSnapshotData>();

        // ????
        public FrameSnapshotData CurrentSnapshot { get; set; }

        // ???????
        public int MaxHistoryFrames { get; set; } = 300;

        // ??????
        public bool IsRecording { get; set; }

        // ????
        public bool IsReplaying { get; set; }
        public int ReplayFrameIndex { get; set; }
        public List<InputFrame> RecordedInputs { get; set; } = new List<InputFrame>();

        public void Awake()
        {
        }
    }

    /// <summary>
    /// ?? SubFeature System
    /// </summary>
    [EntitySystemOf(typeof(ETBattleSnapshotComponent))]
    [FriendOf(typeof(ETBattleSnapshotComponent))]
    [FriendOf(typeof(ETBattleLifecycleComponent))]
    [FriendOf(typeof(ETBattleComponent))]
    public static partial class ETBattleSnapshotSystem
    {
        [EntitySystem]
        private static void Awake(this ETBattleSnapshotComponent self)
        {
            Log.Info("[ETBattleSnapshot] Awake");
            self.SnapshotHistory = new List<FrameSnapshotData>();
            self.RecordedInputs = new List<InputFrame>();
            self.IsRecording = false;
            self.IsReplaying = false;
        }

        [EntitySystem]
        private static void Destroy(this ETBattleSnapshotComponent self)
        {
            self.SnapshotHistory?.Clear();
            self.RecordedInputs?.Clear();
        }

        public static void StartRecording(this ETBattleSnapshotComponent self)
        {
            self.IsRecording = true;
            self.IsReplaying = false;
            self.SnapshotHistory.Clear();
            self.RecordedInputs.Clear();
            Log.Info("[ETBattleSnapshot] Recording started");
        }

        public static void StopRecording(this ETBattleSnapshotComponent self)
        {
            self.IsRecording = false;
            Log.Info($"[ETBattleSnapshot] Recording stopped: {self.SnapshotHistory.Count} snapshots");
        }

        public static void RecordSnapshot(this ETBattleSnapshotComponent self, in FrameSnapshotData snapshot)
        {
            if (!self.IsRecording)
                return;

            self.CurrentSnapshot = snapshot;
            self.SnapshotHistory.Add(snapshot);

            // ????
            if (self.SnapshotHistory.Count > self.MaxHistoryFrames)
            {
                self.SnapshotHistory.RemoveAt(0);
            }
        }

        public static void RecordInput(this ETBattleSnapshotComponent self, InputFrame input)
        {
            if (!self.IsRecording)
                return;

            self.RecordedInputs.Add(input);
        }

        public static FrameSnapshotData? GetSnapshot(this ETBattleSnapshotComponent self, int frame)
        {
            if (frame < 0 || frame >= self.SnapshotHistory.Count)
                return null;

            return self.SnapshotHistory[frame];
        }

        public static void StartReplay(this ETBattleSnapshotComponent self, List<FrameSnapshotData> snapshots, List<InputFrame> inputs)
        {
            self.IsReplaying = true;
            self.IsRecording = false;
            self.SnapshotHistory = snapshots ?? new List<FrameSnapshotData>();
            self.RecordedInputs = inputs ?? new List<InputFrame>();
            self.ReplayFrameIndex = 0;
            Log.Info($"[ETBattleSnapshot] Replay started: {self.SnapshotHistory.Count} snapshots, {self.RecordedInputs.Count} inputs");
        }

        public static void StopReplay(this ETBattleSnapshotComponent self)
        {
            self.IsReplaying = false;
            Log.Info("[ETBattleSnapshot] Replay stopped");
        }

        public static bool GetReplayData(this ETBattleSnapshotComponent self, out List<FrameSnapshotData> snapshots, out List<InputFrame> inputs)
        {
            snapshots = self.SnapshotHistory;
            inputs = self.RecordedInputs;
            return self.IsReplaying || (!self.IsRecording && self.SnapshotHistory.Count > 0);
        }
    }
}
