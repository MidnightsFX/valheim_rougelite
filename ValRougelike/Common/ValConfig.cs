using BepInEx;
using BepInEx.Configuration;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections;
using System.IO;

namespace Deathlink.Common;

public class ValConfig
{
    public static ConfigFile cfg;
    public static ConfigEntry<bool> EnableDebugMode;
    public static ConfigEntry<string> ItemsNotSkillChecked;
    public static ConfigEntry<float> SkillGainOnKills;
    public static ConfigEntry<float> SkillGainOnBossKills;
    public static ConfigEntry<float> SkillGainOnCrafts;
    public static ConfigEntry<float> SkillGainOnResourceGathering;
    public static ConfigEntry<float> SkillGainOnBuilding;
    public static ConfigEntry<bool> ShowDeathMapMarker;
    //public static ConfigEntry<bool> EffectRemovalOnDeath;

    const string cfgFolder = "Deathlink";
    const string deathChoicesCfg = "DeathChoices.yaml";
    const string deathSettingsCfg = "CharacterSettings.yaml";
    internal static String deathChoicesPath = Path.Combine(Paths.ConfigPath, cfgFolder, deathChoicesCfg);
    internal static String playerSettingsPath = Path.Combine(Paths.ConfigPath, cfgFolder, deathSettingsCfg);

    private static CustomRPC deathChoiceRPC;
    private static CustomRPC characterSettingRPC;

    public static ConfigEntry<float> SkillProgressUpdateCheckInterval;
    
    public ValConfig(ConfigFile Config)
    {
        // ensure all the config values are created
        cfg = Config;
        cfg.SaveOnConfigSet = true;
        CreateConfigValues(Config);
        SetupConfigRPCs();
        LoadYamlConfigs();
    }

    public static string GetSecondaryConfigDirectoryPath() {
        var patchesFolderPath = Path.Combine(Paths.ConfigPath, cfgFolder);
        var dirInfo = Directory.CreateDirectory(patchesFolderPath);

        return dirInfo.FullName;
    }

    public void SetupConfigRPCs()
    {
        deathChoiceRPC = NetworkManager.Instance.AddRPC("DEATHLK_CH", OnServerRecieveConfigs, OnClientReceiveDeathChoiceConfigs);
        characterSettingRPC = NetworkManager.Instance.AddRPC("DEATHLK_PSET", OnServerRecievePlayerSettingsConfig, OnClientReceivePlayerSettingsConfigs);

        SynchronizationManager.Instance.AddInitialSynchronization(deathChoiceRPC, SendDeathChoices);
        SynchronizationManager.Instance.AddInitialSynchronization(characterSettingRPC, SendCharSettings);
    }

    // Create Configuration and load it.
    private void CreateConfigValues(ConfigFile Config)
    {
        ItemsNotSkillChecked = BindServerConfig("DeathProgression", "ItemsNotSkillChecked", "Tin,TinOre,Copper,CopperOre,CopperScrap,Bronze,Iron,IronScrap,Silver,SilverOre,DragonEgg,chest_hildir1,chest_hildir2,chest_hildir3,BlackMetal,BlackMetalScrap,DvergrNeedle,MechanicalSpring,FlametalNew,FlametalOreNew", "List of items that are not rolled to be saved through death progression.");

        SkillGainOnKills = BindServerConfig("DeathSkillGain", "SkillGainOnKills", 5f, "Skill Gain from killing non-boss creatures.");
        SkillGainOnBossKills = BindServerConfig("DeathSkillGain", "SkillGainOnBossKills", 20f, "Skill Gain from killing boss creatures.");
        SkillGainOnCrafts = BindServerConfig("DeathSkillGain", "SkillGainOnCrafts", 0.8f, "Skill Gain from crafting.");
        SkillGainOnResourceGathering = BindServerConfig("DeathSkillGain", "SkillGainOnResourceGathering", 0.1f, "Skill Gain from resource gathering.");
        SkillGainOnBuilding = BindServerConfig("DeathSkillGain", "SkillGainOnBuilding", 0.5f, "Skill Gain from building.");

        SkillProgressUpdateCheckInterval = BindServerConfig("DeathSkillGain", "SkillProgressUpdateCheckInterval", 1f, "How frequently skill gains are computed and added. More frequently means smaller xp gains more often.", true, 0.1f, 5f);

        ShowDeathMapMarker = BindServerConfig("DeathTweaks", "ShowDeathMapMarker", true, "Whether or not a map marker is placed on your death location.");

        // Debugmode
        EnableDebugMode = Config.Bind("Client config", "EnableDebugMode", false,
            new ConfigDescription("Enables Debug logging.",
            null,
            new ConfigurationManagerAttributes { IsAdvanced = true }));
        EnableDebugMode.SettingChanged += Logger.enableDebugLogging;
        Logger.CheckEnableDebugLogging();
    }

