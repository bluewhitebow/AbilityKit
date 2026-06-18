using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Triggering.Runtime;
using AbilityKit.Ability.World.DI;
using AbilityKit.Core.Continuous;
using AbilityKit.Core.Logging;
using AbilityKit.Demo.Moba.Config.BattleDemo.MO;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Demo.Moba.Components;
using AbilityKit.GameplayTags;
using AbilityKit.Trace;

namespace AbilityKit.Demo.Moba.Services
{
    internal readonly struct BuffLifecycleRejectResult
    {
        public static readonly BuffLifecycleRejectResult None = new BuffLifecycleRejectResult(null, null);

        public BuffLifecycleRejectResult(string code, string message)
        {
            Code = code;
            Message = message;
        }

        public string Code { get; }
        public string Message { get; }
        public bool HasValue => !string.IsNullOrEmpty(Code) || !string.IsNullOrEmpty(Message);

        public override string ToString()
        {
            if (string.IsNullOrEmpty(Code)) return string.IsNullOrEmpty(Message) ? "unknown" : Message;
            if (string.IsNullOrEmpty(Message)) return Code;
            return Code + ": " + Message;
        }
    }

    internal sealed class BuffLifecycleExecutor
    {
        private readonly MobaConfigDatabase _configs;
        private readonly MobaActorLookupService _actors;
        private readonly MobaRuntimeLifecycleHookService _lifecycleHooks;
        private readonly IMobaEffectiveTagQueryService _tags;
        private readonly IMobaContinuousTagTemplateRegistry _tagTemplates;
        private readonly BuffRepository _repo;
        private readonly BuffContextService _ctx;
        private readonly BuffEventPublisher _events;
        private readonly BuffStageEffectExecutor _stageEffects;
        private readonly BuffStackingPolicyApplier _stacking;
        private readonly BuffContinuousBindingService _continuousBindings;
        private readonly MobaSkillCastRuntimeService _skillRuntimes;
        private readonly MobaBuffPresentationCueReporter _presentationCues;
        private readonly Dictionary<int, int[]> _triggerIdsByBuffId = new Dictionary<int, int[]>();

        public BuffLifecycleRejectResult LastReject { get; private set; }
        public string LastRejectReason => LastReject.Message;

        public BuffLifecycleExecutor(
            MobaConfigDatabase configs,
            MobaActorLookupService actors,
            MobaRuntimeLifecycleHookService lifecycleHooks,
            IMobaEffectiveTagQueryService tags,
            IMobaContinuousTagTemplateRegistry tagTemplates,
            BuffRepository repo,
            BuffContextService ctx,
            BuffEventPublisher events,
            BuffStageEffectExecutor stageEffects,
            BuffStackingPolicyApplier stacking,
            BuffContinuousBindingService continuousBindings,
            MobaSkillCastRuntimeService skillRuntimes,
            MobaBuffPresentationCueReporter presentationCues)
        {
            _configs = configs;
            _actors = actors;
            _lifecycleHooks = lifecycleHooks;
            _tags = tags;
            _tagTemplates = tagTemplates;
            _repo = repo ?? new BuffRepository();
            _ctx = ctx;
            _events = events;
            _stageEffects = stageEffects;
            _stacking = stacking ?? new BuffStackingPolicyApplier();
            _continuousBindings = continuousBindings;
            _skillRuntimes = skillRuntimes;
            _presentationCues = presentationCues;
        }

