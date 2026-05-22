using System;
using System.Collections.Generic;
using BattleState = ET.AbilityKit.Demo.ET.Share.BattleState;

namespace ET.Logic
{
    /// <summary>
    /// 玩家注册数据
    /// </summary>
    public struct PlayerRegistration
    {
        public int PlayerId;
        public int CharacterId;
        public string PlayerName;
        public int TeamId;
        public bool IsReady;
        public long RegisterTime;
    }

    /// <summary>
    /// 战斗玩家注册组件
    /// 管理玩家加入、准备和实体创建流程
    ///
    /// 正确流程:
    /// 1. 配置规定最大玩家数量 (MaxPlayerCount)
    /// 2. 客户端发送准备信号
    /// 3. 等待全部玩家准备完成
    /// 4. 才派发实体创建
    /// </summary>
    [ComponentOf(typeof(ETBattleComponent))]
    public class ETBattlePlayerRegistryComponent : Entity, IAwake
    {
        /// <summary>
        /// 最大玩家数量（从配置读取）
        /// </summary>
        public int MaxPlayerCount { get; set; }

        /// <summary>
        /// 当前注册的玩家列表
        /// </summary>
        public List<PlayerRegistration> RegisteredPlayers { get; set; } = new List<PlayerRegistration>();

        /// <summary>
        /// 已准备的玩家数量
        /// </summary>
        public int ReadyPlayerCount => GetReadyPlayerCount();

        /// <summary>
        /// 是否所有玩家都已准备
        /// </summary>
        public bool AreAllPlayersReady => ReadyPlayerCount >= MaxPlayerCount;

        /// <summary>
        /// 本地玩家ID
        /// </summary>
        public int LocalPlayerId { get; set; }

        /// <summary>
        /// 是否已开始实体创建
        /// </summary>
        public bool HasSpawnedEntities { get; set; }

        public void Awake()
        {
            RegisteredPlayers = new List<PlayerRegistration>();
            HasSpawnedEntities = false;
        }

        /// <summary>
        /// 设置最大玩家数量
        /// </summary>
        public void SetMaxPlayerCount(int count)
        {
            MaxPlayerCount = count;
            Log.Info($"[ETBattlePlayerRegistry] MaxPlayerCount set to {count}");
        }

        /// <summary>
        /// 注册玩家
        /// </summary>
        public bool RegisterPlayer(int playerId, string playerName, int characterId = 1001, int teamId = 1)
        {
            // 检查是否已注册
            if (FindPlayer(playerId) != null)
            {
                Log.Warning($"[ETBattlePlayerRegistry] Player {playerId} already registered");
                return false;
            }

            // 检查是否达到最大数量
            if (RegisteredPlayers.Count >= MaxPlayerCount)
            {
                Log.Warning($"[ETBattlePlayerRegistry] Max player count ({MaxPlayerCount}) reached");
                return false;
            }

            var registration = new PlayerRegistration
            {
                PlayerId = playerId,
                PlayerName = playerName,
                CharacterId = characterId,
                TeamId = teamId,
                IsReady = false,
                RegisterTime = Environment.TickCount64
            };

            RegisteredPlayers.Add(registration);
            Log.Info($"[ETBattlePlayerRegistry] Player registered: {playerName} ({playerId}), Total: {RegisteredPlayers.Count}/{MaxPlayerCount}");

            return true;
        }

        /// <summary>
        /// 玩家准备完成
        /// </summary>
        public bool SetPlayerReady(int playerId)
        {
            for (int i = 0; i < RegisteredPlayers.Count; i++)
            {
                if (RegisteredPlayers[i].PlayerId == playerId)
                {
                    if (RegisteredPlayers[i].IsReady)
                    {
                        Log.Info($"[ETBattlePlayerRegistry] Player {playerId} already ready");
                        return true;
                    }

                    var p = RegisteredPlayers[i];
                    p.IsReady = true;
                    RegisteredPlayers[i] = p;
                    Log.Info($"[ETBattlePlayerRegistry] Player {playerId} ready! ({ReadyPlayerCount}/{MaxPlayerCount})");
                    return true;
                }
            }
            Log.Warning($"[ETBattlePlayerRegistry] Cannot ready unregistered player {playerId}");
            return false;
        }

        /// <summary>
        /// 玩家取消准备
        /// </summary>
        public bool UnreadyPlayer(int playerId)
        {
            for (int i = 0; i < RegisteredPlayers.Count; i++)
            {
                if (RegisteredPlayers[i].PlayerId == playerId)
                {
                    var p = RegisteredPlayers[i];
                    p.IsReady = false;
                    RegisteredPlayers[i] = p;
                    Log.Info($"[ETBattlePlayerRegistry] Player {playerId} unready ({ReadyPlayerCount}/{MaxPlayerCount})");
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 查找玩家
        /// </summary>
        public PlayerRegistration? FindPlayer(int playerId)
        {
            foreach (var player in RegisteredPlayers)
            {
                if (player.PlayerId == playerId)
                {
                    return player;
                }
            }
            return null;
        }

        /// <summary>
        /// 获取本地玩家
        /// </summary>
        public PlayerRegistration? GetLocalPlayer()
        {
            return FindPlayer(LocalPlayerId);
        }

        /// <summary>
        /// 获取已准备的玩家数量
        /// </summary>
        private int GetReadyPlayerCount()
        {
            int count = 0;
            foreach (var player in RegisteredPlayers)
            {
                if (player.IsReady)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// 获取所有已准备的玩家
        /// </summary>
        public List<PlayerRegistration> GetReadyPlayers()
        {
            var ready = new List<PlayerRegistration>();
            foreach (var player in RegisteredPlayers)
            {
                if (player.IsReady)
                {
                    ready.Add(player);
                }
            }
            return ready;
        }

        /// <summary>
        /// 清空所有玩家
        /// </summary>
        public void Clear()
        {
            RegisteredPlayers.Clear();
            HasSpawnedEntities = false;
        }
    }
}
