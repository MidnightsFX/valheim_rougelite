using BepInEx.Configuration;

namespace ValRougelike.Common;

public class ValConfig
{
    public static ConfigFile cfg;
    public static ConfigEntry<bool> EnableDebugMode;
    public static ConfigEntry<float> DeathSkillPerLevelBonus;
    public static ConfigEntry<float> MaxPercentEquipmentRetainedOnDeath;
    public static ConfigEntry<float> MaxPercentResourcesRetainedOnDeath;
    public static ConfigEntry<float> MaxPercentTotalItemsRetainedOnDeath;
    
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
        MaxPercentEquipmentRetainedOnDeath = BindServerConfig("DeathProgression","MaxPercentEquipmentRetainedOnDeath",100f,"The maximum amount of Equipment that can be retained on death, depends on players individual skill.", true, 0f, 100f);
        MaxPercentResourcesRetainedOnDeath = BindServerConfig("DeathProgression","MaxPercentResourcesRetainedOnDeath",20f,"The maximum amount of Resources that can be retained on death, depends on players individual skill.", true, 0f, 100f);
        MaxPercentTotalItemsRetainedOnDeath = BindServerConfig("DeathProgression","MaxPercentTotalItemsRetainedOnDeath",20f,"The maximum amount of total items that can be retained on death, depends on players individual skill.", true, 0f, 100f);
        
        
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
    /// <param name="advanced"></param>
    /// <returns></returns>
    public static ConfigEntry<bool> BindServerConfig(string catagory, string key, bool value, string description, bool advanced = false)
    {
        return cfg.Bind(catagory, key, value,
            new ConfigDescription(description,
            null,
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
    public static ConfigEntry<string> BindServerConfig(string catagory, string key, string value, string description, bool advanced = false)
    {
        return cfg.Bind(catagory, key, value,
            new ConfigDescription(description, null,
            new ConfigurationManagerAttributes { IsAdminOnly = true, IsAdvanced = advanced })
            );
    }
}