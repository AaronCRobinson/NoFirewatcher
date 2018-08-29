using RimWorld;
using Verse;
using Harmony;

namespace NoFirewatcher
{

#if DEBUG
    [StaticConstructorOnStartup]
    class DisableWeatherChange
    {
        static DisableWeatherChange()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.why_is_that.NoFirewatcher.DisableWeatherChange");
            harmony.Patch(AccessTools.Method(typeof(WeatherDecider), nameof(WeatherDecider.StartNextWeather)), new HarmonyMethod(typeof(FirewatcerPatches), nameof(Prefix)), null);
        }

        public static bool Prefix() { return false;  }
    }
#endif

}
