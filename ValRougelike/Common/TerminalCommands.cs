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
            public override string Help => "Format: [steamID|playerName] resets the deathlink choice for the target player. Accepts a platform Steam ID or a case-insensitive player name.";

            public override void Run(string[] args)
            {
                if (SynchronizationManager.Instance.PlayerIsAdmin == false) {
                    Logger.LogWarning("You are not an admin, and not allowed to run this command.");
                    return;
                }

                if (args.Length < 1) {
                    Logger.LogInfo("Player Steam ID or name required");
                    return;
                }

                string inputArg = args[0];
                bool matched = false;

                foreach (ZNet.PlayerInfo player in ZNet.instance.GetPlayerList()) {
                    string platformUserID = player.m_userInfo.m_id.m_userID.ToString();
                    string displayName = player.m_userInfo.m_displayName;

                    bool idMatch = platformUserID == inputArg;
                    bool nameMatch = string.Equals(displayName, inputArg, StringComparison.OrdinalIgnoreCase);

                    Logger.LogDebug($"Checking Player {displayName} (platformID: {platformUserID}) against '{inputArg}'");

                    if (idMatch || nameMatch) {
                        ZPackage resetpkg = new ZPackage();
                        resetpkg.Write(platformUserID);
                        Logger.LogDebug("Requesting server reset of player saved Deathlink choice");
                        ValConfig.resetChoiceRPC.SendPackage(ZRoutedRpc.instance.GetServerPeerID(), resetpkg);
                        Logger.LogDebug($"Checking for {player.m_name} peer");
                        ZNetPeer peer = ZNet.instance.GetPeerByPlayerName(player.m_name);
                        if (peer == null) {
                            Logger.LogWarning($"Player {displayName} found in player list but has no active peer.");
                            break;
                        }
                        ValConfig.resetChoiceRPC.SendPackage(peer.m_uid, resetpkg);
                        Logger.LogInfo($"Matched player {player.m_name}|{displayName} (platformID: {platformUserID}, peerUID: {peer.m_uid})");
                        matched = true;
                        break;
                    }
                }
                if (!matched) {
                    Logger.LogInfo("Player could not be found, are they online?");
                }

            }
        }
    }
}