        public bool Apply(in BuffApplyRequest request)
        {
            LastReject = BuffLifecycleRejectResult.None;
            if (!request.IsValid) return Reject("buff.apply.invalidRequest", $"apply request is invalid. target={request.TargetActorId} buffId={request.BuffId} source={request.SourceActorId}.");
            if (_configs == null) return Reject("buff.apply.configDatabaseMissing", $"config database is missing. target={request.TargetActorId} buffId={request.BuffId} source={request.SourceActorId}.");
            if (!_configs.TryGetBuff(request.BuffId, out var buff) || buff == null) return Reject("buff.apply.configNotFound", $"buff config not found. target={request.TargetActorId} buffId={request.BuffId} source={request.SourceActorId}.");

            if (!TryGetTarget(request.TargetActorId, out var target)) return Reject("buff.apply.targetNotFound", $"target actor not found. target={request.TargetActorId} buffId={request.BuffId} source={request.SourceActorId}.");

            var targetActorId = request.TargetActorId;
            var requirements = BuffTagLifecycle.ResolveRequirements(buff, _tagTemplates);
            if (!BuffTagLifecycle.CanActivate(_tags, targetActorId, requirements)) return Reject("buff.apply.tagRequirementsBlocked", $"tag requirements blocked activation. target={targetActorId} buffId={request.BuffId} source={request.SourceActorId}.");

            var list = _repo.GetOrCreateList(target);
            if (list == null) return Reject("buff.apply.runtimeListUnavailable", $"buff runtime list is unavailable. target={targetActorId} buffId={request.BuffId} source={request.SourceActorId}.");

            var duration = request.DurationOverrideMs > 0 ? request.DurationOverrideMs : buff.DurationMs;
            var context = new BuffOperationContext
            {
                ApplyRequest = request,
                Buff = buff,
                TargetActorId = targetActorId,
                DurationSeconds = duration > 0 ? duration / 1000f : 0f,
                Requirements = requirements,
            };

            var existingKey = BuffRuntimeKey.MatchApplyRequest(in request);
            if (!request.ForceNewInstance && BuffRepository.TryGetRuntime(list, in existingKey, out var existingRuntime, out var existingIndex))
            {
                context.Runtime = existingRuntime;
                context.IsExistingRuntime = true;
                var applied = ApplyToExisting(target, ref context);
                if (applied) BuffRepository.MarkDirty(list);
                return applied;
            }

            return ApplyNew(target, list, ref context);
        }

        public bool Remove(in BuffRemoveRequest request)
        {
            LastReject = BuffLifecycleRejectResult.None;
            if (!request.IsValid) return Reject("buff.remove.invalidRequest", $"remove request is invalid. target={request.TargetActorId} buffId={request.BuffId} source={request.SourceActorId} reason={request.Reason}.");
            if (!TryGetTarget(request.TargetActorId, out var target)) return Reject("buff.remove.targetNotFound", $"target actor not found. target={request.TargetActorId} buffId={request.BuffId} source={request.SourceActorId} reason={request.Reason}.");
            if (!target.hasBuffs) return Reject("buff.remove.buffsComponentMissing", $"target has no buffs component. target={request.TargetActorId} buffId={request.BuffId} source={request.SourceActorId} reason={request.Reason}.");

            var list = target.buffs.Active;
            if (list == null || list.Count == 0) return Reject("buff.remove.noActiveRuntimes", $"target has no active buff runtimes. target={request.TargetActorId} buffId={request.BuffId} source={request.SourceActorId} reason={request.Reason}.");

            var removed = false;
            var normalizedReason = NormalizeRemoveReason(request.Reason);
            var key = BuffRuntimeKey.MatchRemoveRequest(in request);
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var runtime = list[i];
                if (!key.Matches(runtime)) continue;

                removed = true;
                EndRuntime(target, list, i, runtime, request.SourceActorId > 0 ? request.SourceActorId : runtime.SourceId, normalizedReason);
            }

            if (!removed) return Reject("buff.remove.runtimeNotFound", $"buff runtime not found. target={request.TargetActorId} buffId={request.BuffId} source={request.SourceActorId} reason={request.Reason}.");
            return true;
        }

        private bool Reject(string code, string message)
        {
            LastReject = new BuffLifecycleRejectResult(code, message);
            return false;
        }

        private bool Reject(string message)
        {
            return Reject("buff.lifecycle.rejected", message);
        }

        public void EndRuntime(global::ActorEntity target, List<BuffRuntime> list, int index, BuffRuntime runtime, int sourceActorId, TraceLifecycleReason reason)
        {
            if (target == null) return;
            if (!target.hasActorId) return;
            if (runtime == null) return;

            var targetActorId = target.actorId.Value;
            var normalizedReason = reason == TraceLifecycleReason.None ? TraceLifecycleReason.Expired : reason;
            var buffId = runtime.BuffId;
            var sourceContextId = runtime.SourceContextId;
            var hadContinuous = runtime.Continuous != null;
            var hadSkillRuntimeRetain = runtime.SkillRuntimeRetainHandle.IsValid;
            var hadModifierBindings = runtime.ModifierBindings != null && runtime.ModifierBindings.Count > 0;

            var continuousReason = BuffContinuousBindingService.ToContinuousEndReason(normalizedReason);
            _continuousBindings?.End(runtime, continuousReason);
            _continuousBindings?.Cleanup(target, targetActorId, runtime, applyRemovalTags: true);
            _ctx?.EndByRuntimeNoClear(runtime, normalizedReason);
            CleanupOwnerBindings(target, targetActorId, runtime.SourceContextId);
            PublishRemove(targetActorId, sourceActorId, runtime, normalizedReason);
            ReportEnded(targetActorId, sourceActorId, runtime, normalizedReason);
            ReleaseSkillRuntime(runtime);
            NotifyLifecycle(runtime, MobaRuntimeLifecycleEventKind.Ended, "buff.lifecycle.ended");
 
            var removedFromList = BuffRepository.RemoveAt(list, index, runtime);
            if (removedFromList)
            {
                new BuffRuntimeView(runtime).ClearRuntimeBindings();
                BuffRepository.ReleaseRuntime(runtime);
            }

            LogBuffCleanup(buffId, targetActorId, sourceActorId, sourceContextId, normalizedReason, hadContinuous, removedFromList, hadSkillRuntimeRetain, removedFromList, hadModifierBindings, removedFromList, removedFromList);
        }

