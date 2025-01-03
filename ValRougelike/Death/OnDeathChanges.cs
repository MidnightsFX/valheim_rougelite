using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Deathlink.Common;

namespace Deathlink.Death;

public static class OnDeathChanges
{
    [HarmonyPatch(typeof(Player), nameof(Player.OnDeath))]
    static class OnDeath_Tombstone_Patch
    {
        private static bool Prefix(Player __instance)
        {
            if (!__instance.m_nview.IsOwner())
            {
                Debug.Log("OnDeath call but not the owner");
                return false;
            }

            bool flag = __instance.HardDeath();
            __instance.m_nview.GetZDO().Set(ZDOVars.s_dead, value: true);
            __instance.m_nview.InvokeRPC(ZNetView.Everybody, "OnDeath");
            Game.instance.IncrementPlayerStat(PlayerStatType.Deaths);
            OnDeathStat(__instance);

            Game.instance.GetPlayerProfile().SetDeathPoint(__instance.transform.position);
            __instance.CreateDeathEffects();
            // Tombstone item dropped modifications
            TombstoneOnDeath(__instance);

            // Set food loss status based on configs & skills
            FoodLossOnDeath(__instance);

            // Vanilla death reset skills
            if (ZoneSystem.instance.GetGlobalKey(GlobalKeys.DeathSkillsReset)) {
                __instance.m_skills.Clear();
            } else if (flag) {
                __instance.m_skills.OnDeath();
            }
            __instance.m_seman.RemoveAllStatusEffects();
            Game.instance.RequestRespawn(10f, afterDeath: true);
            __instance.m_timeSinceDeath = 0f;
            // Maybe remove the gods merciful mention
            if (!flag)
            {
                __instance.Message(MessageHud.MessageType.TopLeft, "$msg_softdeath");
            }
            __instance.Message(MessageHud.MessageType.Center, "$msg_youdied");
            __instance.ShowTutorial("death");

            // Whether or not to spawn the death marker on the map
            if (ValConfig.ShowDeathMapMarker.Value) {
                Minimap.instance.AddPin(__instance.transform.position, Minimap.PinType.Death, $"$hud_mapday {EnvMan.instance.GetDay(ZNet.instance.GetTimeSeconds())}", save: true, isChecked: false, 0L);
            }
            
            if (__instance.m_onDeath != null){
                __instance.m_onDeath();
            }

            string eventLabel = "biome:" + __instance.GetCurrentBiome();
            Gogan.LogEvent("Game", "Death", eventLabel, 0L);

            // Skip the normal ondeath call
            return false;
        }

        public static void FoodLossOnDeath(Player instance)
        {
            if (ValConfig.FoodLossOnDeath.Value)
            {
                if (ValConfig.FoodLossOnDeathBySkillLevel.Value)
                {
                    float skill_level = DeathProgressionSkill.DeathSkillCalculatePercentWithBonus();
                    if (skill_level >= 0.9f)
                    {
                        // We don't remove any food if the players skill percentage is above 90%
                    }
                    else if (skill_level >= 0.6f && skill_level < 0.9f)
                    {
                        instance.m_foods.Remove(instance.m_foods[0]);
                    }
                    else if (skill_level > 0.3f && skill_level < 0.6f)
                    {
                        instance.m_foods.Remove(instance.m_foods[0]);
                        instance.m_foods.Remove(instance.m_foods[0]);
                    }
                    else
                    {
                        instance.m_foods.Clear();
                    }
                }
                else
                {
                    instance.m_foods.Clear();
                }
            }
        }

