using System.Collections.Generic;
using HarmonyLib;
using ValRougelike.Common;

namespace ValRougelike.Death;

public static class SkillsChanges
{
    // we track skill gains since the last death, and will only calculate losses for the gains
    // skills that did not have any gains since the last death will not see any losses
    public static Dictionary<Skills.SkillType, float> PlayerSkillGains = new Dictionary<Skills.SkillType, float> { };
    
    [HarmonyPatch(typeof(Skills), nameof(Skills.OnDeath))]
    static class OnDeath_Patch
    {
        private static bool Prefix(Skills __instance)
        {
            foreach (KeyValuePair<Skills.SkillType, float> IncreasedSkill in PlayerSkillGains)
            {
                float lostEXP = IncreasedSkill.Value * ValConfig.GainedSkillLossFactor.Value;
                __instance.m_skillData[IncreasedSkill.Key].m_level -= lostEXP;
            }
            PlayerSkillGains.Clear();
            // We are skipping the original skill decrease
            return false;
        }
    }
    
    [HarmonyPatch(typeof(Skills), nameof(Skills.RaiseSkill))]
    static class SkillRaisePatch
    {
        private static void Postfix(Skills __instance, Skills.SkillType skillType, float factor)
        {
            if (PlayerSkillGains.ContainsKey(skillType))
            {
                PlayerSkillGains[skillType] += factor;
            } else {
                PlayerSkillGains.Add(skillType, factor);
            }
        }
    }
}