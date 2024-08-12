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
            __instance.m_inventory.GetEquippedItems();
        }
    }
}