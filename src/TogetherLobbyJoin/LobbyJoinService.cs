/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * Copyright (c) 2026 Bilibili UID:1951065665
 */
using System;
using Steamworks;
using Together;
using UnityEngine;

namespace TogetherLobbyJoin
{
    internal sealed class LobbyInspection
    {
        internal ulong LobbyId;
        internal string Name = string.Empty;
        internal string Version = string.Empty;
        internal string MyVersion = string.Empty;
        internal int Members;
        internal int Limit;
        internal bool HasPassword;
        internal bool MidjoinLocked;
        internal bool Exists;

        internal bool VersionMismatch
        {
            get { return !string.IsNullOrEmpty(Version) && !string.Equals(Version, MyVersion, StringComparison.Ordinal); }
        }

        internal bool Full
        {
            get { return Limit > 0 && Members >= Limit; }
        }

        internal string Describe()
        {
            return "id=" + LobbyId + " name=\"" + Name + "\" version=\"" + Version + "\" (mine " + MyVersion + ")"
                   + " members=" + Members + "/" + Limit + " password=" + HasPassword + " locked=" + MidjoinLocked;
        }
    }

    internal static class LobbyJoinService
    {
        private const string KeyVersion = "COOP_MOD_VERSION";
        private const string KeyLobbyName = "COOP_MOD_LOBBYNAME";
        private const string KeyHasPassword = "COOP_MOD_HASPASSWORD";
        private const string KeyLocked = "COOP_MOD_LOCKED";
        private const string KeyMods = "COOP_MOD_MODS";
        private const string KeyPlrCount = "COOP_MOD_PLRCOUNT";
        private const string KeyGamemode = "COOP_MOD_GAMEMODE";

        private enum Phase
        {
            Idle,
            WaitingData,
            Joining,
        }

        private static Phase _phase = Phase.Idle;
        private static ulong _lobbyId;
        private static float _timer;
        private static string _watchedStatus = string.Empty;
        private static float _watchUntil;
        private static float _pendingUntil;

        private static bool _editionLogged;
        private static ulong _myLobbySeen;

        internal static LobbyInspection Pending;

        internal static bool IsBusy
        {
            get { return _phase != Phase.Idle || Time.realtimeSinceStartup < _watchUntil; }
        }

        internal static void BeginJoin(string rawInput)
        {
            try
            {
                string input = (rawInput ?? string.Empty).Trim();

                Pending = null;

                if (input.Length == 0)
                {
                    Fail(Loc.EmptyId);
                    return;
                }

                if (Net.IsRunning)
                {
                    Fail(Loc.AlreadyConnected);
                    return;
                }

                if (!KSteam.Loaded)
                {
                    Fail(Loc.SteamNotReady);
                    return;
                }

                ulong lobbyId;
                if (!LobbyIdParser.TryParse(input, out lobbyId) || !LobbyIdParser.IsLobbyId(lobbyId))
                {
                    Fail(Loc.BadId);
                    return;
                }

                _lobbyId = lobbyId;
                _phase = Phase.WaitingData;
                _timer = 0f;
                _watchedStatus = Multiplayer.multiplayerStatusMessage ?? string.Empty;
                _watchUntil = Time.realtimeSinceStartup + 20f;

                try
                {
                    SteamMatchmaking.RequestLobbyData(new CSteamID(lobbyId));
                }
                catch (Exception e)
                {
                    LobbyJoinPlugin.Log.LogWarning("[JoinByRoomId] RequestLobbyData: " + e.Message);
                }

                Multiplayer.DoMultiplayerStatusMessageLog(string.Format(Loc.CheckingFmt, lobbyId));
                LobbyJoinPlugin.Log.LogInfo("[JoinByRoomId] inspect start: lobby=" + lobbyId + " requestData sent");
            }
            catch (Exception e)
            {
                Fail(string.Format(Loc.JoinErrorFmt, e.Message));
                LobbyJoinPlugin.Log.LogError("[JoinByRoomId] BeginJoin: " + e);
            }
        }

