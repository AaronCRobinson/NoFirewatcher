using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;
using RimWorld;
using Harmony;
using System.Linq;

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

            // Set LargeFireDangerPresent calls to be 'false' (but still leave functionality intake.
            harmony.Patch(AccessTools.Method(typeof(WeatherDecider), nameof(WeatherDecider.WeatherDeciderTick)), null, null, new HarmonyMethod(typeof(HarmonyPatches), nameof(ReplaceLargeFireDangerPresentCalls)));
            harmony.Patch(AccessTools.Method(typeof(WeatherDecider), "CurrentWeatherCommonality"), null, null, new HarmonyMethod(typeof(HarmonyPatches), nameof(ReplaceLargeFireDangerPresentCalls)));

            harmony.Patch(AccessTools.Method(typeof(Map), nameof(Map.MapPostTick)), null, null, new HarmonyMethod(typeof(HarmonyPatches), nameof(RemoveDefaultFireWatcherTick)));
#if DEBUG
            HarmonyInstance.DEBUG = false;
#endif
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
