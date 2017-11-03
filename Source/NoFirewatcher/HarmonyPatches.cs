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
            return RemoveSequence(instructionList, target, new CodeInstruction(OpCodes.Ldc_I4_0));
        }

        public static IEnumerable<CodeInstruction> RemoveDefaultFireWatcherTick(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> target = new List<CodeInstruction>() {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Map), nameof(Map.fireWatcher))),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(FireWatcher), nameof(FireWatcher.FireWatcherTick)))
            };

            List<CodeInstruction> instructionList = instructions.ToList();
            return RemoveSequence(instructionList, target);
        }

        private static List<CodeInstruction> RemoveSequence(List<CodeInstruction> instructionList, List<CodeInstruction> target, CodeInstruction newInstruction = null)
        {
            int seqIdx = 0;
            int i = 0;
            while (i < instructionList.Count)
            {
                if (instructionList[i].opcode == target[seqIdx].opcode && instructionList[i].operand == target[seqIdx].operand)
                {
                    seqIdx++;
                    if (seqIdx == target.Count)
                    {
                        i -= (seqIdx - 1);
                        instructionList.RemoveRange(i, seqIdx);
                        seqIdx = 0;
                        if (newInstruction != null)
                            instructionList.Insert(i, newInstruction);
                    }
                }
                else
                    seqIdx = 0;
                i++;
            }
            // NOTE: unhandle edge case here
            return instructionList;
        }

    }

}
