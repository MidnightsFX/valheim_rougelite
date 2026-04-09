using AzuEPI.Core.Slots;
using AzuExtendedPlayerInventory;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

#nullable enable
namespace AzuEPI {
    [PublicAPI]
    public class API {
        internal static HashSet<Model.EquipmentSlot?> CustomSlots { get; } = new HashSet<Model.EquipmentSlot>();

        public static event Action<Hud>? OnHudAwake;

        public static event Action<Hud>? OnHudAwakeComplete;

        public static event Action<Hud>? OnHudUpdate;

        public static event Action<Hud>? OnHudUpdateComplete;

        public static event Action? OnBeforeQuickSlotsAdded;

        public static event Action? OnQuickSlotsAdded;

        public static event API.SlotAddedHandler? SlotAdded;

        public static event API.SlotRemovedHandler? SlotRemoved;

        public static event Action<string>? OnRegisterVisualPrefab;

        public static bool IsLoaded() => false;

        public static ItemDrop.ItemData.ItemType GetFakeItemType() => (ItemDrop.ItemData.ItemType)0;

        public static bool AddSlot(
          string slotName,
          Func<Player, ItemDrop.ItemData?> getItem,
          Func<ItemDrop.ItemData, bool> isValid,
          int index = -1) {
            return false;
        }

        public static bool AddSlot(string slotName, string prefabName, int index = -1) => false;

        public static bool AddSlot(string slotName, IEnumerable<string> prefabNames, int index = -1) {
            return false;
        }

        public static bool AddSlot(
          string slotName,
          Func<ItemDrop.ItemData, bool> isValid,
          int index = -1,
          IEnumerable<string>? prefabNamesForVisuals = null) {
            return false;
        }

        public static bool AddQuickSlot(string slotName, bool showName = false, int index = -1) {
            return false;
        }

        public static bool RemoveSlot(string slotName) => false;

        public static SlotInfo GetSlots() => API.BuildSlotInfo((Func<Model.Slot, bool>)(_ => true));

        public static SlotInfo GetQuickSlots() {
            return API.BuildSlotInfo((Func<Model.Slot, bool>)(s => s.IsQuickSlot));
        }

        public static SlotInfo GetEquipmentSlots() {
            return API.BuildSlotInfo((Func<Model.Slot, bool>)(s => !s.IsQuickSlot && s.EquipmentSlot != null));
        }

        private static SlotInfo BuildSlotInfo(Func<Model.Slot, bool> filter) => new SlotInfo();

        public static List<ItemDrop.ItemData> GetQuickSlotsItems() => new List<ItemDrop.ItemData>();

        public static int GetAddedRows(int width) => 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetFullHeight(int width) => 0;

        public static void RegisterVisualPrefabs(
          string slotName,
          params (string prefabName, string visualName)[] pairs) {
        }

        public static int GetSlotCount() => 0;

        public static bool TryGetSlotIndexByName(string slotName, out int index, bool allowLocalized = true) {
            index = -1;
            return false;
        }

        public static bool TryGetSlotDescriptor(int index, out SlotDescriptor desc) {
            desc = new SlotDescriptor();
            return false;
        }

        public static int GetSlotGridLinearIndex(Inventory inv, int slotIndex) => -1;

        public static Vector2i GetSlotGridPos(Inventory inv, int slotIndex) => new Vector2i(-1, -1);

        public static bool TryGetSlotIndexAtGridPos(Inventory inv, Vector2i gridPos, out int slotIndex) {
            slotIndex = -1;
            return false;
        }

        public static bool IsEquipmentCell(Inventory inv, int x, int y, out int slotIndex) {
            slotIndex = -1;
            return false;
        }

        public static bool IsQuickCell(Inventory inv, int x, int y, out int slotIndex) {
            slotIndex = -1;
            return false;
        }

        public static bool TryGetSlotSnapshot(Inventory inv, int slotIndex, out SlotSnapshot snapshot) {
            snapshot = new SlotSnapshot();
            return false;
        }

        public static IEnumerable<SlotDescriptor> EnumerateSlots() {
            yield break;
        }

        public static IEnumerable<SlotSnapshot> GetAllSlotSnapshots(Inventory inv) {
            yield break;
        }

        public static IEnumerable<SlotSnapshot> GetQuickSlotSnapshots(Inventory inv) {
            yield break;
        }

        public static IEnumerable<SlotSnapshot> GetEquipmentSlotSnapshots(Inventory inv) {
            yield break;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetSlotIndexByItem(
          Player player,
          ItemDrop.ItemData item,
          out int slotIndex) {
            slotIndex = -1;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetSlotIndexByItem(ItemDrop.ItemData item, out int slotIndex) {
            slotIndex = -1;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetSlotDescriptorByItem(
          Player player,
          ItemDrop.ItemData item,
          out SlotDescriptor desc) {
            desc = new SlotDescriptor();
            return false;
        }

        public static bool SlotValidates(int slotIndex, ItemDrop.ItemData item) => false;

        public static bool TryGetEquippedItem(int slotIndex, out ItemDrop.ItemData? item) {
            item = (ItemDrop.ItemData)null;
            return false;
        }

        public static bool TryGetSlotDescriptorByName(
          string slotName,
          out SlotDescriptor desc,
          bool allowLocalized = true) {
            desc = new SlotDescriptor();
            int index;
            return API.TryGetSlotIndexByName(slotName, out index, allowLocalized) && API.TryGetSlotDescriptor(index, out desc);
        }

        public delegate void SlotAddedHandler(string slotName);

        public delegate void SlotRemovedHandler(string slotName);
    }
}
