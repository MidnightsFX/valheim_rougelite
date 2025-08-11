using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Deathlink.Death
{
    public static class LootModifiers
    {
        [HarmonyPatch(typeof(CharacterDrop))]
        public static class CalculateLootPerLevelStyle
        {
            //[HarmonyDebug]
            [HarmonyPostfix]
            [HarmonyPatch(nameof(CharacterDrop.GenerateDropList))]
            static void Postfix(List<KeyValuePair<GameObject, int>> __result, CharacterDrop __instance) {
                if (Deathlink.pcfg().DeathLootModifiers == null || Deathlink.pcfg().DeathLootModifiers.Count == 0) { return; }

                List<KeyValuePair<GameObject, int>> modified = new List<KeyValuePair<GameObject, int>>();

                foreach (var kvp in __result) {
                    float mod = Deathlink.pcfg().GetResouceEarlyCache(kvp.Key.name);
                    if (mod == 1f) { 
                        modified.Add(kvp); 
                    } else {
                        int amount = Mathf.RoundToInt(kvp.Value * mod);
                        if (amount >= 1){ modified.Add(new KeyValuePair<GameObject, int>(key: kvp.Key, value: amount)); }
                    }
                }

                List<KeyValuePair<GameObject, int>> killloot = Deathlink.pcfg().RollKillLoot();
                if (killloot.Count > 0) { modified.AddRange(killloot); }

                __result = modified;
            }
        }
    }
}
