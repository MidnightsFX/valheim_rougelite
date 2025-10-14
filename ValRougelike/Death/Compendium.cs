using Deathlink.Common;
using HarmonyLib;
using System.Text;
using static Deathlink.Common.DataObjects;

namespace Deathlink.Death
{
    public static class Compendium
    {
        static string specialColor = "#ffa64d";
        //static string positiveColor = Color.green.ToString();

        [HarmonyPatch(typeof(TextsDialog), nameof(TextsDialog.UpdateTextsList))]
        public static class TextsDialog_UpdateTextsList_Patch
        {
            public static void Postfix(TextsDialog __instance) {
                AddDeathLinkExplanationPage(__instance);
            }

            private static void AddDeathLinkExplanationPage(TextsDialog textsDialog)
            {
                DeathChoiceLevel deathlinkPlayerSettings = DeathConfigurationData.playerDeathConfiguration;

                StringBuilder sb = new StringBuilder();

                sb.AppendLine($"<size=48>{Localization.instance.Localize("$comp_header")}: <color={specialColor}>{deathlinkPlayerSettings.DisplayName}</color></size>");
                //sb.AppendLine($"<size=12><b>Deathlink</b> is a multiplayer mod where the death of one player causes the death of all other players.</size>");
                sb.AppendLine();

                sb.AppendLine($"<size=30><b>Death Effects</b></size>");
                sb.AppendLine(deathlinkPlayerSettings.GetDeathStyleDescription());
                sb.AppendLine();

                if (deathlinkPlayerSettings.SkillModifiers.Count > 0) {
                    sb.AppendLine($"<size=30><b>Skill Modifiers</b></size>");
                    sb.AppendLine(deathlinkPlayerSettings.GetSkillModiferDescription());
                    sb.AppendLine();
                }

                if (deathlinkPlayerSettings.ResourceModifiers.Count > 0) {
                    sb.AppendLine($"<size=30><b>Resource Modifiers</b></size>");
                    sb.AppendLine(deathlinkPlayerSettings.GetResourceModiferDescription());
                    sb.AppendLine();
                }

                if (deathlinkPlayerSettings.DeathLootModifiers.Count > 0) {
                    sb.AppendLine($"<size=30><b>Loot Modifiers</b></size>");
                    sb.AppendLine(deathlinkPlayerSettings.GetLootModifiersDescription());
                    sb.AppendLine();
                }

                textsDialog.m_texts.Insert(0, new TextsDialog.TextInfo( Localization.instance.Localize($"$deathlink_settings"), Localization.instance.Localize(sb.ToString())));
            }
        }
    }
}