        private static void LogBuffCleanup(int buffId, int targetActorId, int sourceActorId, long sourceContextId, TraceLifecycleReason reason, bool hadContinuous, bool continuousCleared, bool hadSkillRuntimeRetain, bool skillRuntimeCleared, bool hadModifierBindings, bool modifierBindingsCleared, bool removedFromList)
        {
            if (IsExpectedLifecycleEnd(reason)) return;

            Log.Warning($"[MobaBuffCleanup] buff ended unexpectedly. buffId={buffId}, target={targetActorId}, source={sourceActorId}, sourceContextId={sourceContextId}, reason={reason}, hadContinuous={hadContinuous}, continuousCleared={continuousCleared}, hadSkillRuntimeRetain={hadSkillRuntimeRetain}, skillRuntimeCleared={skillRuntimeCleared}, hadModifierBindings={hadModifierBindings}, modifierBindingsCleared={modifierBindingsCleared}, removedFromList={removedFromList}");
        }

        private static bool IsExpectedLifecycleEnd(TraceLifecycleReason reason)
        {
            return reason == TraceLifecycleReason.Expired || reason == TraceLifecycleReason.Completed;
        }

        private bool ApplyToExisting(global::ActorEntity target, ref BuffOperationContext context)
        {
            var runtime = context.Runtime;
            var buff = context.Buff;
            var request = context.ApplyRequest;
            if (runtime == null) return Reject("existing buff runtime is null.");
            if (buff == null) return Reject("existing buff config is null.");

            var runtimeState = context.RuntimeView;
            var oldStackCount = runtime.StackCount;
            var isReplace = buff.StackingPolicy == BuffStackingPolicy.Replace;
            var oldOwnerKey = runtime.SourceContextId;
            if (isReplace)
            {
                _continuousBindings?.End(runtime, AbilityKit.Core.Continuous.ContinuousEndReason.Interrupted);
                _continuousBindings?.Cleanup(target, context.TargetActorId, runtime, applyRemovalTags: false);
                _ctx?.CancelAndEnd(runtime);
                CleanupOwnerBindings(target, context.TargetActorId, oldOwnerKey);
                ReleaseSkillRuntime(runtime);
                NotifyLifecycle(runtime, MobaRuntimeLifecycleEventKind.Cleared, "buff.lifecycle.replaced");
                runtimeState.ClearRuntimeBindings();
            }

            var applied = _stacking.ApplyToExisting(runtime, buff, request.SourceActorId, context.DurationSeconds);
            _ctx?.EnsureBuffContext(runtime, buff.Id, request.SourceActorId, context.TargetActorId, request.Origin);
            NotifyLifecycle(runtime, MobaRuntimeLifecycleEventKind.Activated, "buff.lifecycle.active");
            BindSkillRuntime(runtime, request);
            if (applied || runtime.TagRequirements == null)
            {
                runtimeState.SetTagRequirements(context.Requirements);
            }

            if (applied && !EnsureContinuousRuntime(runtime, buff, request.SourceActorId, context.TargetActorId, context.DurationSeconds, context.Requirements))
            {
                if (isReplace)
                {
                    _ctx?.CancelAndEnd(runtime);
                    ReleaseSkillRuntime(runtime);
                    NotifyLifecycle(runtime, MobaRuntimeLifecycleEventKind.Failed, "buff.lifecycle.activateFailed");
                    runtimeState.ClearRuntimeBindings();
                }

                return Reject($"continuous runtime activation failed for existing buff. target={context.TargetActorId} buffId={buff.Id} source={request.SourceActorId} sourceContextId={runtime.SourceContextId}.");
            }

            UpsertOngoingTriggerPlans(target, runtime.SourceContextId, buff);
            _events?.PublishApplyOrRefresh(buff, request.SourceActorId, context.TargetActorId, context.DurationSeconds, runtime);
            ReportExistingApplied(buff, request.SourceActorId, context.TargetActorId, runtime, oldStackCount, applied);
            if (applied)
            {
                BuffStackingPolicyApplier.ResetInterval(runtime, buff);
                _stageEffects?.Execute(buff.OnAddEffects, buff.Id, request.SourceActorId, context.TargetActorId, runtime.SourceContextId, MobaBuffTriggering.Stages.Add, runtime, durationSeconds: context.DurationSeconds);
                _events?.PublishPerEffect(MobaBuffTriggering.Events.ApplyOrRefresh, buff.OnAddEffects, MobaBuffTriggering.Stages.Add, request.SourceActorId, context.TargetActorId, runtime);
            }

            return true;
        }

