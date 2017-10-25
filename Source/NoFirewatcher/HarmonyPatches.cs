using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using Verse.Sound;
using RimWorld;
using Harmony;

namespace NoFirewatcher
{
    [StaticConstructorOnStartup]
    class HarmonyPatches
    {
        static HarmonyPatches()
        {
#if DEBUG
            HarmonyInstance.DEBUG = true;
#endif
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.whyisthat.nofirewatcher.main");

            harmony.Patch(AccessTools.Property(typeof(FireWatcher), nameof(FireWatcher.LargeFireDangerPresent)).GetGetMethod(), new HarmonyMethod(typeof(HarmonyPatches), nameof(DoNothingDetour)), null);
            harmony.Patch(AccessTools.Method(typeof(FireWatcher), nameof(FireWatcher.FireWatcherTick)), new HarmonyMethod(typeof(HarmonyPatches), nameof(DoNothingDetour)), null);
        }

        public static bool DoNothingDetour() { return false; }

    }

}
