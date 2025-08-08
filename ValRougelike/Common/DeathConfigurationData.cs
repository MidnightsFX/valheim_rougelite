using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                    DeathStyle = new DeathProgressionDetails() { itemLossStyle = ItemLossStyle.None, itemSavedStyle = ItemSavedStyle.Tombstone, minSkillLossPercentage = 0.05f, maxSkillLossPercentage = 0.05f },
                    DeathLootModifiers = new Dictionary<string, DeathLootModifier>() { },
                    ResourceModifiers = new Dictionary<string, DeathResourceModifier> { },
                    SkillModifiers = new Dictionary<string, DeathSkillModifier>() { },
                }
            },
            {
                "Rougelike1", new DeathChoiceLevel() {
                    DisplayName = "ShieldBearer",
                    DeathStyle = new DeathProgressionDetails() { itemLossStyle = ItemLossStyle.DeathlinkBased, itemSavedStyle = ItemSavedStyle.Tombstone, minItemsKept = 6, maxItemsKept = 20, minSkillLossPercentage = 0.03f, maxSkillLossPercentage = 0.13f },
                    DeathLootModifiers = new Dictionary<string, DeathLootModifier>() { },
                    ResourceModifiers = new Dictionary<string, DeathResourceModifier> {
                        { "Wood", new DeathResourceModifier() { bonusModifer = 1.1f, bonusActions = new List<ResourceGainTypes>(){ ResourceGainTypes.Harvesting } } }
                    },
                    SkillModifiers = new Dictionary<string, DeathSkillModifier>() {
                        { "All", new DeathSkillModifier() { bonusModifer = 1.05f } }
                    },
                }
            },
            {
                "Rougelike2", new DeathChoiceLevel() {
                    DisplayName = "Raider",
                    DeathStyle = new DeathProgressionDetails() { itemLossStyle = ItemLossStyle.DeathlinkBased, itemSavedStyle = ItemSavedStyle.Tombstone, minItemsKept = 3, maxItemsKept = 9, minSkillLossPercentage = 0.02f, maxSkillLossPercentage = 0.14f },
                    DeathLootModifiers = new Dictionary<string, DeathLootModifier>() { },
                    ResourceModifiers = new Dictionary<string, DeathResourceModifier> {
                        { "Wood", new DeathResourceModifier() { bonusModifer = 1.2f, bonusActions = new List<ResourceGainTypes>(){ ResourceGainTypes.Harvesting } } },
                        { "Ore", new DeathResourceModifier() { bonusModifer = 1.2f, bonusActions = new List<ResourceGainTypes>(){ ResourceGainTypes.Harvesting } } }
                    },
                    SkillModifiers = new Dictionary<string, DeathSkillModifier>() {
                        { "All", new DeathSkillModifier() { bonusModifer = 1.1f } }
                    },
                }
            },
            {
                "Rougelike3", new DeathChoiceLevel() {
                    DisplayName = "Berserker",
                    DeathStyle = new DeathProgressionDetails() { itemLossStyle = ItemLossStyle.DeathlinkBased, itemSavedStyle = ItemSavedStyle.OnCharacter, minItemsKept = 0, maxItemsKept = 3, minSkillLossPercentage = 0.05f, maxSkillLossPercentage = 0.2f },
                    DeathLootModifiers = new Dictionary<string, DeathLootModifier>() {
                        { "AmberPearl", new DeathLootModifier() { chance = 0.05f, prefab = "AmberPearl", bonusActions = new List<ResourceGainTypes>() { ResourceGainTypes.Kills } } }
                    },
                    ResourceModifiers = new Dictionary<string, DeathResourceModifier> {
                        { "Wood", new DeathResourceModifier() { bonusModifer = 1.5f, bonusActions = new List<ResourceGainTypes>(){ ResourceGainTypes.Harvesting } } },
                        { "Ore", new DeathResourceModifier() { bonusModifer = 1.5f, bonusActions = new List<ResourceGainTypes>(){ ResourceGainTypes.Harvesting } } }
                    },
                    SkillModifiers = new Dictionary<string, DeathSkillModifier>() {
                        { "All", new DeathSkillModifier() { bonusModifer = 1.2f } }
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
                        { "Wood", new DeathResourceModifier() { bonusModifer = 2.0f, bonusActions = new List<ResourceGainTypes>(){ ResourceGainTypes.Harvesting } } },
                        { "Stone", new DeathResourceModifier() { bonusModifer = 2.0f, bonusActions = new List<ResourceGainTypes>(){ ResourceGainTypes.Harvesting } } },
                        { "Ore", new DeathResourceModifier() { bonusModifer = 2.0f, bonusActions = new List<ResourceGainTypes>(){ ResourceGainTypes.Harvesting } } }
                    },
                    SkillModifiers = new Dictionary<string, DeathSkillModifier>() {
                        { "All", new DeathSkillModifier() { bonusModifer = 1.3f } }
                    },
                }
            }
        };

        public static Dictionary<long, DeathConfiguration> playerSettings = new Dictionary<long, DeathConfiguration>() { };

        public static Dictionary<string, DeathChoiceLevel> DeathLevels = defaultDeathLevels;

        public static DeathChoiceLevel playerDeathConfiguration;

        public static void CheckAndSetPlayerDeathConfig() {
            if (Player.m_localPlayer == null) { return; }
            long playerID = Player.m_localPlayer.GetPlayerID();
            if (playerSettings.ContainsKey(playerID)) {
                string selectedDeathConfig = playerSettings[playerID].DeathChoiceLevel;
                if (DeathLevels.ContainsKey(selectedDeathConfig)) {
                    playerDeathConfiguration = DeathLevels[selectedDeathConfig];
                } else {
                    Logger.LogWarning("Player preference setting is not an available config, using fallback");
                    playerDeathConfiguration = DeathLevels.First().Value;
                }
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
            File.WriteAllText(ValConfig.characterChoicePath, yamlserializer.Serialize(playerSettings));
        }

        public static void UpdateDeathLevelsConfig(string rawyaml) {
            DeathLevels = yamldeserializer.Deserialize<Dictionary<string, DeathChoiceLevel>>(rawyaml);
        }

        public static void UpdatePlayerConfigSettings(string rawyaml) {
            var added_cfgs = yamldeserializer.Deserialize<Dictionary<long, DeathConfiguration>>(rawyaml);

            foreach (var kvp in added_cfgs) {
                playerSettings.Add(kvp.Key, kvp.Value);
            }
        }
    }
}