        private bool ApplyNew(global::ActorEntity target, List<BuffRuntime> list, ref BuffOperationContext context)
        {
            var buff = context.Buff;
            var request = context.ApplyRequest;
            if (buff == null) return Reject("new buff config is null.");

            var runtime = _stacking.CreateNewRuntime(buff, request.SourceActorId, context.DurationSeconds);
            runtime.SourceContextId = request.SourceContextId;
            context.Runtime = runtime;
            _ctx?.EnsureBuffContext(runtime, buff.Id, request.SourceActorId, context.TargetActorId, request.Origin);
            NotifyLifecycle(runtime, MobaRuntimeLifecycleEventKind.Activated, "buff.lifecycle.active");
            BindSkillRuntime(runtime, request);
            context.RuntimeView.SetTagRequirements(context.Requirements);
            if (!EnsureContinuousRuntime(runtime, buff, request.SourceActorId, context.TargetActorId, context.DurationSeconds, context.Requirements))
            {
                var failedSourceContextId = runtime.SourceContextId;
                _ctx?.CancelAndEnd(runtime);
                ReleaseSkillRuntime(runtime);
                NotifyLifecycle(runtime, MobaRuntimeLifecycleEventKind.Failed, "buff.lifecycle.activateFailed");
                new BuffRuntimeView(runtime).ClearRuntimeBindings();
                BuffRepository.ReleaseRuntime(runtime);
                return Reject($"continuous runtime activation failed for new buff. target={context.TargetActorId} buffId={buff.Id} source={request.SourceActorId} sourceContextId={failedSourceContextId}.");
            }

            list.Add(runtime);
            BuffRepository.RegisterRuntime(list, runtime);
 
            UpsertOngoingTriggerPlans(target, runtime.SourceContextId, buff);
            _events?.PublishApplyOrRefresh(buff, request.SourceActorId, context.TargetActorId, context.DurationSeconds, runtime);
            _presentationCues?.Started(buff, request.SourceActorId, context.TargetActorId, runtime);
            _stageEffects?.Execute(buff.OnAddEffects, buff.Id, request.SourceActorId, context.TargetActorId, runtime.SourceContextId, MobaBuffTriggering.Stages.Add, runtime, durationSeconds: context.DurationSeconds);
            _events?.PublishPerEffect(MobaBuffTriggering.Events.ApplyOrRefresh, buff.OnAddEffects, MobaBuffTriggering.Stages.Add, request.SourceActorId, context.TargetActorId, runtime);
            return true;
        }

        private void PublishRemove(int targetActorId, int sourceActorId, BuffRuntime runtime, TraceLifecycleReason reason)
        {
            if (_configs == null) return;
            if (!_configs.TryGetBuff(runtime.BuffId, out var buff) || buff == null) return;
 
            _events?.PublishRemove(buff, sourceActorId, targetActorId, runtime, reason);
            _stageEffects?.Execute(buff.OnRemoveEffects, buff.Id, sourceActorId, targetActorId, runtime.SourceContextId, MobaBuffTriggering.Stages.Remove, runtime, reason);
        }

        private void ReportExistingApplied(BuffMO buff, int sourceActorId, int targetActorId, BuffRuntime runtime, int oldStackCount, bool applied)
        {
            if (!applied) return;
            if (runtime == null) return;

            if (runtime.StackCount != oldStackCount)
            {
                _presentationCues?.StackChanged(buff, sourceActorId, targetActorId, runtime);
                return;
            }

            _presentationCues?.Refreshed(buff, sourceActorId, targetActorId, runtime);
        }

