using Harmony;
using RimWorld;
using Verse;

namespace NoFirewatcher
{

#if DEBUG
    [StaticConstructorOnStartup]
    class DisableWeatherChange
    {
        static DisableWeatherChange()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.why_is_that.NoFirewatcher.DisableWeatherChange");
            harmony.Patch(AccessTools.Method(typeof(WeatherDecider), nameof(WeatherDecider.StartNextWeather)), new HarmonyMethod(typeof(HarmonyPatches), nameof(Prefix)), null);
        }

        public static bool Prefix() { return false;  }
    }
#endif

}
