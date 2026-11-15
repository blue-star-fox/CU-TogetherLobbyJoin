/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * Copyright (c) 2026 Bilibili UID:1951065665
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace TogetherLobbyJoin
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("CasualtiesUnknown.exe")]
    [BepInDependency("CasualtiesMP", BepInDependency.DependencyFlags.HardDependency)]
    public sealed class LobbyJoinPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "casualties.together.lobbyjoin";
        public const string PluginName = "Together - Join By Room ID";
        public const string PluginVersion = "1.8.1";
        public const string PluginAuthor = "Bilibili UID:1951065665";
        public const string PluginCopyright = "Copyright © 2026 Bilibili UID:1951065665";

        internal static LobbyJoinPlugin Instance;
        internal static ManualLogSource Log;

        internal static ConfigEntry<string> LastRoomId;
        internal static ConfigEntry<bool> EnableJoinRow;
        internal static ConfigEntry<int> MaxIdLength;
        internal static ConfigEntry<float> FontScale;
        internal static ConfigEntry<string> Language;
        internal static ConfigEntry<KeyboardShortcut> ToggleWindowKey;
        internal static ConfigEntry<bool> ForceJoin;

        internal static bool RowPatchApplied;
        internal static bool ShowStandaloneWindow;

        private Harmony _harmony;
        private MethodInfo _isMpMenuOpen;
        private float _modListLogTimer;
        private bool _modListLogged;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            EnableJoinRow = Config.Bind(
                "General",
                "EnableJoinRow",
                true,
                "在多人联机面板里显示“房间ID + 加入房间”这一行 / Show the join-by-ID row inside the multiplayer panel.");

            MaxIdLength = Config.Bind(
                "General",
                "MaxIdLength",
                96,
                new ConfigDescription(
                    "房间ID 输入框最大字符数（够长才能整段粘贴 steam://joinlobby/... 邀请链接）/ Max characters of the room ID field.",
                    new AcceptableValueRange<int>(6, 256)));

            LastRoomId = Config.Bind(
                "General",
                "LastRoomId",
                string.Empty,
                "上次输入的房间ID（自动保存，下次开游戏还在）/ Last room ID, remembered between sessions.");

            ForceJoin = Config.Bind(
                "General",
                "ForceJoin",
                false,
                "跳过加入前的版本/锁定/密码/人数预检（预检只是提前告知，真实判定仍由服务器做）/ Skip the pre-join metadata checks.");

            FontScale = Config.Bind(
                "UI",
                "FontScale",
                1f,
                new ConfigDescription(
                    "UI 文字倍率：字号已经跟 MOD 面板一致，嫌小就调大（0.5~3）/ Text scale of the room-ID row.",
                    new AcceptableValueRange<float>(0.5f, 3f)));

            Language = Config.Bind(
                "UI",
                "Language",
                "Auto",
                new ConfigDescription(
                    "Auto = 跟随 MOD 界面语言；也可强制 zh-CN / en (Auto follows the mod UI language).",
                    new AcceptableValueList<string>("Auto", "zh-CN", "en")));

            ToggleWindowKey = Config.Bind(
                "General",
                "ToggleWindowKey",
                new KeyboardShortcut(KeyCode.F8),
                "开关备用输入小窗的快捷键 / Hotkey for the fallback input window.");

            LobbyJoinUi.InputText = LastRoomId.Value ?? string.Empty;

            _isMpMenuOpen = AccessTools.Method(AccessTools.TypeByName("Together.UIMainMenu"), "IsOpen", Type.EmptyTypes);

            _harmony = new Harmony(PluginGuid);
            RowPatchApplied = ApplyUiPatch();
            ApplyModListHiding();
            ApplyDiagnostics();

            if (!RowPatchApplied)
            {
                ShowStandaloneWindow = true;
                Log.LogWarning("[JoinByRoomId] 没能在多人联机面板里插入输入行，已启用备用小窗（默认 F8 开关）。");
            }

            Log.LogInfo("[JoinByRoomId] v" + PluginVersion + " ready. rowPatch=" + RowPatchApplied);
            Log.LogInfo("[JoinByRoomId] author: " + PluginAuthor + " | " + PluginCopyright);
        }

        private bool ApplyUiPatch()
        {
            try
            {
                Type uiType = AccessTools.TypeByName("Together.UIMainMenu");
                if (uiType == null)
                {
                    Log.LogError("[JoinByRoomId] 找不到 Together.UIMainMenu，确认 Together(CasualtiesMP) MOD 已正确安装。");
                    return false;
                }

                MethodInfo steamRow = FindMethod(uiType, "_GUI___DirectConnect_DoSteamInfoo", 4);
                if (steamRow != null)
                {
                    MethodInfo postfix = AccessTools.Method(typeof(LobbyJoinPlugin), nameof(PostfixSteamRow));
                    _harmony.Patch(steamRow, postfix: new HarmonyMethod(postfix));
                    Log.LogInfo("[JoinByRoomId] patched " + Signature(steamRow) + " applied=" + IsPatched(steamRow));
                    return IsPatched(steamRow);
                }

                MethodInfo panel = FindMethod(uiType, "_GUI__DirectConnect", 1);
                if (panel != null)
                {
                    MethodInfo postfix = AccessTools.Method(typeof(LobbyJoinPlugin), nameof(PostfixDirectConnectPanel));
                    _harmony.Patch(panel, postfix: new HarmonyMethod(postfix));
                    Log.LogWarning("[JoinByRoomId] 主目标缺失，改用面板级兜底补丁 " + Signature(panel));
                    return IsPatched(panel);
                }

                Log.LogError("[JoinByRoomId] 两个目标方法都没找到，MOD 版本可能改动过大。");
                return false;
            }
            catch (Exception e)
            {
                Log.LogError("[JoinByRoomId] ApplyUiPatch: " + e);
                return false;
            }
        }

        private bool IsPatched(MethodBase m)
        {
            foreach (MethodBase patched in _harmony.GetPatchedMethods())
            {
                if (patched == m)
                {
                    return true;
                }
            }

            return false;
        }

        private static MethodInfo FindMethod(Type type, string name, int parameterCount)
        {
            foreach (MethodInfo m in type.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (m.Name != name)
                {
                    continue;
                }

                ParameterInfo[] ps = m.GetParameters();
                if (ps.Length != parameterCount || ps[0].ParameterType != typeof(Rect))
                {
                    continue;
                }

                return m;
            }

            return null;
        }

        private static string Signature(MethodInfo m)
        {
            var parts = new List<string>();
            foreach (ParameterInfo p in m.GetParameters())
            {
                parts.Add(p.ParameterType.Name + " " + p.Name);
            }

            return m.DeclaringType?.FullName + "." + m.Name + "(" + string.Join(", ", parts) + ")";
        }

        internal static void PostfixSteamRow(Rect r2, float fieldheight, float fieldnameswidth, float fieldswidth)
        {
            if (!EnableJoinRow.Value)
            {
                return;
            }

            try
            {
                LobbyJoinUi.DrawRow(r2, fieldheight, fieldnameswidth, fieldswidth);
            }
            catch (Exception e)
            {
                Log.LogError("[JoinByRoomId] PostfixSteamRow: " + e);
            }
        }

        internal static void PostfixDirectConnectPanel(Rect r2)
        {
            if (!EnableJoinRow.Value)
            {
                return;
            }

            try
            {
                LobbyJoinUi.DrawFallbackRow(r2);
            }
            catch (Exception e)
            {
                Log.LogError("[JoinByRoomId] PostfixDirectConnectPanel: " + e);
            }
        }

        private void ApplyModListHiding()
        {
            try
            {
                Type mp = AccessTools.TypeByName("Together.Multiplayer");
                if (mp == null)
                {
                    return;
                }

                MethodInfo getPlugins = AccessTools.Method(mp, "GetBepinPlugins", Type.EmptyTypes);
                if (getPlugins != null)
                {
                    _harmony.Patch(
                        getPlugins,
                        postfix: new HarmonyMethod(AccessTools.Method(typeof(LobbyJoinPlugin), nameof(GetBepinPluginsPostfix))));
                    Log.LogInfo("[JoinByRoomId] hid plugin from GetBepinPlugins applied=" + IsPatched(getPlugins));
                }

                MethodInfo getGuids = AccessTools.Method(mp, "GetModGUIDs", Type.EmptyTypes);
                if (getGuids != null)
                {
                    _harmony.Patch(
                        getGuids,
                        postfix: new HarmonyMethod(AccessTools.Method(typeof(LobbyJoinPlugin), nameof(GetModGUIDsPostfix))));
                    Log.LogInfo("[JoinByRoomId] hid plugin from GetModGUIDs applied=" + IsPatched(getGuids));
                }
            }
            catch (Exception e)
            {
                Log.LogWarning("[JoinByRoomId] ApplyModListHiding: " + e.Message);
            }
        }

        internal static void GetBepinPluginsPostfix(ref List<PluginInfo> __result)
        {
            try
            {
                __result?.RemoveAll(p => p?.Metadata != null && p.Metadata.GUID == PluginGuid);
            }
            catch
            {
            }
        }

        internal static void GetModGUIDsPostfix(ref string[] __result)
        {
            try
            {
                if (__result == null)
                {
                    return;
                }

                var filtered = new List<string>(__result.Length);
                foreach (string g in __result)
                {
                    if (g != PluginGuid)
                    {
                        filtered.Add(g);
                    }
                }

                __result = filtered.ToArray();
            }
            catch
            {
            }
        }

        private void ApplyDiagnostics()
        {
            try
            {
                Type ksteam = AccessTools.TypeByName("Together.KSteam");
                if (ksteam != null)
                {
                    foreach (MethodInfo m in ksteam.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        if (m.Name != "LeaveLobbyAndResetNet")
                        {
                            continue;
                        }

                        _harmony.Patch(
                            m,
                            prefix: new HarmonyMethod(AccessTools.Method(typeof(LobbyJoinPlugin), nameof(LeaveLobbyPrefix))));
                        Log.LogInfo("[JoinByRoomId] diagnostic patch: " + Signature(m));
                    }
                }

                Type transport = AccessTools.TypeByName("Together.TransportSteamworks");
                MethodInfo createClientSocket = transport == null
                    ? null
                    : AccessTools.Method(transport, "CreateClientSocket", Type.EmptyTypes);
                if (createClientSocket != null)
                {
                    _harmony.Patch(
                        createClientSocket,
                        postfix: new HarmonyMethod(AccessTools.Method(typeof(LobbyJoinPlugin), nameof(CreateClientSocketPostfix))));
                    Log.LogInfo("[JoinByRoomId] diagnostic patch: CreateClientSocket");
                }
            }
            catch (Exception e)
            {
                Log.LogWarning("[JoinByRoomId] ApplyDiagnostics: " + e.Message);
            }
        }

        internal static void LeaveLobbyPrefix()
        {
            try
            {
                LobbyJoinService.OnLeaveLobbyCalled(FrameStack(7));
            }
            catch
            {
            }
        }

        internal static void CreateClientSocketPostfix(bool __result)
        {
            try
            {
                string owner = "?";
                try
                {
                    owner = Together.KSteam.CURRENT_LOBBY.ownerID.m_SteamID.ToString();
                }
                catch
                {
                }

                LobbyJoinService.OnClientSocketResult(__result, owner);
            }
            catch
            {
            }
        }

        private static string FrameStack(int frames)
        {
            try
            {
                var st = new StackTrace(1, false);
                var sb = new System.Text.StringBuilder();
                int count = Mathf.Min(frames, st.FrameCount);
                for (int i = 0; i < count; i++)
                {
                    MethodBase mb = st.GetFrame(i)?.GetMethod();
                    if (mb != null)
                    {
                        sb.Append("\n   at ").Append(mb.DeclaringType?.Name).Append('.').Append(mb.Name);
                    }
                }

                return sb.ToString();
            }
            catch
            {
                return "";
            }
        }

        private void Update()
        {
            try
            {
                if (ToggleWindowKey.Value.IsDown())
                {
                    ShowStandaloneWindow = !ShowStandaloneWindow;
                }
            }
            catch
            {
            }

            try
            {
                LobbyJoinService.Tick();
            }
            catch (Exception e)
            {
                Log.LogError("[JoinByRoomId] Tick: " + e);
            }

            LogModListOnce();
        }

        private void LogModListOnce()
        {
            if (_modListLogged)
            {
                return;
            }

            _modListLogTimer += Time.unscaledDeltaTime;
            if (_modListLogTimer < 12f)
            {
                return;
            }

            _modListLogged = true;
            try
            {
                Log.LogInfo("[JoinByRoomId] mod list sent to servers = [" + string.Join(", ", Together.Multiplayer.GetModGUIDs()) + "]");
            }
            catch (Exception e)
            {
                Log.LogWarning("[JoinByRoomId] GetModGUIDs: " + e.Message);
            }
        }

        private void OnGUI()
        {
            if (!IsMpMenuOpen())
            {
                return;
            }

            if (LobbyJoinService.Pending != null)
            {
                LobbyJoinUi.DrawInspectWindow();
            }

            if (ShowStandaloneWindow)
            {
                LobbyJoinUi.DrawStandaloneWindow();
            }
        }

        private bool IsMpMenuOpen()
        {
            try
            {
                if (_isMpMenuOpen == null)
                {
                    _isMpMenuOpen = AccessTools.Method(AccessTools.TypeByName("Together.UIMainMenu"), "IsOpen", Type.EmptyTypes);
                }

                return _isMpMenuOpen == null || (bool)_isMpMenuOpen.Invoke(null, null);
            }
            catch
            {
                return true;
            }
        }
    }
}