        internal static void ConfirmJoin(string password)
        {
            try
            {
                LobbyInspection info = Pending;
                if (info == null)
                {
                    return;
                }

                if (Net.IsRunning)
                {
                    Pending = null;
                    Fail(Loc.AlreadyConnected);
                    return;
                }

                string pwd = (password ?? string.Empty).Trim();
                if (info.HasPassword && pwd.Length == 0)
                {
                    Multiplayer.DoMultiplayerStatusMessageError("[JoinByRoomId] " + Loc.PasswordRequired);
                    PlaySound(global::PlayerCamera.UISoundType.Deny);
                    return;
                }

                if (info.HasPassword)
                {
                    Multiplayer.INPUT_PASSWORD = pwd;
                    LobbyJoinPlugin.Log.LogInfo("[JoinByRoomId] password set for lobby " + info.LobbyId + " (length=" + pwd.Length + ")");
                }

                Pending = null;
                _lobbyId = info.LobbyId;
                StartJoin();
            }
            catch (Exception e)
            {
                Fail(string.Format(Loc.JoinErrorFmt, e.Message));
                LobbyJoinPlugin.Log.LogError("[JoinByRoomId] ConfirmJoin: " + e);
            }
        }

        internal static void CancelInspect()
        {
            if (Pending == null)
            {
                return;
            }

            LobbyJoinPlugin.Log.LogInfo("[JoinByRoomId] inspect cancelled by user: " + Pending.LobbyId);
            Pending = null;
            PlaySound(global::PlayerCamera.UISoundType.Close);
        }

        internal static void Tick()
        {
            try
            {
                LogEditionOnce();
                LogMyLobbyId();
                WatchStatusMessage();

                if (Pending != null && _pendingUntil > 0f && Time.realtimeSinceStartup > _pendingUntil)
                {
                    Pending = null;
                }

                if (_phase == Phase.Idle)
                {
                    return;
                }

                _timer += Time.unscaledDeltaTime;

                if (_phase == Phase.WaitingData)
                {
                    bool dataReady = HasLobbyData();
                    if (!dataReady && _timer < 2.5f)
                    {
                        return;
                    }

                    if (!dataReady)
                    {
                        LobbyJoinPlugin.Log.LogWarning("[JoinByRoomId] lobby data not received in 2.5s, treating it as 'not visible'");
                    }

                    LobbyInspection info = Inspect();
                    _phase = Phase.Idle;

                    if (info == null || !info.Exists)
                    {
                        Fail(string.Format(Loc.RefuseLobbyMissing, _lobbyId, KSteam.STEAM_APPID));
                        return;
                    }

                    Pending = info;
                    _pendingUntil = Time.realtimeSinceStartup + 180f;

                    LobbyJoinPlugin.Log.LogInfo("[JoinByRoomId] inspect ready: " + info.Describe());
                    Multiplayer.DoMultiplayerStatusMessageLog(
                        string.Format(Loc.InspectReadyFmt, string.IsNullOrEmpty(info.Name) ? Loc.UnnamedRoom : info.Name));
                    PlaySound(global::PlayerCamera.UISoundType.MiniClick);
                    return;
                }

                if (KSteam.IS_IN_LOBBY || Net.IsRunning || Net.IsConnecting)
                {
                    LobbyJoinPlugin.Log.LogInfo(
                        "[JoinByRoomId] mod took over: inLobby=" + KSteam.IS_IN_LOBBY + " running=" + Net.IsRunning + " connecting=" + Net.IsConnecting
                        + " transport=" + (Net.Transport == null ? "null" : Net.Transport.GetType().Name));
                    _phase = Phase.Idle;
                    return;
                }

                if (_timer > 6f)
                {
                    LobbyJoinPlugin.Log.LogWarning("[JoinByRoomId] no state change 6s after JoinLobby (steam never entered the lobby?)");
                    _phase = Phase.Idle;
                }
            }
            catch (Exception e)
            {
                LobbyJoinPlugin.Log.LogError("[JoinByRoomId] Tick: " + e);
                _phase = Phase.Idle;
            }
        }

