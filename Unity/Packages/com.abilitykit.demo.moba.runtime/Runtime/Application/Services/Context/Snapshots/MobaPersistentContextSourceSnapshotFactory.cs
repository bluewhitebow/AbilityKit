namespace AbilityKit.Demo.Moba.Services
{
    public static class MobaPersistentContextSourceSnapshotFactory
    {
        public static MobaPersistentContextSourceSnapshot FromContextSource(in MobaContextSourceView source)
        {
            return new MobaPersistentContextSourceSnapshot(in source);
        }

        public static MobaPersistentContextSourceSnapshot FromLineage(
            in MobaTriggerLineageContext lineageContext,
            MobaSkillCastRuntimeHandle skillRuntimeHandle = default,
            string runtimeKind = null,
            int runtimeConfigId = 0)
        {
            var source = MobaContextSourceView.FromLineage(
                in lineageContext,
                MobaContextSourceResolveKind.Lineage,
                MobaContextSourceBoundary.Snapshot,
                skillRuntimeHandle,
                false,
                runtimeKind,
                runtimeConfigId);
            return FromContextSource(in source);
        }

        public static MobaPersistentContextSourceSnapshot FromOrigin(
            in MobaGameplayOrigin origin,
            EffectContextKind contextKind = EffectContextKind.Unknown,
            string runtimeKind = null,
            int runtimeConfigId = 0)
        {
            var source = MobaContextSourceView.FromOrigin(
                in origin,
                MobaContextSourceResolveKind.Origin,
                MobaContextSourceBoundary.Snapshot,
                false,
                runtimeKind,
                runtimeConfigId);

            if (contextKind != EffectContextKind.Unknown && source.IsValid)
            {
                source = new MobaContextSourceView(
                    source.ResolveKind,
                    MobaContextSourceBoundary.Snapshot,
                    contextKind,
                    source.TraceKind,
                    source.SourceActorId,
                    source.TargetActorId,
                    source.SourceContextId,
                    source.ParentContextId,
                    source.RootContextId,
                    source.OwnerContextId,
                    source.ConfigId,
                    source.TriggerId,
                    source.Frame,
                    source.RuntimeKind,
                    source.RuntimeConfigId,
                    false,
                    source.SkillRuntimeHandle);
            }

            return FromContextSource(in source);
        }

        public static MobaPersistentContextSourceSnapshot FromExecutionSnapshot(
            in MobaTriggerExecutionSnapshot snapshot,
            string runtimeKind = null,
            int runtimeConfigId = 0)
        {
            var source = MobaContextSourceView.FromExecutionSnapshot(in snapshot);
            if ((runtimeKind != null || runtimeConfigId != 0) && source.IsValid)
            {
                source = new MobaContextSourceView(
                    source.ResolveKind,
                    MobaContextSourceBoundary.Snapshot,
                    source.ContextKind,
                    source.TraceKind,
                    source.SourceActorId,
                    source.TargetActorId,
                    source.SourceContextId,
                    source.ParentContextId,
                    source.RootContextId,
                    source.OwnerContextId,
                    source.ConfigId,
                    source.TriggerId,
                    source.Frame,
                    runtimeKind ?? source.RuntimeKind,
                    runtimeConfigId != 0 ? runtimeConfigId : source.RuntimeConfigId,
                    false,
                    source.SkillRuntimeHandle);
            }

            return FromContextSource(in source);
        }

        public static MobaPersistentContextSourceSnapshot FromCombatSource(in MobaCombatContextSource source)
        {
            var view = source.ToContextSourceView(MobaContextSourceResolveKind.CombatExecutionContext, MobaContextSourceBoundary.Snapshot);
            return FromContextSource(in view);
        }

        public static bool TryCapture(object payload, out MobaPersistentContextSourceSnapshot snapshot)
        {
            snapshot = default;
            if (payload == null) return false;

            if (payload is MobaPersistentContextSourceSnapshot direct && direct.IsValid)
            {
                snapshot = direct;
                return true;
            }

            if (payload is IMobaPersistentContextSourceProvider persistentProvider
                && persistentProvider.TryGetPersistentContextSource(out snapshot)
                && snapshot.IsValid)
            {
                return true;
            }

            if (payload is MobaContextSourceView directSource && directSource.IsValid)
            {
                snapshot = FromContextSource(in directSource);
                return snapshot.IsValid;
            }

            if (payload is IMobaContextSourceProvider sourceProvider
                && sourceProvider.TryGetContextSource(out var source)
                && source.IsValid)
            {
                snapshot = FromContextSource(in source);
                return snapshot.IsValid;
            }

            if (payload is IMobaCombatContextSource combatSourceProvider
                && combatSourceProvider.TryGetCombatContextSource(out var combatSource)
                && combatSource.IsValid)
            {
                snapshot = FromCombatSource(in combatSource);
                return snapshot.IsValid;
            }

            if (payload is IMobaTriggerExecutionSnapshotProvider executionProvider
                && executionProvider.TryGetExecutionSnapshot(out var executionSnapshot)
                && executionSnapshot.IsValid)
            {
                snapshot = FromExecutionSnapshot(in executionSnapshot);
                return snapshot.IsValid;
            }

            if (payload is IMobaOriginContextProvider originProvider
                && originProvider.TryGetOrigin(out var origin)
                && origin.IsValid)
            {
                snapshot = FromOrigin(in origin);
                return snapshot.IsValid;
            }

            if (payload is IMobaTriggerLineageContextProvider lineageProvider
                && lineageProvider.TryGetLineageContext(out var lineageContext))
            {
                var handle = default(MobaSkillCastRuntimeHandle);
                if (payload is IMobaTriggerSkillRuntimeContext skillRuntimeProvider)
                {
                    skillRuntimeProvider.TryGetSkillRuntimeHandle(out handle);
                }

                snapshot = FromLineage(in lineageContext, handle);
                return snapshot.IsValid;
            }

            return false;
        }
    }
}
