using Deathlink.Common;
using HarmonyLib;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static Deathlink.Common.DataObjects;

namespace Deathlink.Death
{
    /// <summary>
    /// Server-authoritative leaderboard. Clients collect their own stats (native PlayerProfile
    /// stats + custom accumulators stored as player unique keys) and report a snapshot to the
    /// server; the server merges, persists to leaderboard.yaml, and broadcasts the full board back
    /// to all clients on a slow timer. Mirrors the config-sync design in ValConfig.cs.
    /// </summary>
    internal static class LeaderboardData
    {
        // Server store + client cache, keyed by character/player id.
        public static Dictionary<long, LeaderboardEntry> leaderboard = new Dictionary<long, LeaderboardEntry>();

        private static CustomRPC leaderboardRPC;

        // Damage dealt this session, accumulated cheaply in the Character.Damage hook and flushed
        // to the persistent unique key at snapshot time (avoids touching unique keys on every hit).
        private static long sessionDamage = 0;

        // Ensures the one-shot "report shortly after spawn" only fires once per session.
        private static bool initialReportSent = false;

        // ---------------------------------------------------------------------
        // Persistence (server)
        // ---------------------------------------------------------------------

        internal static void Init()
        {
            try {
                if (File.Exists(ValConfig.leaderboardPath)) {
                    UpdateLeaderboardFromYaml(File.ReadAllText(ValConfig.leaderboardPath));
                }
            } catch (Exception e) {
                Logger.LogWarning($"Failed to load leaderboard data, starting empty. Exception: {e}");
            }
        }

        public static void Write()
        {
            try {
                File.WriteAllText(ValConfig.leaderboardPath, yamlserializer.Serialize(leaderboard));
            } catch (Exception e) {
                Logger.LogWarning($"Failed to write leaderboard data. Exception: {e}");
            }
        }

        public static void UpdateLeaderboardFromYaml(string rawyaml)
        {
            if (string.IsNullOrWhiteSpace(rawyaml)) { return; }
            var parsed = yamldeserializer.Deserialize<Dictionary<long, LeaderboardEntry>>(rawyaml);
            if (parsed != null) { leaderboard = parsed; }
        }

        public static void MergeSnapshot(LeaderboardEntry entry)
        {
            if (entry == null || entry.PlayerID == 0) { return; }
            leaderboard[entry.PlayerID] = entry;
            Write();
        }

        // ---------------------------------------------------------------------
        // RPC wiring (called from ValConfig.SetupConfigRPCs)
        // ---------------------------------------------------------------------

        public static void SetupRPC()
        {
            leaderboardRPC = NetworkManager.Instance.AddRPC("DEATHLK_LB", OnServerReceiveSnapshot, OnClientReceiveBoard);
            SynchronizationManager.Instance.AddInitialSynchronization(leaderboardRPC, SendFullBoardPackage);
        }

        // Server side: a client reported its single-entry snapshot.
        private static IEnumerator OnServerReceiveSnapshot(long sender, ZPackage package)
        {
            string yaml = package.ReadString();
            try {
                var entry = yamldeserializer.Deserialize<LeaderboardEntry>(yaml);
                MergeSnapshot(entry);
            } catch (Exception e) {
                Logger.LogWarning($"Failed to parse leaderboard snapshot from {sender}. Exception: {e}");
            }
            yield return null;
        }

        // Client side: the server pushed the full board (periodic broadcast or initial sync).
        private static IEnumerator OnClientReceiveBoard(long sender, ZPackage package)
        {
            string yaml = package.ReadString();
            try {
                UpdateLeaderboardFromYaml(yaml);
                LeaderboardUI.RefreshIfVisible();
            } catch (Exception e) {
                Logger.LogWarning($"Failed to parse leaderboard board. Exception: {e}");
            }
            yield return null;
        }

        private static ZPackage SendFullBoardPackage()
        {
            ZPackage pkg = new ZPackage();
            pkg.Write(yamlserializer.Serialize(leaderboard));
            return pkg;
        }

        public static void BroadcastFullBoard()
        {
            if (ZNet.instance == null || leaderboardRPC == null) { return; }
            leaderboardRPC.SendPackage(ZNet.instance.m_peers, SendFullBoardPackage());
        }

        public static void SendLocalSnapshotToServer()
        {
            LeaderboardEntry snap = BuildLocalSnapshot();
            if (snap == null) { return; }
            // On a listen-server host we are the server: merge directly instead of sending to ourselves.
            if (ZNet.instance != null && ZNet.instance.IsServer()) {
                MergeSnapshot(snap);
                return;
            }
            if (leaderboardRPC == null || ZRoutedRpc.instance == null) { return; }
            ZPackage pkg = new ZPackage();
            pkg.Write(yamlserializer.Serialize(snap));
            leaderboardRPC.SendPackage(ZRoutedRpc.instance.GetServerPeerID(), pkg);
        }

        // ---------------------------------------------------------------------
        // Snapshot building (client)
        // ---------------------------------------------------------------------

