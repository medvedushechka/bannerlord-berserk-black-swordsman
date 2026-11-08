using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace BannerlordUpgradeGuard
{
    // Diagnostic compatibility module for the supplied Bannerlord CampaignSystem DLL.
    // No persistent game data is modified by this code.
    public sealed class SubModule : MBSubModuleBase
    {
        private const string HarmonyId = "local.bannerlord.upgradeguard.v6";

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            Log.Start();
            Log.Write("MODULE_LOAD UpgradeGuard v6; Harmony patch inventory enabled");

            try
            {
                var harmony = new Harmony(HarmonyId);
                InstallUpgradeGuard(harmony);
                InstallUpgradeUiCompatibility(harmony);
                InstallPartyScreenTrace(harmony);
                InstallPlayerRosterTrace(harmony);
                Log.Write("INSTALLATION_COMPLETE; patch inventory will be collected at first party-screen open and on failure");
            }
            catch (Exception ex)
            {
                Log.Write("ERROR unexpected module initialization failure: " + ex);
            }
        }

        private static Type FindType(string fullName, string assemblyName)
        {
            try { return Type.GetType(fullName + ", " + assemblyName, false); }
            catch (Exception ex) { Log.Write("WARN cannot load type " + fullName + ": " + ex.Message); return null; }
        }

        private static void InstallUpgradeGuard(Harmony harmony)
        {
            try
            {
                Type type = FindType("TaleWorlds.CampaignSystem.CharacterObject", "TaleWorlds.CampaignSystem");
                if (type == null) { Log.Write("ERROR CharacterObject type not found; guard INACTIVE"); return; }
                MethodInfo original = null;
                foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    ParameterInfo[] p = method.GetParameters();
                    if (method.Name == "GetUpgradeGoldCost" && p.Length == 2 &&
                        p[1].ParameterType == typeof(int) && method.ReturnType == typeof(int))
                    { original = method; break; }
                }
                if (original == null) { Log.Write("ERROR GetUpgradeGoldCost signature not found; guard INACTIVE"); return; }

                UpgradePatch.Configure(type);
                var prefix = typeof(UpgradePatch).GetMethod("Prefix", BindingFlags.Public | BindingFlags.Static);
                var finalizer = typeof(UpgradePatch).GetMethod("Finalizer", BindingFlags.Public | BindingFlags.Static);
                harmony.Patch(original, new HarmonyMethod(prefix), null, null, new HarmonyMethod(finalizer));
                Log.Write("ACTIVE UPGRADE_GUARD method=" + original + "; args=[" +
                    string.Join(",", Array.ConvertAll(original.GetParameters(), x => x.Name)) + "]");
            }
            catch (Exception ex) { Log.Write("ERROR UPGRADE_GUARD inactive: " + ex); }
        }

        // Surgical compatibility patch for this exact game build. The game's UI
        // repeatedly accesses a tiny UpgradeTargets getter, while BetterBandits
        // changes its result via a Harmony postfix. Calls from the UI can become
        // inconsistent when the getter is inlined or when the VM list is stale.
        private static void InstallUpgradeUiCompatibility(Harmony harmony)
        {
            try
            {
                Type vm = FindType("TaleWorlds.CampaignSystem.ViewModelCollection.Party.PartyCharacterVM",
                    "TaleWorlds.CampaignSystem.ViewModelCollection");
                if (vm == null) { Log.Write("ERROR UI_COMPAT type not found"); return; }
                MethodInfo method = vm.GetMethod("InitializeUpgrades",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                    null, Type.EmptyTypes, null);
                if (method == null) { Log.Write("ERROR UI_COMPAT InitializeUpgrades not found"); return; }
                UpgradeUiPatch.Configure(vm);
                HarmonyMethod prefix = new HarmonyMethod(typeof(UpgradeUiPatch).GetMethod("Prefix"));
                prefix.priority = Priority.Last;
                prefix.after = new string[] { "GwynMyLittleWarbandEdition3" };
                HarmonyMethod transpiler = new HarmonyMethod(typeof(UpgradeUiPatch).GetMethod("Transpiler"));
                harmony.Patch(method, prefix, null, transpiler, null);
                Log.Write("ACTIVE UI_COMPAT method=" + method + "; normalized UI count; redirected getter calls");
            }
            catch (Exception ex) { Log.Write("ERROR UI_COMPAT inactive: " + ex); }
        }

        private static void InstallPartyScreenTrace(Harmony harmony)
        {
            try
            {
                Type type = FindType("TaleWorlds.CampaignSystem.ViewModelCollection.Party.PartyVM",
                    "TaleWorlds.CampaignSystem.ViewModelCollection");
                if (type == null) { Log.Write("WARN PartyVM type not found; screen tracing disabled"); return; }
                MethodInfo original = null;
                foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                    if (method.Name == "InitializeTroopLists" && method.GetParameters().Length == 0)
                    { original = method; break; }
                if (original == null) { Log.Write("WARN InitializeTroopLists not found; screen tracing disabled"); return; }
                harmony.Patch(original,
                    new HarmonyMethod(typeof(PartyScreenPatch).GetMethod("Prefix")),
                    new HarmonyMethod(typeof(PartyScreenPatch).GetMethod("Postfix")),
                    null,
                    new HarmonyMethod(typeof(PartyScreenPatch).GetMethod("Finalizer")));
                Log.Write("ACTIVE PARTY_SCREEN_TRACE method=" + original);
            }
            catch (Exception ex) { Log.Write("WARN PARTY_SCREEN_TRACE unavailable: " + ex); }
        }

        private static void InstallPlayerRosterTrace(Harmony harmony)
        {
            try
            {
                if (!RosterPatch.Configure())
                { Log.Write("WARN PLAYER_ROSTER_TRACE unavailable: main-party properties missing"); return; }
                Type type = FindType("TaleWorlds.CampaignSystem.Roster.TroopRoster", "TaleWorlds.CampaignSystem");
                if (type == null) { Log.Write("WARN TroopRoster not found; roster tracing disabled"); return; }
                int patched = 0;
                foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    ParameterInfo[] p = method.GetParameters();
                    if (method.Name != "AddToCounts" || p.Length < 2 ||
                        p[1].ParameterType != typeof(int) ||
                        p[0].ParameterType.Name != "CharacterObject") continue;
                    try
                    {
                        harmony.Patch(method, null, new HarmonyMethod(typeof(RosterPatch).GetMethod("Postfix")));
                        patched++;
                    }
                    catch (Exception ex) { Log.Write("WARN roster overload not patched: " + method + "; " + ex.Message); }
                }
                if (patched == 0) Log.Write("WARN no compatible AddToCounts overload; roster tracing disabled");
                else Log.Write("ACTIVE PLAYER_ROSTER_TRACE overloads=" + patched);
            }
            catch (Exception ex) { Log.Write("WARN PLAYER_ROSTER_TRACE unavailable: " + ex); }
        }
    }

    // Only changes the in-memory UI representation. Does not edit troop XML,
    // troop definitions, upgrade prices, or saved campaign data.
    public static class UpgradeUiPatch
    {
        private static PropertyInfo vmCharacter;
        private static PropertyInfo vmUpgrades;
        private static PropertyInfo targets;
        private static MethodInfo getter;
        private static int transpiledCalls;
        private static int trimmed;
        private static int readWarnings;

        public static void Configure(Type vm)
        {
            vmCharacter = AccessTools.Property(vm, "Character");
            vmUpgrades = AccessTools.Property(vm, "Upgrades");
            Type troop = typeof(TaleWorlds.CampaignSystem.CharacterObject);
            targets = AccessTools.Property(troop, "UpgradeTargets");
            getter = targets == null ? null : targets.GetGetMethod(true);
            if (vmCharacter == null || vmUpgrades == null || getter == null)
                throw new MissingMemberException("PartyCharacterVM.Character/Upgrades or CharacterObject.UpgradeTargets not found");
        }

        // Reflection dispatch avoids JIT inlining the seven-byte getter body.
        // This ensures that BetterBandits' Harmony postfix runs on every UI read.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TaleWorlds.CampaignSystem.CharacterObject[] ReadTargets(
            TaleWorlds.CampaignSystem.CharacterObject character)
        {
            if (character == null) return new TaleWorlds.CampaignSystem.CharacterObject[0];
            try
            {
                return (TaleWorlds.CampaignSystem.CharacterObject[])targets.GetValue(character, null)
                    ?? new TaleWorlds.CampaignSystem.CharacterObject[0];
            }
            catch (Exception ex)
            {
                if (System.Threading.Interlocked.Increment(ref readWarnings) <= 8)
                    Log.Write("WARN UI_COMPAT getter reflection failed: " + ex);
                return character.UpgradeTargets ?? new TaleWorlds.CampaignSystem.CharacterObject[0];
            }
        }

        // Replace ONLY calls to CharacterObject.get_UpgradeTargets inside the
        // game's PartyCharacterVM.InitializeUpgrades, preserving labels/blocks.
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            int count = 0;
            MethodInfo replacement = typeof(UpgradeUiPatch).GetMethod("ReadTargets");
            foreach (CodeInstruction instruction in instructions)
            {
                MethodInfo called = instruction.operand as MethodInfo;
                if ((instruction.opcode == OpCodes.Callvirt || instruction.opcode == OpCodes.Call) &&
                    called != null && called.Name == "get_UpgradeTargets" &&
                    called.DeclaringType == typeof(TaleWorlds.CampaignSystem.CharacterObject))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = replacement;
                    count++;
                }
                yield return instruction;
            }
            System.Threading.Interlocked.Exchange(ref transpiledCalls, count);
            Log.Write("UI_COMPAT_TRANSPILE getterCallsReplaced=" + count + "; expected=2");
            if (count != 2) throw new InvalidOperationException("Unexpected InitializeUpgrades IL: getter call count " + count);
        }

        // Runs AFTER My Little Warband's prefix. That mod can extend Upgrades,
        // but does not trim it when BetterBandits returns fewer targets.
        public static void Prefix(object __instance)
        {
            try
            {
                object character = vmCharacter.GetValue(__instance, null);
                var targetArray = ReadTargets(character as TaleWorlds.CampaignSystem.CharacterObject);
                IList upgrades = vmUpgrades.GetValue(__instance, null) as IList;
                if (upgrades == null) return;
                int oldCount = upgrades.Count;
                if (oldCount > targetArray.Length)
                {
                    while (upgrades.Count > targetArray.Length) upgrades.RemoveAt(upgrades.Count - 1);
                    if (System.Threading.Interlocked.Increment(ref trimmed) <= 50)
                        Log.Write("UI_COMPAT_TRIM troopId=" + UpgradePatch.TroopId(character) +
                            "; oldCount=" + oldCount + "; targetCount=" + targetArray.Length);
                }
                else if (oldCount < targetArray.Length)
                {
                    Log.Write("WARN UI_COMPAT_SHORT_LIST troopId=" + UpgradePatch.TroopId(character) +
                        "; uiCount=" + oldCount + "; targetCount=" + targetArray.Length);
                }
            }
            catch (Exception ex) { Log.Write("ERROR UI_COMPAT prefix: " + ex); }
        }
    }

    public static class UpgradePatch
    {
        private static PropertyInfo targetsProperty;
        private static FieldInfo targetsField;
        private static PropertyInfo idProperty;
        private static PropertyInfo nameProperty;
        private static readonly HashSet<string> reported = new HashSet<string>();
        private static readonly object sync = new object();
        private const int InvalidCost = 1000000;

        public static void Configure(Type characterType)
        {
            targetsProperty = AccessTools.Property(characterType, "UpgradeTargets");
            targetsField = AccessTools.Field(characterType, "UpgradeTargets");
            idProperty = AccessTools.Property(characterType, "StringId");
            nameProperty = AccessTools.Property(characterType, "Name");
            Log.Write("UPGRADE_DATA targetsProperty=" + (targetsProperty != null) +
                "; targetsField=" + (targetsField != null) + "; idProperty=" + (idProperty != null));
            if (targetsProperty == null && targetsField == null)
                Log.Write("WARN UpgradeTargets inaccessible; only finalizer can diagnose an exception");
        }

        private static string GetProperty(PropertyInfo prop, object instance)
        {
            if (prop == null || instance == null) return "<unknown>";
            try { object value = prop.GetValue(instance, null); return value == null ? "<null>" : value.ToString(); }
            catch { return "<unavailable>"; }
        }

        private static bool TryTargets(object instance, out Array targets)
        {
            targets = null;
            if (instance == null || (targetsProperty == null && targetsField == null)) return false;
            try
            {
                object raw = targetsProperty != null ? targetsProperty.GetValue(instance, null) : targetsField.GetValue(instance);
                targets = raw as Array;
                return true;
            }
            catch (Exception ex)
            {
                Log.Write("WARN reading UpgradeTargets: " + ex.GetType().Name + " " + ex.Message);
                return false;
            }
        }

        private static void DescribeProblem(object troop, int index, string reason)
        {
            string id = GetProperty(idProperty, troop);
            string name = GetProperty(nameProperty, troop);
            Array targets;
            bool read = TryTargets(troop, out targets);
            int length = !read || targets == null ? -1 : targets.Length;
            string key = id + ":" + index + ":" + length + ":" + reason;
            lock (sync) { if (!reported.Add(key)) return; }
            string details = "";
            if (targets != null)
            {
                int count = Math.Min(targets.Length, 12);
                for (int i = 0; i < count; i++)
                {
                    if (i != 0) details += ",";
                    try { details += i + "=" + GetProperty(idProperty, targets.GetValue(i)); }
                    catch { details += i + "=<unavailable>"; }
                }
            }
            Log.Write("INVALID_UPGRADE reason=" + reason + "; troopId=" + id + "; name=" + name +
                "; requestedIndex=" + index + "; targetsLength=" + length + "; targets=[" + details + "]");
            Log.Write("INVALID_UPGRADE_CALL_STACK " + new StackTrace(1, false));
            PatchDiagnostics.Dump("INVALID_UPGRADE");
        }

        // __1 binds by argument POSITION, avoiding the index/upgradeTarget name mismatch in v2.
        public static bool Prefix(object __instance, int __1, ref int __result)
        {
            try
            {
                Array targets;
                if (!TryTargets(__instance, out targets)) return true;
                if (targets != null && __1 >= 0 && __1 < targets.Length) return true;
                DescribeProblem(__instance, __1, "index_out_of_bounds");
                __result = InvalidCost;
                Log.Write("GUARD_APPLIED invalid upgrade index; substituted cost=" + InvalidCost);
                return false;
            }
            catch (Exception ex)
            {
                Log.Write("ERROR UPGRADE_GUARD prefix failed: " + ex);
                return true;
            }
        }

        // Also handles an out-of-range access inside a DIFFERENT array in the original method.
        public static Exception Finalizer(object __instance, int __1, ref int __result, Exception __exception)
        {
            if (__exception == null) return null;
            try
            {
                DescribeProblem(__instance, __1, "exception_" + __exception.GetType().Name);
                Log.Write("UPGRADE_METHOD_EXCEPTION " + __exception);
                if (__exception is IndexOutOfRangeException)
                {
                    __result = InvalidCost;
                    Log.Write("GUARD_APPLIED caught IndexOutOfRangeException; substituted cost=" + InvalidCost);
                    return null;
                }
            }
            catch (Exception ex) { Log.Write("ERROR finalizer diagnostics: " + ex.Message); }
            return __exception; // Never hide unrelated exceptions.
        }

        public static string TroopId(object troop) { return GetProperty(idProperty, troop); }
    }

    public static class PartyScreenPatch
    {
        public static void Prefix()
        {
            PatchDiagnostics.DumpOnce();
            Log.Write("PARTY_SCREEN troop-list initialization BEGIN");
        }
        public static void Postfix() { Log.Write("PARTY_SCREEN troop-list initialization END"); }
        public static Exception Finalizer(Exception __exception)
        {
            if (__exception != null)
            {
                Log.Write("PARTY_SCREEN_EXCEPTION " + __exception);
                PatchDiagnostics.Dump("PARTY_SCREEN_EXCEPTION");
            }
            return __exception; // Diagnostic only; never swallow screen exceptions.
        }
    }

    // Read-only inspection of Harmony's current patch registry. Does not patch
    // PartyCharacterVM.InitializeUpgrades or change troop upgrade data.
    public static class PatchDiagnostics
    {
        private static int firstScreenDump;
        private static readonly BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Instance | BindingFlags.Static;

        public static void DumpOnce()
        {
            if (System.Threading.Interlocked.Exchange(ref firstScreenDump, 1) == 0)
                Dump("FIRST_PARTY_SCREEN");
        }

        private static object Member(object instance, string name)
        {
            if (instance == null) return null;
            Type t = instance.GetType();
            PropertyInfo prop = t.GetProperty(name, Flags);
            if (prop != null) return prop.GetValue(instance, null);
            FieldInfo field = t.GetField(name, Flags);
            return field == null ? null : field.GetValue(instance);
        }

        private static string Value(object o)
        {
            if (o == null) return "<null>";
            if (o is string) return (string)o;
            IEnumerable e = o as IEnumerable;
            if (e == null) return o.ToString();
            var list = new List<string>();
            foreach (object item in e)
            {
                if (list.Count >= 20) { list.Add("..."); break; }
                list.Add(item == null ? "<null>" : item.ToString());
            }
            return string.Join(",", list.ToArray());
        }

        private static MethodBase Find(string fullType, string assembly, string methodName, int paramCount)
        {
            Type type = Type.GetType(fullType + ", " + assembly, false);
            if (type == null) { Log.Write("PATCH_TARGET_MISSING type=" + fullType); return null; }
            foreach (MethodInfo m in type.GetMethods(Flags))
                if (m.Name == methodName && m.GetParameters().Length == paramCount) return m;
            Log.Write("PATCH_TARGET_MISSING method=" + fullType + "." + methodName);
            return null;
        }

        private static void Inspect(MethodBase target, string label, MethodInfo getPatchInfo)
        {
            if (target == null) return;
            Log.Write("PATCH_TARGET " + label + "; signature=" + target);
            object info = getPatchInfo.Invoke(null, new object[] { target });
            if (info == null) { Log.Write("PATCH_NONE " + label); return; }
            string[] groups = { "Prefixes", "Postfixes", "Transpilers", "Finalizers" };
            foreach (string group in groups)
            {
                IEnumerable entries = Member(info, group) as IEnumerable;
                if (entries == null) continue;
                int count = 0;
                foreach (object patch in entries)
                {
                    if (++count > 64) { Log.Write("PATCH_TRUNCATED " + label + "/" + group); break; }
                    object methodObj = Member(patch, "PatchMethod");
                    MethodBase patchMethod = methodObj as MethodBase;
                    string patchName = patchMethod == null ? Value(methodObj) :
                        patchMethod.DeclaringType.FullName + "." + patchMethod.Name;
                    string assemblyName = patchMethod == null ? "<unknown>" :
                        patchMethod.DeclaringType.Assembly.GetName().Name;
                    Log.Write("PATCH_ENTRY target=" + label + "; kind=" + group +
                        "; owner=" + Value(Member(patch, "owner")) +
                        "; method=" + patchName + "; assembly=" + assemblyName +
                        "; priority=" + Value(Member(patch, "priority")) +
                        "; before=[" + Value(Member(patch, "before")) + "]" +
                        "; after=[" + Value(Member(patch, "after")) + "]");
                }
                if (count == 0) Log.Write("PATCH_EMPTY target=" + label + "; kind=" + group);
            }
        }

        public static void Dump(string reason)
        {
            try
            {
                Log.Write("PATCH_INVENTORY_BEGIN reason=" + reason);
                MethodInfo getPatchInfo = typeof(Harmony).GetMethod("GetPatchInfo", Flags, null,
                    new Type[] { typeof(MethodBase) }, null);
                if (getPatchInfo == null)
                {
                    Log.Write("PATCH_INVENTORY_UNAVAILABLE Harmony.GetPatchInfo(MethodBase) not found");
                    return;
                }
                Inspect(Find("TaleWorlds.CampaignSystem.ViewModelCollection.Party.PartyCharacterVM",
                    "TaleWorlds.CampaignSystem.ViewModelCollection", "InitializeUpgrades", 0),
                    "PartyCharacterVM.InitializeUpgrades", getPatchInfo);
                Inspect(Find("TaleWorlds.CampaignSystem.CharacterObject",
                    "TaleWorlds.CampaignSystem", "GetUpgradeGoldCost", 2),
                    "CharacterObject.GetUpgradeGoldCost", getPatchInfo);
                Inspect(Find("TaleWorlds.CampaignSystem.ViewModelCollection.Party.PartyVM",
                    "TaleWorlds.CampaignSystem.ViewModelCollection", "InitializeTroopLists", 0),
                    "PartyVM.InitializeTroopLists", getPatchInfo);
                Inspect(Find("TaleWorlds.CampaignSystem.CharacterObject",
                    "TaleWorlds.CampaignSystem", "get_UpgradeTargets", 0),
                    "CharacterObject.get_UpgradeTargets", getPatchInfo);
            }
            catch (Exception ex) { Log.Write("PATCH_INVENTORY_ERROR " + ex); }
            finally { Log.Write("PATCH_INVENTORY_END reason=" + reason); }
        }
    }

    public static class RosterPatch
    {
        private static PropertyInfo mainParty;
        private static PropertyInfo memberRoster;
        private static PropertyInfo prisonRoster;
        private static int logged;

        public static bool Configure()
        {
            Type t = Type.GetType("TaleWorlds.CampaignSystem.Party.MobileParty, TaleWorlds.CampaignSystem", false);
            if (t == null) return false;
            mainParty = t.GetProperty("MainParty", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            memberRoster = t.GetProperty("MemberRoster", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            prisonRoster = t.GetProperty("PrisonRoster", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return mainParty != null && (memberRoster != null || prisonRoster != null);
        }

        public static void Postfix(object __instance, object[] __args)
        {
            try
            {
                if (__instance == null || __args == null || __args.Length < 2 || logged >= 1000) return;
                object party = mainParty.GetValue(null, null);
                if (party == null) return;
                object members = memberRoster == null ? null : memberRoster.GetValue(party, null);
                object prisoners = prisonRoster == null ? null : prisonRoster.GetValue(party, null);
                string kind = Object.ReferenceEquals(__instance, members) ? "members" :
                    (Object.ReferenceEquals(__instance, prisoners) ? "prisoners" : null);
                if (kind == null) return;
                int delta = (int)__args[1];
                if (delta == 0) return;
                if (System.Threading.Interlocked.Increment(ref logged) <= 1000)
                    Log.Write("PLAYER_ROSTER_CHANGE kind=" + kind + "; troopId=" +
                        UpgradePatch.TroopId(__args[0]) + "; countDelta=" + delta);
            }
            catch (Exception ex) { Log.Write("WARN roster trace failed: " + ex.GetType().Name + " " + ex.Message); }
        }
    }

    internal static class Log
    {
        private static readonly object gate = new object();
        private static string folder;
        private static string path;
        private static string marker;
        private static bool unhandled;
        private static int firstChanceCount;

        public static void Start()
        {
            try
            {
                folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BannerlordUpgradeGuard");
                Directory.CreateDirectory(folder);
                path = Path.Combine(folder, "upgrade_guard.log");
                marker = Path.Combine(folder, "session.open");
                if (File.Exists(marker))
                    Write("PREVIOUS_SESSION_NOT_CLOSED_CLEANLY (could be crash, forced exit, or power loss; not proof of a crash)");
                File.WriteAllText(marker, DateTime.Now.ToString("o"));
                Write("SESSION_START v6; PID=" + System.Diagnostics.Process.GetCurrentProcess().Id +
                    "; CLR=" + Environment.Version + "; OS=" + Environment.OSVersion);
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
                AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;
                AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            }
            catch { /* Logging must never crash Bannerlord. */ }
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            unhandled = true;
            Write("UNHANDLED_MANAGED_EXCEPTION terminating=" + args.IsTerminating + "; " + args.ExceptionObject);
        }

        private static void OnFirstChanceException(object sender, FirstChanceExceptionEventArgs args)
        {
            try
            {
                if (!(args.Exception is IndexOutOfRangeException) || firstChanceCount >= 20) return;
                string stack = args.Exception.StackTrace ?? "";
                if (stack.IndexOf("GetUpgradeGoldCost", StringComparison.Ordinal) < 0 &&
                    stack.IndexOf("InitializeTroopLists", StringComparison.Ordinal) < 0) return;
                if (System.Threading.Interlocked.Increment(ref firstChanceCount) <= 20)
                    Write("FIRST_CHANCE_RELEVANT_EXCEPTION " + args.Exception);
            }
            catch { }
        }

        private static void OnProcessExit(object sender, EventArgs args)
        {
            Write("PROCESS_EXIT event; unhandledManagedException=" + unhandled);
            if (!unhandled)
            {
                try { if (marker != null && File.Exists(marker)) File.Delete(marker); }
                catch { }
            }
        }

        public static void Write(string message)
        {
            try
            {
                lock (gate)
                {
                    if (folder == null)
                        folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BannerlordUpgradeGuard");
                    Directory.CreateDirectory(folder);
                    if (path == null) path = Path.Combine(folder, "upgrade_guard.log");
                    if (File.Exists(path) && new FileInfo(path).Length > 5 * 1024 * 1024)
                    {
                        string old = path + ".previous";
                        if (File.Exists(old)) File.Delete(old);
                        File.Move(path, old);
                    }
                    File.AppendAllText(path, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " [tid=" + System.Threading.Thread.CurrentThread.ManagedThreadId + "] " + message + Environment.NewLine);
                }
            }
            catch { /* A logging error must never crash the game. */ }
        }
    }
}
