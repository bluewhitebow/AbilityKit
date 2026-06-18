using System;
using System.Collections.Generic;

namespace AbilityKit.Network.Runtime.Sync
{
    /// <summary>
    /// 审计迁移步骤 6“枚举收敛”决策的单一事实来源：<see cref="NetworkSyncModel"/> 保留为向后兼容的别名 key，
    /// 实际能力描述位于 <see cref="NetworkSyncProfile"/>。该注册表是唯一知道所有已知模型、规范档案与稳定显示名的位置，
    /// 因此映射关系不再需要同时维护手写 switch 与静态属性集合。
    /// </summary>
    /// <remarks>
    /// 该注册表与玩法和协议无关：它只引用帧、tick、策略层概念。游戏层可以继续通过旧 API 传入
    /// <see cref="NetworkSyncModel"/>；新逻辑应读取解析后的 <see cref="NetworkSyncProfile"/> 策略字段，而不是对别名分支。
    /// </remarks>
    public static class NetworkSyncProfileRegistry
    {
        private readonly struct Entry
        {
            public Entry(NetworkSyncModel model, string name, NetworkSyncProfile profile)
            {
                Model = model;
                Name = name;
                Profile = profile;
            }

            public NetworkSyncModel Model { get; }

            public string Name { get; }

            public NetworkSyncProfile Profile { get; }
        }

        // 按 NetworkSyncModel 枚举值排序，确保调用方枚举时得到稳定顺序。
        private static readonly Entry[] Entries =
        {
            new Entry(NetworkSyncModel.Unspecified, nameof(NetworkSyncModel.Unspecified), NetworkSyncProfiles.Unspecified),
            new Entry(NetworkSyncModel.Lockstep, nameof(NetworkSyncModel.Lockstep), NetworkSyncProfiles.Lockstep),
            new Entry(NetworkSyncModel.PredictRollback, nameof(NetworkSyncModel.PredictRollback), NetworkSyncProfiles.PredictRollback),
            new Entry(NetworkSyncModel.AuthoritativeInterpolation, nameof(NetworkSyncModel.AuthoritativeInterpolation), NetworkSyncProfiles.AuthoritativeInterpolation),
            new Entry(NetworkSyncModel.BatchStateSync, nameof(NetworkSyncModel.BatchStateSync), NetworkSyncProfiles.BatchStateSync),
            new Entry(NetworkSyncModel.MassBattleLodSync, nameof(NetworkSyncModel.MassBattleLodSync), NetworkSyncProfiles.MassBattleLodSync),
            new Entry(NetworkSyncModel.HybridHeroPrediction, nameof(NetworkSyncModel.HybridHeroPrediction), NetworkSyncProfiles.HybridHeroPrediction),
            new Entry(NetworkSyncModel.FastReconnect, nameof(NetworkSyncModel.FastReconnect), NetworkSyncProfiles.FastReconnect),
            new Entry(NetworkSyncModel.ServerRewindLagCompensation, nameof(NetworkSyncModel.ServerRewindLagCompensation), NetworkSyncProfiles.ServerRewindLagCompensation),
        };

        /// <summary>
        /// 已注册兼容模型数量。
        /// </summary>
        public static int Count => Entries.Length;

        /// <summary>
        /// 解析兼容模型对应的规范 <see cref="NetworkSyncProfile"/>。
        /// 对未知模型抛出 <see cref="ArgumentOutOfRangeException"/>，避免调用方静默使用空档案运行。
        /// </summary>
        public static NetworkSyncProfile Resolve(NetworkSyncModel model)
        {
            if (TryResolve(model, out var profile))
            {
                return profile;
            }

            throw new ArgumentOutOfRangeException(nameof(model), model, "Unknown network sync compatibility model.");
        }

        /// <summary>
        /// 尝试解析兼容模型对应的规范档案，不抛出异常。
        /// </summary>
        public static bool TryResolve(NetworkSyncModel model, out NetworkSyncProfile profile)
        {
            foreach (var entry in Entries)
            {
                if (entry.Model == model)
                {
                    profile = entry.Profile;
                    return true;
                }
            }

            profile = NetworkSyncProfiles.Unspecified;
            return false;
        }

        /// <summary>
        /// 返回兼容模型的稳定显示名（与枚举成员名一致）。对未知模型抛出 <see cref="ArgumentOutOfRangeException"/>。
        /// </summary>
        public static string GetName(NetworkSyncModel model)
        {
            foreach (var entry in Entries)
            {
                if (entry.Model == model)
                {
                    return entry.Name;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(model), model, "Unknown network sync compatibility model.");
        }

        /// <summary>
        /// 按枚举顺序遍历所有已注册兼容模型。
        /// </summary>
        public static IEnumerable<NetworkSyncModel> Models()
        {
            foreach (var entry in Entries)
            {
                yield return entry.Model;
            }
        }

        /// <summary>
        /// 按枚举顺序遍历所有已注册档案。适合在不手写每个 profile 的情况下构建能力矩阵。
        /// </summary>
        public static IEnumerable<NetworkSyncProfile> Profiles()
        {
            foreach (var entry in Entries)
            {
                yield return entry.Profile;
            }
        }
    }
}
