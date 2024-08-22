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
            // These items have priority because they are being used
            __instance.m_inventory.GetEquippedItems();
            
            // Equipment items are handled differently than resources etc
            __instance.m_inventory.GetAllItems().GetEquipment();
            
        }
    }
}