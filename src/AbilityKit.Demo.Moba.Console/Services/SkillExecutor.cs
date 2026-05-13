using System;
using System.Collections.Generic;
using AbilityKit.Demo.Moba.Console.Bootstrap;
using AbilityKit.Demo.Moba.Console.Platform;

namespace AbilityKit.Demo.Moba.Console.Services
{
    /// <summary>
    /// 技能施法请求
    /// </summary>
    public readonly struct SkillCastRequest
    {
        public int SkillId { get; }
        public int SkillSlot { get; }
        public int CasterActorId { get; }
        public int TargetActorId { get; }
        public float AimX { get; }
        public float AimZ { get; }
        public float DirX { get; }
        public float DirZ { get; }

        public SkillCastRequest(
            int skillId,
            int skillSlot,
            int casterActorId,
            int targetActorId = 0,
            float aimX = 0,
            float aimZ = 0,
            float dirX = 0,
            float dirZ = 0)
        {
            SkillId = skillId;
            SkillSlot = skillSlot;
            CasterActorId = casterActorId;
            TargetActorId = targetActorId;
            AimX = aimX;
            AimZ = aimZ;
            DirX = dirX;
            DirZ = dirZ;
        }
    }

    /// <summary>
    /// 技能执行结果
    /// </summary>
    public readonly struct SkillCastResult
    {
        public bool Success { get; }
        public string FailReason { get; }
        public int SkillId { get; }
        public int CasterActorId { get; }
        public int TargetActorId { get; }
        public float Damage { get; }

        private SkillCastResult(bool success, int skillId, int casterActorId, int targetActorId, float damage, string failReason)
        {
            Success = success;
            SkillId = skillId;
            CasterActorId = casterActorId;
            TargetActorId = targetActorId;
            Damage = damage;
            FailReason = failReason ?? "";
        }

        public static SkillCastResult Succeeded(int skillId, int casterActorId, int targetActorId, float damage = 0)
        {
            return new SkillCastResult(true, skillId, casterActorId, targetActorId, damage, null);
        }

        public static SkillCastResult Failed(int skillId, int casterActorId, string reason)
        {
            return new SkillCastResult(false, skillId, casterActorId, 0, 0, reason);
        }
    }

    /// <summary>
    /// 技能执行器
    /// 处理技能输入并执行技能
    /// </summary>
    public sealed class SkillExecutor
    {
        private readonly MobaConfigDatabase _config;
        private readonly BattleServices _battleServices;
        private readonly Dictionary<int, int> _cooldownBySlot = new();
        private int _globalCooldownFrames;

        public bool AllowParallel { get; set; }
        public bool InterruptRunning { get; set; }

        public SkillExecutor(MobaConfigDatabase config, BattleServices battleServices)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _battleServices = battleServices ?? throw new ArgumentNullException(nameof(battleServices));
            AllowParallel = false;
            InterruptRunning = false;
        }

        /// <summary>
        /// 处理技能输入事件
        /// </summary>
        public SkillCastResult HandleInput(int actorId, in SkillInputEvent evt)
        {
            switch (evt.Phase)
            {
                case SkillInputPhase.Press:
                    return HandlePress(actorId, evt.Slot);

                case SkillInputPhase.Release:
                    return HandleRelease(actorId, evt);

                case SkillInputPhase.Cancel:
                    return HandleCancel(actorId, evt.Slot);

                default:
                    return SkillCastResult.Failed(evt.Slot, actorId, $"Unknown phase: {evt.Phase}");
            }
        }

        /// <summary>
        /// 按槽位施放技能
        /// </summary>
        public SkillCastResult CastBySlot(int actorId, int slot)
        {
            return HandlePress(actorId, slot);
        }

        /// <summary>
        /// 带瞄准的技能施放
        /// </summary>
        public SkillCastResult CastBySlot(int actorId, int slot, float aimX, float aimZ, float dirX = 0, float dirZ = 0)
        {
            var request = new SkillCastRequest(
                skillId: GetSkillIdBySlot(slot),
                skillSlot: slot,
                casterActorId: actorId,
                aimX: aimX,
                aimZ: aimZ,
                dirX: dirX,
                dirZ: dirZ);

            return ExecuteSkill(request);
        }

        /// <summary>
        /// 取消所有技能
        /// </summary>
        public void CancelAll(int actorId)
        {
            _cooldownBySlot.Clear();
            Log.Skill($"[SkillExecutor] CancelAll for actor {actorId}");
        }

        /// <summary>
        /// 按技能ID取消
        /// </summary>
        public void CancelBySkillId(int actorId, int skillId)
        {
            Log.Skill($"[SkillExecutor] CancelBySkillId {skillId} for actor {actorId}");
        }

        /// <summary>
        /// 帧同步推进
        /// </summary>
        public void Step(int actorId)
        {
            if (_globalCooldownFrames > 0)
            {
                _globalCooldownFrames--;
            }

            // 减少各槽位冷却
            var toRemove = new List<int>();
            foreach (var kvp in _cooldownBySlot)
            {
                if (kvp.Value > 0)
                {
                    _cooldownBySlot[kvp.Key] = kvp.Value - 1;
                    if (_cooldownBySlot[kvp.Key] <= 0)
                    {
                        toRemove.Add(kvp.Key);
                    }
                }
            }

            foreach (var slot in toRemove)
            {
                _cooldownBySlot.Remove(slot);
                Log.Skill($"[SkillExecutor] Skill slot {slot} cooldown ready");
            }
        }

