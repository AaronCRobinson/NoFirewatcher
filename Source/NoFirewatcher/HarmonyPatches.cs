using Harmony;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.AI;
using System.Reflection.Emit;
using Verse.Sound;
using System.Diagnostics;

namespace NoFirewatcher
{
    [StaticConstructorOnStartup]
    class HarmonyPatches
    {

        static HarmonyPatches()
        {
            HarmonyInstance.DEBUG = true;
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.why_is_that.NoFirewatcher.main");

            harmony.Patch(AccessTools.Method(typeof(WeatherDecider), "CurrentWeatherCommonality"), null, null, new HarmonyMethod(typeof(HarmonyPatches), nameof(RemoveLargeFireDangerPresentCheckTranspiler)));
            harmony.Patch(AccessTools.Method(typeof(WeatherDecider), nameof(WeatherDecider.WeatherDeciderTick)), null, null, new HarmonyMethod(typeof(HarmonyPatches), nameof(RemoveLargeFireDangerPresentCheckTranspiler)));

            harmony.Patch(AccessTools.Method(typeof(TickManager), nameof(TickManager.DoSingleTick)), new HarmonyMethod(typeof(HarmonyPatches), nameof(TickManagerPrefix)), null);
            harmony.Patch(AccessTools.Method(typeof(Fire), nameof(Fire.Tick)), null, null, new HarmonyMethod(typeof(HarmonyPatches), nameof(FireTickTranspiler)));

            // TO BE REMOVED
            //harmony.Patch(AccessTools.Method(typeof(Game), nameof(Game.UpdatePlay)), new HarmonyMethod(typeof(HarmonyPatches), nameof(StartWatch)), new HarmonyMethod(typeof(HarmonyPatches), nameof(StopWatch)));
        }

        /*static Stopwatch watch;

        public static void StartWatch()
        {
            watch = new Stopwatch();
            watch.Start();
        }

        public static void StopWatch()
        {
            watch.Stop();
            Log.Message("Time: " + watch.ElapsedTicks.ToString());
        }*/

        public static IEnumerable<CodeInstruction> RemoveLargeFireDangerPresentCheckTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo largeFireDangerPresentMethodInfo = AccessTools.Property(typeof(FireWatcher), nameof(FireWatcher.LargeFireDangerPresent)).GetGetMethod();

            List<CodeInstruction> instructionList = instructions.ToList<CodeInstruction>();
            int i;
            bool found = false;
            for (i = 0; i < instructionList.Count - 1; i++)
            {
                if (instructionList[i].opcode == OpCodes.Callvirt && instructionList[i].operand == largeFireDangerPresentMethodInfo)
                {
                    found = true;
                    break;
                }
            }

            if (found) // consider being smarter here
                instructionList.RemoveRange(i - 3, 5);

            for (i = 0; i < instructionList.Count; i++) yield return instructionList[i];
        }

        // NOTE: this is not being used right now.
        //TickManager.DoSingleTick => handle counting fires
        public static void TickManagerPrefix()
        {
            Traverse t = Traverse.Create(typeof(Fire));
            //Fire.fireCount = base.Map.listerThings.ThingsOfDef(ThingDefOf.Fire).Count;
            t.Property("fireCount").SetValue(Find.VisibleMap.listerThings.ThingsOfDef(ThingDefOf.Fire).Count);
        }

        private static FieldInfo ticksUntilSmokeFieldInfo = AccessTools.Field(typeof(Fire), "ticksUntilSmoke");
        private static FieldInfo ticksSinceSpawnFieldInfo = AccessTools.Field(typeof(Fire), "ticksSinceSpawn");
        private static FieldInfo ticksSinceSpreadFieldInfo = AccessTools.Field(typeof(Fire), "ticksSinceSpread");
        private static MethodInfo highPerformanceFireTickMethodInfo = AccessTools.Method(typeof(HighPerformanceFire), nameof(HighPerformanceFire.Tick));

        public static IEnumerable<CodeInstruction> FireTickTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            // consider traverse?
            List<CodeInstruction> instructionList = instructions.ToList<CodeInstruction>();
            
            yield return new CodeInstruction(OpCodes.Ldarg_0); //this
            yield return new CodeInstruction(OpCodes.Call, AccessTools.Property(typeof(Thing), nameof(Thing.Map)).GetGetMethod());
            yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Map), nameof(Map.fireWatcher)));
            yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Property(typeof(FireWatcher), nameof(FireWatcher.LargeFireDangerPresent)).GetGetMethod());

            Label elseLabel = il.DefineLabel();
            yield return new CodeInstruction(OpCodes.Brfalse, elseLabel); // branch

            // NOTE: having issues passing sustainer
            yield return new CodeInstruction(OpCodes.Ldarg_0); //this
            yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Fire), "sustainer"));
            Label jump = il.DefineLabel();
            yield return new CodeInstruction(OpCodes.Brfalse, jump);

            yield return new CodeInstruction(OpCodes.Ldarg_0); //this
            yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Fire), "sustainer"));
            yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Sustainer), nameof(Sustainer.Maintain)));

            yield return new CodeInstruction(OpCodes.Ldarg_0){ labels = new List<Label>() { jump } }; //this
            //yield return new CodeInstruction(OpCodes.Dup);
            //yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Fire), "sustainer"));
            yield return new CodeInstruction(OpCodes.Dup); // this
            yield return new CodeInstruction(OpCodes.Ldflda, ticksUntilSmokeFieldInfo);

            yield return new CodeInstruction(OpCodes.Ldarg_0); // this
            yield return new CodeInstruction(OpCodes.Ldflda, ticksSinceSpawnFieldInfo);

            yield return new CodeInstruction(OpCodes.Ldarg_0); // this
            yield return new CodeInstruction(OpCodes.Ldflda, ticksSinceSpreadFieldInfo);

            yield return new CodeInstruction(OpCodes.Call, highPerformanceFireTickMethodInfo);

            Label returnLabel = il.DefineLabel();
            yield return new CodeInstruction(OpCodes.Br, returnLabel);

            // handle labels
            instructionList[0].labels.Add(elseLabel); // else
            instructionList[instructionList.Count - 1].labels.Add(returnLabel); // end if

            int i;
            for (i = 0; i < instructionList.Count; i++) yield return instructionList[i];
        }
    }
}
