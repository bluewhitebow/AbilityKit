using AbilityKit.Ability.Host;
using AbilityKit.Protocol.Moba.StateSync;

namespace ET.Logic
{
    /// <summary>
    /// PlayerId 工具类
    ///
    /// Design:
    /// - 集中管理 PlayerId 与 ActorId 之间的转换逻辑
    /// - 提供统一的转换接口，避免重复代码
    /// </summary>
    public static class PlayerIdUtils
    {
        /// <summary>
        /// 将 PlayerId 转换为 ActorId（int）
        /// </summary>
        /// <param name="playerId">PlayerId 实例</param>
        /// <param name="defaultValue">转换失败时返回的默认值</param>
        /// <returns>ActorId，转换失败返回 defaultValue</returns>
        public static int ToActorId(PlayerId playerId, int defaultValue = 0)
        {
            if (playerId.Value == null || string.IsNullOrEmpty(playerId.Value))
                return defaultValue;

            if (int.TryParse(playerId.Value, out var actorId))
                return actorId;

            return defaultValue;
        }

        /// <summary>
        /// 将 ActorId 转换为 PlayerId
        /// </summary>
        /// <param name="actorId">ActorId</param>
        /// <returns>PlayerId 实例</returns>
        public static PlayerId ToPlayerId(int actorId)
        {
            return new PlayerId(actorId.ToString());
        }

        /// <summary>
        /// 检查 PlayerId 是否可以转换为有效的 ActorId
        /// </summary>
        /// <param name="playerId">PlayerId 实例</param>
        /// <returns>是否可以转换</returns>
        public static bool IsValidActorId(PlayerId playerId)
        {
            if (playerId.Value == null || string.IsNullOrEmpty(playerId.Value))
                return false;

            return int.TryParse(playerId.Value, out var actorId) && actorId > 0;
        }
    }
}
