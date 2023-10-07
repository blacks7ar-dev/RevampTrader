using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public const string modVersion = "1.0.5";
        public const string modLink = "https://valheim.thunderstore.io/package/blacks7ar/RevampedTrader/";
        private static string buyYaml = modGUID + ".Buy.yml";
        private static readonly string configPath = Paths.ConfigPath;
        private static string fullBuyYamlPath = Paths.ConfigPath + Path.DirectorySeparatorChar + buyYaml;
        private static string sellYaml = modGUID + ".Sell.yml";
        private static string fullSellYamlPath = Paths.ConfigPath + Path.DirectorySeparatorChar + sellYaml;
        public static readonly ManualLogSource RTLogger = BepInEx.Logging.Logger.CreateLogSource(modName);
        private static readonly Harmony _harmony = new(modGUID);
        
        private static readonly ConfigSync _configSync = new(modGUID)
        {
            DisplayName = modName,
            CurrentVersion = modVersion,
            MinimumRequiredVersion = modVersion,
            ModRequired = true
        };
        
        private static ConfigEntry<Toggle> _serverConfigLocked;

        private static readonly CustomSyncedValue<List<TraderYaml>> _BuyYaml = new(_configSync, "Trader Items",
            new List<TraderYaml>());

        private static readonly CustomSyncedValue<List<SellableItem>> _SellYaml = new(_configSync, "Player Items",
            new List<SellableItem>());

        private static List<TraderYaml> _traderItem { get; set; }
        private static List<SellableItem> _playerItem { get; set; }

        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
            bool synchronizedConfig = true)
        {
            var configDescription =
                new ConfigDescription(
                    description.Description +
                    (synchronizedConfig ? " [Synced with Server]" : " [Not Synced with Server]"),
                    description.AcceptableValues, description.Tags);
            var configEntry = Config.Bind(group, name, value, configDescription);
            _configSync.AddConfigEntry(configEntry).SynchronizedConfig = synchronizedConfig;
            return configEntry;
        }
        
        private void TraderWatcher()
        {
            var watcher = new FileSystemWatcher(configPath, buyYaml);
            watcher.Changed += ReadBuyYaml;
            watcher.Created += ReadBuyYaml;
            watcher.Renamed += ReadBuyYaml;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
            var watcher2 = new FileSystemWatcher(configPath, sellYaml);
            watcher2.Changed += ReadSellYaml;
            watcher2.Created += ReadSellYaml;
            watcher2.Renamed += ReadSellYaml;
            watcher2.IncludeSubdirectories = true;
            watcher2.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher2.EnableRaisingEvents = true;
        }

        private void ReadSellYaml(object sender, FileSystemEventArgs e)
        {
            try
            {
                var streamReader = File.OpenText(fullSellYamlPath);
                _playerItem = ReadSerializedSellYml(streamReader.ReadToEnd());
                streamReader.Close();
                _SellYaml.AssignLocalValue(_playerItem);
            }
            catch
            {
                Logging.LogError($"There was an issue loading you {sellYaml}");
                Logging.LogError("Please check your entries for spelling and format!");
            }
        }

        private void ReadBuyYaml(object sender, FileSystemEventArgs e)
        {
            try
            {
                var streamReader = File.OpenText(fullBuyYamlPath);
                _traderItem = ReadSerializedBuyYml(streamReader.ReadToEnd());
                streamReader.Close();
                _BuyYaml.AssignLocalValue(_traderItem);
            }
            catch
            {
                Logging.LogError($"There was an issue loading you {buyYaml}");
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
            if (!File.Exists(fullBuyYamlPath))
            {
                CreateBuyYml();
                Logging.LogWarning($"File not found - {buyYaml}!");
                Logging.LogWarning("Creating a new one..");
            }
            ReadBuyYaml(null, null);
            _BuyYaml.ValueChanged += BuyYamlOnValueChanged;
            if (!File.Exists(fullSellYamlPath))
            {
                CreateSellYml();
                Logging.LogWarning($"File not found - {sellYaml}!");
                Logging.LogWarning("Creating a new one..");
            }
            ReadSellYaml(null, null);
            _SellYaml.ValueChanged += SellYamlOnValueChanged;
            TraderWatcher();
        }

        private static void SellYamlOnValueChanged()
        {
            foreach (var prefab in ObjectDB.instance.m_items)
            {
                foreach (var item in _playerItem.Where(item => prefab.name == item.m_prefab))
                {
                    prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_value = item.m_value;
                }
            }
            ObjectDB.instance.UpdateItemHashes();
            Logging.LogDebug($"{sellYaml} changed..");
        }

        private static void BuyYamlOnValueChanged()
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
            Logging.LogDebug($"{buyYaml} changed..");
        }

        private void OnDestroy()
        {
            _harmony.UnpatchSelf();
        }

        [HarmonyPatch]
        public static class Patches
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.SetupLocations))]
            public static void SetupLocations_Postfix()
            {
                _BuyYaml.ValueChanged += BuyYamlOnValueChanged;
                BuyYamlOnValueChanged();
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
            public static void Awake_Postfix()
            {
                _SellYaml.ValueChanged += SellYamlOnValueChanged;
                SellYamlOnValueChanged();
            }
        }

        private static List<TraderYaml> ReadSerializedBuyYml(string str)
        {
            return new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build()
                .Deserialize<List<TraderYaml>>(str);
        }

        private static List<SellableItem> ReadSerializedSellYml(string str)
        {
            return new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build()
                .Deserialize<List<SellableItem>>(str);
        }
        
        private static void CreateBuyYml()
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
            using var streamWriter = new StreamWriter(fullBuyYamlPath);
            var serializer = new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            serializer.Serialize(streamWriter, traderList);
        }

        private static void CreateSellYml()
        {
            var sellableList = new List<SellableItem>
            {
                new()
                {
                    m_prefab = "Amber",
                    m_value = 5
                },
                new()
                {
                    m_prefab = "AmberPearl",
                    m_value = 10
                },
                new()
                {
                    m_prefab = "Ruby",
                    m_value = 20
                },
                new()
                {
                    m_prefab = "SilverNecklace",
                    m_value = 30
                }
            };
            using var streamWriter = new StreamWriter(fullSellYamlPath);
            var serializer = new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
            serializer.Serialize(streamWriter, sellableList);
        }
    }
}