using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using UnityEngine;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Deathlink.Common;

public class DataObjects
{
    public static IDeserializer yamldeserializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
    public static ISerializer yamlserializer = new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).DisableAliases().ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults).Build();

    public enum ItemLossStyle
    {
        None,
        DestroyNonWeaponArmor,
        DeathlinkBased,
        DestroyAll
    }

    public enum ItemSavedStyle
    {
        OnCharacter,
        Tombstone
    }

    public enum ResourceGainTypes
    {
        Kills,
        Harvesting
    }

    public enum NonSkillCheckedItemAction
    {
        Destroy,
        Tombstone,
        Save
    }

    const string color_good = "#b9f2ff";
    const string color_bad = "#ff4040";

    public class DeathProgressionDetails
    {
        public bool foodLossOnDeath = true;
        public bool foodLossUsesDeathlink = true;
        public int minItemsKept;
        public int maxItemsKept;
        public int minEquipmentKept;
        public int maxEquipmentKept;
        public bool skillLossOnDeath = true;
        public float maxSkillLossPercentage;
        public float minSkillLossPercentage;
        public ItemLossStyle itemLossStyle;
        public ItemSavedStyle itemSavedStyle;
        public NonSkillCheckedItemAction nonSkillCheckedItemAction = NonSkillCheckedItemAction.Tombstone;
    }

    public class DeathResourceModifier
    {
        public bool skillInfluence { get; set; } = true;
        public List<string> prefabs { get; set; }
        public float bonusModifer { get; set; }
        public List<ResourceGainTypes> bonusActions { get; set; }
    }

    public class DeathSkillModifier
    {
        public bool skillInfluence { get; set; } = true;
        public Skills.SkillType skill { get; set; }
        public float bonusModifer { get; set; }
    }

    public class DeathLootModifier
    {
        bool skillInfluence { get; set; } = true;
        public string prefab { get; set; }
        public float chance { get; set; }
        public int amount { get; set; } = 1;
        public List<ResourceGainTypes> bonusActions { get; set; }
    }

    public class DeathChoiceLevel
    {
        public string DisplayName { get; set; }
        public DeathProgressionDetails DeathStyle { get; set; }
        public float DeathSkillRate { get; set; } = 1f;
        public Dictionary<string, DeathResourceModifier> ResourceModifiers { get; set; }
        public Dictionary<string, DeathSkillModifier> SkillModifiers { get; set; }
        public Dictionary<string, DeathLootModifier> DeathLootModifiers { get; set; }

        private Dictionary<Skills.SkillType, float> CalculatedSkillMods = new Dictionary<Skills.SkillType, float>();
        private Dictionary<string, float> CalculatedResourceMods = new Dictionary<string, float>();
        private bool CalculatedResourceModsCached = false;
        private Dictionary<GameObject, Tuple<float, int>> KillLootModifiers = new Dictionary<GameObject, Tuple<float, int>>();
        private bool CalculatedKillLootModifiersCached = false;
        private Dictionary<GameObject, Tuple<float, int>> ResourceLootModifiers = new Dictionary<GameObject, Tuple<float, int>>();
        private bool CalculatedHarvestLootModifiersCached = false;

        public List<KeyValuePair<GameObject, int>> RollKillLoot() {
            if (CalculatedKillLootModifiersCached == false) {
                if (DeathLootModifiers != null && DeathLootModifiers.Count > 0) {
                    foreach (var kvp in DeathLootModifiers) {
                        if (kvp.Value.bonusActions.Contains(ResourceGainTypes.Kills)) {
                            GameObject lootGO = PrefabManager.Instance.GetPrefab(kvp.Value.prefab);
                            if (lootGO == null) {
                                Logger.LogWarning($"Could not find prefab {kvp.Value.prefab} while building kill loot table, it will be skipped.");
                                continue;
                            }
                            KillLootModifiers.Add(lootGO, new Tuple<float, int>(kvp.Value.chance, kvp.Value.amount));
                        }
                    }
                }
                CalculatedKillLootModifiersCached = true;
            }
            List<KeyValuePair<GameObject, int>> lootresults = new List<KeyValuePair<GameObject, int>>();
            foreach (var kvp in KillLootModifiers) {
                float chanceroll = UnityEngine.Random.value;
                Logger.LogDebug($"Rolling chance loot for: {kvp.Key.gameObject.name} {chanceroll} < {kvp.Value.Item1}");
                if (chanceroll < kvp.Value.Item1) {
                    lootresults.Add(new KeyValuePair<GameObject, int>(kvp.Key, kvp.Value.Item2));
                }
            }
            return lootresults;
        }

        public List<KeyValuePair<GameObject, int>> RollHarvestLoot() {
            if (CalculatedHarvestLootModifiersCached == false) {
                if (DeathLootModifiers != null && DeathLootModifiers.Count > 0) {
                    foreach (var kvp in DeathLootModifiers) {
                        if (kvp.Value.bonusActions.Contains(ResourceGainTypes.Harvesting)) {
                            GameObject lootGO = PrefabManager.Instance.GetPrefab(kvp.Value.prefab);
                            if (lootGO == null) {
                                Logger.LogWarning($"Could not find prefab {kvp.Value.prefab} while building harvest loot table, it will be skipped.");
                                continue;
                            }
                            ResourceLootModifiers.Add(lootGO, new Tuple<float, int>(kvp.Value.chance, kvp.Value.amount));
                        }
                    }
                }
                CalculatedHarvestLootModifiersCached = true;
            }
            List<KeyValuePair<GameObject, int>> lootresults = new List<KeyValuePair<GameObject, int>>();
            foreach (var kvp in ResourceLootModifiers) {
                if (UnityEngine.Random.value < kvp.Value.Item1) {
                    lootresults.Add(new KeyValuePair<GameObject, int>(kvp.Key, kvp.Value.Item2));
                }
            }
            return lootresults;
        }

        public float GetResouceEarlyCache(string prefab) {
            if (CalculatedResourceModsCached == false) {
                Logger.LogDebug($"Building cache entry for {prefab}");
                if (ResourceModifiers != null) {
                    foreach (var entry in ResourceModifiers) {
                        Logger.LogDebug($"Checking resource modifiers {entry.Value.prefabs}");
                        if (entry.Value.prefabs != null) {
                            foreach (string pnam in entry.Value.prefabs) {
                                Logger.LogDebug($"Building cache entry for {pnam} - {entry.Value.bonusModifer}");
                                CalculatedResourceMods.Add(pnam, entry.Value.bonusModifer);
                            }
                        }
                    }
                }
                CalculatedResourceModsCached = true;
            }
            if (CalculatedResourceMods.ContainsKey(prefab)) { return CalculatedResourceMods[prefab]; }
            return 1f;
        }

        public float GetResouceEarlyCache(GameObject prefab) {
            if (prefab == null) { return 1f; }
            return GetResouceEarlyCache(prefab.name);
        }

        public float GetSkillBonusLazyCache(Skills.SkillType skilltype) {
            if (CalculatedSkillMods.TryGetValue(skilltype, out float skillbonus)) {  return skillbonus; }
            float modifier_sum = 0;
            if (SkillModifiers != null && SkillModifiers.Count > 0) {
                foreach (var skillMod in SkillModifiers) {
                    if (skillMod.Value.skill == Skills.SkillType.All || skillMod.Value.skill == skilltype) {
                        modifier_sum += skillMod.Value.bonusModifer;
                    }
                }
            }

            CalculatedSkillMods.Add(skilltype, modifier_sum);
            return modifier_sum;
        }

        public string GetLootModifiersDescription() {
            StringBuilder sb = new StringBuilder();
            foreach (var entry in DeathLootModifiers) {
                sb.AppendLine(Localization.instance.Localize($"<color={color_good}>{entry.Value.chance*100}%</color> $loot_desc_pt1 {entry.Key} $loot_desc_pt2 {string.Join(",", entry.Value.bonusActions)}"));
            }
            return sb.ToString();
        }

        public string GetSkillModiferDescription() {
            StringBuilder sb = new StringBuilder();
            foreach (var entry in SkillModifiers) {
                if (entry.Value.bonusModifer > 1f) {
                    sb.AppendLine(Localization.instance.Localize($"{entry.Key} +<color={color_good}>{Mathf.Round((entry.Value.bonusModifer - 1f)*100)}%</color> $xp"));
                } else {
                    sb.AppendLine(Localization.instance.Localize($"{entry.Key} -<color={color_bad}>{Mathf.Round((1f - entry.Value.bonusModifer)*100)}%</color> $xp"));
                }
            }
            return sb.ToString();
        }

        public string GetResourceModiferDescription() {
            StringBuilder sb = new StringBuilder();
            foreach (var entry in ResourceModifiers) {
                if (entry.Value.bonusModifer > 1f) {
                    sb.AppendLine(Localization.instance.Localize($"{entry.Key} $drops <color={color_good}>{(entry.Value.bonusModifer - 1) * 100}%</color> $more {string.Join(",", entry.Value.bonusActions)}"));
                } else {
                    sb.AppendLine(Localization.instance.Localize($"{entry.Key} $drops <color={color_bad}{(1 - entry.Value.bonusModifer) * 100}%</color> $less {string.Join(",", entry.Value.bonusActions)}"));
                }
            }

            return sb.ToString();
        }

        public string GetDeathStyleDescription() {
            StringBuilder sb = new StringBuilder();
            
            switch (DeathStyle.itemLossStyle)
            {
                case ItemLossStyle.None:
                    sb.AppendLine(Localization.instance.Localize($"$no_item_loss"));
                    break;
                case ItemLossStyle.DestroyNonWeaponArmor:
                    sb.AppendLine(Localization.instance.Localize($"$no_equipment_loss"));
                    break;
                case ItemLossStyle.DestroyAll:
                    sb.AppendLine(Localization.instance.Localize($"$all_item_loss"));
                    break;
                case ItemLossStyle.DeathlinkBased:
                    sb.AppendLine(Localization.instance.Localize($"$limited_saved_deathlink"));
                    sb.AppendLine(Localization.instance.Localize($"$equipment_kept <color={color_good}>{DeathStyle.minEquipmentKept}</color> - <color={color_good}>{DeathStyle.maxEquipmentKept}</color>"));
                    sb.AppendLine(Localization.instance.Localize($"$items_kept <color={color_good}>{DeathStyle.minItemsKept}</color> - <color={color_good}>{DeathStyle.maxItemsKept}</color>"));
                    break;
            }
            //sb.AppendLine();
            if (DeathStyle.itemLossStyle != ItemLossStyle.DestroyAll) {
                if (DeathStyle.itemSavedStyle == ItemSavedStyle.OnCharacter) {
                    sb.AppendLine(Localization.instance.Localize($"$saved_to_character"));
                } else {
                    sb.AppendLine(Localization.instance.Localize($"$saved_to_tombstone"));
                }
                if (DeathStyle.nonSkillCheckedItemAction == NonSkillCheckedItemAction.Tombstone) {
                    sb.AppendLine(Localization.instance.Localize($"$non_skill_items_tombstone"));
                }
                if (DeathStyle.nonSkillCheckedItemAction == NonSkillCheckedItemAction.Save) {
                    sb.AppendLine(Localization.instance.Localize($"$non_skill_items_character"));
                }
                if (DeathStyle.nonSkillCheckedItemAction == NonSkillCheckedItemAction.Destroy) {
                    sb.AppendLine(Localization.instance.Localize($"$non_skill_items_destroy"));
                }
            }

            if (DeathStyle.foodLossOnDeath) {
                if (DeathStyle.foodLossUsesDeathlink) {
                    sb.AppendLine(Localization.instance.Localize($"$food_loss_deathlink"));
                } else {
                    sb.AppendLine(Localization.instance.Localize($"$food_loss"));
                }
            }

            //sb.AppendLine();
            if (DeathStyle.maxSkillLossPercentage == DeathStyle.minSkillLossPercentage) {
                sb.AppendLine(Localization.instance.Localize($"$skill_loss_desc <color={color_bad}>{DeathStyle.maxSkillLossPercentage * 100f}%</color>"));
            } else {
                sb.AppendLine(Localization.instance.Localize($"$skill_loss_desc <color={color_bad}>{DeathStyle.maxSkillLossPercentage * 100f}%</color> - <color={color_bad}>{DeathStyle.minSkillLossPercentage * 100f}%</color> $influenced_by_deathlink"));
            }
                

            return sb.ToString();
        }
    }

    public class DeathConfiguration
    {
        public string DeathChoiceLevel { get; set; }
    }

    public class PlayerDeathConfiguration {
        public Dictionary<long, DeathConfiguration> selectedDeathStyle { get; set; }
    }

    public abstract class ZNetProperty<T>
    {
        public string Key { get; private set; }
        public T DefaultValue { get; private set; }
        protected readonly ZNetView zNetView;

        protected ZNetProperty(string key, ZNetView zNetView, T defaultValue)
        {
            Key = key;
            DefaultValue = defaultValue;
            this.zNetView = zNetView;
        }

        private void ClaimOwnership()
        {
            if (!zNetView.IsOwner())
            {
                zNetView.ClaimOwnership();
            }
        }

        public void Set(T value)
        {
            SetValue(value);
        }

        public void ForceSet(T value)
        {
            ClaimOwnership();
            Set(value);
        }

        public abstract T Get();

        protected abstract void SetValue(T value);
    }

    public class BoolZNetProperty : ZNetProperty<bool>
    {
        public BoolZNetProperty(string key, ZNetView zNetView, bool defaultValue) : base(key, zNetView, defaultValue)
        {
        }

        public override bool Get()
        {
            return zNetView.GetZDO().GetBool(Key, DefaultValue);
        }

        protected override void SetValue(bool value)
        {
            zNetView.GetZDO().Set(Key, value);
        }
    }

    public class IntZNetProperty : ZNetProperty<int>
    {
        public IntZNetProperty(string key, ZNetView zNetView, int defaultValue) : base(key, zNetView, defaultValue)
        {
        }

        public override int Get()
        {
            return zNetView.GetZDO().GetInt(Key, DefaultValue);
        }

        protected override void SetValue(int value)
        {
            zNetView.GetZDO().Set(Key, value);
        }
    }

    public class StringZNetProperty : ZNetProperty<string>
    {
        public StringZNetProperty(string key, ZNetView zNetView, string defaultValue) : base(key, zNetView, defaultValue)
        {
        }

        public override string Get()
        {
            return zNetView.GetZDO().GetString(Key, DefaultValue);
        }

        protected override void SetValue(string value)
        {
            zNetView.GetZDO().Set(Key, value);
        }
    }

    public class Vector3ZNetProperty : ZNetProperty<Vector3>
    {
        public Vector3ZNetProperty(string key, ZNetView zNetView, Vector3 defaultValue) : base(key, zNetView, defaultValue)
        {
        }

        public override Vector3 Get()
        {
            return zNetView.GetZDO().GetVec3(Key, DefaultValue);
        }

        protected override void SetValue(Vector3 value)
        {
            zNetView.GetZDO().Set(Key, value);
        }
    }

    public class DictionaryZNetProperty : ZNetProperty<Dictionary<Skills.SkillType, float>>
    {
        BinaryFormatter binFormatter = new BinaryFormatter();
        public DictionaryZNetProperty(string key, ZNetView zNetView, Dictionary<Skills.SkillType, float> defaultValue) : base(key, zNetView, defaultValue)
        {
        }

        public override Dictionary<Skills.SkillType, float> Get()
        {
            var stored = zNetView.GetZDO().GetByteArray(Key);
            // we can't deserialize a null buffer
            if (stored == null) { return new Dictionary<Skills.SkillType, float>(); }
            var mStream = new MemoryStream(stored);
            var deserializedDictionary = (Dictionary<Skills.SkillType, float>)binFormatter.Deserialize(mStream);
            return deserializedDictionary;
        }

        protected override void SetValue(Dictionary<Skills.SkillType, float> value)
        {
            
            var mStream = new MemoryStream();
            binFormatter.Serialize(mStream, value);

            zNetView.GetZDO().Set(Key, mStream.ToArray());
        }

        public void UpdateDictionary()
        {
            
        }
    }

    public class ZDOIDZNetProperty : ZNetProperty<ZDOID>
    {
        public ZDOIDZNetProperty(string key, ZNetView zNetView, ZDOID defaultValue) : base(key, zNetView, defaultValue)
        {
        }

        public override ZDOID Get()
        {
            return zNetView.GetZDO().GetZDOID(Key);
        }

        protected override void SetValue(ZDOID value)
        {
            zNetView.GetZDO().Set(Key, value);
        }
    }
}