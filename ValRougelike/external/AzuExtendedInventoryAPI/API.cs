using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using UnityEngine;

#nullable enable
namespace AzuExtendedPlayerInventory {
    [PublicAPI]
    public class API {
        private static readonly Dictionary<API.SlotAddedHandler, AzuEPI.API.SlotAddedHandler> _slotAddedMap = new Dictionary<API.SlotAddedHandler, AzuEPI.API.SlotAddedHandler>();
        private static readonly Dictionary<API.SlotRemovedHandler, AzuEPI.API.SlotRemovedHandler> _slotRemovedMap = new Dictionary<API.SlotRemovedHandler, AzuEPI.API.SlotRemovedHandler>();
        private static readonly Dictionary<Action<Hud>, Action<Hud>> _hudAwakeMap = new Dictionary<Action<Hud>, Action<Hud>>();
        private static readonly Dictionary<Action<Hud>, Action<Hud>> _hudAwakeCompleteMap = new Dictionary<Action<Hud>, Action<Hud>>();
        private static readonly Dictionary<Action<Hud>, Action<Hud>> _hudUpdateMap = new Dictionary<Action<Hud>, Action<Hud>>();
        private static readonly Dictionary<Action<Hud>, Action<Hud>> _hudUpdateCompleteMap = new Dictionary<Action<Hud>, Action<Hud>>();

        public static event API.SlotAddedHandler? SlotAdded {
            add {
                if (value == null || API._slotAddedMap.ContainsKey(value))
                    return;
                AzuEPI.API.SlotAddedHandler slotAddedHandler = (AzuEPI.API.SlotAddedHandler)(s =>
                {
                    try {
                        value(s);
                    } catch {
                    }
                });
                API._slotAddedMap[value] = slotAddedHandler;
                AzuEPI.API.SlotAdded += slotAddedHandler;
            }
            remove {
                AzuEPI.API.SlotAddedHandler slotAddedHandler;
                if (value == null || !API._slotAddedMap.TryGetValue(value, out slotAddedHandler))
                    return;
                AzuEPI.API.SlotAdded -= slotAddedHandler;
                API._slotAddedMap.Remove(value);
            }
        }

        public static event API.SlotRemovedHandler? SlotRemoved {
            add {
                if (value == null || API._slotRemovedMap.ContainsKey(value))
                    return;
                AzuEPI.API.SlotRemovedHandler slotRemovedHandler = (AzuEPI.API.SlotRemovedHandler)(s =>
                {
                    try {
                        value(s);
                    } catch {
                    }
                });
                API._slotRemovedMap[value] = slotRemovedHandler;
                AzuEPI.API.SlotRemoved += slotRemovedHandler;
            }
            remove {
                AzuEPI.API.SlotRemovedHandler slotRemovedHandler;
                if (value == null || !API._slotRemovedMap.TryGetValue(value, out slotRemovedHandler))
                    return;
                AzuEPI.API.SlotRemoved -= slotRemovedHandler;
                API._slotRemovedMap.Remove(value);
            }
        }

        public static event Action<Hud>? OnHudAwake {
            add {
                API.AddHudEvent(value, API._hudAwakeMap, (Action<Action<Hud>>)(h => AzuEPI.API.OnHudAwake += h));
            }
            remove {
                API.RemoveHudEvent(value, API._hudAwakeMap, (Action<Action<Hud>>)(h => AzuEPI.API.OnHudAwake -= h));
            }
        }

        public static event Action<Hud>? OnHudAwakeComplete {
            add {
                API.AddHudEvent(value, API._hudAwakeCompleteMap, (Action<Action<Hud>>)(h => AzuEPI.API.OnHudAwakeComplete += h));
            }
            remove {
                API.RemoveHudEvent(value, API._hudAwakeCompleteMap, (Action<Action<Hud>>)(h => AzuEPI.API.OnHudAwakeComplete -= h));
            }
        }

