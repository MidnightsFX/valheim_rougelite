using System.Collections.Generic;
using HarmonyLib;

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
            List<ItemDrop.ItemData> player_items = __instance.m_inventory.GetAllItems();
            int number_of_items_savable = (int)(player_items.Count * DeathProgressionSkill.DeathSkillCalculatePercentWithBonus());
            
            // Equipment items are handled differently than resources etc
            player_items.GetEquipment();
            
            // Get non-equipment this is where we want the majority of our death penalty to go
            player_items.GetNotEquipment();
            
        }
    }
}