using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Deathlink.Common;
using System.ComponentModel;

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
                if (nonSkillCheckedItems.Contains(item.m_dropPrefab.name)) {
                    playerNonSkillCheckItems.Add(item);
                } else {
                    playerItemsWithoutNonSkillCheckedItems.Add(item);
                }
            }
            int numberOfItemsSavable = 0;
            if (ValConfig.DeathSkillPercentageStyle.Value == "InventorySize")
            {
                int inventory_size = instance.m_inventory.m_width * instance.m_inventory.m_height;
                if (Deathlink.AzuEPILoaded) {
                    int azu_quickslots = AzuExtendedPlayerInventory.API.GetQuickSlots().SlotNames.Count();
                    inventory_size += azu_quickslots;
                    Jotunn.Logger.LogDebug($"Azu quickslots increasing total inventory size by {azu_quickslots}");
                }

                numberOfItemsSavable = (int)(inventory_size * DeathProgressionSkill.DeathSkillCalculatePercentWithBonus()) + ValConfig.MinimumEquipmentRetainedOnDeath.Value;
            } else {
                numberOfItemsSavable = (int)(playerItems.Count * DeathProgressionSkill.DeathSkillCalculatePercentWithBonus()) + ValConfig.MinimumEquipmentRetainedOnDeath.Value;
            }
            
            Jotunn.Logger.LogDebug($"Player number of items {playerItems.Count}, savable due to skill {numberOfItemsSavable}");
            if (ValConfig.MaxPercentTotalItemsRetainedOnDeath.Value < ((float)numberOfItemsSavable / playerItems.Count))
            {
                numberOfItemsSavable = (int)(playerItems.Count * (ValConfig.MaxPercentTotalItemsRetainedOnDeath.Value / 100));
                Jotunn.Logger.LogDebug($"Number of items savable reduced due to configured max ({ValConfig.MaxPercentTotalItemsRetainedOnDeath.Value}%) now: {numberOfItemsSavable}");
            }

            if (Deathlink.AzuEPILoaded)
            {
                foreach (ItemDrop.ItemData item in AzuExtendedPlayerInventory.API.GetQuickSlotsItems())
                {
                    if (nonSkillCheckedItems.Contains(item.m_shared.m_name)) {
                        playerNonSkillCheckItems.Add(item);
                    } else {
                        playerItemsWithoutNonSkillCheckedItems.Add(item);
                    }
                }
            }

            // Equipment items are handled differently than resources etc
            List <ItemDrop.ItemData> playerEquipment = Deathlink.shuffleList(playerItemsWithoutNonSkillCheckedItems.GetEquipment());
            int max_percent_equipment_savable = (int)(playerEquipment.Count * (ValConfig.MaximumPercentEquipmentRetainedOnDeath.Value / 100));
            int equipment_saved = 0;
            if (playerEquipment.Count <= numberOfItemsSavable)
            {
                // we have enough items savable to save all equipment
                // we'll reduce the number of items we save from our potential savable poolsize
                Jotunn.Logger.LogDebug($"Can save all equipment.");
                foreach (ItemDrop.ItemData equipment in playerEquipment) {
                    RemoveEquipmentByStyle(equipment_saved, max_percent_equipment_savable, instance, equipment, savedItems);
                }
                numberOfItemsSavable -= playerEquipment.Count;
            } else {
                Jotunn.Logger.LogDebug($"Not enough to save all equipment.");
                // we do not have enough to save all equipped items
                // first we shuffle the equipment, so we do not delete the same items each death
                foreach (var equipment in playerEquipment)
                {
                    if (numberOfItemsSavable > 0) {
                        RemoveEquipmentByStyle(equipment_saved, max_percent_equipment_savable, instance, equipment, savedItems, numberOfItemsSavable);
                    } else {
                        instance.m_inventory.RemoveItem(equipment);
                    }
                }
                // we saved as much equipment as we could, everything else will be lost
                instance.m_inventory.RemoveUnequipped();
                return;
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


            // Quickslot changes happen after the inventory is cleaned up to avoid overstuffing the inventory
            if (Deathlink.AzuEPILoaded)
            {
                Jotunn.Logger.LogDebug($"Quickslot items found {AzuExtendedPlayerInventory.API.GetQuickSlotsItems().Count}");
                // If the item was not saved, it should be removed from the quickslots
                foreach (var item in AzuExtendedPlayerInventory.API.GetQuickSlotsItems())
                {
                    if (savedItems.Contains(item)) { continue; }
                    Jotunn.Logger.LogDebug($"Removing quickslot item that was not saved {item.m_dropPrefab.name}");
                    instance.UnequipItem(item);
                    instance.m_inventory.RemoveItem(item);
                }
            }

            // Handle items that are defined in the non-skill-checked section
            // we do this right after clearing the inventory to allow creating a tombstone from the empty inventory (which can just contain our non-skill handled items)
            switch (ValConfig.ItemsNotSkillCheckedAction.Value)
            {
                case "DropOnDeath":
                    Jotunn.Logger.LogDebug("Dropping non-skill-checked items on death");
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
                    Jotunn.Logger.LogDebug("Destroying non-skill-checked items on death");
                    // we do nothing, these items will be destroyed
                    break;
                case "AlwaysSave":
                    Jotunn.Logger.LogDebug("Saving non-skill-checked items on death");
                    savedItems.AddRange(playerNonSkillCheckItems);
                    break;
            }
            foreach (var item in savedItems)
            {
                // might need to check if we actually can add the item here
                instance.m_inventory.AddItem(item);
            }

            if (ValConfig.ItemsSavedToTombstone.Value) {
                instance.CreateTombStone();
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

        internal static void RemoveEquipmentByStyle(int equipment_saved, int max_percent_equipment_savable, Player instance, ItemDrop.ItemData equipment, List<ItemDrop.ItemData> saved_equipment, int numberOfItemsSavable = 0)
        {
            if (ValConfig.MaximumEquipmentRetainedStyle.Value == "Percentage")
            {
                if (equipment_saved >= max_percent_equipment_savable)
                {
                    Jotunn.Logger.LogDebug($"Max equipment retained ({max_percent_equipment_savable}) reached, deleting {equipment.m_dropPrefab.name}");
                    instance.m_inventory.RemoveItem(equipment);
                    return;
                }
            }
            else
            {
                if (equipment_saved >= ValConfig.MaximumEquipmentRetainedOnDeath.Value)
                {
                    Jotunn.Logger.LogDebug($"Max equipment retained ({ValConfig.MaximumEquipmentRetainedOnDeath.Value}) reached, deleting {equipment.m_dropPrefab.name}");
                    instance.m_inventory.RemoveItem(equipment);
                    return;
                }
            }

            Jotunn.Logger.LogDebug($"Saving equipment remaining savable?({numberOfItemsSavable}) {equipment.m_dropPrefab.name}");
            equipment_saved = equipment_saved + 1;
            numberOfItemsSavable = numberOfItemsSavable - 1;
            if (equipment.m_equipped)
            {
                return;
            }
            // If the item is not equipped but is still equipment, it should be saved since we have space for it
            saved_equipment.Add(equipment);
        }
    }
}