using System;
using System.Collections.Generic;
using AbilityKit.Ability.Config;
using AbilityKit.Demo.Moba.Console.Battle;
using AbilityKit.Demo.Moba.Console.Platform;

namespace AbilityKit.Demo.Moba.Console.Bootstrap
{
    /// <summary>
    /// Console ?????
    /// ??????????????????
    /// </summary>
    public static class ConsoleConfigLoader
    {
        /// <summary>
        /// ??????
        /// </summary>
        public const string ConfigDirName = "Configs";

        /// <summary>
        /// ??????????? ConfigDir?
        /// </summary>
        public const string MobaConfigDir = "moba";

        /// <summary>
        /// ????????
        /// </summary>
        public static BattleStartConfig LoadBattleStartConfig(ITextAssetLoader? loader = null)
        {
            loader ??= new ConsoleTextAssetLoader();

            var configPath = $"{MobaConfigDir}/battle_start";
            if (loader.TryLoadText(configPath, out var json) && !string.IsNullOrEmpty(json))
            {
                try
                {
                    var config = Newtonsoft.Json.JsonConvert.DeserializeObject<BattleStartConfig>(json);
                    if (config != null)
                    {
                        Log.System($"Loaded BattleStartConfig from: {configPath}");
                        return config;
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn($"Failed to parse BattleStartConfig from {configPath}: {ex.Message}");
                }
            }

            Log.System("Using default BattleStartConfig");
            return BattleStartConfig.CreateDefault();
        }

        /// <summary>
        /// ?????????
        /// </summary>
        public static MobaConfigDatabase LoadMobaConfig(ITextAssetLoader? loader = null)
        {
            loader ??= new ConsoleTextAssetLoader();
            return new MobaConfigDatabase(loader);
        }

        /// <summary>
        /// ??????
        /// </summary>
        public static MobaConfigDatabase LoadDefault()
        {
            return LoadMobaConfig();
        }
    }
}