        private void ReportEnded(int targetActorId, int sourceActorId, BuffRuntime runtime, TraceLifecycleReason reason)
        {
            if (_configs == null) return;
            if (runtime == null) return;
            if (!_configs.TryGetBuff(runtime.BuffId, out var buff) || buff == null) return;

            _presentationCues?.Ended(buff, sourceActorId, targetActorId, runtime, reason);
        }

        private bool BindSkillRuntime(BuffRuntime runtime, in BuffApplyRequest request)
        {
            if (runtime == null) return false;
            if (!request.SkillRuntimeHandle.IsValid) return true;
            if (runtime.SkillRuntimeRetainHandle.IsValid) return true;

            var childId = runtime.SourceContextId;
            if (childId == 0L) return false;

            var child = new MobaSkillRuntimeChildRef(MobaSkillRuntimeChildKind.Buff, childId, runtime.SourceContextId, runtime.BuffId);
            var runtimeHandle = request.SkillRuntimeHandle;
            if (_skillRuntimes != null && _skillRuntimes.RetainChild(in runtimeHandle, in child, out var retainHandle))
            {
                new BuffRuntimeView(runtime).BindSkillRuntime(in runtimeHandle, in retainHandle);
                return true;
            }

            return false;
        }

        private void NotifyLifecycle(BuffRuntime runtime, MobaRuntimeLifecycleEventKind kind, string reason)
        {
            if (runtime == null || _lifecycleHooks == null) return;
            var source = runtime.ContextSource.IsValid
                ? runtime.ContextSource
                : MobaPersistentContextSourceSnapshotFactory.TryCapture(runtime, out var snapshot) && snapshot.TryGetContextSource(out var snapshotSource)
                    ? snapshotSource
                    : default;
            var lifecycleEvent = new MobaRuntimeLifecycleEvent(kind, runtime, in source, reason);
            _lifecycleHooks.Notify(in lifecycleEvent);
        }

        private void ReleaseSkillRuntime(BuffRuntime runtime)
        {
            if (runtime == null) return;
            var retainHandle = runtime.SkillRuntimeRetainHandle;
            if (!retainHandle.IsValid) return;

            try
            {
                _skillRuntimes?.ReleaseChild(in retainHandle);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, $"[BuffLifecycleExecutor] Release skill runtime retain failed (buffId={runtime.BuffId}, sourceContextId={runtime.SourceContextId})");
            }