        private static void StartJoin()
        {
            _phase = Phase.Joining;
            _timer = 0f;

            LobbyJoinPlugin.Log.LogInfo("[JoinByRoomId] confirmed by user, calling OnWantToJoinLobby(" + _lobbyId + ")");
            Multiplayer.DoMultiplayerStatusMessageLog(string.Format(Loc.JoinAttemptFmt, _lobbyId));

            bool started;
            try
            {
                started = TransportSteamworks.OnWantToJoinLobby(_lobbyId);
            }
            catch (Exception e)
            {
                Fail(string.Format(Loc.JoinErrorFmt, e.Message));
                LobbyJoinPlugin.Log.LogError("[JoinByRoomId] OnWantToJoinLobby: " + e);
                return;
            }

            if (!started)
            {
                Fail(string.Format(Loc.JoinFailedFmt, _lobbyId));
                return;
            }

            PlaySound(global::PlayerCamera.UISoundType.MiniClick);
        }

        private static bool HasLobbyData()
        {
            try
            {
                return !string.IsNullOrEmpty(SteamMatchmaking.GetLobbyData(new CSteamID(_lobbyId), KeyVersion));
            }
            catch
            {
                return false;
            }
        }

        private static LobbyInspection Inspect()
        {
            try
            {
                var lobby = new CSteamID(_lobbyId);
                var info = new LobbyInspection
                {
                    LobbyId = _lobbyId,
                    Name = SteamMatchmaking.GetLobbyData(lobby, KeyLobbyName) ?? string.Empty,
                    Version = SteamMatchmaking.GetLobbyData(lobby, KeyVersion) ?? string.Empty,
                    MyVersion = Multiplayer.FULL_VERSION_TAG ?? "?",
                    Members = SteamMatchmaking.GetNumLobbyMembers(lobby),
                    Limit = SteamMatchmaking.GetLobbyMemberLimit(lobby),
                    HasPassword = string.Equals(SteamMatchmaking.GetLobbyData(lobby, KeyHasPassword), "1", StringComparison.Ordinal),
                    MidjoinLocked = string.Equals(SteamMatchmaking.GetLobbyData(lobby, KeyLocked), "1", StringComparison.Ordinal),
                };

                CSteamID owner = SteamMatchmaking.GetLobbyOwner(lobby);
                info.Exists = owner.m_SteamID != 0UL || info.Members > 0 || !string.IsNullOrEmpty(info.Version) || !string.IsNullOrEmpty(info.Name);

                string mods = SteamMatchmaking.GetLobbyData(lobby, KeyMods);
                string gamemode = SteamMatchmaking.GetLobbyData(lobby, KeyGamemode);
                string plrCount = SteamMatchmaking.GetLobbyData(lobby, KeyPlrCount);

                LobbyJoinPlugin.Log.LogInfo(
                    "[JoinByRoomId] lobby info: " + info.Describe()
                    + " owner=" + owner.m_SteamID
                    + " gamemode=" + gamemode
                    + " plrCount=" + plrCount
                    + " mods=\"" + Shorten(mods) + "\""
                    + " myMods=\"" + string.Join(",", Multiplayer.GetModGUIDs()) + "\"");

                return info;
            }
            catch (Exception e)
            {
                LobbyJoinPlugin.Log.LogWarning("[JoinByRoomId] Inspect failed: " + e.Message);
                return null;
            }
        }

        private static string Shorten(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }

            return s.Length <= 160 ? s : s.Substring(0, 160) + "...";
        }

        private static void WatchStatusMessage()
        {
            if (Time.realtimeSinceStartup > _watchUntil)
            {
                return;
            }

            string current = Multiplayer.multiplayerStatusMessage ?? string.Empty;
            if (current == _watchedStatus)
            {
                return;
            }

            _watchedStatus = current;
            LobbyJoinPlugin.Log.LogInfo("[JoinByRoomId] mod status: " + Sanitize(current));

            if (current.IndexOf("does not exist", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                try
                {
                    Multiplayer.DoMultiplayerStatusMessageError("[JoinByRoomId] " + Loc.HintLobbyMissing);
                }
                catch
                {
                }
            }
        }

        private static string Sanitize(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }

            string one = s.Replace("\r", " ").Replace("\n", " | ");
            return one.Length <= 400 ? one : one.Substring(0, 400) + "...";
        }

