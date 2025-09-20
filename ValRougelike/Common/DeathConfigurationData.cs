using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static Deathlink.Common.DataObjects;

namespace Deathlink.Common
{
    internal static class DeathConfigurationData
    {
        public static readonly Dictionary<string, DeathChoiceLevel> defaultDeathLevels = new Dictionary<string, DeathChoiceLevel>()
        {
            {
                "Vanilla", new DeathChoiceLevel() {
                    DisplayName = "Vanilla",
                    DeathStyle = new DeathProgressionDetails() { itemLossStyle = ItemLossStyle.None, foodLossOnDeath = true, itemSavedStyle = ItemSavedStyle.Tombstone, minSkillLossPercentage = 0.05f, maxSkillLossPercentage = 0.05f },
                    DeathLootModifiers = new Dictionary<string, DeathLootModifier>() { },
                    ResourceModifiers = new Dictionary<string, DeathResourceModifier> { },
                    SkillModifiers = new Dictionary<string, DeathSkillModifier>() { },
                }
            },
            {
                "Rougelike1", new DeathChoiceLevel() {
                    DisplayName = "ShieldBearer",
                    DeathStyle = new DeathProgressionDetails() { itemLossStyle = ItemLossStyle.DeathlinkBased, foodLossUsesDeathlink = true, itemSavedStyle = ItemSavedStyle.Tombstone, minEquipmentKept = 3, maxEquipmentKept = 9, minItemsKept = 3, maxItemsKept = 15, minSkillLossPercentage = 0.03f, maxSkillLossPercentage = 0.13f },
                    DeathLootModifiers = new Dictionary<string, DeathLootModifier>() { },
                    ResourceModifiers = new Dictionary<string, DeathResourceModifier> {
                        { "Wood", new DeathResourceModifier() { prefabs = new List<string>() { "Wood", "FineWood", "RoundLog", "YggdrasilWood", "Blackwood" }, bonusModifer = 1.1f, bonusActions = new List<ResourceGainTypes>(){ ResourceGainTypes.Harvesting } } }
                    },
                    SkillModifiers = new Dictionary<string, DeathSkillModifier>() {
                        { "All", new DeathSkillModifier() { bonusModifer = 1.05f, skill = Skills.SkillType.All } }
                    },
                }
            },
            {
                "Rougelike2", new DeathChoiceLevel() {
                    DisplayName = "Raider",
                    DeathStyle = new DeathProgressionDetails() { itemLossStyle = ItemLossStyle.DeathlinkBased, itemSavedStyle = ItemSavedStyle.Tombstone, minEquipmentKept = 2, maxEquipmentKept = 6, minSkillLossPercentage = 0.02f, maxSkillLossPercentage = 0.14f },
                    DeathLootModifiers = new Dictionary<string, DeathLootModifier>() { },
                    ResourceModifiers = new Dictionary<string, DeathResourceModifier> {
                        { "Wood", new DeathResourceModifier() { prefabs = new List<string>() { "Wood", "FineWood", "RoundLog", "YggdrasilWood", "Blackwood" }, bonusModifer = 1.2f, bonusActions = new List<ResourceGainTypes>(){ ResourceGainTypes.Harvesting } } },
                        { "Ore", new DeathResourceModifier() { prefabs = new List<string>() { "CopperOre", "TinOre", "IronScrap", "SilverOre", "BlackMetalScrap", "CopperScrap", "FlametalOreNew" }, bonusModifer = 1.2f, bonusActions = new List<ResourceGainTypes>(){ ResourceGainTypes.Harvesting } } }
                    },
                    SkillModifiers = new Dictionary<string, DeathSkillModifier>() {
                        { "All", new DeathSkillModifier() { bonusModifer = 1.1f, skill = Skills.SkillType.All } }
                    },
                }
            },
            {
                "Rougelike3", new DeathChoiceLevel() {
                    DisplayName = "Berserker",
                    DeathStyle = new DeathProgressionDetails() { itemLossStyle = ItemLossStyle.DeathlinkBased, itemSavedStyle = ItemSavedStyle.OnCharacter, minEquipmentKept = 0, maxEquipmentKept = 3, minSkillLossPercentage = 0.05f, maxSkillLossPercentage = 0.2f },
                    DeathLootModifiers = new Dictionary<string, DeathLootModifier>() {
                        { "AmberPearl", new DeathLootModifier() { chance = 0.05f, prefab = "AmberPearl", bonusActions = new List<ResourceGainTypes>() { ResourceGainTypes.Kills } } }
                    },
                    ResourceModifiers = new Dictionary<string, DeathResourceModifier> {
                        { "Wood", new DeathResourceModifier() { prefabs = new List<string>() { "Wood", "FineWood", "RoundLog", "YggdrasilWood", "Blackwood" }, bonusModifer = 1.5f, bonusActions = new List<ResourceGainTypes>(){ ResourceGainTypes.Harvesting } } },
                        { "Ore", new DeathResourceModifier() { prefabs = new List<string>() { "CopperOre", "TinOre", "IronScrap", "SilverOre", "BlackMetalScrap", "CopperScrap", "FlametalOreNew" }, bonusModifer = 1.5f, bonusActions = new List<ResourceGainTypes>(){ ResourceGainTypes.Harvesting } } }
                    },
                    SkillModifiers = new Dictionary<string, DeathSkillModifier>() {
                        { "All", new DeathSkillModifier() { bonusModifer = 1.2f, skill = Skills.SkillType.All } }
                    },
                }
            },
            {
                "Hardcore", new DeathChoiceLevel() {
                    DisplayName = "Deathbringer",
                    DeathStyle = new DeathProgressionDetails() { itemLossStyle = ItemLossStyle.DestroyAll, minSkillLossPercentage = 0.05f, maxSkillLossPercentage = 0.25f },
                    DeathLootModifiers = new Dictionary<string, DeathLootModifier>() {
                        { "AmberPearl", new DeathLootModifier() { chance = 0.05f, prefab = "AmberPearl", bonusActions = new List<ResourceGainTypes>() { ResourceGainTypes.Kills } } },
                        { "SmallHealthPotion", new DeathLootModifier() { chance = 0.01f, prefab = "MeadHealthMinor", bonusActions = new List<ResourceGainTypes>() { ResourceGainTypes.Kills } } }
                    },
                    ResourceModifiers = new Dictionary<string, DeathResourceModifier> {
                        { "Wood", new DeathResourceModifier() { prefabs = new List<string>() { "Wood", "FineWood", "RoundLog", "YggdrasilWood", "Blackwood" }, bonusModifer = 2.0f, bonusActions = new List<ResourceGainTypes>(){ ResourceGainTypes.Harvesting } } },
                        { "Stone", new DeathResourceModifier() { prefabs = new List<string>() { "Flint", "Stone", "BlackMarble", "Grausten" }, bonusModifer = 2.0f, bonusActions = new List<ResourceGainTypes>(){ ResourceGainTypes.Harvesting } } },
                        { "Ore", new DeathResourceModifier() { prefabs = new List<string>() { "CopperOre", "TinOre", "IronScrap", "SilverOre", "BlackMetalScrap", "CopperScrap", "FlametalOreNew" }, bonusModifer = 2.0f, bonusActions = new List<ResourceGainTypes>(){ ResourceGainTypes.Harvesting } } }
                    },
                    SkillModifiers = new Dictionary<string, DeathSkillModifier>() {
                        { "All", new DeathSkillModifier() { bonusModifer = 1.3f, skill = Skills.SkillType.All } }
                    },
                }
            }
        };

