using JetBrains.Annotations;
using UnityEngine;

#nullable enable
namespace AzuEPI {
    [PublicAPI]
    public struct SlotDescriptor {
        public SlotDescriptor(
            int index,
            string name,
            string originalName,
            bool isQuickSlot,
            bool isEquipmentSlot,
            bool isCustom,
            Vector2 uiPosition) {
            Index = index;
            Name = name ?? string.Empty;
            OriginalName = originalName ?? string.Empty;
            IsQuickSlot = isQuickSlot;
            IsEquipmentSlot = isEquipmentSlot;
            IsCustom = isCustom;
            UiPosition = uiPosition;
        }

        public int Index {
            get;
        }
        public string Name {
            get;
        }
        public string OriginalName {
            get;
        }
        public bool IsQuickSlot {
            get;
        }
        public bool IsEquipmentSlot {
            get;
        }
        public bool IsCustom {
            get;
        }
        public Vector2 UiPosition {
            get;
        }
    }
}
