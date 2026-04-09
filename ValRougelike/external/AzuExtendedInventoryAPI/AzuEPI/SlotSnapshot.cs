using JetBrains.Annotations;
using System.Runtime.CompilerServices;

#nullable enable
namespace AzuEPI {
    [PublicAPI]
    public struct SlotSnapshot {
        public SlotDescriptor Descriptor {
            get;
        }
        public bool Occupied {
            get;
        }
        public Vector2i GridPos {
            get;
        }
        public int LinearGridIndex {
            get;
        }

        public SlotSnapshot(SlotDescriptor descriptor, bool occupied, Vector2i gridPos, int linearGridIndex) {
            this.Descriptor = descriptor;
            this.Occupied = occupied;
            this.GridPos = gridPos;
            this.LinearGridIndex = linearGridIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Validates(ItemDrop.ItemData item) => API.SlotValidates(this.Descriptor.Index, item);
    }
}
