using UnityEngine;
using Verse;
using RimWorld;
using SettingsHelper;
using System;
using System.Collections.Generic;

namespace NoFirewatcher
{
    public enum FirewatcherChieftainness : byte { Original, LilChief, NoChief}

    public class NoFirewatcherSettings : ModSettings
    {
        public FirewatcherChieftainness chieftainness = FirewatcherChieftainness.NoChief;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.chieftainness, "chieftainness", FirewatcherChieftainness.NoChief);
        }
    }

    class NoFirewatcherMod : Mod
    {
        public static NoFirewatcherSettings settings;

        public NoFirewatcherMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<NoFirewatcherSettings>();
        }

        public override string SettingsCategory() => "NoFirewatcher";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);
            listing.AddLabeledSlider<FirewatcherChieftainness>("FireWatcher Chieftainness", ref settings.chieftainness);
            listing.End();
            settings.Write();
        }
    }

}
