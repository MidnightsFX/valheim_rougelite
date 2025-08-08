using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Deathlink.Death
{
    internal static class DeathSavingOptions
    {
        static int savableItemsCount = 0;
        static Inventory currentUnsavedInventory = null;
        static List<ItemDrop.ItemData> unsavedItems = new List<ItemDrop.ItemData>();
        static List<ItemDrop.ItemData> selectedToSaveItems = new List<ItemDrop.ItemData>();
        static List<InventoryGrid.Element> unsavedDisplays = new List<InventoryGrid.Element>();
        static GameObject saveUI = null;

        public static void SetupSavableItemsChoice(List<ItemDrop.ItemData> items)
        {
            int width = Mathf.RoundToInt(items.Count / 2);
            int height = Mathf.RoundToInt(items.Count / 2) + 1;
            unsavedItems.Clear();
            selectedToSaveItems.Clear();
            //currentUnsavedInventory = new Inventory("deathlinkPlayerChoiceInv",GUIManager.Instance.GetSprite("woodpanel_trophys"), width, height);
            //foreach(ItemDrop.ItemData item in items) {
            //    currentUnsavedInventory.AddItem(item);
            //}

        }

        private static void SetupSaveUI(List<ItemDrop.ItemData> items, int savableItems)
        {
            saveUI = GUIManager.Instance.CreateWoodpanel(
                parent: GUIManager.CustomGUIFront.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(0f, 0f),
                width: 400f,
                height: 300f,
                draggable: true);

            GameObject header = GUIManager.Instance.CreateText(
                text: $"Deathlink Item Persistence",
                parent: saveUI.transform,
                anchorMin: new Vector2(0.5f, 1f),
                anchorMax: new Vector2(0.5f, 1f),
                position: new Vector2(0f, -100f),
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: 30,
                color: GUIManager.Instance.ValheimOrange,
                outline: true,
                outlineColor: Color.black,
                width: 350f,
                height: 40f,
                addContentSizeFitter: false);

            GameObject savableDesc = GUIManager.Instance.CreateText(
                text: $"Items Available to save {savableItems}",
                parent: saveUI.transform,
                anchorMin: new Vector2(0.5f, 1f),
                anchorMax: new Vector2(0.5f, 1f),
                position: new Vector2(0f, -100f),
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: 30,
                color: GUIManager.Instance.ValheimOrange,
                outline: true,
                outlineColor: Color.black,
                width: 350f,
                height: 40f,
                addContentSizeFitter: false);

            float height = saveUI.transform.position.x;
            foreach (ItemDrop.ItemData item in items)
            {
                
                float horizontal = saveUI.transform.position.y;
                var go = UnityEngine.GameObject.Instantiate(new GameObject());
                go.transform.position = new Vector3(horizontal, height, 0f);
                Image imgcomp = go.AddComponent<Image>();
                imgcomp.sprite = item.GetIcon();
                var checkbox = GUIManager.Instance.CreateToggle(
                    parent: saveUI.transform,
                    width: 40f,
                    height: 40f);
                checkbox.transform.position = new Vector3(0f, 0f, 0f);

                height -= item.GetIcon().texture.height + 5;
            }
            var buttonObject = GUIManager.Instance.CreateButton(
                text: "Save Select Items",
                parent: saveUI.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(0, -250f),
                width: 250f,
                height: 60f);
            buttonObject.GetComponent<Button>().onClick.AddListener(() => SaveButtonOnclick());
        }

        private static void SaveButtonOnclick() {

        }
    }
}
