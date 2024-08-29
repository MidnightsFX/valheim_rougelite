using System.Collections.Generic;
using HarmonyLib;
using ValRougelike.Common;

namespace ValRougelike.Death;

public static class TombstoneChanges
{
    [HarmonyPatch(typeof(Player), nameof(Player.OnDeath))]
    static class OnDeath__Patch
    {
        
    }
    
    [HarmonyPatch(typeof(Player), nameof(Player.CreateTombStone))]
    static class OnDeath_Tombstone_Patch
    {
        private static void Prefix(Player __instance)
        {
            List<ItemDrop.ItemData> saved_items = new List<ItemDrop.ItemData>();
            List<ItemDrop.ItemData> player_items = __instance.m_inventory.GetAllItems();
            int number_of_items_savable = (int)(player_items.Count * DeathProgressionSkill.DeathSkillCalculatePercentWithBonus());

            if (ValConfig.MaxPercentTotalItemsRetainedOnDeath.Value > ((float)number_of_items_savable / player_items.Count))
            {
                number_of_items_savable = (int)(player_items.Count * ValConfig.MaxPercentTotalItemsRetainedOnDeath.Value);
            }
            
            // Equipment items are handled differently than resources etc
            List<ItemDrop.ItemData> player_equipment = player_items.GetEquipment();
            if (player_equipment.Count <= number_of_items_savable)
            {
                // we arnt adding these to the saved items because we just delete unequipped items
                // saved_items.AddRange(player_equipment);
                number_of_items_savable -= player_equipment.Count;
            } else
            {
                player_equipment = ValRougelike.shuffleList(player_items);
                foreach (var equipment in player_equipment)
                {
                    if (number_of_items_savable > 0)
                    {
                        number_of_items_savable -= 1;
                        continue;
                    }
                    __instance.UnequipItem(equipment);
                }
                // we saved as much equipment as we could, everything else is lost
                __instance.m_inventory.RemoveUnequipped();
                return;
                // shuffle and save as much equipment as we can
            }

            if (number_of_items_savable > 0)
            {
                List<ItemDrop.ItemData> non_equip_items = ValRougelike.shuffleList(player_items.GetNotEquipment());
                foreach (var item in non_equip_items)
                {
                    for (int i = 0; i < number_of_items_savable; i++)
                    {
                        if (i > non_equip_items.Count) { break; } // ensure there are still items to save, likely not needed
                        saved_items.Add(item);
                    }
                }
            }
            __instance.m_inventory.RemoveUnequipped();
            foreach (var item in saved_items)
            {
                // might need to check if we actually can add the item here
                __instance.m_inventory.AddItem(item);
            }
        }
    }
}