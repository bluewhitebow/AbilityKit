using System;
using System.Collections.Generic;
using AbilityKit.Ability.Config;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Demo.Moba.Share;
using ET.AbilityKit.Demo.ET.Share;
using BattleState = ET.AbilityKit.Demo.ET.Share.BattleState;

namespace ET.Logic
{
    /// <summary>
    /// ETBattlePlayerRegistryComponent System
    /// 管理玩家注册、准备和实体创建流程
    ///
    /// 正确流程:
    /// 1. 配置规定最大玩家数量 (MaxPlayerCount)
    /// 2. 客户端发送准备信号
    /// 3. 等待全部玩家准备完成
    /// 4. 才派发实体创建
    /// </summary>
    [EntitySystemOf(typeof(ETBattlePlayerRegistryComponent))]
    [FriendOf(typeof(ETBattlePlayerRegistryComponent))]
    [FriendOf(typeof(ETBattleComponent))]
    [FriendOf(typeof(ETMobaBattleDriver))]
    public static partial class ETBattlePlayerRegistrySystem
    {
        /// <summary>
        /// 配置数据库接口
        /// </summary>
        private class JsonCharacter
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int ModelId { get; set; }
            public int AttributeTemplateId { get; set; }
            public List<int> SkillIds { get; set; }
            public List<int> PassiveSkillIds { get; set; }
        }

        private class JsonAttributeTemplate
        {
            public int Id { get; set; }
            public List<int> ActiveSkills { get; set; }
            public List<int> PassiveSkills { get; set; }
            public float Hp { get; set; }
            public float MaxHp { get; set; }
            public float PhysicsAttack { get; set; }
            public float MagicAttack { get; set; }
            public float PhysicsDefense { get; set; }
            public float MagicDefense { get; set; }
            public float MoveSpeed { get; set; }
        }

        [EntitySystem]
        private static void Awake(this ETBattlePlayerRegistryComponent self)
        {
            Log.Info("[ETBattlePlayerRegistry] Awake");
        }

        /// <summary>
        /// 初始化玩家注册组件
        /// </summary>
        /// <param name="self">玩家注册组件</param>
        /// <param name="maxPlayerCount">最大玩家数量</param>
        /// <param name="localPlayerId">本地玩家ID</param>
        public static void Initialize(this ETBattlePlayerRegistryComponent self, int maxPlayerCount, int localPlayerId)
        {
            self.SetMaxPlayerCount(maxPlayerCount);
            self.LocalPlayerId = localPlayerId;
            Log.Info($"[ETBattlePlayerRegistry] Initialized: MaxPlayers={maxPlayerCount}, LocalPlayer={localPlayerId}");
        }

        /// <summary>
        /// 处理玩家准备完成
        /// 当玩家准备好后调用此方法
        /// 如果所有玩家都已准备，则触发实体创建
        /// </summary>
        public static void OnPlayerReady(this ETBattlePlayerRegistryComponent self, int playerId)
        {
            if (self.HasSpawnedEntities)
            {
                Log.Warning($"[ETBattlePlayerRegistry] Entities already spawned, ignoring ready signal");
                return;
            }

            self.SetPlayerReady(playerId);

            if (self.AreAllPlayersReady)
            {
                Log.Info($"[ETBattlePlayerRegistry] All players ready! Triggering entity spawn...");
                SpawnEntitiesForAllPlayers(self);
            }
            else
            {
                Log.Info($"[ETBattlePlayerRegistry] Waiting for players: {self.ReadyPlayerCount}/{self.MaxPlayerCount}");
            }
        }

        /// <summary>
        /// 为所有已准备玩家创建实体
        /// </summary>
        private static void SpawnEntitiesForAllPlayers(ETBattlePlayerRegistryComponent self)
        {
            var battleComponent = self.GetParent<ETBattleComponent>();
            if (battleComponent == null)
            {
                Log.Error("[ETBattlePlayerRegistry] BattleComponent not found");
                return;
            }

            var battleDriver = battleComponent.BattleDriver;
            if (battleDriver == null)
            {
                Log.Error("[ETBattlePlayerRegistry] BattleDriver not found");
                return;
            }

            // 获取已准备的玩家
            var readyPlayers = self.GetReadyPlayers();

            // 构建玩家列表并传递给 BattleDriver
            var playerSpawnList = BuildPlayerSpawnList(readyPlayers);

            // 通知 BattleDriver 开始游戏（传递玩家列表）
            if (battleDriver is ETMobaBattleDriver mobaDriver)
            {
                mobaDriver.OnAllPlayersReady(playerSpawnList);
            }

            self.HasSpawnedEntities = true;
            Log.Info($"[ETBattlePlayerRegistry] Spawned entities for {readyPlayers.Count} players");
        }