            new BuffRuntimeView(runtime).ClearSkillRuntimeBinding();
        }

        private bool EnsureContinuousRuntime(BuffRuntime runtime, BuffMO buff, int sourceActorId, int targetActorId, float remainingSeconds, ContinuousTagRequirements requirements)
        {
            return _continuousBindings != null && _continuousBindings.EnsureActive(runtime, buff, sourceActorId, targetActorId, remainingSeconds, requirements);
        }

        private void CleanupOwnerBindings(global::ActorEntity target, int targetActorId, long ownerKey)
        {
            if (target == null) return;
            if (ownerKey == 0) return;

            RemoveOngoingTriggerPlansEntry(target, ownerKey);
            RemoveEffectListeners(target, ownerKey);
        }

        private bool TryGetTarget(int actorId, out global::ActorEntity target)
        {
            target = null;
            if (actorId <= 0) return false;
            if (_actors == null) return false;
            return _actors.TryGetActorEntity(actorId, out target) && target != null && target.hasActorId;
        }

        private static TraceLifecycleReason NormalizeRemoveReason(TraceLifecycleReason reason)
        {
            return reason == TraceLifecycleReason.None ? TraceLifecycleReason.Dispelled : reason;
        }

        private void UpsertOngoingTriggerPlans(global::ActorEntity e, long ownerKey, BuffMO buff)
        {
            if (e == null) return;
            if (ownerKey == 0) return;
            if (buff == null) return;

            if (buff.TriggerIds == null || buff.TriggerIds.Count == 0)
            {
                RemoveOngoingTriggerPlansEntry(e, ownerKey);
                return;
            }

            var ids = GetOrCreateTriggerIds(buff);
            var list = e.hasOngoingTriggerPlans ? e.ongoingTriggerPlans.Active : null;
            if (list == null)
            {
                list = new List<OngoingTriggerPlanEntry>(1);
            }

            var replaced = false;
            for (int i = 0; i < list.Count; i++)
            {
                var it = list[i];
                if (it == null) continue;
                if (it.OwnerKey != ownerKey) continue;

                it.TriggerIds = ids;
                replaced = true;
                break;
            }

            if (!replaced)
            {
                list.Add(new OngoingTriggerPlanEntry { OwnerKey = ownerKey, TriggerIds = ids });
            }

            var rev = e.hasOngoingTriggerPlans ? e.ongoingTriggerPlans.Revision + 1 : 1;
            if (e.hasOngoingTriggerPlans) e.ReplaceOngoingTriggerPlans(list, rev);
            else e.AddOngoingTriggerPlans(list, rev);
        }

        private int[] GetOrCreateTriggerIds(BuffMO buff)
        {
            if (buff == null || buff.TriggerIds == null || buff.TriggerIds.Count == 0) return Array.Empty<int>();
            if (_triggerIdsByBuffId.TryGetValue(buff.Id, out var ids) && ids != null && ids.Length == buff.TriggerIds.Count)
            {
                return ids;
            }

            ids = new int[buff.TriggerIds.Count];
            for (int i = 0; i < buff.TriggerIds.Count; i++) ids[i] = buff.TriggerIds[i];
            _triggerIdsByBuffId[buff.Id] = ids;
            return ids;
        }

        private static void RemoveOngoingTriggerPlansEntry(global::ActorEntity e, long ownerKey)
        {
            if (e == null) return;
            if (ownerKey == 0) return;
            if (!e.hasOngoingTriggerPlans) return;

            var list = e.ongoingTriggerPlans.Active;
            if (list == null || list.Count == 0) return;

            var removedAny = false;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var it = list[i];
                if (it == null || it.OwnerKey == ownerKey)
                {
                    list.RemoveAt(i);
                    removedAny = true;
                }
            }

            if (!removedAny) return;

            var rev = e.ongoingTriggerPlans.Revision + 1;
            if (list.Count == 0) e.RemoveOngoingTriggerPlans();
            else e.ReplaceOngoingTriggerPlans(list, rev);
        }

        private static void RemoveEffectListeners(global::ActorEntity e, long ownerKey)
        {
            if (e == null) return;
            if (ownerKey == 0) return;
            if (!e.hasEffectListeners) return;

            var listeners = e.effectListeners.Active;
            if (listeners == null || listeners.Count == 0) return;

            for (int i = listeners.Count - 1; i >= 0; i--)
            {
                var listener = listeners[i];
                if (listener == null) continue;
                if (listener.SourceContextId != ownerKey) continue;
                listeners.RemoveAt(i);
            }
        }
    }

    internal static class BuffLifecycleExecutorFactory
    {
        public static BuffLifecycleExecutor Create(IWorldResolver services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services), "Buff lifecycle executor requires a world service resolver.");
            }

            services.TryResolve(out MobaConfigDatabase configs);
            services.TryResolve(out AbilityKit.Triggering.Eventing.IEventBus eventBus);
            services.TryResolve(out ITriggerActionRunner actionRunner);
            services.TryResolve(out MobaTraceRegistry trace);
            services.TryResolve(out MobaEffectExecutionService effects);
            services.TryResolve(out IMobaEffectiveTagQueryService tags);
            services.TryResolve(out IMobaContinuousTagTemplateRegistry tagTemplates);
            services.TryResolve(out IFrameTime frameTime);
            services.TryResolve(out IContinuousManager continuous);
            services.TryResolve(out MobaActorLookupService actors);
            services.TryResolve(out MobaSkillCastRuntimeService skillRuntimes);
            services.TryResolve(out MobaPresentationCueSnapshotService cueSnapshots);
 
            var repo = new BuffRepository();
            var ctx = new BuffContextService(trace, actionRunner, frameTime);
            var events = new BuffEventPublisher(eventBus);
            var stageEffects = new BuffStageEffectExecutor(effects);
            var stacking = new BuffStackingPolicyApplier();
            var presentationCues = new MobaBuffPresentationCueReporter(configs, cueSnapshots);
            var continuousBindings = new BuffContinuousBindingService(continuous, tags);

            var lifecycleHooks = MobaRuntimeLifecycleHookFactory.CreateDefault(trace);

            return new BuffLifecycleExecutor(
                configs,
                actors,
                lifecycleHooks,
                tags,
                tagTemplates,
                repo,
                ctx,
                events,
                stageEffects,
                stacking,
                continuousBindings,
                skillRuntimes,
                presentationCues);
        }
    }
}
