/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * Copyright (c) 2026 Bilibili UID:1951065665
 */
using System;
using System.IO;
using System.Linq;
using System.Reflection;

internal static class Program
{
    private static string _gameDir;
    private static string _pluginDll;
    private static int _failures;

    private static string[] ProbeDirs() => new[]
    {
        Path.Combine(_gameDir, @"CasualtiesUnknown_Data\Managed"),
        Path.Combine(_gameDir, @"BepInEx\core"),
        Path.Combine(_gameDir, @"BepInEx\plugins\Together"),
        Path.GetDirectoryName(_pluginDll) ?? ".",
    };

    private static Assembly Resolve(object sender, ResolveEventArgs e)
    {
        string simple = new AssemblyName(e.Name).Name + ".dll";
        foreach (string dir in ProbeDirs())
        {
            string candidate = Path.Combine(dir, simple);
            if (File.Exists(candidate))
            {
                return Assembly.LoadFrom(candidate);
            }
        }

        return null;
    }

    private static int Main(string[] args)
    {
        _gameDir = args.Length > 0
            ? args[0]
            : Environment.GetEnvironmentVariable("GAMEDIR")
              ?? @"G:\SteamLibrary\steamapps\common\Casualties Unknown";

        _pluginDll = args.Length > 1
            ? args[1]
            : Path.Combine(AppContext.BaseDirectory, "TogetherLobbyJoin.dll");

        if (!File.Exists(_pluginDll))
        {
            Console.WriteLine("PLUGIN DLL NOT FOUND: " + _pluginDll);
            return 2;
        }

        AppDomain.CurrentDomain.AssemblyResolve += Resolve;

        Console.WriteLine("game   : " + _gameDir);
        Console.WriteLine("plugin : " + _pluginDll);
        Console.WriteLine(new string('-', 78));

        Assembly mod = Assembly.LoadFrom(Path.Combine(_gameDir, @"BepInEx\plugins\Together\CasualtiesMP.dll"));
        Assembly plugin = Assembly.LoadFrom(_pluginDll);

        Type pluginTypeRoot = plugin.GetType("TogetherLobbyJoin.LobbyJoinPlugin", throwOnError: false);
        Check("TogetherLobbyJoin.LobbyJoinPlugin type", pluginTypeRoot != null);

        var bep = pluginTypeRoot?.GetCustomAttributes().FirstOrDefault(a => a.GetType().Name == "BepInPlugin");
        Check("BepInPlugin attribute", bep != null);
        if (bep != null)
        {
            Type t = bep.GetType();
            Console.WriteLine("      guid=" + t.GetProperty("GUID")?.GetValue(bep)
                              + " name=" + t.GetProperty("Name")?.GetValue(bep)
                              + " version=" + t.GetProperty("Version")?.GetValue(bep));
        }

        var dep = pluginTypeRoot?.GetCustomAttributes().FirstOrDefault(a => a.GetType().Name == "BepInDependency");
        Check("BepInDependency on CasualtiesMP", dep != null);
        if (dep != null)
        {
            Type t = dep.GetType();
            Console.WriteLine("      dependency=" + t.GetProperty("DependencyGUID")?.GetValue(dep));
        }

        Type ui = mod.GetType("Together.UIMainMenu", throwOnError: false);
        Check("Together.UIMainMenu exists", ui != null);
        if (ui == null)
        {
            return Finish();
        }

        MethodInfo steamRow = ui.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .FirstOrDefault(m => m.Name == "_GUI___DirectConnect_DoSteamInfoo" && m.GetParameters().Length == 4);

        Check("target Together.UIMainMenu._GUI___DirectConnect_DoSteamInfoo(Rect,float,float,float)", steamRow != null);
        if (steamRow != null)
        {
            Console.WriteLine("      isStatic=" + steamRow.IsStatic + " visibility=" + (steamRow.IsPrivate ? "private" : "non-private"));
            Console.WriteLine("      params  : " + Describe(steamRow));

            Type pluginType = plugin.GetType("TogetherLobbyJoin.LobbyJoinPlugin");
            MethodInfo postfix = pluginType?.GetMethod("PostfixSteamRow", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            Check("plugin postfix TogetherLobbyJoin.LobbyJoinPlugin.PostfixSteamRow", postfix != null);
            if (postfix != null)
            {
                Console.WriteLine("      params  : " + Describe(postfix));
                Check("postfix parameter names match target (Harmony injects by name)", NamesMatch(steamRow, postfix));
            }
        }

        MethodInfo panel = ui.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .FirstOrDefault(m => m.Name == "_GUI__DirectConnect" && m.GetParameters().Length == 1);

        Check("fallback target Together.UIMainMenu._GUI__DirectConnect(Rect)", panel != null);
        if (panel != null)
        {
            Type pluginType = plugin.GetType("TogetherLobbyJoin.LobbyJoinPlugin");
            MethodInfo postfix = pluginType?.GetMethod("PostfixDirectConnectPanel", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            Check("fallback postfix exists", postfix != null);
            if (postfix != null)
            {
                Check("fallback postfix parameter names match", NamesMatch(panel, postfix));
            }
        }

        RequireMember(mod, "Together.TransportSteamworks", "OnWantToJoinLobby", staticMethod: true, typeof(ulong));
        RequireMember(mod, "Together.Net", "IsRunning", staticMethod: false, null);
        RequireMember(mod, "Together.KSteam", "Loaded", staticMethod: false, null);
        RequireMember(mod, "Together.KSteam", "IS_IN_LOBBY", staticMethod: false, null);
        RequireMember(mod, "Together.Multiplayer", "DoMultiplayerStatusMessageLog", staticMethod: true, typeof(string));
        RequireMember(mod, "Together.Multiplayer", "DoMultiplayerStatusMessageError", staticMethod: true, typeof(string));
        RequireMember(mod, "Together.UIMainMenu", "GetMenuUIScale", staticMethod: true, null);
        RequireMember(mod, "Together.UIMainMenu", "IsOpen", staticMethod: true, null);
        RequireMember(mod, "Together.UIMainMenu", "DoTooltipOnLastGUIRectNoLang", staticMethod: true, typeof(string));
        RequireAnyMethod(mod, "CasualtiesTogetherUtils.Util", "PlayUISound");

        Assembly game = Assembly.LoadFrom(Path.Combine(_gameDir, @"CasualtiesUnknown_Data\Managed\Assembly-CSharp.dll"));
        Type uiSound = game.GetType("PlayerCamera+UISoundType", throwOnError: false);
        Check("PlayerCamera.UISoundType exists", uiSound != null);
        if (uiSound != null)
        {
            Check("  UISoundType.MiniClick", Enum.IsDefined(uiSound, "MiniClick"));
            Check("  UISoundType.Deny", Enum.IsDefined(uiSound, "Deny"));
        }

        Type locale = game.GetType("Locale", throwOnError: false);
        bool localeOk = locale != null
                        && (locale.GetProperty("currentLangName", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) != null
                            || locale.GetField("currentLangName", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) != null);
        Check("Locale.currentLangName exists", localeOk);

        Assembly steam = Assembly.LoadFrom(Path.Combine(_gameDir, @"CasualtiesUnknown_Data\Managed\com.rlabrecque.steamworks.net.dll"));
        Type csteamId = steam.GetType("Steamworks.CSteamID", throwOnError: false);
        Check("Steamworks.CSteamID exists", csteamId != null);
        if (csteamId != null)
        {
            Check("  CSteamID.IsLobby()", csteamId.GetMethod("IsLobby", Type.EmptyTypes) != null);
            Check("  CSteamID.IsValid()", csteamId.GetMethod("IsValid", Type.EmptyTypes) != null);
            Check("  CSteamID(ulong) ctor", csteamId.GetConstructor(new[] { typeof(ulong) }) != null);
        }

        return Finish();
    }

    private static void RequireMember(Assembly asm, string typeName, string memberName, bool staticMethod, Type paramType)
    {
        Type t = asm.GetType(typeName, throwOnError: false);
        if (t == null)
        {
            Check(typeName + " exists", false);
            return;
        }

        if (paramType == null)
        {
            bool ok = t.GetProperty(memberName, BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) != null
                      || t.GetField(memberName, BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) != null
                      || t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                          .Any(m => m.Name == memberName && m.GetParameters().Length == 0);
            Check(typeName + "." + memberName, ok);
            return;
        }

        BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | (staticMethod ? BindingFlags.Static : BindingFlags.Instance);
        Type byRef = paramType.IsValueType ? null : paramType.MakeByRefType();
        bool found = t.GetMethods(flags).Any(m =>
        {
            ParameterInfo[] ps = m.GetParameters();
            if (m.Name != memberName || ps.Length < 1)
            {
                return false;
            }

            Type first = ps[0].ParameterType;
            return first == paramType || (byRef != null && first == byRef);
        });
        Check(typeName + "." + memberName + "(" + paramType.Name + ")", found);
    }

    private static void RequireAnyMethod(Assembly asm, string typeName, string memberName)
    {
        Type t = asm.GetType(typeName, throwOnError: false);
        if (t == null)
        {
            Check(typeName + " exists", false);
            return;
        }

        bool ok = t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Any(m => m.Name == memberName);
        Check(typeName + "." + memberName + "(...)", ok);
    }

    private static string Describe(MethodInfo m) =>
        string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name));

    private static bool NamesMatch(MethodInfo target, MethodInfo postfix)
    {
        ParameterInfo[] a = target.GetParameters();
        ParameterInfo[] b = postfix.GetParameters();
        if (a.Length != b.Length)
        {
            return false;
        }

        for (int i = 0; i < a.Length; i++)
        {
            if (a[i].ParameterType != b[i].ParameterType || a[i].Name != b[i].Name)
            {
                return false;
            }
        }

        return true;
    }

    private static void Check(string what, bool ok)
    {
        if (!ok)
        {
            _failures++;
        }

        Console.WriteLine((ok ? "[ OK ] " : "[FAIL] ") + what);
    }

    private static int Finish()
    {
        Console.WriteLine(new string('-', 78));
        Console.WriteLine(_failures == 0 ? "ALL CHECKS PASSED" : _failures + " CHECK(S) FAILED");
        return _failures == 0 ? 0 : 1;
    }
}
