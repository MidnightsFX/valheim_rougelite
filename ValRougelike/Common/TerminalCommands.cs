using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Valheim.UI;
using static HarmonyLib.InlineSignature;

namespace Deathlink.Common
{
    internal static class TerminalCommands
    {
        internal static void AddCommands()
        {
            CommandManager.Instance.AddConsoleCommand(new ResetPlayerDeathChoice());
        }

        internal class ResetPlayerDeathChoice : ConsoleCommand
        {
            public override string Name => "DL-RESET-CHOICE";

            public override bool IsNetwork => true;
            public override string Help => "Format: [playerID] resets the deathlink choice for the selected player and sends the updated config.";

            public override void Run(string[] args)
            {
                if (SynchronizationManager.Instance.PlayerIsAdmin == false) {
                    Logger.LogWarning("You are not an admin, and not allowed to run this command.");
                    return;
                }

                if (args.Length < 1) {
                    Logger.LogInfo("Player ID required");
                }
                long.TryParse(args[0], out long targetPlayerID);
                bool matched = false;

                foreach (ZNet.PlayerInfo player in ZNet.instance.GetPlayerList()) {
                    ZDO zDO = ZDOMan.instance.GetZDO(player.m_characterID);
                    long PlayerID = 0;
                    if (zDO != null) {
                        PlayerID = zDO.GetLong(ZDOVars.s_playerID, 0L);
                    }
                    Logger.LogDebug($"Checking Player {player.m_userInfo.m_displayName} == {args[0]} || {PlayerID} == {targetPlayerID}");
                    if (PlayerID == targetPlayerID || player.m_userInfo.m_displayName == args[0])
                    {
                        Logger.LogInfo($"Matched player {PlayerID}");
                        ZPackage package = new ZPackage();
                        package.Write(PlayerID);
                        ValConfig.resetChoiceRPC.SendPackage(PlayerID, package);
                        ValConfig.resetChoiceRPC.SendPackage(ZRoutedRpc.instance.GetServerPeerID(), package);
                        matched = true;
                        break;
                    }
                }
                if (matched == false) {
                    Logger.LogInfo("Player could not be found, are they online?");
                }
                
            }
        }
    }
}
