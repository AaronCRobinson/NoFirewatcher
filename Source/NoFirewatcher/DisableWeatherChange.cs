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
    class DisableWeatherChange
    {
        static DisableWeatherChange()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.why_is_that.NoFirewatcher.DisableWeatherChange");
            //harmony.Patch(AccessTools.Method(typeof(WeatherDecider), nameof(WeatherDecider.StartNextWeather)), new HarmonyMethod(typeof(HarmonyPatches), nameof(Prefix)), null);
        }

        public static bool Prefix() { return false;  }
    }
}
