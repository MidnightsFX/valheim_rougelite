using System;
using HarmonyLib;
using JetBrains.Annotations;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;
using ValRougelike.Common;

namespace ValRougelike.Death;

public static class DeathProgressionSkill
{
    public static Skills.SkillType DeathSkill = SetupDeathSkill();
    private static float lastSkillIncreaseTickTime = 0f;
    private static readonly float increaseSkillInterval = 15f;
    public static Skills.SkillType SetupDeathSkill()
    {
        SkillConfig deathskill = new SkillConfig();
        deathskill.Name = "Death Defiance";
        deathskill.Description = "How apt you are at avoiding item and skill loss from death.";
        // icon
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

    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    public class Deathskill_EXP_Patch
    {
        public static void Postfix(Player __instance)
        {
            if (Player.m_localPlayer == __instance)
            {
                float fdt = Time.fixedDeltaTime;
                if (lastSkillIncreaseTickTime == 0f)
                {
                    lastSkillIncreaseTickTime = fdt + increaseSkillInterval;
                }

                if (fdt > lastSkillIncreaseTickTime)
                {
                    // set when we should tick the next increase
                    lastSkillIncreaseTickTime = fdt + increaseSkillInterval;
                    
                    // calculate the skill bonus for how long the player has been alive
                    float skillbonus = ((float)Math.Sqrt(__instance.m_timeSinceDeath) / 5) + 0.5f;
                    Player.m_localPlayer.RaiseSkill(DeathSkill, skillbonus);
                }
                
            }
        }
        
        
    }
}