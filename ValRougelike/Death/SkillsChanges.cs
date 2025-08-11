using System.Collections.Generic;
using HarmonyLib;
using Deathlink.Common;
using static Skills;
using Jotunn.Managers;
using System.Linq;
using System;

namespace Deathlink.Death;

public static class SkillsChanges
{
    public static List<Skills.SkillType> skills_to_avoid_standard_death_penalty = new List<Skills.SkillType> { };
    public static List<Skills.SkillType> skills_to_avoid_gain_death_penalty = new List<Skills.SkillType> { };

    [HarmonyPatch(typeof(Skills), nameof(Skills.OnDeath))]
    static class OnDeath_Patch
    {
        private static bool Prefix(Skills __instance)
        {
            if (Deathlink.pcfg().DeathStyle.skillLossOnDeath) {
                // Do a decrease of skills, with our configuration
                float skill_based_diff = DeathProgressionSkill.DeathSkillCalculatePercentWithBonus() * (Deathlink.pcfg().DeathStyle.maxSkillLossPercentage - Deathlink.pcfg().DeathStyle.minSkillLossPercentage);
                LowerConfigurableSkills(__instance, skill_based_diff);
            }

            // We are skipping the original skill decrease
            return false;
        }

        private static void LowerConfigurableSkills(Skills skills, float factor)
        {
            foreach (KeyValuePair<SkillType, Skill> skillDatum in skills.m_skillData) {
                if (skills_to_avoid_standard_death_penalty.Contains(skillDatum.Key)) {
                    Logger.LogDebug($"Skipping lowering skill {skillDatum.Key} current level: {skillDatum.Value.m_level}");
                    continue;
                }
                float num = skillDatum.Value.m_level * factor;
                skillDatum.Value.m_level -= num;
                skillDatum.Value.m_accumulator = 0f;
            }

            if (Player.m_localPlayer != null) {
                Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "$msg_skills_lowered");
            }
        }
    }
    
    [HarmonyPatch(typeof(Skills), nameof(Skills.RaiseSkill))]
    static class SkillRaisePatch
    {
        private static void Prefix(Skills __instance, Skills.SkillType skillType, float factor) {
            float mod = Deathlink.pcfg().GetSkillBonusLazyCache(skillType);
            Logger.LogDebug($"{skillType} skillGain Modified {mod}");
            if (mod != 0f) {  factor *= mod; }
        }
    }
}