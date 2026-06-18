using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AbilityKit.Core.Pooling;
using AbilityKit.Demo.Moba.Components;

namespace AbilityKit.Demo.Moba.Services
{
    internal sealed class BuffRepository
    {
        private static readonly ObjectPool<List<BuffRuntime>> s_runtimeListPool = Pools.GetPool(
            createFunc: () => new List<BuffRuntime>(8),
            onRelease: list => list.Clear(),
            defaultCapacity: 32,
            maxSize: 512,
            collectionCheck: false);

        private static readonly ObjectPool<BuffRuntime> s_runtimePool = Pools.GetPool(
            createFunc: () => new BuffRuntime(),
            onRelease: runtime => new BuffRuntimeView(runtime).ClearRuntimeBindings(),
            defaultCapacity: 64,
            maxSize: 2048,
            collectionCheck: false);

        private static readonly ConditionalWeakTable<List<BuffRuntime>, BuffRuntimeIndex> s_indexes = new ConditionalWeakTable<List<BuffRuntime>, BuffRuntimeIndex>();

        public List<BuffRuntime> GetOrCreateList(global::ActorEntity target)
        {
            if (target == null) return null;

            if (!target.hasBuffs)
            {
                target.AddBuffs(s_runtimeListPool.Get());
            }

            var list = target.buffs.Active;
            if (list == null)
            {
                list = s_runtimeListPool.Get();
                target.ReplaceBuffs(list);
            }

            return list;
        }

        public static BuffRuntime RentRuntime()
        {
            return s_runtimePool.Get();
        }

        public static void ReleaseRuntime(BuffRuntime runtime)
        {
            if (runtime == null) return;
            s_runtimePool.Release(runtime);
        }

        public static void ReleaseList(global::ActorEntity target)
        {
            if (target == null || !target.hasBuffs) return;

            var list = target.buffs.Active;
            if (list == null) return;

            for (var i = 0; i < list.Count; i++)
            {
                ReleaseRuntime(list[i]);
            }

            target.RemoveBuffs();
            s_runtimeListPool.Release(list);
        }

        public static int FindExistingBuffIndex(List<BuffRuntime> list, int buffId)
        {
            return FindExistingBuffIndex(list, BuffRuntimeKey.MatchBuff(buffId));
        }

        public static int FindExistingBuffIndex(List<BuffRuntime> list, in BuffRuntimeKey key)
        {
            if (list == null) return -1;
            if (TryGetIndexedRuntime(list, in key, out var indexedRuntime))
            {
                var indexedIndex = list.IndexOf(indexedRuntime);
                if (indexedIndex >= 0) return indexedIndex;
            }

            for (int i = 0; i < list.Count; i++)
            {
                var runtime = list[i];
                if (runtime == null) continue;
                if (key.Matches(runtime)) return i;
            }

            return -1;
        }

        public static bool TryGetRuntime(List<BuffRuntime> list, in BuffRuntimeKey key, out BuffRuntime runtime, out int index)
        {
            runtime = null;
            index = FindExistingBuffIndex(list, in key);
            if (index < 0) return false;

            runtime = list[index];
            return runtime != null;
        }

        public static void RegisterRuntime(List<BuffRuntime> list, BuffRuntime runtime)
        {
            if (list == null || runtime == null) return;
            MarkDirty(list);
        }

        public static void MarkDirty(List<BuffRuntime> list)
        {
            if (list == null) return;
            s_indexes.GetOrCreateValue(list).MarkDirty();
        }

        public static bool RemoveAt(List<BuffRuntime> list, int index, BuffRuntime expectedRuntime)
        {
            if (list == null) return false;
            if (index < 0 || index >= list.Count) return false;
            if (expectedRuntime != null && !ReferenceEquals(list[index], expectedRuntime)) return false;

            list.RemoveAt(index);
            MarkDirty(list);
            return true;
        }

        private static bool TryGetIndexedRuntime(List<BuffRuntime> list, in BuffRuntimeKey key, out BuffRuntime runtime)
        {
            runtime = null;
            if (list == null || list.Count == 0) return false;

            var index = s_indexes.GetOrCreateValue(list);
            index.RebuildIfNeeded(list);
            return index.TryGet(in key, out runtime) && runtime != null && key.Matches(runtime);
        }

        private sealed class BuffRuntimeIndex
        {
            private readonly Dictionary<long, BuffRuntime> _bySourceContextId = new Dictionary<long, BuffRuntime>(16);
            private readonly Dictionary<int, BuffRuntime> _firstByBuffId = new Dictionary<int, BuffRuntime>(16);
            private int _count = -1;
            private bool _dirty = true;

            public void MarkDirty()
            {
                _dirty = true;
            }

            public void RebuildIfNeeded(List<BuffRuntime> list)
            {
                if (list == null) return;
                if (!_dirty && _count == list.Count) return;

                _bySourceContextId.Clear();
                _firstByBuffId.Clear();
                for (var i = 0; i < list.Count; i++)
                {
                    var runtime = list[i];
                    if (runtime == null) continue;

                    if (!_firstByBuffId.ContainsKey(runtime.BuffId))
                    {
                        _firstByBuffId[runtime.BuffId] = runtime;
                    }

                    if (runtime.SourceContextId != 0L)
                    {
                        _bySourceContextId[runtime.SourceContextId] = runtime;
                    }
                }

                _count = list.Count;
                _dirty = false;
            }

            public bool TryGet(in BuffRuntimeKey key, out BuffRuntime runtime)
            {
                runtime = null;
                if (key.SourceContextId != 0L)
                {
                    return _bySourceContextId.TryGetValue(key.SourceContextId, out runtime);
                }

                if (key.SourceActorId <= 0 && key.BuffId > 0)
                {
                    return _firstByBuffId.TryGetValue(key.BuffId, out runtime);
                }

                return false;
            }
        }
    }
}
