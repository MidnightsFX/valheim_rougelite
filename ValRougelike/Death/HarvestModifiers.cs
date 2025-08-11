using Deathlink.Common;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static InventoryGrid;

namespace Deathlink.Death
{
    public static class HarvestModifiers
    {
        [HarmonyPatch(typeof(TreeLog), nameof(TreeLog.Destroy))]
        public static class IncreaseDropsFromTree
        {
            private static void Postfix(TreeLog __instance, HitData hitData)
            {
                if (Deathlink.pcfg().ResourceModifiers != null && Deathlink.pcfg().ResourceModifiers.Count > 0 && hitData != null && Player.m_localPlayer != null && hitData.m_attacker == Player.m_localPlayer.GetZDOID()) {
                    IncreaseDrops(__instance.m_dropWhenDestroyed, __instance.transform.position);
                }
            }
        }


        [HarmonyPatch(typeof(MineRock5), nameof(MineRock5.RPC_SetAreaHealth))]
        public static class Minerock5DestroyPatch
        {
            private static void Postfix(MineRock5 __instance, long sender, int index, float health)
            {
                if (Deathlink.pcfg().ResourceModifiers != null && Deathlink.pcfg().ResourceModifiers.Count > 0 && Player.m_localPlayer != null && health <= 0)
                {
                    IncreaseDrops(__instance.m_dropItems, __instance.gameObject.transform.position);
                }
            }
        }

        [HarmonyPatch(typeof(Destructible), nameof(Destructible.Destroy))]
        public static class IncreaseDropsFromDestructible
        {
            private static void Prefix(Destructible __instance, HitData hit)
            {
                if (Deathlink.pcfg().ResourceModifiers != null && Deathlink.pcfg().ResourceModifiers.Count > 0 && hit != null && Player.m_localPlayer != null && hit.m_attacker == Player.m_localPlayer.GetZDOID())
                {
                    IncreaseDestructibleDrops(__instance);
                }
            }

            public static void IncreaseDestructibleDrops(Destructible destructible) {
                // Skip drop increases if the rock is set to spawn a fracture when destroyed
                if (destructible.m_spawnWhenDestroyed != null) { return; }

                Vector3 position = destructible.transform.position;
                DropOnDestroyed drops = destructible.GetComponent<DropOnDestroyed>();
                if (drops == null || drops.m_dropWhenDestroyed == null) { return; }

                IncreaseDrops(drops.m_dropWhenDestroyed, position);
            }
        }

        public static void IncreaseDrops(DropTable drops, Vector3 position) {
            List<KeyValuePair<GameObject, int>> drops_to_add = new List<KeyValuePair<GameObject, int>>();

            // Roll harvestable random loot
            List<KeyValuePair<GameObject, int>> harvestloot = Deathlink.pcfg().RollHarvestLoot();
            if (harvestloot.Count > 0) { drops_to_add.AddRange(harvestloot); }

            // Randomize total drop bonus max size?
            int total_drop_increase_size = UnityEngine.Random.Range(drops.m_dropMin, drops.m_dropMax);
            int per_drop_average = Mathf.RoundToInt(total_drop_increase_size / drops.m_drops.Count);
            // Check for loot bonuses on the prefabs in this
            foreach (var drop in drops.m_drops)
            {
                float mod = Deathlink.pcfg().GetResouceEarlyCache(drop.m_item);
                if (mod == 1f) { continue; } // 1f means this item is not modified
                int drop_amount = Mathf.RoundToInt(per_drop_average * mod);
                if (drop_amount <= 0) { continue; }
                drops_to_add.Add(new KeyValuePair<GameObject, int>(key: drop.m_item, drop_amount));
            }

            if (drops_to_add.Count > 0)
            {
                Logger.LogDebug($"Deathlink drop increase.");
                foreach (var drop in drops_to_add)
                {
                    Quaternion rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f);
                    int max_stack_size = drop.Key.GetComponent<ItemDrop>().m_itemData.m_shared.m_maxStackSize;
                    int drop_amount = drop.Value;
                    if (drop_amount > max_stack_size)
                    {
                        int stacks = drop_amount / max_stack_size;
                        for (int i = 0; i < stacks; i++)
                        {
                            var extra_drop = UnityEngine.Object.Instantiate(drop.Key, position, rotation);
                            extra_drop.GetComponent<ItemDrop>().m_itemData.m_stack = max_stack_size;
                            Logger.LogDebug($"Dropping {max_stack_size} of {drop.Key.name} to the world.");
                        }
                        drop_amount -= (max_stack_size * stacks);
                    }
                    var edrop = UnityEngine.Object.Instantiate(drop.Key, position, rotation);
                    edrop.GetComponent<ItemDrop>().m_itemData.m_stack = drop_amount;
                    Logger.LogDebug($"Dropping {drop_amount} of {drop.Key.name} to the world.");
                }
            }
        }

    }
}
