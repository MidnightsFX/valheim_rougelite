using JetBrains.Annotations;
using System;
using UnityEngine;

#nullable enable
namespace AzuExtendedPlayerInventory {
    [PublicAPI]
    public class SlotInfo {
        public string[] SlotNames { get; set; } = Array.Empty<string>();

        public Vector2[] SlotPositions { get; set; } = Array.Empty<Vector2>();

        public Func<Player, ItemDrop.ItemData?>?[] GetItemFuncs { get; set; } = Array.Empty<Func<Player, ItemDrop.ItemData>>();

        public Func<ItemDrop.ItemData, bool>?[] IsValidFuncs { get; set; } = Array.Empty<Func<ItemDrop.ItemData, bool>>();
    }
}
