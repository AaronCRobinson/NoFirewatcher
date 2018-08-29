using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;
using RimWorld;
using Harmony;
using System.Linq;
using System;
using static NoFirewatcher.HarmonyTools;

namespace NoFirewatcher
{
    [StaticConstructorOnStartup]
    public class FirewatcerPatches
    {
        static FirewatcerPatches()
        {
#if DEBUG
            HarmonyInstance.DEBUG = true;
#endif
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.whyisthat.nofirewatcher.main");

            switch (NoFirewatcherMod.settings.chieftainness)
            {
                case FirewatcherChieftainness.NoChief:

                    // Set LargeFireDangerPresent calls to be 'false' (but still leave functionality intake.
                    harmony.Patch(AccessTools.Method(typeof(WeatherDecider), nameof(WeatherDecider.WeatherDeciderTick)), null, null, new HarmonyMethod(typeof(FirewatcerPatches), nameof(ReplaceLargeFireDangerPresentCalls)));
                    harmony.Patch(AccessTools.Method(typeof(WeatherDecider), "CurrentWeatherCommonality"), null, null, new HarmonyMethod(typeof(FirewatcerPatches), nameof(ReplaceLargeFireDangerPresentCalls)));
                    harmony.Patch(AccessTools.Method(typeof(Map), nameof(Map.MapPostTick)), null, null, new HarmonyMethod(typeof(FirewatcerPatches), nameof(RemoveDefaultFireWatcherTick)));
                    break;

                case FirewatcherChieftainness.LilChief:

                    harmony.Patch(AccessTools.Property(typeof(FireWatcher), nameof(FireWatcher.LargeFireDangerPresent)).GetGetMethod(), null, null, new HarmonyMethod(typeof(FirewatcerPatches), nameof(LargeFireDangerPresentTranspiler)));
                    break;
            }
#if DEBUG
            HarmonyInstance.DEBUG = false;
#endif
        }

        public static IEnumerable<CodeInstruction> LargeFireDangerPresentTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> newInstructions = new List<CodeInstruction>() { new CodeInstruction(OpCodes.Ldc_R4, 180) };
            return HarmonyTools.Bigofactorunie(OpCodes.Ldc_R4, 90f, newInstructions).Invoke(instructions);
        }

        public static IEnumerable<CodeInstruction> ReplaceLargeFireDangerPresentCalls(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> target = new List<CodeInstruction>() {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(WeatherDecider), "map")),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Map), nameof(Map.fireWatcher))),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Property(typeof(FireWatcher), nameof(FireWatcher.LargeFireDangerPresent)).GetGetMethod())
            };

            List<CodeInstruction> instructionList = instructions.ToList();
            return HarmonyTools.RemoveSequence(instructionList, target, new CodeInstruction(OpCodes.Ldc_I4_0));
        }

        public static IEnumerable<CodeInstruction> RemoveDefaultFireWatcherTick(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> target = new List<CodeInstruction>() {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Map), nameof(Map.fireWatcher))),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(FireWatcher), nameof(FireWatcher.FireWatcherTick)))
            };

            List<CodeInstruction> instructionList = instructions.ToList();
            return HarmonyTools.RemoveSequence(instructionList, target);
        }



    }

}
