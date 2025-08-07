using Deathlink.Common;
using HarmonyLib;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using static Deathlink.Common.DataObjects;
using static Deathlink.Death.DeathChoices;

namespace Deathlink.Death
{
    public static class DeathChoiceEnable
    {
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
        public static class ShowDeathChoiceUI {
            public static void Postfix(InventoryGui __instance) {
                if (__instance.gameObject.GetComponent<DeathChoiceUI>() == null) { __instance.gameObject.AddComponent<DeathChoiceUI>(); }
                DeathChoiceUI.Instance.Show();
            }
        }


        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
        public static class HideDeathChoiceUI {
            public static void Postfix(InventoryGui __instance) {
                DeathChoiceUI.Instance.Hide();
            }
        }
        
    }

    public class DeathChoices
    {

        public class DeathChoiceUI : MonoBehaviour
        {
            public static DeathChoiceUI Instance => _instance ??= new DeathChoiceUI();
            private static DeathChoiceUI _instance;

            private static GameObject DeathChoicePanel;
            private static GameObject ChoicesScrollView;
            private static GameObject ChoicesContent;
            private static GameObject ChoicesContainer;
            private static GameObject manualCloseButton;
            private static GameObject selectChoiceButton;
            private static Text DeathPenaltyDescription;
            private static Text XPModifiersDescription;
            private static Text LootModifersDescription;
            private static Text HarvestModifiersDescription;

            private static Dictionary<string, DeathChoiceLevel> DeathChoices = DeathConfigurationData.deathLevels;

            private static List<Toggle> difficultyToggles = new List<Toggle>();
            private static ToggleGroup choiceGroup;

            public void Awake()
            {
                CreateStaticUIObjects();
                SetChoiceList();
                //SetupListeners();
            }

            public void Show() {
                if (DeathChoicePanel == null) {
                    CreateStaticUIObjects();
                }
                DeathChoicePanel.SetActive(true);
            }

            public void Hide() {
                // Logger.LogDebug("Closing");
                DeathChoicePanel?.SetActive(false);
                GUIManager.BlockInput(false);
            }

            public void MakePlayerDeathSelection() {

            }

            // TODO make this configurable and loaded from a config file
            private void SetChoiceList()
            {
                difficultyToggles.Clear();
                int y_value = -50;
                //Logger.LogDebug($"Setting up {DeathChoices.Count} death styles.");
                foreach (var entry in DeathChoices) {
                    var newDeathChoice = GameObject.Instantiate(ChoicesContainer, ChoicesContent.transform);
                    //Logger.LogDebug("Created container");
                    var selector = newDeathChoice.transform.Find("selecter");
                    //Logger.LogDebug($"Finding selector... null? {selector == null}");
                    selector.Find("Label").GetComponent<Text>().text = entry.Key;
                    //Logger.LogDebug("Set label text");
                    newDeathChoice.transform.Find("ChoiceName").GetComponent<Text>().text = entry.Value.DisplayName;
                    //Logger.LogDebug("Set display name");
                    var toggle = selector.GetComponent<Toggle>();
                    toggle.group = choiceGroup;
                    toggle.onValueChanged.AddListener((isOn) => {
                        //Logger.LogDebug("Setting up onclock");
                        DeathPenaltyDescription.GetComponent<Text>().text = entry.Value.GetDeathStyleDescription();
                        //Logger.LogDebug("Set death description");
                        XPModifiersDescription.GetComponent<Text>().text = entry.Value.GetSkillModiferDescription();
                        //Logger.LogDebug("Set xp mod");
                        LootModifersDescription.GetComponent<Text>().text = entry.Value.GetLootModifiers();
                        //Logger.LogDebug("Set loot mod");
                        HarvestModifiersDescription.GetComponent<Text>().text = entry.Value.GetResourceModiferDescription();
                        //Logger.LogDebug("Set harvest mod");
                    });
                    //Logger.LogDebug("Created onclick");
                    newDeathChoice.SetActive(true);
                    newDeathChoice.transform.localPosition = new Vector3() { x = 260, y = y_value };
                    difficultyToggles.Add(toggle);
                    y_value -= 50;
                }
            }

