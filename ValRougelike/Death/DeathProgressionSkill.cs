using System;
using HarmonyLib;
using JetBrains.Annotations;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.Profiling;
using ValRougelike.Common;

namespace ValRougelike.Death;

public static class DeathProgressionSkill
{
    public static Skills.SkillType DeathSkill = SetupDeathSkill();
    private static float lastSkillIncreaseTickTime = 0f;
    private static float timeSinceGameStart = 0;
    private static float _bossKills = 0f;
    private static float _enemykills = 0f;
    private static float _piecesBuilt = 0f;
    private static float _mineAmount = 0f;
    private static float _treesChopped = 0f;
    private static float _craftAndUpgrades = 0f;
    public static Skills.SkillType SetupDeathSkill()
    {
        SkillConfig deathskill = new SkillConfig();
        deathskill.Name = "Death Defiance";
        deathskill.Description = "How apt you are at avoiding item and skill loss from death.";
        deathskill.Icon = ValRougelike.EmbeddedResourceBundle.LoadAsset<Sprite>("Assets/Custom/Icons/death_skill");
        deathskill.Identifier = "DEATH_DEFIANCE";
        deathskill.IncreaseStep = 1f;
        return SkillManager.Instance.AddSkill(deathskill);
    }

    public static float DeathSkillCalculatePercentWithBonus(float bonus = 0.0f, float min = 0.1f, float max = 1.0f)
    {
        float percentage = 0f;
        if (Player.m_localPlayer != null)
        {
            float player_skill_level = Player.m_localPlayer.GetSkillFactor(DeathSkill);
            percentage += player_skill_level * ValConfig.DeathSkillPerLevelBonus.Value;
        }
        
        percentage += bonus;
        if (percentage < min) {  percentage = min;  }
        if (percentage > max) { percentage = max; }
        return percentage;
    }

    [HarmonyPatch(typeof(Player), nameof(Player.RaiseSkill))]
    public class Deathskill_EXP_Patch
    {
        public static void Postfix(Player __instance)
        {
            timeSinceGameStart += Time.deltaTime;
            if (lastSkillIncreaseTickTime == 0f)
            {
                ValRougelike.Player_death_skill_monitor.Setup();
                lastSkillIncreaseTickTime = timeSinceGameStart + ValConfig.SkillProgressUpdateCheckInterval.Value;
                PlayerProfile profile = Game.instance.GetPlayerProfile();
                _bossKills = profile.m_playerStats.m_stats[PlayerStatType.BossKills];
                _enemykills = profile.m_playerStats.m_stats[PlayerStatType.EnemyKills];
                _piecesBuilt = profile.m_playerStats.m_stats[PlayerStatType.Builds];
                _treesChopped = profile.m_playerStats.m_stats[PlayerStatType.TreeChops];
                _mineAmount = profile.m_playerStats.m_stats[PlayerStatType.Mines];
                _craftAndUpgrades = profile.m_playerStats.m_stats[PlayerStatType.CraftsOrUpgrades];
            }
            Jotunn.Logger.LogDebug($"DeathSkill increase interval check: {timeSinceGameStart} > {lastSkillIncreaseTickTime}");
            if (timeSinceGameStart > lastSkillIncreaseTickTime)
            {
                PlayerProfile profile = Game.instance.GetPlayerProfile();
                float bkillstat = profile.m_playerStats.m_stats[PlayerStatType.BossKills];
                float killstat = profile.m_playerStats.m_stats[PlayerStatType.EnemyKills];
                float builtpieces = profile.m_playerStats.m_stats[PlayerStatType.Builds];
                float treesChopped = profile.m_playerStats.m_stats[PlayerStatType.TreeChops];
                float miningAmount = profile.m_playerStats.m_stats[PlayerStatType.Mines];
                float craftAndUpgrade = profile.m_playerStats.m_stats[PlayerStatType.CraftsOrUpgrades];
                if (bkillstat > _bossKills || killstat > _enemykills || builtpieces > _piecesBuilt || treesChopped > _treesChopped || miningAmount > _mineAmount || craftAndUpgrade > _craftAndUpgrades)
                {
                    // Update the stat and provide a bonus based on boss kills and/or kills
                    if (bkillstat > _bossKills)
                    {
                        float bosskillxp = (bkillstat - _bossKills) * ValConfig.SkillGainOnBossKills.Value;
                        float killxp = (killstat - _enemykills) * ValConfig.SkillGainOnKills.Value;
                        float buildxp = (builtpieces - _piecesBuilt) * ValConfig.SkillGainOnBuilding.Value;
                        float treeharvestxp = (treesChopped - _treesChopped) * ValConfig.SkillGainOnResourceGathering.Value;
                        float mineharvestxp = (miningAmount - _mineAmount) * ValConfig.SkillGainOnResourceGathering.Value;
                        float craftupgradexp = (craftAndUpgrade - _craftAndUpgrades) * ValConfig.SkillGainOnCrafts.Value;
                        float totalxpgain = craftupgradexp + mineharvestxp + treeharvestxp + buildxp + killxp + bosskillxp;
                        Jotunn.Logger.LogDebug($"Raising DeathProgression skill; KillXP:{killxp + bosskillxp} buildXP:{buildxp} harvestXP:{treeharvestxp + mineharvestxp} craftXP:{craftupgradexp} totalXP:{totalxpgain}");
                        Player.m_localPlayer.RaiseSkill(DeathSkill, totalxpgain);
                    }
                    _bossKills = bkillstat;
                    _enemykills = killstat;
                    _piecesBuilt = builtpieces;
                    _treesChopped = treesChopped;
                    _mineAmount = miningAmount;
                    _craftAndUpgrades = craftAndUpgrade;
                }
                    
                // set when we should tick the next increase
                lastSkillIncreaseTickTime = timeSinceGameStart + ValConfig.SkillProgressUpdateCheckInterval.Value;
                    
                // calculate the skill bonus for how long the player has been alive
                float skillbonus = ((float)Math.Sqrt(__instance.m_timeSinceDeath) / 5) + 0.5f;
                Player.m_localPlayer.RaiseSkill(DeathSkill, skillbonus);
            }
        }
    }
}