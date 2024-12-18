using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Deathlink.Common;

namespace Deathlink.Death;

public static class TombstoneChanges
{
    [HarmonyPatch(typeof(Player), nameof(Player.CreateTombStone))]
    static class OnDeath_Tombstone_Patch
    {
        private static bool Prefix(Player __instance)
        {
            List<ItemDrop.ItemData> savedItems = new List<ItemDrop.ItemData>();
            List<ItemDrop.ItemData> playerItems = __instance.m_inventory.GetAllItems();
            List<ItemDrop.ItemData> playerNonSkillCheckItems = new List<ItemDrop.ItemData>();
            List<ItemDrop.ItemData> playerItemsWithoutNonSkillCheckedItems = new List<ItemDrop.ItemData>();

            string[] nonSkillCheckedItems = ValConfig.ItemsNotSkillChecked.Value.Split(',');
            foreach (ItemDrop.ItemData item in playerItems)
            {
                if (nonSkillCheckedItems.Contains(item.m_shared.m_name))
                {
                    playerNonSkillCheckItems.Add(item);
                } else {
                    playerItemsWithoutNonSkillCheckedItems.Add(item);
                }
            }
            int numberOfItemsSavable = (int)(playerItems.Count * DeathProgressionSkill.DeathSkillCalculatePercentWithBonus()) + ValConfig.MinimumEquipmentRetainedOnDeath.Value;
            Jotunn.Logger.LogDebug($"Player number of items {playerItems.Count}, savable due to skill {numberOfItemsSavable}");
            if (ValConfig.MaxPercentTotalItemsRetainedOnDeath.Value > ((float)numberOfItemsSavable / playerItems.Count))
            {
                numberOfItemsSavable = (int)(playerItems.Count * (ValConfig.MaxPercentTotalItemsRetainedOnDeath.Value/100));
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
                    if (item.m_equipped) { continue; }
                    // If the item is not equipped but is still equipment, it should be saved since we have space for it
                    savedItems.Add(item);
                }
                numberOfItemsSavable -= playerEquipment.Count;
            } else {
                // we do not have enough to save all equipped items
                // first we shuffle the equipment, so we do not delete the same items each death
                playerEquipment = Deathlink.shuffleList(playerItemsWithoutNonSkillCheckedItems);
                foreach (var equipment in playerEquipment)
                {
                    if (numberOfItemsSavable > 0)
                    {
                        numberOfItemsSavable -= 1;
                        continue;
                    }
                    __instance.UnequipItem(equipment);
                }
                // we saved as much equipment as we could, everything else will be lost
                __instance.m_inventory.RemoveUnequipped();
                return false;
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
            __instance.m_inventory.RemoveUnequipped();
            
            // Handle items that are defined in the non-skill-checked section
            // we do this right after clearing the inventory to allow creating a tombstone from the empty inventory (which can just contain our non-skill handled items)
            switch (ValConfig.ItemsNotSkillCheckedAction.Value)
            {
                case "DropOnDeath":
                    foreach (var item in playerNonSkillCheckItems)
                    {
                        // might need to check if we actually can add the item here
                        __instance.m_inventory.AddItem(item);
                    }
                    GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(__instance.m_tombstone, __instance.GetCenterPoint(), __instance.transform.rotation);
                    gameObject.GetComponent<Container>().GetInventory().MoveInventoryToGrave(__instance.m_inventory);
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
                __instance.m_inventory.AddItem(item);
            }

            //if (ValConfig.FoodLossOnDeath.Value) {
            //    __instance.ClearFood();
            //}
            //if (ValConfig.EffectRemovalOnDeath.Value) {
            //    __instance.m_seman.RemoveAllStatusEffects();
            //}

            return false;
        }
    }
}