using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using ValRougelike.Common;

namespace ValRougelike.Death;

public class DeathSkillContainment
{
    // we track skill gains since the last death, and will only calculate losses for the gains
    // skills that did not have any gains since the last death will not see any losses
    public DataObjects.DictionaryZNetProperty PlayerSkillGains;

    public void Setup()
    {
        if (Player.m_localPlayer == null)
        {
            Jotunn.Logger.LogWarning("Death skill setup failed due to player instance not being available.");
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
    
    [HarmonyPatch(typeof(Skills), nameof(Skills.OnDeath))]
    static class OnDeath_Patch
    {
        private static bool Prefix(Skills __instance)
        {
            foreach (KeyValuePair<Skills.SkillType, float> IncreasedSkill in skillGainMonitor.GetSkillGains())
            {
                float lostEXP = IncreasedSkill.Value * ValConfig.GainedSkillLossFactor.Value;
                __instance.m_skillData[IncreasedSkill.Key].m_level -= lostEXP;
            }
            skillGainMonitor.Clear();
            // We are skipping the original skill decrease
            return false;
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
}