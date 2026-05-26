using System;
using AbilityKit.Ability.World.DI;
using AbilityKit.Core.Common.Log;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Triggering.Runtime.Plan.Json;
using AbilityKit.Triggering.Runtime;

namespace AbilityKit.Demo.Moba.Systems.Bootstrap.Flow.Stages
{
    /// <summary>
    /// PlanTriggering Install Stage
    /// 安装计划触发系统
    /// </summary>
    [MobaBootstrapStage]
    public sealed class PlanTriggeringStage : MobaBootstrapStageBase
    {
        public override string Name => "Install.PlanTriggering";

        protected internal override void Install(
            Entitas.IContexts contexts,
            Entitas.Systems systems,
            IWorldResolver services)
        {
            try
            {
                Log.Info("[PlanTriggeringStage] starting...");
                if (services.TryResolve<TriggerPlanJsonDatabase>(out var db) && db != null
                    && services.TryResolve<MobaEffectExecutionService>(out var effects) && effects != null)
                {
                    Log.Info("[PlanTriggeringStage] initializing plan actions...");
                    effects.InitializePlanActions();

                    if (services.TryResolve<TriggerRunner<IWorldResolver>>(out var runner) && runner != null)
                    {
                        db.RegisterAll(runner);
                    }
                    Log.Info($"[PlanTriggeringStage] PlanTriggering initialized. records={db.Records?.Count ?? 0}");
                }
                else
                {
                    Log.Warning("[PlanTriggeringStage] init skipped (missing deps: db or effects)");
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "[PlanTriggeringStage] PlanTriggering init exception");
            }
        }
    }
}
