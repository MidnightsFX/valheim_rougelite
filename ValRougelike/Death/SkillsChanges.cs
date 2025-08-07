using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Deathlink.Common;
using static Skills;
using Jotunn.Managers;
using System.Linq;
using System;

namespace Deathlink.Death;

public class DeathSkillContainment
{
    // we track skill gains since the last death, and will only calculate losses for the gains
    // skills that did not have any gains since the last death will not see any losses
    public DataObjects.DictionaryZNetProperty PlayerSkillGains;

    public void Setup()
    {
        if (Player.m_localPlayer == null)
        {
            Logger.LogWarning("Death skill setup failed due to player instance not being available.");
            return;
        }
        if (PlayerSkillGains != null) { return; }
        Dictionary<Skills.SkillType, float> skillgain = new Dictionary<Skills.SkillType, float>();
        PlayerSkillGains = new DataObjects.DictionaryZNetProperty("PlayerSkillGains", Player.m_localPlayer.GetComponent<ZNetView>(), skillgain);
    }

    public void AddSkillIncrease(Skills.SkillType skill, float value)
    {
        if (PlayerSkillGains == null) { return; }
        Dictionary<Skills.SkillType, float> psg = PlayerSkillGains.Get();
        if (PlayerSkillGains.Get().ContainsKey(skill)) {
            psg[skill] += value;
        } else {
            psg.Add(skill, value);
        }
        PlayerSkillGains.Set(psg);
    }

    public Dictionary<Skills.SkillType, float> GetSkillGains()
    {
        if (PlayerSkillGains == null) { return new Dictionary<Skills.SkillType, float>(); }
        return PlayerSkillGains.Get();
    }

    public void Clear()
    {
        if (PlayerSkillGains == null) { return; }
        PlayerSkillGains.Set(new Dictionary<Skills.SkillType, float>());
    }

    public bool ContainsKey(Skills.SkillType skill)
    {
        return PlayerSkillGains.Get().ContainsKey(skill);
    }
}

public static class SkillsChanges
{
    static DeathSkillContainment skillGainMonitor = new DeathSkillContainment();
    public static List<Skills.SkillType> skills_to_avoid_standard_death_penalty = new List<Skills.SkillType> { };
    public static List<Skills.SkillType> skills_to_avoid_gain_death_penalty = new List<Skills.SkillType> { };

    [HarmonyPatch(typeof(Skills), nameof(Skills.OnDeath))]
    static class OnDeath_Patch
    {
        private static bool Prefix(Skills __instance)
        {
            if (ValConfig.EnableXPLossFromGainedXP.Value) {
                foreach (KeyValuePair<Skills.SkillType, float> IncreasedSkill in skillGainMonitor.GetSkillGains()) {
                    if (skills_to_avoid_standard_death_penalty.Contains(IncreasedSkill.Key)) {
                        Logger.LogDebug($"Skipping reducing skill gained skill from {IncreasedSkill.Key}");
                        continue;
                    }
                    float lostEXP = IncreasedSkill.Value * ValConfig.GainedSkillLossFactor.Value;
                    __instance.m_skillData[IncreasedSkill.Key].m_level -= lostEXP;
                }
                skillGainMonitor.Clear();
            }
            
            if (ValConfig.EnableSkillsXPLossOnDeath.Value) {
                // Do a decrease of skills, with our configuration
                LowerConfigurableSkills(__instance, ValConfig.DeathXPLoss.Value);
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
        private static void Postfix(Skills __instance, Skills.SkillType skillType, float factor)
        {
            skillGainMonitor.AddSkillIncrease(skillType, factor);
        }
    }

    public static void SkillsWithoutDeathPenaltyChange(object s, EventArgs e) {
        SetupXPSkillList(ValConfig.SkillsWithoutDeathPenalty.Value, skills_to_avoid_standard_death_penalty);
    }

    public static void SkillsWithoutGainDeathPenaltyChange(object s, EventArgs e) {
        SetupXPSkillList(ValConfig.SkillsWithoutGainDeathPenalty.Value, skills_to_avoid_gain_death_penalty);
    }

    public static void SetupSkillsWithoutGains() {
        SetupXPSkillList(ValConfig.SkillsWithoutDeathPenalty.Value, skills_to_avoid_standard_death_penalty);
        SetupXPSkillList(ValConfig.SkillsWithoutGainDeathPenalty.Value, skills_to_avoid_gain_death_penalty);
    }

    public static void SetupXPSkillList(string stringListOfSkills, List<Skills.SkillType> skillList)
    {
        if (Player.m_localPlayer == null) { return; }
        List<Skills.SkillType> tunallowed = new List<Skills.SkillType>() { };
        List<Skills.SkillType> player_skills = Player.m_localPlayer.GetSkills().m_skillData.Keys.ToList();
        bool add_info_about_invalid_enum = false;
        if (stringListOfSkills != "")
        {
            foreach (var item in stringListOfSkills.Split(','))
            {
                // Check Jotun for a registered skill, this covers all custom Jotunn skills
                Skills.SkillDef sd_item = SkillManager.Instance.GetSkill(item);
                if (sd_item != null) { tunallowed.Add(sd_item.m_skill); continue; }

                // Mods which add skills using skill manager do not have a central location to check
                // We are checking skills that the player already has, which means that we won't always get all skills here
                // But without a central registry of skills, or skills adding their enums to the master list- it doesn't matter
                // If the player has got XP for the skill- it will be registered here.
                try {
                    foreach (var pskill in player_skills)
                    {
                        if (pskill.ToString().Equals(item, StringComparison.OrdinalIgnoreCase))
                        {
                            if (!tunallowed.Contains(pskill))
                            {
                                tunallowed.Add(pskill);
                            }
                            break;
                        }
                    }
                }
                catch (Exception ex) {
                    Logger.LogError($"Error parsing {item} as skill enum: {ex}");
                }
            }
        }

        if (tunallowed.Count > 0) {
            skillList.Clear();
            skillList.AddRange(tunallowed);
        }
        if (add_info_about_invalid_enum == true) {
            Logger.LogWarning($"Some of the skills you provided in the config are not valid skill types. Invalid skill types will be ignored. A comma seperated of valid skill names is required.");
            Logger.LogWarning($"Valid skill types are: {string.Join(", ", Skills.s_allSkills)}");
        }
    }
}