        public static Dictionary<long, DeathConfiguration> playerSettings = new Dictionary<long, DeathConfiguration>() { };

        public static Dictionary<string, DeathChoiceLevel> DeathLevels = defaultDeathLevels;

        public static DeathChoiceLevel playerDeathConfiguration = new DeathChoiceLevel() { DeathStyle = new DeathProgressionDetails() {
            foodLossOnDeath = true,
            foodLossUsesDeathlink = false,
            itemLossStyle = ItemLossStyle.None,
            minItemsKept = 0,
            maxItemsKept = 0,
            minEquipmentKept = 0,
            maxEquipmentKept = 0,
            skillLossOnDeath = true,
            maxSkillLossPercentage = 0.05f,
            minSkillLossPercentage = 0.05f,
            itemSavedStyle = ItemSavedStyle.Tombstone,
            nonSkillCheckedItemAction = NonSkillCheckedItemAction.Tombstone
        }
        };

        internal static void Init() {
            try {
                UpdateDeathLevelsConfig(File.ReadAllText(ValConfig.deathChoicesPath));
            }
            catch (Exception e) { Jotunn.Logger.LogWarning($"There was an error updating the Death choice Level values, defaults will be used. Exception: {e}"); }

            try
            {
                UpdatePlayerConfigSettings(File.ReadAllText(ValConfig.playerSettingsPath));
            }
            catch (Exception e) { Jotunn.Logger.LogWarning($"There was an error updating the player choice configs, defaults will be used. Exception: {e}"); }
        }