        private static void LogEditionOnce()
        {
            if (_editionLogged || !KSteam.Loaded)
            {
                return;
            }

            _editionLogged = true;
            try
            {
                bool gameIsFull = true;
                try
                {
                    gameIsFull = CasualtiesTogetherUtils.Util.GameIsFull();
                }
                catch
                {
                }

                string appidFile = "(missing)";
                string appidPath = string.Empty;
                try
                {
                    appidPath = System.IO.Path.Combine(BepInEx.Paths.GameRootPath, "steam_appid.txt");
                    if (System.IO.File.Exists(appidPath))
                    {
                        appidFile = System.IO.File.ReadAllText(appidPath).Trim();
                    }
                }
                catch
                {
                }

                string wanted = string.Empty;
                try
                {
                    string cfg = System.IO.Path.Combine(BepInEx.Paths.BepInExRootPath, "config", "TogetherLobbyJoin.appid.txt");
                    if (System.IO.File.Exists(cfg))
                    {
                        string all = System.IO.File.ReadAllText(cfg);
                        int nl = all.IndexOfAny(new[] { '\r', '\n' });
                        wanted = (nl < 0 ? all : all.Substring(0, nl)).Trim();
                    }
                }
                catch
                {
                }

                LobbyJoinPlugin.Log.LogInfo(
                    "[JoinByRoomId] steam appid=" + KSteam.STEAM_APPID
                    + " full=" + KSteam.APP_IS_FULL
                    + " demo=" + KSteam.APP_IS_DEMO
                    + " playtest=" + KSteam.APP_IS_PLAYTEST
                    + " gameFilesAreFullGame=" + gameIsFull
                    + " steam_appid.txt=\"" + appidFile + "\" path=\"" + appidPath + "\""
                    + " wantedAppId=\"" + wanted + "\""
                    + " version=" + Multiplayer.FULL_VERSION_TAG);

                if (!string.IsNullOrEmpty(wanted) && wanted != KSteam.STEAM_APPID.ToString())
                {
                    LobbyJoinPlugin.Log.LogWarning(
                        "[JoinByRoomId] 配置里要求 AppID=" + wanted + "，但实际运行在 " + KSteam.STEAM_APPID
                        + " —— 说明这次是 Steam 启动的（AppID 由 Steam 启动入口决定）。直接双击 CasualtiesUnknown.exe 启动才会生效。");
                }
            }
            catch
            {
            }
        }

        private static void LogMyLobbyId()
        {
            try
            {
                ulong id = KSteam.CURRENT_LOBBY.lobby_steamID.m_SteamID;
                if (id == _myLobbySeen)
                {
                    return;
                }

                _myLobbySeen = id;
                if (id != 0UL)
                {
                    LobbyJoinPlugin.Log.LogInfo(
                        "[JoinByRoomId] my lobby id = " + id
                        + " (appid=" + KSteam.STEAM_APPID + " role=" + (Net.IsServer ? "host" : "client") + ")");
                }
            }
            catch
            {
            }
        }

        internal static void OnLeaveLobbyCalled(string detail)
        {
            if (!IsBusy)
            {
                return;
            }

            LobbyJoinPlugin.Log.LogWarning("[JoinByRoomId] LeaveLobbyAndResetNet during join attempt: " + detail);
        }

        internal static void OnClientSocketResult(bool ok, string owner)
        {
            if (!IsBusy)
            {
                return;
            }

            LobbyJoinPlugin.Log.LogInfo("[JoinByRoomId] CreateClientSocket=" + ok + " owner=" + owner);
        }

        private static bool Fail(string message)
        {
            _phase = Phase.Idle;

            try
            {
                Multiplayer.DoMultiplayerStatusMessageError("[JoinByRoomId] " + message);
            }
            catch
            {
                LobbyJoinPlugin.Log.LogWarning("[JoinByRoomId] " + message);
            }

            LobbyJoinPlugin.Log.LogWarning("[JoinByRoomId] refuse: " + Sanitize(message));
            PlaySound(global::PlayerCamera.UISoundType.Deny);
            return false;
        }

        internal static void PlaySound(global::PlayerCamera.UISoundType type)
        {
            try
            {
                CasualtiesTogetherUtils.Util.PlayUISound(type);
            }
            catch
            {
            }
        }
    }
}