        private SkillCastResult HandlePress(int actorId, int slot)
        {
            if (_globalCooldownFrames > 0)
            {
                return SkillCastResult.Failed(GetSkillIdBySlot(slot), actorId, "Global cooldown not ready");
            }

            if (_cooldownBySlot.TryGetValue(slot, out var cd) && cd > 0)
            {
                return SkillCastResult.Failed(GetSkillIdBySlot(slot), actorId, $"Skill slot {slot} on cooldown: {cd} frames");
            }

            var request = new SkillCastRequest(
                skillId: GetSkillIdBySlot(slot),
                skillSlot: slot,
                casterActorId: actorId);

            return ExecuteSkill(request);
        }

        private SkillCastResult HandleRelease(int actorId, in SkillInputEvent evt)
        {
            var request = new SkillCastRequest(
                skillId: GetSkillIdBySlot(evt.Slot),
                skillSlot: evt.Slot,
                casterActorId: actorId,
                targetActorId: evt.TargetActorId,
                aimX: evt.AimX,
                aimZ: evt.AimZ,
                dirX: evt.DirX,
                dirZ: evt.DirZ);

            return ExecuteSkill(request);
        }

        private SkillCastResult HandleCancel(int actorId, int slot)
        {
            CancelBySkillId(actorId, GetSkillIdBySlot(slot));
            return SkillCastResult.Failed(GetSkillIdBySlot(slot), actorId, "Cancelled");
        }

        private SkillCastResult ExecuteSkill(in SkillCastRequest request)
        {
            var skillId = request.SkillId;

            // 查找技能配置
            if (!_config.TryGetSkill(skillId, out var skillConfig))
            {
                Log.Warn($"[SkillExecutor] Skill config not found: {skillId}");
                // 对于未配置技能，使用默认处理
                return ExecuteDefaultSkill(request);
            }

            Log.Skill($"[SkillExecutor] Executing skill {skillConfig.Name} (ID:{skillId}) from slot {request.SkillSlot}");

            // 设置冷却
            var cooldownFrames = (int)(skillConfig.Cooldown * 30); // 假设 30 FPS
            if (cooldownFrames > 0)
            {
                _cooldownBySlot[request.SkillSlot] = cooldownFrames;
                _globalCooldownFrames = 3; // 0.1秒全局冷却
            }

            // 获取施法者和目标
            var caster = _battleServices.GetActor(request.CasterActorId);
            if (caster == null)
            {
                return SkillCastResult.Failed(skillId, request.CasterActorId, "Caster not found");
            }

            var target = request.TargetActorId > 0 ? _battleServices.GetActor(request.TargetActorId) : null;

            // 使用配置中的伤害值，结合施法者攻击力计算最终伤害
            float damage = skillConfig.Damage + caster.Attack * 0.5f;

            if (target != null)
            {
                // 伤害 = (技能伤害 + 攻击加成) * (1 - 防御减免)
                var defenseReduction = target.Defense / (target.Defense + 100f);
                damage *= (1f - defenseReduction);

                // 应用伤害
                _battleServices.ApplyDamage(request.TargetActorId, damage, request.CasterActorId, skillId);

                Log.Skill($"[SkillExecutor] {skillConfig.Name} dealt {damage:F1} damage to #{request.TargetActorId}");
            }
            else if (request.AimX != 0 || request.AimZ != 0)
            {
                // 区域伤害检测
                var hitActorId = _battleServices.FindActorAtPosition(request.AimX, request.AimZ);
                if (hitActorId > 0)
                {
                    _battleServices.ApplyDamage(hitActorId, damage, request.CasterActorId, skillId);
                    Log.Skill($"[SkillExecutor] {skillConfig.Name} dealt {damage:F1} damage to #{hitActorId} at ({request.AimX:F1}, {request.AimZ:F1})");
                }
            }

            // 触发技能事件
            _battleServices.OnSkillCast(request.CasterActorId, skillId, request.SkillSlot);

            return SkillCastResult.Succeeded(skillId, request.CasterActorId, request.TargetActorId, damage);
        }

        private SkillCastResult ExecuteDefaultSkill(in SkillCastRequest request)
        {
            Log.Skill($"[SkillExecutor] Executing default skill slot {request.SkillSlot} for actor {request.CasterActorId}");

            // 默认技能效果
            float damage = 50f; // 默认伤害

            if (request.AimX != 0 || request.AimZ != 0)
            {
                var hitActorId = _battleServices.FindActorAtPosition(request.AimX, request.AimZ);
                if (hitActorId > 0)
                {
                    _battleServices.ApplyDamage(hitActorId, damage, request.CasterActorId, request.SkillId);
                }
            }

            _battleServices.OnSkillCast(request.CasterActorId, request.SkillId, request.SkillSlot);

            return SkillCastResult.Succeeded(request.SkillId, request.CasterActorId, request.TargetActorId, damage);
        }

        private int GetSkillIdBySlot(int slot)
        {
            // 默认技能ID映射
            return slot switch
            {
                1 => 101, // Skill1 -> 101
                2 => 102, // Skill2 -> 102
                3 => 103, // Skill3 -> 103
                _ => 100 + slot
            };
        }

        /// <summary>
        /// 检查冷却状态
        /// </summary>
        public bool IsOnCooldown(int slot)
        {
            return _cooldownBySlot.TryGetValue(slot, out var cd) && cd > 0;
        }

        /// <summary>
        /// 获取冷却剩余帧数
        /// </summary>
        public int GetCooldownRemaining(int slot)
        {
            return _cooldownBySlot.TryGetValue(slot, out var cd) ? cd : 0;
        }
    }
}
