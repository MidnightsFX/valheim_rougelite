using System;
using UnityEngine;

#nullable enable
namespace AzuEPI.Core.Slots {
    public class Model {
        internal class Slot {
            public string Name;
            public string OriginalName;
            public Vector2 Position;
            public bool IsQuickSlot;
            public bool IsAPIAdded;
            public bool Occupied;

            public Model.EquipmentSlot? EquipmentSlot => this as Model.EquipmentSlot;
        }

        internal class EquipmentSlot : Model.Slot {
            public Func<Player, ItemDrop.ItemData?>? Get;
            public Func<ItemDrop.ItemData, bool>? Valid;
        }
    }
}