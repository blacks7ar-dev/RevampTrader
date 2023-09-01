using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using RevampTrader.Functions;
using ServerSync;
using UnityEngine;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RevampTrader
{
    [BepInPlugin(modGUID, modName, modVersion)]
    
    public class Plugin : BaseUnityPlugin
    {
        private const string modGUID = "blacks7ar.RevampTrader";
        public const string modName = "RevampTrader";
        public const string modAuthor = "blacks7ar";
        public const string modVersion = "1.0.0";
        public const string modLink = "";
        private static string traderYml = modGUID + ".yml";
        private static readonly string configPath = Paths.ConfigPath;
        private static string fullYmlPath = Paths.ConfigPath + Path.DirectorySeparatorChar + traderYml;
        public static readonly ManualLogSource RTLogger = BepInEx.Logging.Logger.CreateLogSource(modName);
        private static readonly Harmony _harmony = new(modGUID);
        
        private static readonly ConfigSync _configSync = new(modGUID)
        {
            DisplayName = modName,
            CurrentVersion = modVersion,
            MinimumRequiredVersion = modVersion
        };
        
        private static ConfigEntry<Toggle> _serverConfigLocked;

        private static readonly CustomSyncedValue<List<TraderYaml>> _TraderYml = new(_configSync, "Trader Items",
            new List<TraderYaml>());

        private static List<TraderYaml> _traderItem { get; set; }
        public static string connectionError = "";

        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
            bool synchronizedConfig = true)
        {
            var configDescription =
                new ConfigDescription(
                    description.Description +
                    (synchronizedConfig ? " [Synced with Server]" : " [Not Synced with Server]"),
                    description.AcceptableValues, description.Tags);
            var configEntry = Config.Bind(group, name, value, configDescription);
            var syncedConfigEntry = _configSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedConfig;
            return configEntry;
        }
        
        private void TraderWatcher()
        {
            var watcher = new FileSystemWatcher(configPath, traderYml);
            watcher.Changed += ReadTraderYaml;
            watcher.Created += ReadTraderYaml;
            watcher.Renamed += ReadTraderYaml;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private void ReadTraderYaml(object sender, FileSystemEventArgs e)
        {
            try
            {
                var streamReader = File.OpenText(fullYmlPath);
                _traderItem = ReadSerializedYml(streamReader.ReadToEnd());
                streamReader.Close();
                _TraderYml.AssignLocalValue(_traderItem);
            }
            catch
            {
                Logging.LogError($"There was an issue loading you {traderYml}");
                Logging.LogError("Please check your entries for spelling and format!");
            }
        }

        public void Awake()
        {
            _serverConfigLocked = config("1- ServerSync", "Lock Configuration", Toggle.On,
                new ConfigDescription("If On, the configuration is locked and can be changed by server admins only."));
            _configSync.AddLockingConfigEntry(_serverConfigLocked);
            var assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);
            if (!File.Exists(fullYmlPath))
            {
                CreateTraderYml();
                Logging.LogWarning($"File not found - {traderYml}!");
                Logging.LogWarning("Creating a new one..");
            }
            ReadTraderYaml(null, null);
            _TraderYml.ValueChanged += TraderYmlOnValueChanged;
            TraderWatcher();
        }

        private static void TraderYmlOnValueChanged()
        {
            var array = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var gameObject in array)
            {
                if (!gameObject.name.Contains("Haldor")) continue;
                var component = gameObject.GetComponent<Trader>();
                if (!(bool)component) continue;
                component.m_items.Clear();
                foreach (var item in _traderItem)
                {
                    component.m_items.Add(new Trader.TradeItem
                    {
                        m_prefab = ObjectDB.instance.GetItemPrefab(item.m_prefab).GetComponent<ItemDrop>(),
                        m_stack = item.m_stack,
                        m_price = item.m_price,
                        m_requiredGlobalKey = item.m_requiredGlobalKey
                    });
                }
            }
            Logging.LogDebug($"{traderYml} changed..");
        }

        private void OnDestroy()
        {
            _harmony.UnpatchSelf();
        }
        
        [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.SetupLocations))]
        public static class Patches
        {
            public static void Postfix()
            {
                _TraderYml.ValueChanged += TraderYmlOnValueChanged;
                TraderYmlOnValueChanged();
            }
        }

        private static List<TraderYaml> ReadSerializedYml(string str)
        {
            return new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build()
                .Deserialize<List<TraderYaml>>(str);
        }
        
        private static void CreateTraderYml()
        {
            var traderList = new List<TraderYaml>
            {
                new()
                {
                    m_prefab = "HelmetYule",
                    m_stack = 1,
                    m_price = 100,
                    m_requiredGlobalKey = ""
                },
                new()
                {
                    m_prefab = "HelmetDverger",
                    m_stack = 1,
                    m_price = 620,
                    m_requiredGlobalKey = ""
                },
                new()
                {
                    m_prefab = "BeltStrength",
                    m_stack = 1,
                    m_price = 950,
                    m_requiredGlobalKey = ""
                },
                new()
                {
                    m_prefab = "YmirRemains",
                    m_stack = 1,
                    m_price = 120,
                    m_requiredGlobalKey = "defeated_gdking"
                },
                new()
                {
                    m_prefab = "FishingRod",
                    m_stack = 1,
                    m_price = 350,
                    m_requiredGlobalKey = ""
                },
                new()
                {
                    m_prefab = "FishingBait",
                    m_stack = 20,
                    m_price = 10,
                    m_requiredGlobalKey = ""
                },
                new()
                {
                    m_prefab = "Thunderstone",
                    m_stack = 1,
                    m_price = 50,
                    m_requiredGlobalKey = "defeated_gdking"
                },
                new()
                {
                    m_prefab = "ChickenEgg",
                    m_stack = 1,
                    m_price = 1500,
                    m_requiredGlobalKey = "defeated_goblinking"
                }
            };
            using var streamWriter = new StreamWriter(fullYmlPath);
            var serializer = new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            serializer.Serialize(streamWriter, traderList);
        }
    }
}