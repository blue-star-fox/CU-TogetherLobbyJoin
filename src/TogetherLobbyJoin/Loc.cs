/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * Copyright (c) 2026 Bilibili UID:1951065665
 */
using System;

namespace TogetherLobbyJoin
{
    internal static class Loc
    {
        internal static bool IsChinese
        {
            get
            {
                string forced = LobbyJoinPlugin.Language?.Value;
                if (!string.IsNullOrEmpty(forced))
                {
                    if (forced.StartsWith("zh", StringComparison.OrdinalIgnoreCase) || forced.StartsWith("cn", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    if (forced.StartsWith("en", StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }

                return DetectChineseFromModUi();
            }
        }

        private static bool DetectChineseFromModUi()
        {
            string[] probeKeys = { "mmct_copylobbycode", "mmct_passwordfield", "mmct_steam_lobbytype", "back" };
            foreach (string key in probeKeys)
            {
                try
                {
                    if (Together.Lang.TryGet(key, out string text) && !string.IsNullOrEmpty(text))
                    {
                        if (ContainsCjk(text))
                        {
                            return true;
                        }

                        if (ContainsLatin(text))
                        {
                            return false;
                        }
                    }
                }
                catch
                {
                }
            }

            try
            {
                string name = global::Locale.currentLangName;
                if (!string.IsNullOrEmpty(name))
                {
                    if (name.IndexOf("zh", StringComparison.OrdinalIgnoreCase) >= 0
                        || name.IndexOf("cn", StringComparison.OrdinalIgnoreCase) >= 0
                        || name.IndexOf("chinese", StringComparison.OrdinalIgnoreCase) >= 0
                        || name.IndexOf("中文", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        private static bool ContainsCjk(string s)
        {
            foreach (char c in s)
            {
                if ((c >= 0x3400 && c <= 0x4DBF) || (c >= 0x4E00 && c <= 0x9FFF) || (c >= 0xF900 && c <= 0xFAFF))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsLatin(string s)
        {
            foreach (char c in s)
            {
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
                {
                    return true;
                }
            }

            return false;
        }

        internal static string T(string en, string cn)
        {
            return IsChinese ? cn : en;
        }

        internal static string RoomIdLabel => T("Room ID: ", "房间ID: ");
        internal static string RoomIdLabelShort => T("ID: ", "ID: ");
        internal static string JoinButton => T("Join", "加入房间");
        internal static string PasteButton => T("Paste", "粘贴");
        internal static string CloseButton => T("Close", "关闭");
        internal static string CancelButton => T("Cancel", "取消");
        internal static string WindowTitle => T("Join room by ID", "按房间ID加入");

        internal static string InspectTitle => T("Room info", "房间信息");
        internal static string FieldRoomId => T("Room ID: ", "房间ID: ");
        internal static string FieldName => T("Name: ", "房间名: ");
        internal static string FieldPlayers => T("Players: ", "人数: ");
        internal static string FieldVersion => T("Version: ", "版本: ");
        internal static string FieldPassword => T("Password: ", "需要密码: ");
        internal static string FieldPasswordInput => T("Password: ", "密码: ");
        internal static string Yes => T("yes", "需要");
        internal static string No => T("no", "不需要");
        internal static string UnnamedRoom => T("(unnamed)", "未命名房间");
        internal static string InspectReadyFmt => T("Room info loaded: {0}. Confirm to join.", "已获取房间信息：{0}，请确认是否加入。");
        internal static string WarnVersionMismatch => T(
            "Version mismatch with the host ({0}). The host may kick you.",
            "与房主版本不一致（{0}），进房可能被踢。");
        internal static string WarnMidjoinLocked => T(
            "The host locked mid-run joins. It may still work, but expect trouble.",
            "房主锁定了中途加入，可能进得去但容易出问题。");
        internal static string WarnFull => T(
            "The room looks full.",
            "房间人数已满，可能进不去。");
        internal static string PasswordRequired => T(
            "This room needs a password. Type it in the room-info window first.",
            "该房间需要密码，请先在房间信息窗口里输入密码。");

        internal static string BusyHint => T(
            "Already connected — disconnect first, then join.",
            "当前已连接/连接中，先断开再加入。");
        internal static string WindowHint => T(
            "Paste the room ID the host copied with \"Copy Lobby Id\".",
            "把房主用“复制大厅 ID”复制出来的号码粘进来即可加入。");
        internal static string RowTooltip => T(
            "Room ID = the code the host copied. Friends-only lobbies can be joined by Steam friends only.",
            "房间ID = 房主“复制大厅 ID”得到的那串数字。仅好友大厅只有 Steam 好友能进。");

        internal static string EmptyId => T("Enter a room ID first.", "先输入房间ID。");
        internal static string BadId => T("That is not a valid Steam lobby ID.", "这不是有效的 Steam 房间ID。");
        internal static string AlreadyConnected => T(
            "You are already connected or connecting. Disconnect first.",
            "当前已在连接或游戏中，请先断开连接。");
        internal static string SteamNotReady => T(
            "Steam is not ready yet (the mod is still initialising).",
            "Steam 尚未初始化完成（MOD 还在启动中），稍后再试。");
        internal static string CheckingFmt => T("Checking room {0} ...", "正在检查房间 {0} ...");
        internal static string FoundLobbyFmt => T("Found room \"{0}\" ({1}/{2}), joining ...", "找到房间“{0}”（{1}/{2}），正在加入……");
        internal static string JoinAttemptFmt => T("Join by ID: lobby {0} ...", "按ID加入：房间 {0} ...");
        internal static string JoinFailedFmt => T("Join by ID failed: could not start joining lobby {0}.", "加入失败：无法发起加入房间 {0}。");
        internal static string JoinErrorFmt => T("Join by ID error: {0}", "加入出错：{0}");
        internal static string PastedInvalid => T("Clipboard has no Steam lobby ID.", "剪贴板里没有可用的 Steam 房间ID。");

        internal static string RefuseVersion => T(
            "Version mismatch: room runs [{0}], you run [{1}]. The host would kick you instantly.",
            "版本不一致：房间是 [{0}]，你是 [{1}]，进房会被服务器立刻踢掉。");
        internal static string RefuseNeedPassword => T(
            "This room has a password. Fill the password field above first.",
            "这个房间有密码，请先在上面的密码框填写密码。");
        internal static string RefuseFull => T(
            "The room is full.",
            "房间已满。");
        internal static string RefuseLobbyMissing => T(
            "Steam cannot see room {0} here (this client appid={1}). Either the host already left, the ID is stale, the two sides run different editions (full 4576490 vs demo 4576510 never see each other's lobbies), or the lobby is private/friends-only. Compare the \"steam appid=\" line in both logs.",
            "Steam 上找不到房间 {0}（本机 appid={1}）。只可能是：房主已关房 / ID 是旧的 / 双方不是同一个 App（正式版4576490 与 试玩版4576510 的大厅互不可见）/ 房间是私人或仅好友。确认版本请对比两边日志里的 “steam appid=” 行。");
        internal static string HintLobbyMissing => T(
            "Lobby does not exist: check the host is still in it, the ID is correct, and both sides run the same game edition.",
            "房间不存在：确认房主还在房间里、ID 没抄错，且双方是同一版本（正式版/演示版不能互通）。");
    }
}
