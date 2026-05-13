using System;
using System.Collections.Generic;
using AbilityKit.Ability.Config;
using AbilityKit.Demo.Moba.Console.Platform;

namespace AbilityKit.Demo.Moba.Console.Bootstrap
{
    /// <summary>
    /// Console Moba 配置数据库
    /// 存储游戏运行时的所有配置数据
    /// </summary>
    public sealed class MobaConfigDatabase
    {
        private readonly ITextAssetLoader _loader;
        private readonly Dictionary<string, CharacterConfig> _characters = new();
        private readonly Dictionary<int, CharacterConfig> _charactersById = new();
        private readonly Dictionary<string, SkillConfig> _skills = new();
        private readonly Dictionary<int, SkillConfig> _skillsById = new();
        private readonly Dictionary<string, ProjectileConfig> _projectiles = new();
        private readonly Dictionary<int, ProjectileConfig> _projectilesById = new();
        private readonly Dictionary<string, BuffConfig> _buffs = new();
        private readonly Dictionary<int, BuffConfig> _buffsById = new();

        public const string DefaultResourcesDir = "moba";

        public MobaConfigDatabase(ITextAssetLoader loader)
        {
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        }

        /// <summary>
        /// 从默认目录加载所有配置
        /// </summary>
        public void LoadFromResources(string dir = DefaultResourcesDir)
        {
            LoadCharacters(dir);
            LoadSkills(dir);
            LoadProjectiles(dir);
            LoadBuffs(dir);
        }

        private void LoadCharacters(string dir)
        {
            var path = $"{dir}/characters";
            if (_loader.TryLoadText(path, out var json) && !string.IsNullOrEmpty(json))
            {
                try
                {
                    var characters = Newtonsoft.Json.JsonConvert.DeserializeObject<List<CharacterConfig>>(json);
                    if (characters != null)
                    {
                        foreach (var c in characters)
                        {
                            _characters[c.Code] = c;
                            _charactersById[c.Id] = c;
                        }
                        Log.System($"Loaded {characters.Count} character configs");
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn($"Failed to load characters: {ex.Message}");
                }
            }
            else
            {
                Log.Debug($"No characters config at: {path}");
            }
        }

        private void LoadSkills(string dir)
        {
            var path = $"{dir}/skills";
            if (_loader.TryLoadText(path, out var json) && !string.IsNullOrEmpty(json))
            {
                try
                {
                    var skills = Newtonsoft.Json.JsonConvert.DeserializeObject<List<SkillConfig>>(json);
                    if (skills != null)
                    {
                        foreach (var s in skills)
                        {
                            _skills[s.Code] = s;
                            _skillsById[s.Id] = s;
                        }
                        Log.System($"Loaded {skills.Count} skill configs");
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn($"Failed to load skills: {ex.Message}");
                }
            }
            else
            {
                Log.Debug($"No skills config at: {path}");
            }
        }

        private void LoadProjectiles(string dir)
        {
            var path = $"{dir}/projectiles";
            if (_loader.TryLoadText(path, out var json) && !string.IsNullOrEmpty(json))
            {
                try
                {
                    var projectiles = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ProjectileConfig>>(json);
                    if (projectiles != null)
                    {
                        foreach (var p in projectiles)
                        {
                            _projectiles[p.Code] = p;
                            _projectilesById[p.Id] = p;
                        }
                        Log.System($"Loaded {projectiles.Count} projectile configs");
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn($"Failed to load projectiles: {ex.Message}");
                }
            }
            else
            {
                Log.Debug($"No projectiles config at: {path}");
            }
        }

        private void LoadBuffs(string dir)
        {
            var path = $"{dir}/buffs";
            if (_loader.TryLoadText(path, out var json) && !string.IsNullOrEmpty(json))
            {
                try
                {
                    var buffs = Newtonsoft.Json.JsonConvert.DeserializeObject<List<BuffConfig>>(json);
                    if (buffs != null)
                    {
                        foreach (var b in buffs)
                        {
                            _buffs[b.Code] = b;
                            _buffsById[b.Id] = b;
                        }
                        Log.System($"Loaded {buffs.Count} buff configs");
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn($"Failed to load buffs: {ex.Message}");
                }
            }
            else
            {
                Log.Debug($"No buffs config at: {path}");
            }
        }

        public bool TryGetCharacter(string code, out CharacterConfig config) => _characters.TryGetValue(code, out config);
        public bool TryGetCharacter(int id, out CharacterConfig config) => _charactersById.TryGetValue(id, out config);
        public bool TryGetSkill(string code, out SkillConfig config) => _skills.TryGetValue(code, out config);
        public bool TryGetSkill(int id, out SkillConfig config) => _skillsById.TryGetValue(id, out config);
        public bool TryGetProjectile(string code, out ProjectileConfig config) => _projectiles.TryGetValue(code, out config);
        public bool TryGetProjectile(int id, out ProjectileConfig config) => _projectilesById.TryGetValue(id, out config);
        public bool TryGetBuff(string code, out BuffConfig config) => _buffs.TryGetValue(code, out config);
        public bool TryGetBuff(int id, out BuffConfig config) => _buffsById.TryGetValue(id, out config);

        public int CharacterCount => _characters.Count;
        public int SkillCount => _skills.Count;
        public int ProjectileCount => _projectiles.Count;
        public int BuffCount => _buffs.Count;

        public IEnumerable<CharacterConfig> GetAllCharacters() => _characters.Values;
        public IEnumerable<SkillConfig> GetAllSkills() => _skills.Values;
        public IEnumerable<ProjectileConfig> GetAllProjectiles() => _projectiles.Values;
        public IEnumerable<BuffConfig> GetAllBuffs() => _buffs.Values;
    }

    /// <summary>
    /// 角色配置
    /// </summary>
    public sealed class CharacterConfig
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public int ModelId { get; set; }
        public float BaseHp { get; set; }
        public float BaseMp { get; set; }
        public float BaseAttack { get; set; }
        public float BaseDefense { get; set; }
        public float BaseMoveSpeed { get; set; }
        public float AttackRange { get; set; }
        public int[] Skills { get; set; } = Array.Empty<int>();
    }

    /// <summary>
    /// 技能配置
    /// </summary>
    public sealed class SkillConfig
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public int Type { get; set; }
        public float Cooldown { get; set; }
        public float MpCost { get; set; }
        public float CastRange { get; set; }
        public float CastTime { get; set; }
        public float Damage { get; set; }
    }

    /// <summary>
    /// 弹道配置
    /// </summary>
    public sealed class ProjectileConfig
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public int ModelId { get; set; }
        public float Speed { get; set; }
        public float MaxDistance { get; set; }
    }

    /// <summary>
    /// Buff 配置
    /// </summary>
    public sealed class BuffConfig
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public int Type { get; set; }
        public float Duration { get; set; }
    }
}