        public static LeaderboardEntry BuildLocalSnapshot()
        {
            Player player = Player.m_localPlayer;
            if (player == null) { return null; }
            PlayerProfile profile = Game.instance != null ? Game.instance.GetPlayerProfile() : null;
            if (profile == null || profile.m_playerStats == null) { return null; }

            var stats = profile.m_playerStats.m_stats;

            // Flush this session's accumulated damage into the persistent unique key.
            long persistedDamage = GetLongKey(player, LeaderboardDamageKey);
            if (sessionDamage > 0) {
                persistedDamage += sessionDamage;
                SetLongKey(player, LeaderboardDamageKey, persistedDamage);
                sessionDamage = 0;
            }

            return new LeaderboardEntry {
                PlayerID = player.GetPlayerID(),
                PlayerName = profile.GetName(),
                DeathChoice = DeathConfigurationData.playerDeathConfiguration?.DisplayName ?? "",
                FirstLifeSeconds = GetLongKey(player, LeaderboardFirstLifeKey),
                LongestLifeSeconds = GetLongKey(player, LeaderboardLongestLifeKey),
                TotalLifeSeconds = GetLongKey(player, LeaderboardTotalLifeKey),
                DeathCount = (int)GetLongKey(player, LeaderboardDeathCountKey),
                TotalDamage = persistedDamage,
                BossKills = (int)stats[PlayerStatType.BossKills],
                TreeChops = (int)stats[PlayerStatType.TreeChops],
                Mines = (int)stats[PlayerStatType.Mines],
                CraftsAndBuilds = (int)(stats[PlayerStatType.CraftsOrUpgrades] + stats[PlayerStatType.Builds]),
            };
        }

        // ---------------------------------------------------------------------
        // Periodic sync loop (started from Deathlink.Awake; runs on client and server)
        // ---------------------------------------------------------------------

        public static IEnumerator SyncLoop()
        {
            // Let the game/world finish loading before the first tick.
            yield return new WaitForSeconds(60f);
            while (true) {
                float intervalSeconds = Mathf.Max(60f, ValConfig.LeaderboardSyncInterval.Value * 60f);
                if (ValConfig.EnableLeaderboard.Value && ZNet.instance != null) {
                    try {
                        if (ZNet.instance.IsServer()) {
                            // A listen-server host is also a player: report its own stats first.
                            if (Player.m_localPlayer != null) { SendLocalSnapshotToServer(); }
                            BroadcastFullBoard();
                        } else if (Player.m_localPlayer != null) {
                            SendLocalSnapshotToServer();
                        }
                    } catch (Exception e) {
                        Logger.LogWarning($"Leaderboard sync tick failed: {e}");
                    }
                }
                yield return new WaitForSeconds(intervalSeconds);
            }
        }

        // ---------------------------------------------------------------------
        // Tracking hooks
        // ---------------------------------------------------------------------

        // Survival: record the just-ended life's played time (m_timeSinceDeath) at death.
        [HarmonyPatch(typeof(Player), nameof(Player.OnDeath))]
        public static class Leaderboard_OnDeath_Patch
        {
            [HarmonyPrefix]
            public static void Prefix(Player __instance)
            {
                if (!ValConfig.EnableLeaderboard.Value) { return; }
                if (__instance == null || __instance != Player.m_localPlayer) { return; }

                long life = (long)Mathf.Max(0f, __instance.m_timeSinceDeath);
                long deaths = GetLongKey(__instance, LeaderboardDeathCountKey);
                long total = GetLongKey(__instance, LeaderboardTotalLifeKey);
                long longest = GetLongKey(__instance, LeaderboardLongestLifeKey);

                if (deaths == 0) { SetLongKey(__instance, LeaderboardFirstLifeKey, life); }
                SetLongKey(__instance, LeaderboardDeathCountKey, deaths + 1);
                SetLongKey(__instance, LeaderboardTotalLifeKey, total + life);
                if (life > longest) { SetLongKey(__instance, LeaderboardLongestLifeKey, life); }
            }
        }

        // Combat: accumulate damage the local player deals to non-player characters.
        // Character.Damage runs on the machine owning the target; after a hit, ownership of the
        // target typically transfers to the attacker, so this captures the common case. In some
        // multiplayer situations the value is approximate, which is acceptable for a leaderboard.
        [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
        public static class Leaderboard_Damage_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(Character __instance, HitData hit)
            {
                if (!ValConfig.EnableLeaderboard.Value) { return; }
                if (hit == null || Player.m_localPlayer == null) { return; }
                if (__instance is Player) { return; } // ignore PvP / self damage
                if (hit.GetAttacker() != Player.m_localPlayer) { return; }

                long dmg = (long)Mathf.Max(0f, hit.GetTotalDamage());
                if (dmg > 0) { sessionDamage += dmg; }
            }
        }

        // Report once shortly after the local player spawns so existing persisted stats reach the
        // server promptly (the periodic loop then keeps it in sync on the slow interval).
        [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
        public static class Leaderboard_OnSpawned_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(Player __instance)
            {
                if (!ValConfig.EnableLeaderboard.Value) { return; }
                if (__instance != Player.m_localPlayer || initialReportSent) { return; }
                initialReportSent = true;
                try {
                    SendLocalSnapshotToServer();
                } catch (Exception e) {
                    Logger.LogWarning($"Initial leaderboard report failed: {e}");
                }
            }
        }

        // ---------------------------------------------------------------------
        // Unique-key helpers (numeric values persisted as strings, like DeathChoiceChangesKey)
        // ---------------------------------------------------------------------

        private static long GetLongKey(Player player, string key)
        {
            if (player.TryGetUniqueKeyValue(key, out string raw) && long.TryParse(raw, out long val)) {
                return val;
            }
            return 0L;
        }

        private static void SetLongKey(Player player, string key, long value)
        {
            player.PlayerRemoveUniqueKey(key);
            player.AddUniqueKeyValue(key, value.ToString());
        }
    }
}
