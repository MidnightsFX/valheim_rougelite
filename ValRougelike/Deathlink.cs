using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;
using Deathlink.Common;
using Deathlink.Death;

namespace Deathlink
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid, BepInDependency.DependencyFlags.HardDependency)]
    [NetworkCompatibility(CompatibilityLevel.ClientMustHaveMod, VersionStrictness.Minor)]
    [BepInDependency("Azumatt.AzuExtendedPlayerInventory", BepInDependency.DependencyFlags.SoftDependency)]
    internal class Deathlink : BaseUnityPlugin
    {
        public const string PluginGUID = "MidnightsFX.Deathlink";
        public const string PluginName = "Deathlink";
        public const string PluginVersion = "0.3.3";

        public ValConfig cfg;
        internal static AssetBundle EmbeddedResourceBundle;
        internal static DeathSkillContainment Player_death_skill_monitor;
        internal static bool AzuEPILoaded = false;

        // Use this class to add your own localization to the game
        // https://valheim-modding.github.io/Jotunn/tutorials/localization.html
        public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();

        private void Awake()
        {
            cfg = new ValConfig(Config);
            // Jotunn comes with its own Logger class to provide a consistent Log style for all mods using it

            EmbeddedResourceBundle = AssetUtils.LoadAssetBundleFromResources("Deathlink.AssetsEmbedded.deathless", typeof(Deathlink).Assembly);
            DeathProgressionSkill.SetupDeathSkill();
            Player_death_skill_monitor = new DeathSkillContainment();

            if (AzuExtendedPlayerInventory.API.IsLoaded()) {
                AzuEPILoaded = true;
            }

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
            
            Jotunn.Logger.LogInfo("Death is not the end.");
            
            // To learn more about Jotunn's features, go to
            // https://valheim-modding.github.io/Jotunn/tutorials/overview.html
        }
        
        /// <summary>
        /// Fisher-Yates style list sort for string lists.
        /// </summary>
        /// <param name="inputList"></param>
        /// <returns></returns>
        public static List<T> shuffleList<T>(List<T> inputList)
        {    //take any list of GameObjects and return it with Fischer-Yates shuffle
            int i = 0;
            int t = inputList.Count;
            int r = 0;
            T p = default(T);
            List<T> tempList = new List<T>();
            tempList.AddRange(inputList);

            while (i < t)
            {
                r = UnityEngine.Random.Range(i, tempList.Count);
                p = tempList[i];
                tempList[i] = tempList[r];
                tempList[r] = p;
                i++;
            }

            return tempList;
        }
    }
}