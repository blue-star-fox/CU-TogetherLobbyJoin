/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * Copyright (c) 2026 Bilibili UID:1951065665
 */
using System;
using UnityEngine;

namespace TogetherLobbyJoin
{
    internal static class LobbyJoinUi
    {
        internal const string InputControlName = "TogetherLobbyJoin_RoomIdField";
        private const int WindowId = 0x70A11;
        private const int InspectWindowId = 0x70A12;
        private static Rect _windowRect = new Rect(80f, 140f, 560f, 210f);
        private static Rect _inspectRect = new Rect(0f, 0f, 620f, 360f);
        private static bool _inspectSized;
        private static ulong _inspectShownFor;

        internal static string PasswordInput = string.Empty;

        internal static string InputText = string.Empty;

        private static GUISkin _cachedSkin;
        private static int _cachedFontSize = -1;
        private static GUIStyle _labelStyle;
        private static GUIStyle _fieldStyle;
        private static GUIStyle _buttonStyle;
        private static GUIStyle _infoStyle;
        private static GUIStyle _creditStyle;
        private static bool _loggedFirstDraw;
        private static bool _loggedGeometry;
        private static bool _loggedHidden;
        private static bool _loggedInspect;

        internal static void SetInput(string value)
        {
            string cleaned = (value ?? string.Empty).Trim();
            InputText = cleaned;

            try
            {
                if (LobbyJoinPlugin.LastRoomId != null && LobbyJoinPlugin.LastRoomId.Value != cleaned)
                {
                    LobbyJoinPlugin.LastRoomId.Value = cleaned;
                }
            }
            catch
            {
            }
        }

        internal static bool RowVisible
        {
            get
            {
                try
                {
                    if (Together.Net.IsRunning)
                    {
                        if (!_loggedHidden)
                        {
                            _loggedHidden = true;
                            LobbyJoinPlugin.Log.LogInfo(
                                "[JoinByRoomId] row hidden: running=" + Together.Net.IsRunning
                                + " server=" + Together.Net.IsServer
                                + " connecting=" + Together.Net.IsConnecting);
                        }

                        return false;
                    }

                    _loggedHidden = false;
                    return true;
                }
                catch
                {
                    return true;
                }
            }
        }

        internal static float Scale
        {
            get
            {
                try
                {
                    float s = Together.UIMainMenu.GetMenuUIScale();
                    return s <= 0.05f ? 1f : s;
                }
                catch
                {
                    return 1f;
                }
            }
        }

        private static float FontScale
        {
            get
            {
                try
                {
                    return Mathf.Clamp(LobbyJoinPlugin.FontScale == null ? 1f : LobbyJoinPlugin.FontScale.Value, 0.5f, 3f);
                }
                catch
                {
                    return 1f;
                }
            }
        }

        private static int ResolveFontSize(float scale)
        {
            int modSize;
            try
            {
                modSize = Together.UIBullshit.GetCurFontSize();
            }
            catch
            {
                modSize = 0;
            }

            int labelSize = GUI.skin != null ? GUI.skin.label.fontSize : 0;
            int buttonSize = GUI.skin != null ? GUI.skin.button.fontSize : 0;

            int fs = Mathf.Max(
                Mathf.Max(labelSize, buttonSize),
                Mathf.Max(modSize, Mathf.RoundToInt(20f * scale)));

            if (fs < 12)
            {
                fs = 12;
            }

            return Mathf.RoundToInt(fs * FontScale);
        }

        private static void EnsureStyles(float scale)
        {
            int fs = ResolveFontSize(scale);
            if (_labelStyle != null && _cachedSkin == GUI.skin && _cachedFontSize == fs)
            {
                return;
            }

            _cachedSkin = GUI.skin;
            _cachedFontSize = fs;

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = fs,
                wordWrap = false,
                alignment = TextAnchor.MiddleRight,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
                font = GUI.skin.label.font,
            };

            _fieldStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = fs,
                wordWrap = false,
                alignment = TextAnchor.MiddleLeft,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset((int)(6f * scale), (int)(6f * scale), 0, 0),
                font = GUI.skin.textField.font,
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = fs,
                wordWrap = false,
                alignment = TextAnchor.MiddleCenter,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset((int)(10f * scale), (int)(10f * scale), 0, 0),
                font = GUI.skin.button.font,
            };

