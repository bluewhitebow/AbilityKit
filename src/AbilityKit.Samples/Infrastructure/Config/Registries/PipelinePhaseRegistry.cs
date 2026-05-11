using System;
using System.Reflection;
using AbilityKit.Core.Common.Marker;
using AbilityKit.Samples.Infrastructure.Config.Attributes;

namespace AbilityKit.Samples.Infrastructure.Config
{
    /// <summary>
    /// 管线阶段类型注册表
    /// 通过 PipelinePhaseTypeIdAttribute 自动发现和注册阶段类型
    /// 支持通过类型名称从 JSON 反序列化
    /// </summary>
    public sealed class PipelinePhaseRegistry : KeyedMarkerRegistry<string, PipelinePhaseTypeIdAttribute>
    {
        private static readonly Lazy<PipelinePhaseRegistry> _instance = new(() => new PipelinePhaseRegistry());
        public static PipelinePhaseRegistry Instance => _instance.Value;

        private PipelinePhaseRegistry()
        {
            ScanCurrentAssembly();
        }

        private void ScanCurrentAssembly()
        {
            var assembly = typeof(PipelinePhaseRegistry).Assembly;
            MarkerScanner<PipelinePhaseTypeIdAttribute>.Scan(new[] { assembly }, this);
        }

        /// <summary>
        /// 通过 Attribute 注册
        /// </summary>
        internal void RegisterByAttribute(PipelinePhaseTypeIdAttribute attr, Type implType)
        {
            if (attr == null || implType == null) return;
            Register(attr.TypeName, implType);
        }

        /// <summary>
        /// 根据名称创建阶段实例
        /// </summary>
        public object CreatePhase(string phaseName)
        {
            return GetOrCreateInstance(phaseName);
        }

        /// <summary>
        /// 尝试根据名称创建阶段实例
        /// </summary>
        public bool TryCreatePhase(string phaseName, out object phase)
        {
            if (TryGet(phaseName, out var type))
            {
                phase = Activator.CreateInstance(type);
                return true;
            }
            phase = null;
            return false;
        }

        /// <summary>
        /// 根据名称从 JSON 反序列化阶段实例
        /// </summary>
        public object CreateFromJson(string phaseName, System.Text.Json.JsonElement data)
        {
            var phase = CreatePhase(phaseName);
            ApplyJsonToObject(phase, data);
            return phase;
        }

        /// <summary>
        /// 尝试根据名称从 JSON 反序列化阶段实例
        /// </summary>
        public bool TryCreateFromJson(string phaseName, System.Text.Json.JsonElement data, out object phase)
        {
            if (!TryGet(phaseName, out var type))
            {
                phase = null;
                return false;
            }

            phase = Activator.CreateInstance(type);
            ApplyJsonToObject(phase, data);
            return true;
        }

        /// <summary>
        /// 将 JsonElement 的属性应用到对象（通过反射）
        /// </summary>
        private void ApplyJsonToObject(object obj, System.Text.Json.JsonElement data)
        {
            if (data.ValueKind != System.Text.Json.JsonValueKind.Object)
                return;

            var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                if (!prop.CanWrite)
                    continue;

                if (!data.TryGetProperty(prop.Name, out var element))
                    continue;

                try
                {
                    var value = ConvertJsonValue(element, prop.PropertyType);
                    prop.SetValue(obj, value);
                }
                catch
                {
                    // 忽略无法转换的属性
                }
            }
        }

        /// <summary>
        /// 将 JsonElement 转换为目标类型
        /// </summary>
        private object? ConvertJsonValue(System.Text.Json.JsonElement element, Type targetType)
        {
            if (targetType == typeof(bool))
                return element.GetBoolean();
            if (targetType == typeof(int))
                return element.GetInt32();
            if (targetType == typeof(long))
                return element.GetInt64();
            if (targetType == typeof(float))
                return element.GetSingle();
            if (targetType == typeof(double))
                return element.GetDouble();
            if (targetType == typeof(string))
                return element.GetString();
            if (targetType == typeof(bool?))
                return element.ValueKind == System.Text.Json.JsonValueKind.Null ? null : element.GetBoolean();
            if (targetType == typeof(int?))
                return element.ValueKind == System.Text.Json.JsonValueKind.Null ? null : element.GetInt32();
            if (targetType == typeof(float?))
                return element.ValueKind == System.Text.Json.JsonValueKind.Null ? null : element.GetSingle();
            if (targetType == typeof(string))
                return element.GetString();

            // 对于其他类型，尝试 JSON 反序列化
            var json = element.GetRawText();
            return System.Text.Json.JsonSerializer.Deserialize(json, targetType);
        }
    }
}