        /// <summary>
        /// 构建玩家生成列表
        /// </summary>
        private static List<ETPlayerSpawnData> BuildPlayerSpawnList(List<PlayerRegistration> players)
        {
            var spawnList = new List<ETPlayerSpawnData>();

            int team1Count = 0;
            int team2Count = 0;

            foreach (var player in players)
            {
                // 根据队伍计算位置
                float x, z;
                if (player.TeamId == 1)
                {
                    x = 0f;
                    z = 10f * team1Count;
                    team1Count++;
                }
                else
                {
                    x = 50f;
                    z = 10f * team2Count;
                    team2Count++;
                }

                var spawnData = new ETPlayerSpawnData(
                    player.PlayerId,
                    player.CharacterId,
                    player.PlayerName,
                    player.TeamId,
                    x,
                    0f,
                    z);

                spawnList.Add(spawnData);
            }

            return spawnList;
        }

        /// <summary>
        /// 直接触发实体创建（跳过准备阶段，用于本地测试）
        /// </summary>
        public static void ForceSpawnEntities(this ETBattlePlayerRegistryComponent self, ITextAssetLoader loader, int localPlayerId)
        {
            if (self.HasSpawnedEntities)
            {
                Log.Warning($"[ETBattlePlayerRegistry] Entities already spawned");
                return;
            }

            var battleComponent = self.GetParent<ETBattleComponent>();
            if (battleComponent == null)
            {
                Log.Error("[ETBattlePlayerRegistry] BattleComponent not found");
                return;
            }

            // 从配置加载玩家数据
            var playerSpawnList = LoadPlayersFromConfig(loader, localPlayerId);

            if (playerSpawnList.Count == 0)
            {
                Log.Warning("[ETBattlePlayerRegistry] No players from config, using default");
                AddDefaultPlayers(self, localPlayerId);
                playerSpawnList = LoadPlayersFromConfig(null, localPlayerId);
            }

            var battleDriver = battleComponent.BattleDriver;
            if (battleDriver is ETMobaBattleDriver mobaDriver)
            {
                mobaDriver.OnAllPlayersReady(playerSpawnList);
            }

            self.HasSpawnedEntities = true;
            Log.Info($"[ETBattlePlayerRegistry] Force spawned {playerSpawnList.Count} entities");
        }