        public static void TombstoneOnDeath(Player instance)
        {
            List<ItemDrop.ItemData> savedItems = new List<ItemDrop.ItemData>();
            List<ItemDrop.ItemData> savedQuickslots = new List<ItemDrop.ItemData>();
            List<ItemDrop.ItemData> playerItems = instance.m_inventory.GetAllItems();
            List<ItemDrop.ItemData> playerNonSkillCheckItems = new List<ItemDrop.ItemData>();
            List<ItemDrop.ItemData> playerItemsWithoutNonSkillCheckedItems = new List<ItemDrop.ItemData>();

            string[] nonSkillCheckedItems = ValConfig.ItemsNotSkillChecked.Value.Split(',');
            foreach (ItemDrop.ItemData item in playerItems)
            {
                if (nonSkillCheckedItems.Contains(item.m_shared.m_name)) {
                    playerNonSkillCheckItems.Add(item);
                } else {
                    playerItemsWithoutNonSkillCheckedItems.Add(item);
                }
            }
            int numberOfItemsSavable = (int)(playerItems.Count * DeathProgressionSkill.DeathSkillCalculatePercentWithBonus()) + ValConfig.MinimumEquipmentRetainedOnDeath.Value;
            Jotunn.Logger.LogDebug($"Player number of items {playerItems.Count}, savable due to skill {numberOfItemsSavable}");
            if (ValConfig.MaxPercentTotalItemsRetainedOnDeath.Value > ((float)numberOfItemsSavable / playerItems.Count))
            {
                numberOfItemsSavable = (int)(playerItems.Count * (ValConfig.MaxPercentTotalItemsRetainedOnDeath.Value / 100));
                Jotunn.Logger.LogDebug($"Number of items savable reduced due to configured max ({ValConfig.MaxPercentTotalItemsRetainedOnDeath.Value}%) now: {numberOfItemsSavable}");
            }

            // Equipment items are handled differently than resources etc
            List<ItemDrop.ItemData> playerEquipment = playerItemsWithoutNonSkillCheckedItems.GetEquipment();
            if (playerEquipment.Count <= numberOfItemsSavable)
            {
                // we have enough items savable to save all equipment
                // we'll reduce the number of items we save from our potential savable poolsize
                // savedItems.AddRange(playerEquipment);
                foreach (ItemDrop.ItemData item in playerEquipment)
                {
                    Jotunn.Logger.LogDebug($"Saving equipment {item.m_dropPrefab.name}");
                    if (item.m_equipped) { continue; }
                    // If the item is not equipped but is still equipment, it should be saved since we have space for it
                    savedItems.Add(item);
                }
                numberOfItemsSavable -= playerEquipment.Count;
            }
            else
            {
                // we do not have enough to save all equipped items
                // first we shuffle the equipment, so we do not delete the same items each death
                playerEquipment = Deathlink.shuffleList(playerItemsWithoutNonSkillCheckedItems);
                foreach (var equipment in playerEquipment)
                {
                    if (numberOfItemsSavable > 0)
                    {
                        Jotunn.Logger.LogDebug($"Saving equipment {equipment.m_dropPrefab.name}");
                        numberOfItemsSavable -= 1;
                        continue;
                    }
                    instance.UnequipItem(equipment);
                }
                // we saved as much equipment as we could, everything else will be lost
                instance.m_inventory.RemoveUnequipped();
                return;
            }

            if (Deathlink.AzuEPILoaded)
            {
                List<ItemDrop.ItemData> quickslot_items = AzuExtendedPlayerInventory.API.GetQuickSlotsItems();
                List<ItemDrop.ItemData> quickslot_items_to_remove = new List<ItemDrop.ItemData>();
                foreach (ItemDrop.ItemData item in quickslot_items)
                {
                    if (numberOfItemsSavable > 0)
                    {
                        Jotunn.Logger.LogDebug($"Saving quickslot {item.m_dropPrefab.name}");
                        savedQuickslots.Add(item);
                        numberOfItemsSavable -= 1;
                        continue;
                    } else {
                        quickslot_items_to_remove.Add(item);
                    }
                }
                if (quickslot_items_to_remove.Count > 0)
                {
                    foreach (ItemDrop.ItemData remove_item in quickslot_items_to_remove)
                    {
                        Jotunn.Logger.LogDebug($"Deleting {remove_item.m_dropPrefab.name}");
                        quickslot_items.Remove(remove_item);
                    }
                }
            }

            // we still have savable space after saving any equipped items
            if (numberOfItemsSavable > 0)
            {
                // shuffle inventory items that are not equipment
                List<ItemDrop.ItemData> nonEquippableItems = Deathlink.shuffleList(playerItemsWithoutNonSkillCheckedItems.GetNotEquipment());
                int max_number_resources_savable = (int)(ValConfig.MaxPercentResourcesRetainedOnDeath.Value * nonEquippableItems.Count);
                foreach (var item in nonEquippableItems)
                {
                    if (numberOfItemsSavable > 0 && max_number_resources_savable > 0)
                    {
                        Jotunn.Logger.LogDebug($"Saving {item.m_dropPrefab.name}");
                        savedItems.Add(item);
                        numberOfItemsSavable -= 1;
                        max_number_resources_savable -= 1;
                    }
                    else
                    {
                        break;
                    }

                }
            }

            // Empty the inventory, we already have everything that is getting saved copied off.
            instance.m_inventory.RemoveUnequipped();

            // Handle items that are defined in the non-skill-checked section
            // we do this right after clearing the inventory to allow creating a tombstone from the empty inventory (which can just contain our non-skill handled items)
            switch (ValConfig.ItemsNotSkillCheckedAction.Value)
            {
                case "DropOnDeath":
                    foreach (var item in playerNonSkillCheckItems)
                    {
                        // might need to check if we actually can add the item here
                        instance.m_inventory.AddItem(item);
                    }
                    GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(instance.m_tombstone, instance.GetCenterPoint(), instance.transform.rotation);
                    gameObject.GetComponent<Container>().GetInventory().MoveInventoryToGrave(instance.m_inventory);
                    TombStone component = gameObject.GetComponent<TombStone>();
                    PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
                    string name = playerProfile.GetName();
                    long playerId = playerProfile.GetPlayerID();
                    component.Setup(name, playerId);
                    break;
                case "AlwaysDestroy":
                    // we do nothing, these items will be destroyed
                    break;
                case "AlwaysSave":
                    savedItems.AddRange(playerNonSkillCheckItems);
                    break;
            }
            foreach (var item in savedItems)
            {
                // might need to check if we actually can add the item here
                instance.m_inventory.AddItem(item);
            }
            if (savedQuickslots.Count > 0)
            {
                foreach(var item in savedQuickslots)
                {
                    // Readd Azu Quickslot Items
                    if (Deathlink.AzuEPILoaded)
                    {
                        instance.m_inventory.AddItem(item);
                    }
                }
            }
        }

