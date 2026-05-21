namespace ET.AbilityKit.Demo.ET.Logic
{
    /// <summary>
    /// 战斗组件 System
    /// </summary>
    [EntitySystemOf(typeof(DemoBattleComponent))]
    [FriendOf(typeof(DemoBattleComponent))]
    [FriendOf(typeof(DemoUnitComponent))]
    [FriendOf(typeof(DemoUnit))]
    public static partial class DemoBattleComponentSystem
    {
        [EntitySystem]
        private static void Awake(this DemoBattleComponent self)
        {
            Log.Info($"[DemoBattle] DemoBattleComponent awake");
        }

        [EntitySystem]
        private static void Update(this DemoBattleComponent self, float deltaTime)
        {
            if (self.State != DemoBattleState.InProgress)
                return;

            self.BattleTime += deltaTime;

            // 每5秒打印战斗状态
            if (self.BattleTime > 0 && (int)self.BattleTime % 5 == 0 && self.BattleTime - deltaTime < (int)self.BattleTime)
            {
                self.PrintBattleStatus();
            }
        }

        /// <summary>
        /// 打印战斗状态
        /// </summary>
        private static void PrintBattleStatus(this DemoBattleComponent self)
        {
            Log.Info($"--- Battle Time: {self.BattleTime:F1}s ---");
            var unitComponent = self.Scene().GetComponent<DemoUnitComponent>();
            if (unitComponent != null)
            {
                foreach (var unit in unitComponent.GetAllUnits())
                {
                    string status = unit.IsDead ? "[DEAD]" : $"HP: {unit.Hp:F0}/{unit.MaxHp}";
                    Log.Info($"  {unit.Name}: {status}");
                }
            }
            Log.Info("------------------------");
        }

        /// <summary>
        /// 初始化战斗
        /// </summary>
        public static void InitializeBattle(this DemoBattleComponent self, long playerId, string playerName)
        {
            self.BattleId = IdGenerater.Instance.GenerateId();
            self.PlayerId = playerId;
            self.State = DemoBattleState.Loading;

            Log.Info($"[DemoBattle] Initializing battle {self.BattleId} for player {playerName}...");

            // 获取单位组件
            var unitComponent = self.Scene().GetComponent<DemoUnitComponent>();
            if (unitComponent == null)
            {
                unitComponent = self.Scene().AddComponent<DemoUnitComponent>();
            }

            // 创建玩家单位
            var playerUnit = unitComponent.CreateUnit(playerName, DemoUnitType.Hero, 0, 0);
            self.PlayerUnitId = playerUnit.InstanceId;

            // 创建敌人单位
            unitComponent.CreateUnit("Enemy Archer", DemoUnitType.Monster, 10, 0, 80f);
            unitComponent.CreateUnit("Enemy Warrior", DemoUnitType.Monster, 12, 2, 120f);

            self.State = DemoBattleState.Ready;
            Log.Info($"[DemoBattle] Battle {self.BattleId} ready!");

            // 发布战斗初始化完成事件
            EventSystem.Instance.Publish<Scene, DemoBattleSceneInitFinish>(self.Scene(), new DemoBattleSceneInitFinish()
            {
                PlayerId = self.PlayerId,
                PlayerName = playerName,
                BattleId = self.BattleId
            });
        }

        /// <summary>
        /// 开始战斗
        /// </summary>
        public static void StartBattle(this DemoBattleComponent self)
        {
            if (self.State != DemoBattleState.Ready)
            {
                Log.Info($"[DemoBattle] Cannot start battle, current state: {self.State}");
                return;
            }

            self.State = DemoBattleState.InProgress;
            self.BattleTime = 0f;

            Log.Info($"[DemoBattle] Battle {self.BattleId} started!");
            Log.Info("====================================");

            // 发布战斗开始事件
            EventSystem.Instance.Publish<Scene, DemoBattleStart>(self.Scene(), new DemoBattleStart()
            {
                BattleId = self.BattleId
            });
        }

        /// <summary>
        /// 玩家释放技能
        /// </summary>
        public static void PlayerCastSkill(this DemoBattleComponent self, int skillId, float targetX, float targetY)
        {
            if (self.State != DemoBattleState.InProgress)
                return;

            var unitComponent = self.Scene().GetComponent<DemoUnitComponent>();
            var playerUnit = unitComponent?.GetUnit(self.PlayerUnitId);

            if (playerUnit == null || playerUnit.IsDead)
            {
                Log.Info($"[DemoBattle] Cannot cast skill, player is dead");
                return;
            }

            Log.Info($"[DemoBattle] Player {playerUnit.Name} casts skill {skillId} at ({targetX}, {targetY})");

            // 模拟技能效果
            var enemies = unitComponent.FindUnitsInRange(targetX, targetY, 3f);
            foreach (var enemy in enemies)
            {
                if (enemy.UnitType == DemoUnitType.Monster && !enemy.IsDead)
                {
                    enemy.TakeDamage(25f);
                }
            }
        }

        /// <summary>
        /// 结束战斗
        /// </summary>
        public static void EndBattle(this DemoBattleComponent self, bool isVictory)
        {
            if (self.State != DemoBattleState.InProgress)
                return;

            self.State = DemoBattleState.Ended;

            Log.Info("====================================");
            Log.Info($"[DemoBattle] Battle {self.BattleId} ended!");
            Log.Info($"[DemoBattle] Result: {(isVictory ? "VICTORY" : "DEFEAT")}");
            Log.Info($"[DemoBattle] Duration: {self.BattleTime:F1}s");
            Log.Info("====================================");

            // 发布战斗结束事件
            EventSystem.Instance.Publish<Scene, BattleEnd>(self.Scene(), new BattleEnd()
            {
                BattleId = self.BattleId,
                IsVictory = isVictory
            });
        }

        /// <summary>
        /// 检查战斗是否应该结束
        /// </summary>
        public static void CheckBattleEnd(this DemoBattleComponent self)
        {
            var unitComponent = self.Scene().GetComponent<DemoUnitComponent>();
            if (unitComponent == null)
                return;

            // 检查玩家是否死亡
            var playerUnit = unitComponent.GetUnit(self.PlayerUnitId);
            if (playerUnit != null && playerUnit.IsDead)
            {
                self.EndBattle(false);
                return;
            }

            // 检查是否所有敌人都死亡
            bool allEnemiesDead = true;
            foreach (var unit in unitComponent.GetAllUnits())
            {
                if (unit.UnitType == DemoUnitType.Monster && !unit.IsDead)
                {
                    allEnemiesDead = false;
                    break;
                }
            }

            if (allEnemiesDead)
            {
                self.EndBattle(true);
            }
        }
    }
}
