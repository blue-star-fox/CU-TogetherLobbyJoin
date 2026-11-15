/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * Copyright (c) 2026 Bilibili UID:1951065665
 */
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TogetherLobbyJoin
{
    internal static class LobbyIdParser
    {
        private static readonly Regex Digits = new Regex(@"\d{10,20}", RegexOptions.Compiled);

        internal static bool TryParse(string raw, out ulong lobbyId)
        {
            lobbyId = 0UL;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            string text = raw.Trim();

            if (ulong.TryParse(text, out ulong direct))
            {
                if (IsLobbyId(direct))
                {
                    lobbyId = direct;
                    return true;
                }
            }

            var candidates = new List<ulong>();
            foreach (Match m in Digits.Matches(text))
            {
                if (ulong.TryParse(m.Value, out ulong value) && value != 0UL)
                {
                    candidates.Add(value);
                }
            }

            foreach (ulong candidate in candidates)
            {
                if (IsLobbyId(candidate))
                {
                    lobbyId = candidate;
                    return true;
                }
            }

            if (candidates.Count > 0)
            {
                candidates.Sort((a, b) => b.ToString().Length.CompareTo(a.ToString().Length));
                lobbyId = candidates[0];
                return true;
            }

            return false;
        }

        internal static bool IsLobbyId(ulong id)
        {
            if (id == 0UL)
            {
                return false;
            }

            try
            {
                var cid = new Steamworks.CSteamID(id);
                return cid.IsValid() && cid.IsLobby();
            }
            catch
            {
                return false;
            }
        }

        internal static bool LooksLikeId(string raw)
        {
            return TryParse(raw, out _);
        }
    }
}
