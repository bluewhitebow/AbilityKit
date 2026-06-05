using System;
using System.Collections.Generic;
using System.Linq;
using AbilityKit.Ability.Config;
using AbilityKit.Ability.Triggering.Json;
using AbilityKit.Core.Common.Log;
using AbilityKit.Triggering.Runtime.Plan.Json;
using AbilityKit.Ability.World.DI;
using AbilityKit.Demo.Moba.Systems.Bootstrap;
using AbilityKit.Demo.Moba.Systems.Bootstrap.Flow;

namespace AbilityKit.Demo.Moba.Systems.Bootstrap.Flow.Stages
{
    /// <summary>
    /// TriggerPlans Stage
    /// 注册触发器计划相关的服务
    /// </summary>
    [MobaBootstrapStage]
    public sealed class TriggerPlansStage : MobaBootstrapStageBase
    {
        public override string Name => "TriggerPlans";

        protected internal override void Configure(WorldContainerBuilder builder)
        {
            // 注意：ITextAssetLoader 已经通过 ResourcesTextAssetLoader 的 WorldServiceAttribute 自动注册

            // 注册 TriggerPlanJsonDatabase.ITextLoader
            builder.TryRegister<TriggerPlanJsonDatabase.ITextLoader>(WorldLifetime.Singleton, r =>
            {
                var textAssetLoader = r.Resolve<ITextAssetLoader>();
                var jsonLoader = new UnityResourcesTextLoader(textAssetLoader);
                return new PlanTextLoaderAdapter(jsonLoader);
            });

            builder.TryRegister<TriggerPlanJsonDatabase>(WorldLifetime.Singleton, r =>
            {
                var db = new TriggerPlanJsonDatabase();
                var textAssetLoader = r.Resolve<ITextAssetLoader>();

                // 1. 加载主配置文件 ability_trigger_plans.json（保证向后兼容）
                Log.Info("[TriggerPlansStage] Loading main trigger plans from ability/ability_trigger_plans.json");
                try
                {
                    var fsAdapter = new EtFileSystemAdapter(textAssetLoader);
                    db.Load(fsAdapter, "ability/ability_trigger_plans.json");
                    Log.Info($"[TriggerPlansStage] Main trigger plans loaded. records={db.Records?.Count ?? 0}");
                }
                catch (Exception ex)
                {
                    Log.Warning($"[TriggerPlansStage] Failed to load main trigger plans: {ex.Message}");
                }

                // 2. 从 triggers 目录加载细粒度配置
                Log.Info("[TriggerPlansStage] Loading trigger plans from ability/triggers directory");
                try
                {
                    var fsAdapter = new EtFileSystemAdapter(textAssetLoader);
                    var directoryLoader = new TriggerPlanDirectoryLoader(fsAdapter);
                    var loadedDb = directoryLoader.LoadDirectory("ability/triggers", "**/*.json");

                    if (loadedDb != null && loadedDb.Records != null)
                    {
                        db.MergeFrom(loadedDb, replaceExisting: true);
                        Log.Info($"[TriggerPlansStage] Directory trigger plans merged. total records={db.Records?.Count ?? 0}");
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning($"[TriggerPlansStage] Failed to load directory trigger plans: {ex.Message}");
                }

                // 3. 从 ability/rules 目录加载释放条件、提交消耗等通用技能规则。
                Log.Info("[TriggerPlansStage] Loading ability rules from ability/rules directory");
                try
                {
                    var fsAdapter = new EtFileSystemAdapter(textAssetLoader);
                    var directoryLoader = new TriggerPlanDirectoryLoader(fsAdapter);
                    var loadedDb = directoryLoader.LoadDirectory("ability/rules", "**/*.json");

                    if (loadedDb != null && loadedDb.Records != null)
                    {
                        db.MergeFrom(loadedDb, replaceExisting: true);
                        Log.Info($"[TriggerPlansStage] Ability rules merged. total records={db.Records?.Count ?? 0}");
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning($"[TriggerPlansStage] Failed to load ability rules: {ex.Message}");
                }

                // 4. 加载 MOBA 效果计划，让技能流程事件可以复用触发器计划系统。
                Log.Info("[TriggerPlansStage] Loading moba effect plans from moba/effect_plans.json");
                try
                {
                    var fsAdapter = new EtFileSystemAdapter(textAssetLoader);
                    var effectPlanDb = new TriggerPlanJsonDatabase();
                    effectPlanDb.Load(fsAdapter, "moba/effect_plans.json");
                    db.MergeFrom(effectPlanDb, replaceExisting: true);
                    Log.Info($"[TriggerPlansStage] Moba effect plans merged. total records={db.Records?.Count ?? 0}");
                }
                catch (Exception ex)
                {
                    Log.Warning($"[TriggerPlansStage] Failed to load moba effect plans: {ex.Message}");
                }

                return db;
            });
        }

        /// <summary>
        /// Text asset adapter for trigger plan loading.
        /// </summary>
        private sealed class EtFileSystemAdapter :
            AbilityKit.Ability.Triggering.Json.ITextLoader,
            IFileSystemTextLoader
        {
            private readonly ITextAssetLoader _textAssetLoader;
            private readonly ITextAssetDirectoryLoader _directoryLoader;

            public EtFileSystemAdapter(ITextAssetLoader textAssetLoader)
            {
                _textAssetLoader = textAssetLoader ?? throw new ArgumentNullException(nameof(textAssetLoader));
                _directoryLoader = textAssetLoader as ITextAssetDirectoryLoader;
            }

            bool AbilityKit.Ability.Triggering.Json.ITextLoader.TryLoad(string id, out string text)
            {
                text = null;
                if (string.IsNullOrEmpty(id)) return false;
                return _textAssetLoader.TryLoadText(id, out text);
            }

            bool AbilityKit.Triggering.Runtime.Plan.Json.TriggerPlanJsonDatabase.ITextLoader.TryLoad(string id, out string text)
            {
                return ((AbilityKit.Ability.Triggering.Json.ITextLoader)this).TryLoad(id, out text);
            }

            public IEnumerable<string> GetFiles(string directory, string pattern)
            {
                if (_directoryLoader == null) return Enumerable.Empty<string>();
                return _directoryLoader.GetTextAssetPaths(directory, pattern) ?? Enumerable.Empty<string>();
            }
        }

    }
}
