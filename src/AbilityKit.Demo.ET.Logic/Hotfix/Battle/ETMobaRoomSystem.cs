using System;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Extensions.Moba.Room;

namespace ET.Logic
{
    /// <summary>
    /// ETMobaRoomComponent System
    /// 处理房间逻辑
    /// 使用 host.extension 的 MobaRoomOrchestrator
    /// </summary>
    [EntitySystemOf(typeof(ETMobaRoomComponent))]
    [FriendOf(typeof(ETMobaRoomComponent))]
    [FriendOf(typeof(ETBattleComponent))]
    public static partial class ETMobaRoomSystem
    {
        [EntitySystem]
        private static void Awake(this ETMobaRoomComponent self)
        {
            Log.Info("[ETMobaRoom] ETMobaRoomComponent awake");
        }

        [EntitySystem]
        private static void Destroy(this ETMobaRoomComponent self)
        {
            Log.Info("[ETMobaRoom] ETMobaRoomComponent destroyed");
        }

        /// <summary>
        /// 加入房间（本地玩家）
        /// </summary>
        public static void JoinAsLocalPlayer(this ETMobaRoomComponent self, int teamId = 0)
        {
            var playerId = int.Parse(self.LocalPlayerId.Value);
            var result = self.JoinRoom(playerId, teamId);
            Log.Info($"[ETMobaRoom] Local player joined: {self.LocalPlayerId.Value}, Result={result}");
        }

        /// <summary>
        /// 模拟其他玩家加入（用于本地测试）
        /// </summary>
        public static void SimulateOtherPlayersJoin(this ETMobaRoomComponent self, int count)
        {
            if (self.RoomState == null)
                return;

            var basePlayerId = int.Parse(self.LocalPlayerId.Value) + 1;

            for (int i = 0; i < count; i++)
            {
                int playerId = basePlayerId + i;
                int teamId = (i % 2) + 1; // 轮流分配队伍
                self.JoinRoom(playerId, teamId);
            }
        }

        /// <summary>
        /// 设置玩家准备（本地玩家）
        /// </summary>
        public static void SetLocalPlayerReady(this ETMobaRoomComponent self, bool ready)
        {
            var playerId = int.Parse(self.LocalPlayerId.Value);
            self.SetPlayerReady(playerId, ready);
        }

        /// <summary>
        /// 所有AI玩家自动准备（用于本地测试）
        /// </summary>
        public static void SimulateOtherPlayersReady(this ETMobaRoomComponent self)
        {
            if (self.RoomState == null)
                return;

            foreach (var kv in self.RoomState.Players)
            {
                var playerIdStr = kv.Key;
                if (playerIdStr == self.LocalPlayerId.Value)
                    continue; // 跳过本地玩家

                if (int.TryParse(playerIdStr, out var playerId))
                {
                    self.SetPlayerReady(playerId, true);
                }
            }
        }

        /// <summary>
        /// 本地玩家选择英雄
        /// </summary>
        public static void LocalPlayerPickHero(this ETMobaRoomComponent self, int heroId, int attributeTemplateId = 0)
        {
            var playerId = int.Parse(self.LocalPlayerId.Value);
            self.PickHero(playerId, heroId, attributeTemplateId);
        }

        /// <summary>
        /// 模拟其他玩家选择英雄（用于本地测试）
        /// </summary>
        public static void SimulateOtherPlayersPickHero(this ETMobaRoomComponent self)
        {
            if (self.RoomState == null)
                return;

            var baseHeroId = 1001;
            int index = 0;

            foreach (var kv in self.RoomState.Players)
            {
                var playerIdStr = kv.Key;
                if (playerIdStr == self.LocalPlayerId.Value)
                    continue; // 跳过本地玩家

                if (int.TryParse(playerIdStr, out var playerId))
                {
                    int heroId = baseHeroId + (index % 3); // 轮流选择英雄
                    self.PickHero(playerId, heroId);
                    index++;
                }
            }
        }

        /// <summary>
        /// 本地玩家完成选人并准备
        /// </summary>
        public static void LocalPlayerReadyWithHero(this ETMobaRoomComponent self, int heroId, int attributeTemplateId = 0)
        {
            self.LocalPlayerPickHero(heroId, attributeTemplateId);
            self.SetLocalPlayerReady(true);
        }

        /// <summary>
        /// 自动完成所有本地测试流程
        /// </summary>
        public static void AutoSetupForLocalTest(this ETMobaRoomComponent self, int heroId = 1001, int attributeTemplateId = 0)
        {
            Log.Info("[ETMobaRoom] AutoSetupForLocalTest: Starting...");

            // 本地玩家选择英雄并准备
            self.LocalPlayerReadyWithHero(heroId, attributeTemplateId);

            // 模拟其他玩家加入、选英雄、准备
            int otherPlayerCount = Math.Max(0, self.MaxPlayerCount - 1);
            if (otherPlayerCount > 0)
            {
                self.SimulateOtherPlayersJoin(otherPlayerCount);
                self.SimulateOtherPlayersPickHero();
                self.SimulateOtherPlayersReady();
            }

            Log.Info($"[ETMobaRoom] AutoSetupForLocalTest: Done. Players={self.PlayerCount}, CanStart={self.CanStartBattle}");
        }
    }
}