        public static event Action<Hud>? OnHudUpdate {
            add {
                API.AddHudEvent(value, API._hudUpdateMap, (Action<Action<Hud>>)(h => AzuEPI.API.OnHudUpdate += h));
            }
            remove {
                API.RemoveHudEvent(value, API._hudUpdateMap, (Action<Action<Hud>>)(h => AzuEPI.API.OnHudUpdate -= h));
            }
        }

        public static event Action<Hud>? OnHudUpdateComplete {
            add {
                API.AddHudEvent(value, API._hudUpdateCompleteMap, (Action<Action<Hud>>)(h => AzuEPI.API.OnHudUpdateComplete += h));
            }
            remove {
                API.RemoveHudEvent(value, API._hudUpdateCompleteMap, (Action<Action<Hud>>)(h => AzuEPI.API.OnHudUpdateComplete -= h));
            }
        }

        private static void AddHudEvent(
          Action<Hud>? handler,
          Dictionary<Action<Hud>, Action<Hud>> map,
          Action<Action<Hud>> subscribe) {
            if (handler == null || map.ContainsKey(handler))
                return;
            Action<Hud> action = (Action<Hud>)(h =>
            {
                try {
                    handler(h);
                } catch {
                }
            });
            map[handler] = action;
        }

        private static void RemoveHudEvent(
          Action<Hud>? handler,
          Dictionary<Action<Hud>, Action<Hud>> map,
          Action<Action<Hud>> unsubscribe) {
            if (handler == null || !map.TryGetValue(handler, out Action<Hud> _))
                return;
            map.Remove(handler);
        }

        public static bool IsLoaded() => AzuEPI.API.IsLoaded();

        public static bool AddSlot(
          string slotName,
          Func<Player, ItemDrop.ItemData?> getItem,
          Func<ItemDrop.ItemData, bool> isValid,
          int index = -1) {
            return false;
        }

        public static bool RemoveSlot(string slotName) => AzuEPI.API.RemoveSlot(slotName);

        public static SlotInfo GetSlots() {
            AzuEPI.SlotInfo slots = AzuEPI.API.GetSlots();
            return new SlotInfo() {
                SlotNames = slots.SlotNames ?? Array.Empty<string>(),
                SlotPositions = slots.SlotPositions ?? Array.Empty<Vector2>(),
                GetItemFuncs = slots.GetItemFuncs ?? Array.Empty<Func<Player, ItemDrop.ItemData>>(),
                IsValidFuncs = slots.IsValidFuncs ?? Array.Empty<Func<ItemDrop.ItemData, bool>>()
            };
        }

        public static SlotInfo GetQuickSlots() {
            AzuEPI.SlotInfo quickSlots = AzuEPI.API.GetQuickSlots();
            return new SlotInfo() {
                SlotNames = quickSlots.SlotNames ?? Array.Empty<string>(),
                SlotPositions = quickSlots.SlotPositions ?? Array.Empty<Vector2>(),
                GetItemFuncs = quickSlots.GetItemFuncs ?? Array.Empty<Func<Player, ItemDrop.ItemData>>(),
                IsValidFuncs = quickSlots.IsValidFuncs ?? Array.Empty<Func<ItemDrop.ItemData, bool>>()
            };
        }

        public static List<ItemDrop.ItemData> GetQuickSlotsItems() => AzuEPI.API.GetQuickSlotsItems();

        public static int GetAddedRows(int width) => AzuEPI.API.GetAddedRows(width);

        public static void HudAwake(Hud h) {
        }

        public static void HudAwakeComplete(Hud h) {
        }

        public static void HudUpdate(Hud h) {
        }

        public static void HudUpdateComplete(Hud h) {
        }

        private static void SafeInvoke(Action a) {
            try {
                a();
            } catch {
            }
        }

        public delegate void SlotAddedHandler(string slotName);

        public delegate void SlotRemovedHandler(string slotName);
    }
}