            private void CreateStaticUIObjects()
            {
                if (GUIManager.Instance == null || !GUIManager.CustomGUIFront) {
                    Logger.LogWarning("GUIManager not setup, skipping static object creation.");
                    return;
                }

                // Create the panel object
                DeathChoicePanel = GUIManager.Instance.CreateWoodpanel(
                    parent: GUIManager.CustomGUIFront.transform,
                    anchorMin: new Vector2(0.5f, 0.5f),
                    anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(0, 0),
                    width: 800,
                    height: 800,
                    draggable: true);
                // Hide it right away
                DeathChoicePanel.SetActive(false);

                var textHeader = GUIManager.Instance.CreateText(
                    text: "Deathlink Selection",
                    parent: DeathChoicePanel.transform,
                    anchorMin: new Vector2(0.5f, 0.5f),
                    anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(50f, 360f),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 30,
                    color: GUIManager.Instance.ValheimOrange,
                    outline: true,
                    outlineColor: Color.black,
                    width: 350f,
                    height: 40f,
                    addContentSizeFitter: false);
                textHeader.name = "DLHeader";

                GUIManager.Instance.CreateText(
                    text: "Choose your playstyle. Will you risk everything for increased rewards?",
                    parent: DeathChoicePanel.transform,
                    anchorMin: new Vector2(0.5f, 0.5f),
                    anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(0f, 315f),
                    font: GUIManager.Instance.AveriaSerif,
                    fontSize: 16,
                    color: Color.white,
                    outline: true,
                    outlineColor: Color.black,
                    width: 560f,
                    height: 40f,
                    addContentSizeFitter: false);

                manualCloseButton = GUIManager.Instance.CreateButton(
                    text: "X",
                    parent: DeathChoicePanel.transform,
                    anchorMin: new Vector2(0.5f, 0.5f),
                    anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(360f, 360f),
                    width: 60f,
                    height: 60f);
                Button bclose = manualCloseButton.GetComponent<Button>();
                bclose.interactable = true;
                bclose.onClick.AddListener(Hide);

                var deathpenaltyTitle = GUIManager.Instance.CreateText(
                    text: "Death Penalty",
                    parent: DeathChoicePanel.transform,
                    anchorMin: new Vector2(0.5f, 0.5f),
                    anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(180f, 220f),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 22,
                    color: GUIManager.Instance.ValheimOrange,
                    outline: true,
                    outlineColor: Color.black,
                    width: 350f,
                    height: 40f,
                    addContentSizeFitter: false);
                deathpenaltyTitle.name = "DeathPenaltyTitle";

                var deathpenalty = GUIManager.Instance.CreateText(
                    text: "Death Penalty description",
                    parent: DeathChoicePanel.transform,
                    anchorMin: new Vector2(0.5f, 0.5f),
                    anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(180f, 160f),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 18,
                    color: Color.white,
                    outline: true,
                    outlineColor: Color.black,
                    width: 350f,
                    height: 100f,
                    addContentSizeFitter: false);
                deathpenalty.name = "DeathPenaltyDesc";
                DeathPenaltyDescription = deathpenalty.GetComponent<Text>();

                var xpTitle = GUIManager.Instance.CreateText(
                    text: "XP Modifiers",
                    parent: DeathChoicePanel.transform,
                    anchorMin: new Vector2(0.5f, 0.5f),
                    anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(180f, 30f),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 22,
                    color: GUIManager.Instance.ValheimOrange,
                    outline: true,
                    outlineColor: Color.black,
                    width: 350f,
                    height: 40f,
                    addContentSizeFitter: false);
                xpTitle.name = "xpModifiersTitle";

                var xpMod = GUIManager.Instance.CreateText(
                    text: "XP Modifiers description",
                    parent: DeathChoicePanel.transform,
                    anchorMin: new Vector2(0.5f, 0.5f),
                    anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(180f, -30f),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 16,
                    color: Color.white,
                    outline: true,
                    outlineColor: Color.black,
                    width: 350f,
                    height: 100f,
                    addContentSizeFitter: false);
                xpMod.name = "xpModifiersDesc";
                XPModifiersDescription = xpMod.GetComponent<Text>();

                var lootTitle = GUIManager.Instance.CreateText(
                    text: "Loot Modifiers",
                    parent: DeathChoicePanel.transform,
                    anchorMin: new Vector2(0.5f, 0.5f),
                    anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(180f, -130f),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 22,
                    color: GUIManager.Instance.ValheimOrange,
                    outline: true,
                    outlineColor: Color.black,
                    width: 350f,
                    height: 40f,
                    addContentSizeFitter: false);
                lootTitle.name = "lootModifiersTitle";

                var lootDesc = GUIManager.Instance.CreateText(
                    text: "Loot Modifiers description",
                    parent: DeathChoicePanel.transform,
                    anchorMin: new Vector2(0.5f, 0.5f),
                    anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(180f, -190f),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 16,
                    color: Color.white,
                    outline: true,
                    outlineColor: Color.black,
                    width: 350f,
                    height: 100f,
                    addContentSizeFitter: false);
                lootDesc.name = "lootModifersDesc";
                LootModifersDescription = lootDesc.GetComponent<Text>();

                var harvestTitle = GUIManager.Instance.CreateText(
                    text: "Harvest Modifiers",
                    parent: DeathChoicePanel.transform,
                    anchorMin: new Vector2(0.5f, 0.5f),
                    anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(180f, -260f),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 22,
                    color: GUIManager.Instance.ValheimOrange,
                    outline: true,
                    outlineColor: Color.black,
                    width: 350f,
                    height: 40f,
                    addContentSizeFitter: false);
                harvestTitle.name = "harvestModifiersTitle";

                var harvestDesc = GUIManager.Instance.CreateText(
                    text: "Harvest Modifiers description",
                    parent: DeathChoicePanel.transform,
                    anchorMin: new Vector2(0.5f, 0.5f),
                    anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(180f, -320f),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 16,
                    color: Color.white,
                    outline: true,
                    outlineColor: Color.black,
                    width: 350f,
                    height: 100f,
                    addContentSizeFitter: false);
                harvestDesc.name = "harvestModifersDesc";
                HarvestModifiersDescription = harvestDesc.GetComponent<Text>();

                selectChoiceButton = GUIManager.Instance.CreateButton(
                    text: "Seal Your Fate",
                    parent: DeathChoicePanel.transform,
                    anchorMin: new Vector2(0.5f, 0.5f),
                    anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(-220f, -340f),
                    width: 160f,
                    height: 60f);
                Button bchoice = selectChoiceButton.GetComponent<Button>();
                bchoice.interactable = true;
                bchoice.onClick.AddListener(MakePlayerDeathSelection);

                Logger.LogDebug("Setting up scroll entry");
                // Scroll area
                ChoicesScrollView = GUIManager.Instance.CreateScrollView(DeathChoicePanel.transform, false, true, 10f, 10f, GUIManager.Instance.ValheimScrollbarHandleColorBlock, Color.grey, 200f, 400f);
                ChoicesScrollView.transform.localPosition = new Vector2 { x = -150, y = -30 };
                ChoicesContent = ChoicesScrollView.GetComponentInChildren<ContentSizeFitter>().gameObject;
                choiceGroup = ChoicesContent.AddComponent<ToggleGroup>();

                Logger.LogDebug("Setting up death choice template entry");

                ChoicesContainer = new GameObject("DeathChoice");
                ChoicesContainer.transform.SetParent(DeathChoicePanel.transform);
                ChoicesContainer.transform.position = DeathChoicePanel.transform.position;
                ChoicesContainer.SetActive(false);

                var toggleGo = GUIManager.Instance.CreateToggle(
                    parent: ChoicesContainer.transform,
                    width: 40f,
                    height: 40f
                    );
                toggleGo.transform.localPosition = new Vector2(-220f, 0f);
                toggleGo.name = "selecter";
                toggleGo.transform.Find("Label").gameObject.SetActive(false);
                toggleGo.GetComponent<Toggle>().isOn = false;

                var deathSettingName = GUIManager.Instance.CreateText(
                    text: "Name",
                    parent: ChoicesContainer.transform,
                    anchorMin: new Vector2(0.5f, 0.5f),
                    anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(-20f, 0f),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 20,
                    color: GUIManager.Instance.ValheimYellow,
                    outline: true,
                    outlineColor: Color.black,
                    width: 350f,
                    height: 40f,
                    addContentSizeFitter: false);
                deathSettingName.name = "ChoiceName";
            }
        }
    }
}