    internal void LoadYamlConfigs()
    {
        string externalConfigFolder = ValConfig.GetSecondaryConfigDirectoryPath();
        string[] presentFiles = Directory.GetFiles(externalConfigFolder);
        bool foundDeathChoices = false;
        bool foundCharacterSettings = false;

        foreach (string configFile in presentFiles)
        {
            if (configFile.Contains(deathChoicesCfg))
            {
                Logger.LogDebug($"Found Deathchoice configuration: {configFile}");
                deathChoicesPath = configFile;
                foundDeathChoices = true;
            }

            if (configFile.Contains(deathSettingsCfg))
            {
                Logger.LogDebug($"Found Character configuration: {configFile}");
                playerSettingsPath = configFile;
                foundCharacterSettings = true;
            }
        }

        if (foundDeathChoices == false)
        {
            Logger.LogDebug("Death Choices missing, recreating.");
            using (StreamWriter writetext = new StreamWriter(deathChoicesPath))
            {
                String header = @"#################################################
# Deathlink - Death Choice Configuration
#################################################
";
                writetext.WriteLine(header);
                writetext.WriteLine(DeathConfigurationData.DeathLevelsYamlDefaultConfig());
            }
        }

        if (foundCharacterSettings == false)
        {
            Logger.LogDebug("Character Settings missing, recreating.");
            using (StreamWriter writetext = new StreamWriter(playerSettingsPath))
            {
                String header = @"#################################################
# Deathlink - Character settings
#################################################
";
                writetext.WriteLine(header);
                writetext.WriteLine(DeathConfigurationData.PlayerSettingsDefaultConfig());
            }
        }

        SetupFileWatcher(deathChoicesCfg);
        SetupFileWatcher(deathSettingsCfg);
    }

    private void SetupFileWatcher(string filtername)
    {
        FileSystemWatcher fw = new FileSystemWatcher();
        fw.Path = ValConfig.GetSecondaryConfigDirectoryPath();
        fw.NotifyFilter = NotifyFilters.LastWrite;
        fw.Filter = filtername;
        fw.Changed += new FileSystemEventHandler(UpdateConfigFileOnChange);
        fw.Created += new FileSystemEventHandler(UpdateConfigFileOnChange);
        fw.Renamed += new RenamedEventHandler(UpdateConfigFileOnChange);
        fw.SynchronizingObject = ThreadingHelper.SynchronizingObject;
        fw.EnableRaisingEvents = true;
    }

    private static void UpdateConfigFileOnChange(object sender, FileSystemEventArgs e)
    {
        if (SynchronizationManager.Instance.PlayerIsAdmin == false)
        {
            Logger.LogInfo("Player is not an admin, and not allowed to change local configuration. Ignoring.");
            return;
        }
        if (!File.Exists(e.FullPath)) { return; }

        string filetext = File.ReadAllText(e.FullPath);
        var fileInfo = new FileInfo(e.FullPath);
        Logger.LogDebug($"Filewatch changes from: ({fileInfo.Name}) {fileInfo.FullName}");
        switch (fileInfo.Name)
        {
            case deathChoicesCfg:
                Logger.LogDebug("Triggering Death Choices Settings update.");
                DeathConfigurationData.UpdateDeathLevelsConfig(filetext);
                deathChoiceRPC.SendPackage(ZNet.instance.m_peers, SendFileAsZPackage(e.FullPath));
                break;
            //case deathSettingsCfg:
            //    Logger.LogDebug("Triggering Level Settings update.");
            //    // LevelSystemData.UpdateYamlConfig(filetext);
            //    characterSettingRPC.SendPackage(ZNet.instance.m_peers, SendFileAsZPackage(e.FullPath));
            //    break;
        }
    }

    private static IEnumerator OnClientReceiveDeathChoiceConfigs(long sender, ZPackage package) {
        var yaml = package.ReadString();
        DeathConfigurationData.UpdateDeathLevelsConfig(yaml);
        DeathConfigurationData.WriteDeathChoices();
        yield return null;
    }

    private static IEnumerator OnServerRecieveConfigs(long sender, ZPackage package)
    {
        yield return null;
    }

    private static IEnumerator OnServerRecievePlayerSettingsConfig(long sender, ZPackage package)
    {
        var yaml = package.ReadString();
        DeathConfigurationData.UpdatePlayerConfigSettings(yaml);
        DeathConfigurationData.WritePlayerChoices();
        yield return null;
    }

    private static IEnumerator OnClientReceivePlayerSettingsConfigs(long sender, ZPackage package)
    {
        // only updates in memory
        var yaml = package.ReadString();
        DeathConfigurationData.UpdatePlayerConfigSettings(yaml);
        DeathConfigurationData.CheckAndSetPlayerDeathConfig();
        yield return null;
    }

    private static ZPackage SendFileAsZPackage(string filepath)
    {
        string filecontents = File.ReadAllText(filepath);
        ZPackage package = new ZPackage();
        package.Write(filecontents);
        return package;
    }

    private static ZPackage SendCharSettings()
    {
        return SendFileAsZPackage(playerSettingsPath);
    }
    private static ZPackage SendDeathChoices()
    {
        return SendFileAsZPackage(deathChoicesPath);
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