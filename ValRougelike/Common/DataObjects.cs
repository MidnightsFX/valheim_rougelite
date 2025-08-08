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

    public class DeathProgressionDetails
    {
        public int minItemsKept;
        public int maxItemsKept;
        public int minEquipmentKept;
        public int maxEquipmentKept;
        public float maxSkillLossPercentage;
        public float minSkillLossPercentage;
        public ItemLossStyle itemLossStyle;
        public ItemSavedStyle itemSavedStyle;
        public NonSkillCheckedItemAction nonSkillCheckedItemAction;
    }

    public class DeathResourceModifier
    {
        public bool skillInfluence { get; set; } = true;
        public string prefab { get; set; }
        public float bonusModifer { get; set; }
        public List<ResourceGainTypes> bonusActions { get; set; }
    }

    public class DeathSkillModifier
    {
        public bool skillInfluence { get; set; } = true;
        public string skill { get; set; }
        public float bonusModifer { get; set; }
    }

    public class DeathLootModifier
    {
        bool skillInfluence { get; set; } = true;
        public string prefab { get; set; }
        public float chance { get; set; }
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

        public string GetLootModifiers()
        {
            StringBuilder sb = new StringBuilder();

            foreach (var entry in DeathLootModifiers) {
                sb.AppendLine($"{entry.Value.chance*100}% chance of {entry.Key} dropping from {string.Join(",", entry.Value.bonusActions)}");
            }
            return sb.ToString();
        }

        public string GetSkillModiferDescription() {
            StringBuilder sb = new StringBuilder();

            foreach (var entry in SkillModifiers) {
                if (entry.Value.bonusModifer > 1f) {
                    sb.AppendLine($"{entry.Key} +{Mathf.Round((entry.Value.bonusModifer - 1f)*100)}% XP");
                } else {
                    sb.AppendLine($"{entry.Key} -{Mathf.Round((1f - entry.Value.bonusModifer)*100)}% XP");
                }
            }
            return sb.ToString();
        }

        public string GetResourceModiferDescription() {
            StringBuilder sb = new StringBuilder();

            foreach (var entry in ResourceModifiers) {
                if (entry.Value.bonusModifer > 1f) {
                    sb.AppendLine($"{entry.Key} drops <color=#b9f2ff>{(entry.Value.bonusModifer - 1) * 100}%</color> more when {string.Join(",", entry.Value.bonusActions)}");
                } else {
                    sb.AppendLine($"{entry.Key} drops <color=red>{(1 - entry.Value.bonusModifer) * 100}%</color> less when {string.Join(",", entry.Value.bonusActions)}");
                }
            }

            return sb.ToString();
        }

        public string GetDeathStyleDescription() {
            StringBuilder sb = new StringBuilder();
            
            switch (DeathStyle.itemLossStyle)
            {
                case ItemLossStyle.None:
                    sb.AppendLine($"Items will not be lost on death.");
                    break;
                case ItemLossStyle.DestroyNonWeaponArmor:
                    sb.AppendLine($"Non-equipment gets destroyed on death.");
                    break;
                case ItemLossStyle.DestroyAll:
                    sb.AppendLine($"All items will be destroyed on death.");
                    break;
                case ItemLossStyle.DeathlinkBased:
                    sb.AppendLine($"A limited number of items will be saved, based on Deathlink progression.");
                    sb.AppendLine($"Items kept on death <color=#b9f2ff>{DeathStyle.minItemsKept}</color> - <color=#b9f2ff>{DeathStyle.maxItemsKept}</color>");
                    break;
            }
            //sb.AppendLine();
            if (DeathStyle.itemLossStyle != ItemLossStyle.DestroyAll) {
                if (DeathStyle.itemSavedStyle == ItemSavedStyle.OnCharacter)
                {
                    sb.AppendLine($"Saved items will stay on your character.");
                }
                else
                {
                    sb.AppendLine($"Saved items will be saved to your tombstone.");
                }
            }

            //sb.AppendLine();
            if (DeathStyle.maxSkillLossPercentage == DeathStyle.minSkillLossPercentage) {
                sb.AppendLine($"Skill loss on death <color=red>{DeathStyle.maxSkillLossPercentage * 100f}%</color>");
            } else {
                sb.AppendLine($"Skill loss on death <color=red>{DeathStyle.maxSkillLossPercentage * 100f}%</color> - <color=red>{DeathStyle.minSkillLossPercentage * 100f}%</color>");
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