        public static void OnDeathStat(Player instance)
        {
            switch (instance.m_lastHit.m_hitType)
            {
                case HitData.HitType.Undefined:
                    Game.instance.IncrementPlayerStat(PlayerStatType.DeathByUndefined);
                    break;
                case HitData.HitType.EnemyHit:
                    Game.instance.IncrementPlayerStat(PlayerStatType.DeathByEnemyHit);
                    break;
                case HitData.HitType.PlayerHit:
                    Game.instance.IncrementPlayerStat(PlayerStatType.DeathByPlayerHit);
                    break;
                case HitData.HitType.Fall:
                    Game.instance.IncrementPlayerStat(PlayerStatType.DeathByFall);
                    break;
                case HitData.HitType.Drowning:
                    Game.instance.IncrementPlayerStat(PlayerStatType.DeathByDrowning);
                    break;
                case HitData.HitType.Burning:
                    Game.instance.IncrementPlayerStat(PlayerStatType.DeathByBurning);
                    break;
                case HitData.HitType.Freezing:
                    Game.instance.IncrementPlayerStat(PlayerStatType.DeathByFreezing);
                    break;
                case HitData.HitType.Poisoned:
                    Game.instance.IncrementPlayerStat(PlayerStatType.DeathByPoisoned);
                    break;
                case HitData.HitType.Water:
                    Game.instance.IncrementPlayerStat(PlayerStatType.DeathByWater);
                    break;
                case HitData.HitType.Smoke:
                    Game.instance.IncrementPlayerStat(PlayerStatType.DeathBySmoke);
                    break;
                case HitData.HitType.EdgeOfWorld:
                    Game.instance.IncrementPlayerStat(PlayerStatType.DeathByEdgeOfWorld);
                    break;
                case HitData.HitType.Impact:
                    Game.instance.IncrementPlayerStat(PlayerStatType.DeathByImpact);
                    break;
                case HitData.HitType.Cart:
                    Game.instance.IncrementPlayerStat(PlayerStatType.DeathByCart);
                    break;
                case HitData.HitType.Tree:
                    Game.instance.IncrementPlayerStat(PlayerStatType.DeathByTree);
                    break;
                case HitData.HitType.Self:
                    Game.instance.IncrementPlayerStat(PlayerStatType.DeathBySelf);
                    break;
                case HitData.HitType.Structural:
                    Game.instance.IncrementPlayerStat(PlayerStatType.DeathByStructural);
                    break;
                case HitData.HitType.Turret:
                    Game.instance.IncrementPlayerStat(PlayerStatType.DeathByTurret);
                    break;
                case HitData.HitType.Boat:
                    Game.instance.IncrementPlayerStat(PlayerStatType.DeathByBoat);
                    break;
                case HitData.HitType.Stalagtite:
                    Game.instance.IncrementPlayerStat(PlayerStatType.DeathByStalagtite);
                    break;
                default:
                    ZLog.LogWarning("Not implemented death type " + instance.m_lastHit.m_hitType);
                    break;
            }
        }
    }
}