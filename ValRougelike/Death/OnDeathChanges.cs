using Deathlink.Common;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

namespace Deathlink.Death;

public static class OnDeathChanges
{

    [HarmonyPatch(typeof(Player))]
    public static class OnDeath_Tombstone_Patch
    {
        [HarmonyTranspiler]
        [HarmonyPatch(nameof(Player.OnDeath))]
        static IEnumerable<CodeInstruction> ConstructorTranspiler( IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codeMatcher = new CodeMatcher(instructions, generator);
            codeMatcher.MatchStartForward(
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Player), nameof(Player.CreateTombStone))))
                .Advance(1)
                .InsertAndAdvance(Transpilers.EmitDelegate(ModifyDeath))
                .CreateLabelOffset(out Label label, offset: 4)
                .InsertAndAdvance(new CodeInstruction(OpCodes.Br, label))
                .ThrowIfNotMatch("Unable to patch Deathlink player death changes.");

            return codeMatcher.Instructions();
        }
        private static void ModifyDeath(Player __instance)
        {
            // Tombstone item dropped modifications
            TombstoneOnDeath(__instance);

            // Set food loss status based on configs & skills
            FoodLossOnDeath(__instance);
        }

        public static void FoodLossOnDeath(Player instance)
        {
            if (Deathlink.pcfg().DeathStyle.foodLossOnDeath)
            {
                if (Deathlink.pcfg().DeathStyle.foodLossUsesDeathlink && instance.m_foods.Count > 0)
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
                        if (instance.m_foods.Count > 0) { instance.m_foods.Remove(instance.m_foods[0]); }
                    }
                    else
                    {
                        instance.m_foods.Clear();
                    }
                }
                else {
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
            Inventory inventory = instance.m_inventory;

            List<ItemDrop.ItemData> playerItemsRemoved = new List<ItemDrop.ItemData>();
            List<ItemDrop.ItemData> itemsToDrop = new List<ItemDrop.ItemData>();

            GameObject tombstoneGo = null;
            TombStone tombstone = null;

            string[] nonSkillCheckedItems = ValConfig.ItemsNotSkillChecked.Value.Split(',');
            foreach (ItemDrop.ItemData item in playerItems)
            {
                if (item.m_dropPrefab != null && nonSkillCheckedItems.Contains(item.m_dropPrefab.name)) {
                    playerNonSkillCheckItems.Add(item);
                } else {
                    playerItemsWithoutNonSkillCheckedItems.Add(item);
                }
            }

            float deathskillbonus = DeathProgressionSkill.DeathSkillCalculatePercentWithBonus();
            int items_to_keep = Mathf.RoundToInt(((Deathlink.pcfg().DeathStyle.maxItemsKept - Deathlink.pcfg().DeathStyle.minItemsKept) * deathskillbonus) + Deathlink.pcfg().DeathStyle.minItemsKept);
            int max_equipment_savable = Mathf.RoundToInt(((Deathlink.pcfg().DeathStyle.maxEquipmentKept - Deathlink.pcfg().DeathStyle.minEquipmentKept) * deathskillbonus) + Deathlink.pcfg().DeathStyle.minEquipmentKept);
            int numberOfItemsSavable = items_to_keep + max_equipment_savable;


            // equipment specific
            List<ItemDrop.ItemData> playerEquipment = Deathlink.shuffleList(playerItemsWithoutNonSkillCheckedItems.GetEquipment());
            int max_eq_savable_by_type = max_equipment_savable;

            if (Deathlink.AzuEPILoaded) {
                foreach (ItemDrop.ItemData item in AzuExtendedPlayerInventory.API.GetQuickSlotsItems()) {
                    if (nonSkillCheckedItems.Contains(item.m_shared.m_name)) {
                        playerNonSkillCheckItems.Add(item);
                    } else {
                        playerItemsWithoutNonSkillCheckedItems.Add(item);
                    }
                }
            }

            // Equipment items are handled differently than resources etc
            int equipment_saved = 0;
            foreach (var equipment in playerEquipment){
                if (numberOfItemsSavable > 0) {
                    if (RemoveEquipmentByStyle(equipment_saved, max_equipment_savable, instance, equipment, savedItems, numberOfItemsSavable, out numberOfItemsSavable, out equipment_saved)) {
                        // If the item is not equipped but is still equipment, it should be saved since we have space for it
                        savedItems.Add(equipment);
                    } else {
                        playerItemsRemoved.Add(equipment);
                    }
                } else {
                    playerItemsRemoved.Add(equipment);
                }
            }

            // we still have savable space after saving any equipped items
            List<ItemDrop.ItemData> nonEquippableItems = Deathlink.shuffleList(playerItemsWithoutNonSkillCheckedItems.GetNotEquipment());
            if (numberOfItemsSavable > 0) {
                // shuffle inventory items that are not equipment
                foreach (var item in nonEquippableItems) {
                    if (numberOfItemsSavable > 0 && items_to_keep > 0) {
                        Logger.LogDebug($"Saving {item.m_shared.m_name}");
                        savedItems.Add(item);
                        numberOfItemsSavable -= 1;
                        items_to_keep -= 1;
                    } else {
                        playerItemsRemoved.Add(item);
                    }
                }
            } else {
                playerItemsRemoved.AddRange(nonEquippableItems);
            }


            // Quickslot changes happen after the inventory is cleaned up to avoid overstuffing the inventory
            if (Deathlink.AzuEPILoaded)
            {
                Logger.LogDebug($"Quickslot items found {AzuExtendedPlayerInventory.API.GetQuickSlotsItems().Count}");
                // If the item was not saved, it should be removed from the quickslots
                foreach (var item in AzuExtendedPlayerInventory.API.GetQuickSlotsItems())
                {
                    if (savedItems.Contains(item)) { continue; }
                    Logger.LogDebug($"Removing quickslot item that was not saved {item.m_shared.m_name}");
                    playerItemsRemoved.Add(item);
                    //instance.UnequipItem(item);
                    //instance.m_inventory.RemoveItem(item);
                }
            }

            // Handle items that are defined in the non-skill-checked section
            // we do this right after clearing the inventory to allow creating a tombstone from the empty inventory (which can just contain our non-skill handled items)
            switch (Deathlink.pcfg().DeathStyle.nonSkillCheckedItemAction) {
                case DataObjects.NonSkillCheckedItemAction.Tombstone:
                    Logger.LogDebug($"Dropping non-skill-checked items on death ({playerNonSkillCheckItems.Count()})");
                    itemsToDrop.AddRange(playerNonSkillCheckItems);
                    break;
                case DataObjects.NonSkillCheckedItemAction.Destroy:
                    Logger.LogDebug($"Destroying non-skill-checked items on death ({playerNonSkillCheckItems.Count()})");
                    playerItemsRemoved.AddRange(playerNonSkillCheckItems);
                    break;
                case DataObjects.NonSkillCheckedItemAction.Save:
                    Logger.LogDebug($"Saving non-skill-checked items on death ({playerNonSkillCheckItems.Count()})");
                    savedItems.AddRange(playerNonSkillCheckItems);
                    break;
            }

            bool tombstoneCreated = false;
            if (Deathlink.pcfg().DeathStyle.itemLossStyle == DataObjects.ItemLossStyle.None && playerItemsRemoved.Count > 0) {
                Logger.LogDebug($"Items failed skillcheck {playerItemsRemoved.Count} items on death, dropping to tombestone.");
                itemsToDrop.AddRange(playerItemsRemoved);
            }

            // Empty the inventory, we already have everything that is getting saved copied off.
            foreach (var item in playerItemsRemoved) {
                instance.m_inventory.RemoveItem(item);
            }

            if (Deathlink.pcfg().DeathStyle.itemSavedStyle == DataObjects.ItemSavedStyle.Tombstone && savedItems.Count > 0) {
                Logger.LogDebug($"Saving {savedItems.Count} Items to tombstone");
                itemsToDrop.AddRange(savedItems);
            }

            if (itemsToDrop.Count > 0) {
                Logger.LogDebug("Saving droppable items to tombstone");

                tombstoneCreated = true;
                tombstoneGo = UnityEngine.Object.Instantiate<GameObject>(instance.m_tombstone, instance.GetCenterPoint(), instance.transform.rotation);
                AddItemsToTombstone(tombstoneGo.GetComponent<Container>().GetInventory(), itemsToDrop);
                foreach (var item in itemsToDrop) {
                    instance.m_inventory.RemoveItem(item);
                }
                // gameObject.GetComponent<Container>().GetInventory().MoveInventoryToGrave(instance.m_inventory);
                tombstone = tombstoneGo.GetComponent<TombStone>();
                PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
                string name = playerProfile.GetName();
                long playerId = playerProfile.GetPlayerID();
                tombstone.Setup(name, playerId);
                inventory.Changed();
            }

            // Whether or not to spawn the death marker on the map
            if (ValConfig.ShowDeathMapMarker.Value && tombstoneCreated) {
                Minimap.instance.AddPin(instance.transform.position, Minimap.PinType.Death, $"$hud_mapday {EnvMan.instance.GetDay(ZNet.instance.GetTimeSeconds())}", save: true, isChecked: false, 0L);
            }
        }

        public static void AddItemsToTombstone(Inventory tombstone, List<ItemDrop.ItemData> transferItems) {
            int size = Mathf.RoundToInt(Mathf.Sqrt(transferItems.Count())) + 1;
            tombstone.m_width = size;
            tombstone.m_height = size;
            foreach (ItemDrop.ItemData item in transferItems) {
                tombstone.m_inventory.Add(item);
            }
            tombstone.Changed();
        }

        internal static bool RemoveEquipmentByStyle(int equipment_saved, int max_equipment_savable, Player instance, ItemDrop.ItemData equipment, List<ItemDrop.ItemData> saved_equipment, int numberOfItemsSavable, out int remainingsaves, out int equipment_saved_count)
        {
            equipment_saved_count = 0;
            remainingsaves = 0;
            if (numberOfItemsSavable <= 0) {
                return false;
            }

            if (equipment_saved >= max_equipment_savable) {
                Logger.LogDebug($"Max equipment retained ({max_equipment_savable}) reached, removing {equipment.m_dropPrefab.name}");
                return false;
            }

            Logger.LogDebug($"Saving equipment remaining savable?({numberOfItemsSavable}) {equipment.m_dropPrefab.name}");
            equipment_saved_count = equipment_saved + 1;
            remainingsaves = numberOfItemsSavable - 1;
            return true;
        }
    }
}