            _infoStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Max(12, Mathf.RoundToInt(fs * 0.9f)),
                wordWrap = false,
                alignment = TextAnchor.MiddleLeft,
                margin = new RectOffset(0, 0, 2, 2),
                padding = new RectOffset(0, 0, 0, 0),
                font = GUI.skin.label.font,
            };

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Max(14, Mathf.RoundToInt(fs * 1.35f)),
                wordWrap = false,
                alignment = TextAnchor.MiddleLeft,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
                font = GUI.skin.label.font,
            };

            _creditStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Max(10, Mathf.RoundToInt(fs * 0.72f)),
                wordWrap = false,
                alignment = TextAnchor.MiddleRight,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
                font = GUI.skin.label.font,
            };
        }

        internal static void DrawRow(Rect panelRect, float fieldHeight, float labelWidth, float fieldWidth)
        {
            DrawBlock(panelRect, fieldHeight);
        }

        internal static void DrawFallbackRow(Rect panelRect)
        {
            GUILayout.Space(8f * Scale);
            DrawBlock(panelRect, 30f * Scale);
        }

        private static void DrawBlock(Rect panelRect, float modRowHeight)
        {
            try
            {
                if (!RowVisible)
                {
                    return;
                }

                float scale = Scale;
                EnsureStyles(scale);

                float fs = _cachedFontSize;
                float rowH = Mathf.Max(modRowHeight, fs * 1.6f);
                float gap = 8f * scale;
                bool busy = LobbyJoinService.IsBusy || LobbyJoinService.Pending != null;

                GUILayout.Space(4f * scale);

                float panelW = panelRect.width > 10f ? panelRect.width : Screen.width * 0.35f;
                string labelText = Loc.RoomIdLabel;
                float labelW = _labelStyle.CalcSize(new GUIContent(labelText)).x + 2f * scale;
                float pasteW = _buttonStyle.CalcSize(new GUIContent(Loc.PasteButton)).x + 20f * scale;
                float joinW = _buttonStyle.CalcSize(new GUIContent(Loc.JoinButton)).x + 24f * scale;
                float minFieldW = 120f * scale;

                bool oneLine = panelW >= labelW + minFieldW + pasteW + joinW + gap * 3f;
                if (!oneLine)
                {
                    labelText = Loc.RoomIdLabelShort;
                    labelW = _labelStyle.CalcSize(new GUIContent(labelText)).x + 2f * scale;
                    oneLine = panelW >= labelW + minFieldW + pasteW + joinW + gap * 3f;
                }

                float blockH = oneLine ? rowH : rowH * 2f + gap;
                Rect block = GUILayoutUtility.GetRect(
                    GUIContent.none,
                    GUIStyle.none,
                    new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(blockH) });

                if (block.width <= 1f)
                {
                    block = new Rect(block.x, block.y, panelW, blockH);
                }
                else
                {
                    block.width = Mathf.Min(block.width, panelW);
                }

                Rect labelRect;
                Rect fieldRect;
                Rect pasteRect;
                Rect joinRect;
                float right = block.xMax;

                if (oneLine)
                {
                    labelRect = new Rect(block.x, block.y, labelW, rowH);
                    joinRect = new Rect(right - joinW, block.y, joinW, rowH);
                    pasteRect = new Rect(joinRect.x - gap - pasteW, block.y, pasteW, rowH);
                    float fieldX = block.x + labelW + gap;
                    float fieldW = Mathf.Max(minFieldW, pasteRect.x - gap - fieldX);
                    fieldRect = new Rect(fieldX, block.y, fieldW, rowH);
                }
                else
                {
                    labelRect = new Rect(block.x, block.y, labelW, rowH);
                    float fieldX = block.x + labelW + gap;
                    fieldRect = new Rect(fieldX, block.y, Mathf.Max(minFieldW, block.xMax - fieldX), rowH);

                    float y2 = block.y + rowH + gap * 0.5f;
                    joinRect = new Rect(right - joinW, y2, joinW, rowH);
                    pasteRect = new Rect(joinRect.x - gap - pasteW, y2, pasteW, rowH);
                }

                bool clicked = DrawControls(busy, rowH, labelText, labelRect, fieldRect, pasteRect, joinRect);

                try
                {
                    Together.UIMainMenu.DoTooltipOnLastGUIRectNoLang(busy ? Loc.BusyHint : Loc.RowTooltip);
                }
                catch
                {
                }

                if (!_loggedFirstDraw || !_loggedGeometry)
                {
                    Rect labelScreen = GUIUtility.GUIToScreenRect(labelRect);
                    Rect joinScreen = GUIUtility.GUIToScreenRect(joinRect);
                    Rect blockScreen = GUIUtility.GUIToScreenRect(block);

                    if (!_loggedFirstDraw)
                    {
                        _loggedFirstDraw = true;
                        LobbyJoinPlugin.Log.LogInfo(
                            "[JoinByRoomId] row drawn: font=" + fs + " rowH=" + rowH.ToString("0.#") + " panelW=" + panelW.ToString("0.#")
                            + " scale=" + scale.ToString("0.##") + " lang=" + (Loc.IsChinese ? "zh" : "en"));
                    }

                    if (!_loggedGeometry)
                    {
                        _loggedGeometry = true;
                        LobbyJoinPlugin.Log.LogInfo(
                            "[JoinByRoomId] geometry: oneLine=" + oneLine
                            + " blockScreen=(" + blockScreen.x.ToString("0") + "," + blockScreen.y.ToString("0") + "," + blockScreen.width.ToString("0") + "," + blockScreen.height.ToString("0") + ")"
                            + " labelScreen=(" + labelScreen.x.ToString("0") + "," + labelScreen.y.ToString("0") + "," + labelScreen.width.ToString("0") + "," + labelScreen.height.ToString("0") + ")"
                            + " joinScreen=(" + joinScreen.x.ToString("0") + "," + joinScreen.y.ToString("0") + "," + joinScreen.width.ToString("0") + "," + joinScreen.height.ToString("0") + ")");
                    }
                }

                if (!busy && clicked)
                {
                    LobbyJoinService.BeginJoin(InputText);
                }
            }
            catch (Exception e)
            {
                LobbyJoinPlugin.Log.LogError("[JoinByRoomId] DrawBlock: " + e);
            }
        }

        private static bool DrawControls(
            bool busy,
            float rowH,
            string labelText,
            Rect labelRect,
            Rect fieldRect,
            Rect pasteRect,
            Rect joinRect)
        {
            bool prevEnabled = GUI.enabled;
            bool enterPressed = false;
            bool joinClicked = false;
            bool pasteClicked = false;

            try
            {
                GUI.Label(labelRect, labelText, _labelStyle);

                GUI.enabled = prevEnabled && !busy;
                GUI.SetNextControlName(InputControlName);

                string typed = GUI.TextField(fieldRect, InputText ?? string.Empty, MaxChars, _fieldStyle);
                if (!string.Equals(typed, InputText, StringComparison.Ordinal))
                {
                    SetInput(typed);
                }

                enterPressed = WasEnterPressedOnInput(busy);

                pasteClicked = GUI.Button(pasteRect, Loc.PasteButton, _buttonStyle);
                joinClicked = GUI.Button(joinRect, Loc.JoinButton, _buttonStyle);
            }
            finally
            {
                GUI.enabled = prevEnabled;
            }

            if (pasteClicked)
            {
                PasteFromClipboard();
            }

            return joinClicked || enterPressed;
        }

        private static GUIStyle _titleStyle;

        internal static void DrawInspectWindow()
        {
            try
            {
                LobbyInspection info = LobbyJoinService.Pending;
                if (info == null)
                {
                    _inspectSized = false;
                    _inspectShownFor = 0UL;
                    return;
                }

                if (_inspectShownFor != info.LobbyId)
                {
                    _inspectShownFor = info.LobbyId;
                    PasswordInput = string.Empty;
                    _inspectSized = false;
                }

                float scale = Scale;
                EnsureStyles(scale);

                float rowH = Mathf.Max(28f * scale, _cachedFontSize * 1.5f);
                float pad = 16f * scale;
                float headerH = Mathf.Max(40f * scale, _cachedFontSize * 2.1f);

                int warnCount = (info.VersionMismatch ? 1 : 0) + (info.MidjoinLocked ? 1 : 0) + (info.Full ? 1 : 0);
                float contentH = (rowH + 4f * scale) * 5f
                                 + warnCount * (rowH + 2f * scale)
                                 + (info.HasPassword ? rowH + 10f * scale : 0f);

                float w = Mathf.Clamp(660f * scale, 420f, Screen.width * 0.94f);
                float h = headerH + pad * 1.2f + contentH + pad * 0.6f + rowH + pad * 0.7f;

                if (!_inspectSized)
                {
                    _inspectRect = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
                    _inspectSized = true;
                }
                else
                {
                    _inspectRect.width = w;
                    _inspectRect.height = h;
                }

                _inspectRect = GUI.Window(InspectWindowId, _inspectRect, DrawInspectBody, GUIContent.none, GUIStyle.none);
            }
            catch (Exception e)
            {
                LobbyJoinPlugin.Log.LogError("[JoinByRoomId] InspectWindow: " + e);
            }
        }

        private static void DrawInspectBody(int id)
        {
            LobbyInspection info = LobbyJoinService.Pending;
            if (info == null)
            {
                return;
            }

            float scale = Scale;
            float rowH = Mathf.Max(28f * scale, _cachedFontSize * 1.5f);
            float pad = 16f * scale;
            float headerH = Mathf.Max(40f * scale, _cachedFontSize * 2.1f);
            float keyW = _infoStyle.CalcSize(new GUIContent(Loc.FieldPasswordInput)).x + 10f * scale;

            float w = _inspectRect.width;
            float h = _inspectRect.height;

            try
            {
                Rect panel = new Rect(0f, 0f, w, h);
                Together.UIBullshit._GUI_9SlicePanel(in panel, 1f, 0.94f, false, Together.UIBullshit.unscaled_uiBlockNano);

                Rect header = new Rect(2f * scale, 2f * scale, w - 4f * scale, headerH);
                Together.UIBullshit._GUI_9SlicePanel(in header, 1f, 0.70f, false, Together.UIBullshit.unscaled_uiBlockNano);

                GUI.Label(new Rect(header.x + 12f * scale, header.y, header.width - headerH, header.height), Loc.InspectTitle, _titleStyle);

                Rect closeRect = new Rect(
                    header.xMax - headerH + 4f * scale,
                    header.y + 5f * scale,
                    headerH - 10f * scale,
                    headerH - 10f * scale);

                if (GUI.Button(closeRect, "X", _buttonStyle))
                {
                    LobbyJoinService.CancelInspect();
                    return;
                }

                float x = pad;
                float contentW = w - pad * 2f;
                float y = header.yMax + pad * 0.5f;

                InfoRow(new Rect(x, y, contentW, rowH), Loc.FieldRoomId, info.LobbyId.ToString(), keyW, scale);
                y += rowH + 4f * scale;

                InfoRow(new Rect(x, y, contentW, rowH), Loc.FieldName, string.IsNullOrEmpty(info.Name) ? Loc.UnnamedRoom : info.Name, keyW, scale);
                y += rowH + 4f * scale;

                InfoRow(new Rect(x, y, contentW, rowH), Loc.FieldPlayers, info.Members + " / " + info.Limit, keyW, scale);
                y += rowH + 4f * scale;

                string verText = (string.IsNullOrEmpty(info.Version) ? "?" : info.Version) + (info.VersionMismatch ? "   ✘" : "   ✔");
                InfoRow(new Rect(x, y, contentW, rowH), Loc.FieldVersion, verText, keyW, scale);
                y += rowH + 4f * scale;

                InfoRow(new Rect(x, y, contentW, rowH), Loc.FieldPassword, info.HasPassword ? Loc.Yes : Loc.No, keyW, scale);
                y += rowH + 4f * scale;

                if (info.VersionMismatch)
                {
                    WarnRow(new Rect(x, y, contentW, rowH), string.Format(Loc.WarnVersionMismatch, info.MyVersion), scale);
                    y += rowH + 2f * scale;
                }

                if (info.MidjoinLocked)
                {
                    WarnRow(new Rect(x, y, contentW, rowH), Loc.WarnMidjoinLocked, scale);
                    y += rowH + 2f * scale;
                }

                if (info.Full)
                {
                    WarnRow(new Rect(x, y, contentW, rowH), Loc.WarnFull, scale);
                    y += rowH + 2f * scale;
                }

                if (info.HasPassword)
                {
                    y += 4f * scale;
                    GUI.Label(new Rect(x, y, keyW, rowH), Loc.FieldPasswordInput, _labelStyle);
                    Rect fieldRect = new Rect(x + keyW + 8f * scale, y, contentW - keyW - 8f * scale, rowH);
                    PasswordInput = GUI.PasswordField(fieldRect, PasswordInput ?? string.Empty, '*', 64, _fieldStyle);
                    y += rowH + 10f * scale;
                }

                float joinW = Mathf.Max(170f * scale, _buttonStyle.CalcSize(new GUIContent(Loc.JoinButton)).x + 44f * scale);
                float cancelW = Mathf.Max(130f * scale, _buttonStyle.CalcSize(new GUIContent(Loc.CancelButton)).x + 34f * scale);
                float btnY = h - pad * 0.7f - rowH;
                Rect joinRect = new Rect(x, btnY, joinW, rowH);
                Rect cancelRect = new Rect(joinRect.xMax + 10f * scale, btnY, cancelW, rowH);

                bool prevEnabled = GUI.enabled;
                GUI.enabled = prevEnabled && !(info.HasPassword && string.IsNullOrEmpty((PasswordInput ?? string.Empty).Trim()));
                if (GUI.Button(joinRect, Loc.JoinButton, _buttonStyle))
                {
                    LobbyJoinService.ConfirmJoin(PasswordInput);
                    return;
                }

                GUI.enabled = prevEnabled;
                if (GUI.Button(cancelRect, Loc.CancelButton, _buttonStyle))
                {
                    LobbyJoinService.CancelInspect();
                    return;
                }

                GUI.Label(
                    new Rect(cancelRect.xMax + 12f * scale, btnY, Mathf.Max(0f, x + contentW - cancelRect.xMax - 12f * scale), rowH),
                    "by " + LobbyJoinPlugin.PluginAuthor,
                    _creditStyle);

                GUI.DragWindow(header);

                if (!_loggedInspect)
                {
                    _loggedInspect = true;
                    LobbyJoinPlugin.Log.LogInfo(
                        "[JoinByRoomId] inspect panel drawn: window=(" + _inspectRect.x.ToString("0") + "," + _inspectRect.y.ToString("0") + ","
                        + w.ToString("0") + "," + h.ToString("0") + ")"
                        + " header=" + headerH.ToString("0") + " rowH=" + rowH.ToString("0")
                        + " joinBtn=(" + joinRect.x.ToString("0") + "," + joinRect.y.ToString("0") + ","
                        + joinRect.width.ToString("0") + "," + joinRect.height.ToString("0") + ")"
                        + " cancelBtn=(" + cancelRect.x.ToString("0") + "," + cancelRect.y.ToString("0") + ","
                        + cancelRect.width.ToString("0") + "," + cancelRect.height.ToString("0") + ")"
                        + " password=" + info.HasPassword
                        + " warnings=" + ((info.VersionMismatch ? 1 : 0) + (info.MidjoinLocked ? 1 : 0) + (info.Full ? 1 : 0)));
                }
            }
            catch (Exception e)
            {
                LobbyJoinPlugin.Log.LogError("[JoinByRoomId] InspectBody: " + e);
            }
        }

        private static void InfoRow(Rect row, string key, string value, float keyW, float scale)
        {
            GUI.Label(new Rect(row.x, row.y, keyW, row.height), key, _labelStyle);
            GUI.Label(new Rect(row.x + keyW + 8f * scale, row.y, row.width - keyW - 8f * scale, row.height), value, _infoStyle);
        }

        private static void WarnRow(Rect row, string text, float scale)
        {
            Color prev = GUI.color;
            try
            {
                GUI.color = new Color(1f, 0.62f, 0.25f, 1f);
                GUI.Label(row, "! " + text, _infoStyle);
            }
            finally
            {
                GUI.color = prev;
            }
        }

        internal static void DrawStandaloneWindow()
        {
            try
            {
                if (!RowVisible)
                {
                    return;
                }

                EnsureStyles(Scale);
                _windowRect = GUILayout.Window(WindowId, _windowRect, DrawWindowContents, Loc.WindowTitle, Array.Empty<GUILayoutOption>());
            }
            catch (Exception e)
            {
                LobbyJoinPlugin.Log.LogError("[JoinByRoomId] Window: " + e);
            }
        }

        private static void DrawWindowContents(int id)
        {
            float scale = Scale;
            EnsureStyles(scale);

            float fs = _cachedFontSize;
            float rowH = Mathf.Max(30f * scale, fs * 1.6f);
            bool busy = LobbyJoinService.IsBusy || LobbyJoinService.Pending != null;

            GUIStyle hintStyle = new GUIStyle(_infoStyle)
            {
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
                fontSize = Mathf.Max(12, Mathf.RoundToInt(fs * 0.85f)),
            };

            try
            {
                GUILayout.Label(Loc.WindowHint, hintStyle, new GUILayoutOption[] { GUILayout.ExpandWidth(true) });
                GUILayout.Space(6f * scale);

                GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
                GUILayout.Space(6f * scale);
                Rect row = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(rowH) });
                GUILayout.EndHorizontal();

                GUILayout.Space(8f * scale);

                bool clicked = DrawWindowControls(row, busy, rowH, scale);

                GUILayout.Space(8f * scale);
                GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
                GUILayout.Label("by " + LobbyJoinPlugin.PluginAuthor, _creditStyle, new GUILayoutOption[] { GUILayout.ExpandWidth(true) });
                GUILayout.FlexibleSpace();
                float closeW = _buttonStyle.CalcSize(new GUIContent(Loc.CloseButton)).x + 24f * scale;
                if (GUILayout.Button(Loc.CloseButton, _buttonStyle, new GUILayoutOption[] { GUILayout.Width(closeW), GUILayout.Height(rowH) }))
                {
                    LobbyJoinPlugin.ShowStandaloneWindow = false;
                }

                GUILayout.EndHorizontal();

                if (!busy && clicked)
                {
                    LobbyJoinService.BeginJoin(InputText);
                }
            }
            catch (Exception e)
            {
                LobbyJoinPlugin.Log.LogError("[JoinByRoomId] WindowContents: " + e);
            }

            GUI.DragWindow(new Rect(0f, 0f, _windowRect.width, 26f * scale));
        }

        private static bool DrawWindowControls(Rect row, bool busy, float rowH, float scale)
        {
            bool prevEnabled = GUI.enabled;
            bool enter;
            bool joinClicked;
            bool pasteClicked;

            try
            {
                GUI.enabled = prevEnabled && !busy;

                float pasteW = _buttonStyle.CalcSize(new GUIContent(Loc.PasteButton)).x + 20f * scale;
                float joinW = _buttonStyle.CalcSize(new GUIContent(Loc.JoinButton)).x + 24f * scale;
                float gap = 8f * scale;

                Rect joinRect = new Rect(row.xMax - joinW, row.y, joinW, rowH);
                Rect pasteRect = new Rect(joinRect.x - gap - pasteW, row.y, pasteW, rowH);
                Rect fieldRect = new Rect(row.x, row.y, Mathf.Max(120f * scale, pasteRect.x - gap - row.x), rowH);

                GUI.SetNextControlName(InputControlName);
                string typed = GUI.TextField(fieldRect, InputText ?? string.Empty, MaxChars, _fieldStyle);
                if (!string.Equals(typed, InputText, StringComparison.Ordinal))
                {
                    SetInput(typed);
                }

                enter = WasEnterPressedOnInput(busy);
                pasteClicked = GUI.Button(pasteRect, Loc.PasteButton, _buttonStyle);
                joinClicked = GUI.Button(joinRect, Loc.JoinButton, _buttonStyle);
            }
            finally
            {
                GUI.enabled = prevEnabled;
            }

            if (pasteClicked)
            {
                PasteFromClipboard();
            }

            return joinClicked || enter;
        }

        private static int MaxChars
        {
            get
            {
                try
                {
                    return Mathf.Clamp(LobbyJoinPlugin.MaxIdLength.Value, 6, 256);
                }
                catch
                {
                    return 96;
                }
            }
        }

        private static bool WasEnterPressedOnInput(bool busy)
        {
            if (busy)
            {
                return false;
            }

            Event e = Event.current;
            if (e == null)
            {
                return false;
            }

            bool focused;
            try
            {
                focused = GUI.GetNameOfFocusedControl() == InputControlName;
            }
            catch
            {
                focused = false;
            }

            if (focused && e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter))
            {
                e.Use();
                return true;
            }

            return false;
        }

        private static void PasteFromClipboard()
        {
            try
            {
                string clip = GUIUtility.systemCopyBuffer;
                if (LobbyIdParser.TryParse(clip, out ulong lobbyId))
                {
                    SetInput(lobbyId.ToString());
                    LobbyJoinService.PlaySound(global::PlayerCamera.UISoundType.MiniClick);
                }
                else
                {
                    LobbyJoinService.PlaySound(global::PlayerCamera.UISoundType.Deny);
                    Together.Multiplayer.DoMultiplayerStatusMessageError("[JoinByRoomId] " + Loc.PastedInvalid);
                }
            }
            catch (Exception e)
            {
                LobbyJoinPlugin.Log.LogError("[JoinByRoomId] Paste: " + e);
            }
        }
    }
}