        public static void CheckAndSetPlayerDeathConfig() {
            if (Player.m_localPlayer == null) {
                //Logger.LogWarning("Local player not defined, skipping setup.");
                return;
            }
            
            long playerID = Player.m_localPlayer.GetPlayerID();
            //Logger.LogWarning($"Setting up Deathlink player configuration with id {playerID}");
            CheckAndSetPlayerDeathConfig(playerID);
        }

        public static void CheckAndSetPlayerDeathConfig(long playerID) {
            Logger.LogDebug($"Checking stored configurations for {playerID} {string.Join(",",playerSettings.Keys)}");
            if (playerSettings.ContainsKey(playerID)) {
                string selectedDeathConfig = playerSettings[playerID].DeathChoiceLevel;
                if (DeathLevels.ContainsKey(selectedDeathConfig)) {
                    Logger.LogDebug("Player deathlink configurations set.");
                    playerDeathConfiguration = DeathLevels[selectedDeathConfig];
                } else {
                    Logger.LogDebug("Player preference setting is not an available config, using fallback");
                    playerDeathConfiguration = DeathLevels.First().Value;
                }
            }
        }

        [HarmonyPatch(typeof(Player))]
        public static class SetupDeathLinkPlayerSpecificConfigPatch
        {
            [HarmonyPostfix]
            [HarmonyPatch(nameof(Player.SetPlayerID))]
            static void Postfix() {
                CheckAndSetPlayerDeathConfig();
            }
        }

        public static string PlayerSettingsDefaultConfig()
        {
            return DataObjects.yamlserializer.Serialize(playerSettings);
        }

        public static string DeathLevelsYamlDefaultConfig()
        {
            return DataObjects.yamlserializer.Serialize(DeathLevels);
        }

        public static void WriteDeathChoices()
        {
            File.WriteAllText(ValConfig.deathChoicesPath, yamlserializer.Serialize(DeathLevels));
        }

        public static void WritePlayerChoices()
        {
            File.WriteAllText(ValConfig.playerSettingsPath, yamlserializer.Serialize(playerSettings));
        }

        public static void UpdateDeathLevelsConfig(string rawyaml) {
            DeathLevels = yamldeserializer.Deserialize<Dictionary<string, DeathChoiceLevel>>(rawyaml);
        }

        public static void UpdatePlayerConfigSettings(string rawyaml) {
            var added_cfgs = yamldeserializer.Deserialize<Dictionary<long, DeathConfiguration>>(rawyaml);

            foreach (var kvp in added_cfgs) {
                if (playerSettings.ContainsKey(kvp.Key)) { continue; }
                playerSettings.Add(kvp.Key, kvp.Value);
            }
        }
    }
}
