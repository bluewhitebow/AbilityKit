using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;

namespace AbilityKit.Samples.Infrastructure.Config
{
    /// <summary>
    /// 基于 JSON 的配置提供器
    /// </summary>
    public sealed class JsonConfigProvider : IConfigProvider
    {
        private readonly JsonDocument _document;
        private readonly string _filePath;

        public JsonConfigProvider(string filePath)
        {
            _filePath = filePath;
            var json = File.ReadAllText(filePath);
            _document = JsonDocument.Parse(json);
        }

        public JsonConfigProvider(string filePath, JsonDocumentOptions options)
        {
            _filePath = filePath;
            var json = File.ReadAllText(filePath);
            _document = JsonDocument.Parse(json, options);
        }

        /// <summary>
        /// 从字符串创建
        /// </summary>
        public static JsonConfigProvider FromString(string json)
        {
            var doc = JsonDocument.Parse(json);
            return new JsonConfigProvider(doc);
        }

        private JsonConfigProvider(JsonDocument document)
        {
            _document = document;
            _filePath = string.Empty;
        }

        public T GetSection<T>(string sectionName) where T : class, new()
        {
            var result = GetSectionOrDefault<T>(sectionName);
            return result ?? new T();
        }

        /// <summary>
        /// 获取配置节（可返回 null）
        /// </summary>
        public T? GetSectionOrDefault<T>(string sectionName) where T : class
        {
            if (_document.RootElement.TryGetProperty(sectionName, out var element))
            {
                return JsonSerializer.Deserialize<T>(element.GetRawText(), GetSerializerOptions());
            }
            return null;
        }

        public T GetValue<T>(string key, T defaultValue = default)
        {
            if (_document.RootElement.TryGetProperty(key, out var element))
            {
                try
                {
                    return JsonSerializer.Deserialize<T>(element.GetRawText(), GetSerializerOptions());
                }
                catch
                {
                    // 如果反序列化失败，尝试直接转换
                    if (element.ValueKind == JsonValueKind.String)
                    {
                        return (T)Convert.ChangeType(element.GetString(), typeof(T));
                    }
                    if (element.ValueKind == JsonValueKind.Number)
                    {
                        if (typeof(T) == typeof(int))
                            return (T)(object)element.GetInt32();
                        if (typeof(T) == typeof(float))
                            return (T)(object)element.GetSingle();
                        if (typeof(T) == typeof(double))
                            return (T)(object)element.GetDouble();
                    }
                    if (typeof(T) == typeof(bool))
                        return (T)(object)element.GetBoolean();
                }
            }
            return defaultValue;
        }

        public Dictionary<string, T> GetDictionary<T>(string sectionName) where T : class, new()
        {
            var result = new Dictionary<string, T>();

            if (!_document.RootElement.TryGetProperty(sectionName, out var section))
                return result;

            foreach (var property in section.EnumerateObject())
            {
                var item = JsonSerializer.Deserialize<T>(property.Value.GetRawText(), GetSerializerOptions());
                if (item != null)
                {
                    result[property.Name] = item;
                }
            }

            return result;
        }

        public bool HasSection(string sectionName)
        {
            return _document.RootElement.TryGetProperty(sectionName, out _);
        }

        public bool HasKey(string key)
        {
            return _document.RootElement.TryGetProperty(key, out _);
        }

        /// <summary>
        /// 获取原始的 JsonElement
        /// </summary>
        public bool TryGetElement(string path, out JsonElement element)
        {
            return _document.RootElement.TryGetProperty(path, out element);
        }

        /// <summary>
        /// 获取根元素
        /// </summary>
        public JsonElement RootElement => _document.RootElement;

        private static JsonSerializerOptions GetSerializerOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter() }
            };
        }

        public void Dispose()
        {
            _document.Dispose();
        }
    }
}
