using System;
using BepInEx.Configuration;

namespace ValRougelike.Common;

public class ValConfig
{
    public static ConfigFile cfg;
    public static ConfigEntry<bool> EnableDebugMode;
    public static ConfigEntry<string> ItemsNotSkillCheckedAction;
    public static ConfigEntry<string> ItemsNotSkillChecked;
    public static ConfigEntry<float> DeathSkillPerLevelBonus;
    public static ConfigEntry<int> MinimumEquipmentRetainedOnDeath;
    public static ConfigEntry<float> MaxPercentResourcesRetainedOnDeath;
    public static ConfigEntry<float> MaxPercentTotalItemsRetainedOnDeath;
    public static ConfigEntry<float> GainedSkillLossFactor;
    public static ConfigEntry<float> SkillGainOnKills;
    public static ConfigEntry<float> SkillGainOnBossKills;
    public static ConfigEntry<float> SkillGainOnCrafts;
    public static ConfigEntry<float> SkillGainOnResourceGathering;
    public static ConfigEntry<float> SkillGainOnBuilding;

    public static ConfigEntry<float> SkillProgressUpdateCheckInterval;
    
    public ValConfig(ConfigFile Config)
    {
        // ensure all the config values are created
        cfg = Config;
        cfg.SaveOnConfigSet = true;
        CreateConfigValues(Config);
        
    }

    // Create Configuration and load it.
    private void CreateConfigValues(ConfigFile Config)
    {
        DeathSkillPerLevelBonus = BindServerConfig("DeathProgression","DeathSkillPerLevelBonus",1f,"How impactful death skill progression is. This impacts how much each level improves your skill and item retention.", false, 0f, 10f);
        MinimumEquipmentRetainedOnDeath = BindServerConfig("DeathProgression","MinimumEquipmentRetainedOnDeath",2,"The maximum amount of Equipment that can be retained on death, depends on players individual skill.", true, 0, 30);
        MaxPercentResourcesRetainedOnDeath = BindServerConfig("DeathProgression","MaxPercentResourcesRetainedOnDeath",20f,"The maximum amount of Resources that can be retained on death, depends on players individual skill.", true, 0f, 100f);
        MaxPercentTotalItemsRetainedOnDeath = BindServerConfig("DeathProgression","MaxPercentTotalItemsRetainedOnDeath",20f,"The maximum amount of total items that can be retained on death, depends on players individual skill.", true, 0f, 100f);
        GainedSkillLossFactor = BindServerConfig("SkillLossModifiers", "GainedSkillLossFactor", 0.2f, "The percentage of gained skills that are lost when dying.", false, 0f, 1f);

        ItemsNotSkillChecked = BindServerConfig("DeathProgression", "ItemsNotSkillChecked", "Tin,TinOre,Copper,CopperOre,CopperScrap,Bronze,Iron,IronScrap,Silver,SilverOre,DragonEgg,chest_hildir1,chest_hildir2,chest_hildir3,BlackMetal,BlackMetalScrap,DvergrNeedle,MechanicalSpring,FlametalNew,FlametalOreNew", "List of items that are not rolled to be saved through death progression.");
        ItemsNotSkillCheckedAction = BindServerConfig("DeathProgression", "ItemsNotSkillCheckedAction", "dropOnDeath", 
            "What happens to non-teleportable items. DropOnDeath = placed into a tombstone on death, AlwaysDestroy = never saved, AlwaysSave = These items are never destroyed and do not count towards save limits.", 
            new AcceptableValueList<string>("DropOnDeath", "AlwaysDestroy", "AlwaysSave"));

        SkillGainOnKills = BindServerConfig("DeathSkillGain", "SkillGainOnKills", 0.05f, "Skill Gain from killing non-boss creatures.");
        SkillGainOnBossKills = BindServerConfig("DeathSkillGain", "SkillGainOnBossKills", 0.2f, "Skill Gain from killing boss creatures.");
        SkillGainOnCrafts = BindServerConfig("DeathSkillGain", "SkillGainOnCrafts", 0.08f, "Skill Gain from crafting.");
        SkillGainOnResourceGathering = BindServerConfig("DeathSkillGain", "SkillGainOnResourceGathering", 0.001f, "Skill Gain from resource gathering.");
        SkillGainOnBuilding = BindServerConfig("DeathSkillGain", "SkillGainOnBuilding", 0.005f, "Skill Gain from resource gathering.");

        SkillProgressUpdateCheckInterval = BindServerConfig("DeathSkillGain", "SkillProgressUpdateCheckInterval", 1200f, "How frequently skill gains are computed and added. More frequently means smaller xp gains more often.", true, 1f, 3600f);
        
        // Debugmode
        EnableDebugMode = Config.Bind("Client config", "EnableDebugMode", false,
            new ConfigDescription("Enables Debug logging for Valheim Armory.",
            null,
            new ConfigurationManagerAttributes { IsAdvanced = true }));
    }

    /// <summary>
    ///  Helper to bind configs for bool types
    /// </summary>
    /// <param name="config_file"></param>
    /// <param name="catagory"></param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="description"></param>
    /// <param name="acceptableValues"></param>>
    /// <param name="advanced"></param>
    /// <returns></returns>
    public static ConfigEntry<bool> BindServerConfig(string catagory, string key, bool value, string description, AcceptableValueBase acceptableValues = null, bool advanced = false)
    {
        return cfg.Bind(catagory, key, value,
            new ConfigDescription(description,
                acceptableValues,
            new ConfigurationManagerAttributes { IsAdminOnly = true, IsAdvanced = advanced })
            );
    }

    /// <summary>
    /// Helper to bind configs for int types
    /// </summary>
    /// <param name="config_file"></param>
    /// <param name="catagory"></param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="description"></param>
    /// <param name="advanced"></param>
    /// <param name="valmin"></param>
    /// <param name="valmax"></param>
    /// <returns></returns>
    public static ConfigEntry<int> BindServerConfig(string catagory, string key, int value, string description, bool advanced = false, int valmin = 0, int valmax = 150)
    {
        return cfg.Bind(catagory, key, value,
            new ConfigDescription(description,
            new AcceptableValueRange<int>(valmin, valmax),
            new ConfigurationManagerAttributes { IsAdminOnly = true, IsAdvanced = advanced })
            );
    }

    /// <summary>
    /// Helper to bind configs for float types
    /// </summary>
    /// <param name="config_file"></param>
    /// <param name="catagory"></param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="description"></param>
    /// <param name="advanced"></param>
    /// <param name="valmin"></param>
    /// <param name="valmax"></param>
    /// <returns></returns>
    public static ConfigEntry<float> BindServerConfig(string catagory, string key, float value, string description, bool advanced = false, float valmin = 0, float valmax = 150)
    {
        return cfg.Bind(catagory, key, value,
            new ConfigDescription(description,
            new AcceptableValueRange<float>(valmin, valmax),
            new ConfigurationManagerAttributes { IsAdminOnly = true, IsAdvanced = advanced })
            );
    }
    
    /// <summary>
    /// Helper to bind configs for strings
    /// </summary>
    /// <param name="config_file"></param>
    /// <param name="catagory"></param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="description"></param>
    /// <param name="advanced"></param>
    /// <returns></returns>
    public static ConfigEntry<string> BindServerConfig(string catagory, string key, string value, string description, AcceptableValueList<string> acceptableValues = null, bool advanced = false)
    {
        return cfg.Bind(catagory, key, value,
            new ConfigDescription(
                description,
                acceptableValues,
            new ConfigurationManagerAttributes { IsAdminOnly = true, IsAdvanced = advanced })
            );
    }
}