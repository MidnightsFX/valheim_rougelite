using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using ValRougelike.Common;

namespace ValRougelike.Death;


public class DeathSkillContainment : MonoBehaviour
{
    // we track skill gains since the last death, and will only calculate losses for the gains
    // skills that did not have any gains since the last death will not see any losses
    public DataObjects.DictionaryZNetProperty PlayerSkillGains;

    protected ZNetView zNetView;

    public void Awake()
    {
        this.gameObject.AddComponent<ZNetView>();
        zNetView = this.gameObject.GetComponent<ZNetView>();
        Dictionary<Skills.SkillType, float> skillgain = new Dictionary<Skills.SkillType, float>();
        PlayerSkillGains = new DataObjects.DictionaryZNetProperty("PlayerSkillGains", zNetView, skillgain);
    }

    public void AddSkillIncrease(Skills.SkillType skill, float value)
    {
        PlayerSkillGains.Get().Add(skill, value);
    }
    
    public void IncreaseSkill()
    {
        
    }

    Dictionary<Skills.SkillType, float> GetSkillGains()
    {
        return PlayerSkillGains.Get();
    }

    public void Clear()
    {
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
            foreach (KeyValuePair<Skills.SkillType, float> IncreasedSkill in skillGainMonitor.PlayerSkillGains())
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
            if (skillGainMonitor.ContainsKey(skillType))
            {
                PlayerSkillGains[skillType] += factor;
            } else {
                PlayerSkillGains.Add(skillType, factor);
            }
        }
    }
}