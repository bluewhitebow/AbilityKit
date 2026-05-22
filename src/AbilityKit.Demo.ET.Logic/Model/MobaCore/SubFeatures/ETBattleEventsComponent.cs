using System;
using System.Collections.Generic;

namespace ET.Logic
{
    /// <summary>
    /// ?? SubFeature Component
    /// ????????
    /// </summary>
    [ComponentOf(typeof(ETBattleComponent))]
    public class ETBattleEventsComponent : Entity, IAwake, IDestroy
    {
        // ????
        public Queue<BattleEvent> EventQueue { get; set; } = new Queue<BattleEvent>();

        // ??????
        public Dictionary<string, List<Action<BattleEvent>>> EventHandlers { get; set; } = new Dictionary<string, List<Action<BattleEvent>>>();

        public void Awake()
        {
        }
    }

    /// <summary>
    /// ??????
    /// </summary>
    public class BattleEvent
    {
        public string EventType { get; set; }
        public int Frame { get; set; }
        public double Timestamp { get; set; }
        public long ActorId { get; set; }
        public long TargetId { get; set; }
        public float Value { get; set; }
        public string Data { get; set; }
    }

    /// <summary>
    /// ??????
    /// </summary>
    public static class BattleEventTypes
    {
        public const string ActorSpawn = "ActorSpawn";
        public const string ActorDead = "ActorDead";
        public const string Damage = "Damage";
        public const string Heal = "Heal";
        public const string SkillStart = "SkillStart";
        public const string SkillEnd = "SkillEnd";
        public const string BuffAdd = "BuffAdd";
        public const string BuffRemove = "BuffRemove";
        public const string MoveStart = "MoveStart";
        public const string MoveEnd = "MoveEnd";
    }

    /// <summary>
    /// ?? SubFeature System
    /// </summary>
    [EntitySystemOf(typeof(ETBattleEventsComponent))]
    [FriendOf(typeof(ETBattleEventsComponent))]
    [FriendOf(typeof(ETBattleLifecycleComponent))]
    public static partial class ETBattleEventsSystem
    {
        [EntitySystem]
        private static void Awake(this ETBattleEventsComponent self)
        {
            Log.Info("[ETBattleEvents] Awake");
            self.EventQueue = new Queue<BattleEvent>();
            self.EventHandlers = new Dictionary<string, List<Action<BattleEvent>>>();
        }

        [EntitySystem]
        private static void Destroy(this ETBattleEventsComponent self)
        {
            self.EventQueue?.Clear();
            self.EventHandlers?.Clear();
        }

        public static void Subscribe(this ETBattleEventsComponent self, string eventType, Action<BattleEvent> handler)
        {
            if (!self.EventHandlers.TryGetValue(eventType, out var handlers))
            {
                handlers = new List<Action<BattleEvent>>();
                self.EventHandlers[eventType] = handlers;
            }
            handlers.Add(handler);
            Log.Debug($"[ETBattleEvents] Subscribed to {eventType}");
        }

        public static void Unsubscribe(this ETBattleEventsComponent self, string eventType, Action<BattleEvent> handler)
        {
            if (self.EventHandlers.TryGetValue(eventType, out var handlers))
            {
                handlers.Remove(handler);
            }
        }

        public static void Publish(this ETBattleEventsComponent self, BattleEvent evt)
        {
            self.EventQueue.Enqueue(evt);
        }

        public static void PublishActorDead(this ETBattleEventsComponent self, long actorId, long killerId)
        {
            self.Publish(new BattleEvent
            {
                EventType = BattleEventTypes.ActorDead,
                Timestamp = (double)System.Environment.TickCount64 / 1000.0,
                ActorId = actorId,
                TargetId = killerId
            });
        }

        public static void PublishDamage(this ETBattleEventsComponent self, long attackerId, long targetId, float damage)
        {
            self.Publish(new BattleEvent
            {
                EventType = BattleEventTypes.Damage,
                Timestamp = (double)System.Environment.TickCount64 / 1000.0,
                ActorId = attackerId,
                TargetId = targetId,
                Value = damage
            });
        }

        public static void ProcessEvents(this ETBattleEventsComponent self)
        {
            while (self.EventQueue.Count > 0)
            {
                var evt = self.EventQueue.Dequeue();

                if (self.EventHandlers.TryGetValue(evt.EventType, out var handlers))
                {
                    foreach (var handler in handlers)
                    {
                        try
                        {
                            handler(evt);
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"[ETBattleEvents] Handler error for {evt.EventType}: {ex.Message}");
                        }
                    }
                }
            }
        }
    }
}