        /// <summary>
        /// 从配置加载玩家数据
        /// </summary>
        private static List<ETPlayerSpawnData> LoadPlayersFromConfig(ITextAssetLoader loader, int localPlayerId)
        {
            var players = new List<ETPlayerSpawnData>();

            if (loader == null)
            {
                return players;
            }

            // 加载角色配置
            var characterConfigs = new List<JsonCharacter>();
            if (loader.TryLoadText("Configs/moba/characters.json", out var charsJson) && !string.IsNullOrEmpty(charsJson))
            {
                try
                {
                    var chars = Newtonsoft.Json.JsonConvert.DeserializeObject<List<JsonCharacter>>(charsJson);
                    if (chars != null)
                    {
                        characterConfigs.AddRange(chars);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"[ETBattlePlayerRegistry] Failed to load characters: {ex.Message}");
                }
            }

            // 加载属性模板配置
            var attributeConfigs = new Dictionary<int, JsonAttributeTemplate>();
            if (loader.TryLoadText("Configs/moba/attribute_templates.json", out var attrsJson) && !string.IsNullOrEmpty(attrsJson))
            {
                try
                {
                    var attrs = Newtonsoft.Json.JsonConvert.DeserializeObject<List<JsonAttributeTemplate>>(attrsJson);
                    if (attrs != null)
                    {
                        foreach (var attr in attrs)
                        {
                            attributeConfigs[attr.Id] = attr;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"[ETBattlePlayerRegistry] Failed to load attribute templates: {ex.Message}");
                }
            }

            if (characterConfigs.Count == 0)
            {
                return players;
            }

            int actorIdBase = localPlayerId > 0 ? localPlayerId : 1;

            // Team 1 本地玩家 (HeroId = 1001)
            if (TryFindCharacter(characterConfigs, 1001, out var heroConfig))
            {
                float hp = 500f;
                float maxHp = 500f;

                if (heroConfig.AttributeTemplateId > 0 && attributeConfigs.TryGetValue(heroConfig.AttributeTemplateId, out var attr))
                {
                    hp = attr.Hp;
                    maxHp = attr.MaxHp > 0 ? attr.MaxHp : hp;
                }

                players.Add(new ETPlayerSpawnData(
                    actorIdBase, heroConfig.Id, heroConfig.Name,
                    1, 0f, 0f, 0f, 0f, 1f, hp, maxHp));

                Log.Info($"[ETBattlePlayerRegistry] Loaded player: {heroConfig.Name} (Team 1)");
            }

            // Team 1 AI 玩家
            for (int i = 2; i <= 3; i++)
            {
                int heroId = 1000 + i;
                if (TryFindCharacter(characterConfigs, heroId, out var aiConfig))
                {
                    float hp = 500f;
                    float maxHp = 500f;

                    if (aiConfig.AttributeTemplateId > 0 && attributeConfigs.TryGetValue(aiConfig.AttributeTemplateId, out var attr))
                    {
                        hp = attr.Hp;
                        maxHp = attr.MaxHp > 0 ? attr.MaxHp : hp;
                    }

                    players.Add(new ETPlayerSpawnData(
                        actorIdBase + i, aiConfig.Id, aiConfig.Name,
                        1, 10f * (i - 1), 0f, 0f, 0f, 1f, hp, maxHp));

                    Log.Info($"[ETBattlePlayerRegistry] Loaded AI: {aiConfig.Name} (Team 1)");
                }
            }

            // Team 2 敌人
            for (int i = 1; i <= 3; i++)
            {
                int heroId = 1000 + i;
                if (TryFindCharacter(characterConfigs, heroId, out var enemyConfig))
                {
                    float hp = 500f;
                    float maxHp = 500f;

                    if (enemyConfig.AttributeTemplateId > 0 && attributeConfigs.TryGetValue(enemyConfig.AttributeTemplateId, out var attr))
                    {
                        hp = attr.Hp;
                        maxHp = attr.MaxHp > 0 ? attr.MaxHp : hp;
                    }

                    players.Add(new ETPlayerSpawnData(
                        2000 + i, enemyConfig.Id, enemyConfig.Name,
                        2, 0f, 0f, 50f + 10f * (i - 1), 0f, 1f, hp, maxHp));

                    Log.Info($"[ETBattlePlayerRegistry] Loaded enemy: {enemyConfig.Name} (Team 2)");
                }
            }

            return players;
        }

        /// <summary>
        /// 添加默认玩家（当配置加载失败时）
        /// </summary>
        private static void AddDefaultPlayers(ETBattlePlayerRegistryComponent self, int localPlayerId)
        {
            int actorId = localPlayerId > 0 ? localPlayerId : 1;

            // 本地玩家
            self.RegisterPlayer(actorId, $"Player_{actorId}", 1001, 1);
            self.SetPlayerReady(actorId);

            // AI 玩家
            self.RegisterPlayer(actorId + 1, "AI_Archer", 1002, 1);
            self.SetPlayerReady(actorId + 1);

            self.RegisterPlayer(actorId + 2, "AI_Mage", 1003, 1);
            self.SetPlayerReady(actorId + 2);

            // 敌方玩家
            self.RegisterPlayer(2001, "Enemy_Warrior", 1001, 2);
            self.SetPlayerReady(2001);

            self.RegisterPlayer(2002, "Enemy_Archer", 1002, 2);
            self.SetPlayerReady(2002);

            self.RegisterPlayer(2003, "Enemy_Mage", 1003, 2);
            self.SetPlayerReady(2003);
        }

        private static bool TryFindCharacter(List<JsonCharacter> configs, int id, out JsonCharacter config)
        {
            foreach (var c in configs)
            {
                if (c.Id == id)
                {
                    config = c;
                    return true;
                }
            }
            config = null;
            return false;
        }
    }
}
