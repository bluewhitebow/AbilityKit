using AbilityKit.Demo.Moba.Config.BattleDemo.MO;
using AbilityKit.Demo.Moba.Components;
using AbilityKit.GameplayTags;

namespace AbilityKit.Demo.Moba.Services.Buffs.Tagging
{
    /// <summary>
    /// Buff 标签生命周期工具：解析持续标签模板，并判断 Buff 是否可激活或应被标签条件移除。
    /// </summary>
    internal static class BuffTagLifecycle
    {
        /// <summary>
        /// 从 Buff 配置引用的持续标签模板中解析运行时门禁条件。
        /// </summary>
        public static ContinuousTagRequirements ResolveRequirements(BuffMO buff, IMobaContinuousTagTemplateRegistry registry)
        {
            if (buff == null) return null;
            if (buff.ContinuousTagTemplateId <= 0) return null;
            if (registry == null) return null;

            return registry.TryGet(buff.ContinuousTagTemplateId, out var requirements) ? requirements : null;
        }

        public static void AssignRequirements(BuffRuntime runtime, BuffMO buff, IMobaContinuousTagTemplateRegistry registry)
        {
            if (runtime == null) return;
            runtime.TagRequirements = ResolveRequirements(buff, registry);
        }

        public static bool CanActivate(IMobaEffectiveTagQueryService tags, int targetActorId, ContinuousTagRequirements requirements)
        {
            return tags == null ? requirements == null : tags.CanActivate(targetActorId, requirements);
        }

        public static bool ShouldEnd(IMobaEffectiveTagQueryService tags, int targetActorId, ContinuousTagRequirements requirements)
        {
            return tags != null && tags.ShouldRemove(targetActorId, requirements);
        }
    }
}
