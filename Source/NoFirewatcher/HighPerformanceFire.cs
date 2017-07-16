using Harmony;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace NoFirewatcher
{
    static class HighPerformanceFire
    {
        // RimWorld.Fire (copypasta)
        private static List<Thing> flammableList = new List<Thing>();
        private static float flammabilityMax;

        public const float MinFireSize = 0.1f;
        private const float MaxFireSize = 1.75f;
        private const float TicksBetweenSparksBase = 150f;
        private const int TicksToBurnFloor = 7500;
        private const int ComplexCalcsInterval = 150;
        private const float MinSizeForIgniteMovables = 0.4f;
        private const float HeatPerFireSizePerInterval = 160f;
        private const float HeatFactorWhenDoorPresent = 0.15f;
        private const float SnowClearRadiusPerFireSize = 3f;
        private const float FireBaseGrowthPerTick = 0.00055f;

        private static readonly IntRange SmokeIntervalRange = new IntRange(130, 200);

        //public static void Tick(Fire f, Sustainer sustainer, ref int ticksSinceSpawn, ref int ticksSinceSpread)
        public static void Tick(Fire f, ref int ticksUntilSmoke, ref int ticksSinceSpawn, ref int ticksSinceSpread)
        {
            Map map = f.Map;

            ticksUntilSmoke--; // smoke only applies to orphans

            if (f.fireSize > 1f)
            {
                ticksSinceSpread++;
                // ticksSinceSpread >= SpreadInterval
                if ((float)ticksSinceSpread >= TicksBetweenSparksBase - (f.fireSize - 1f) * 40f)
                {
                    f.TrySpread();
                    ticksSinceSpread = 0;
                }
            }

            if (Gen.IsHashIntervalTick(f, ComplexCalcsInterval))
            {
                if (f.parent == null)
                {
                    f.DoComplexOrphanCalcs();
                    if (ticksUntilSmoke <= 0)
                    {
                        // RimWorld.Fire.SpawnSmokeParticles()
                        if (f.fireSize > 0.5f)
                        {
                            MoteMaker.ThrowFireGlow(f.Position, map, f.fireSize);
                        }
                        float num = f.fireSize / 2f;
                        if (num > 1f)
                        {
                            num = 1f;
                        }
                        num = 1f - num;
                        ticksUntilSmoke = SmokeIntervalRange.Lerped(num) + (int)(10f * Rand.Value);
                    }
                }     
                else
                {
                    f.DoComplexParentedCalcs();
                }
                    
            }

                ticksSinceSpawn++;
            if (ticksSinceSpawn >= TicksToBurnFloor)
            {
                // RimWorld.Fire.TryMakeFloorBurned()
                if (f.parent != null || !f.Spawned) return;

                TerrainDef burnedDef = f.Position.GetTerrain(map)?.burnedDef;

                if (burnedDef != null && f.Position.TerrainFlammableNow(map))
                {
                    TerrainGrid terrainGrid = map.terrainGrid;
                    terrainGrid.RemoveTopLayer(f.Position, false);
                    terrainGrid.SetTerrain(f.Position, burnedDef);
                }
            }
        }

        // RimWorld.Fire.TrySpread()
        private static void TrySpread(this Fire f)
        {
            // consider using args?
            IntVec3 originalPos = f.Position;
            Map map = f.Map;

            IntVec3 intVec;
            bool flag;            

            if (Rand.Chance(0.8f))
            {
                intVec = originalPos + GenRadial.ManualRadialPattern[Rand.RangeInclusive(1, 8)];
                flag = true;
            }
            else
            {
                intVec = originalPos + GenRadial.ManualRadialPattern[Rand.RangeInclusive(10, 20)];
                flag = false;
            }

            if (!intVec.InBounds(map) || !GenSight.LineOfSight(originalPos, intVec, map, CellRect.SingleCell(originalPos), CellRect.SingleCell(intVec))) return;

            if (Rand.Chance(FireUtility.ChanceToStartFireIn(intVec, map)))
            {
                if (!flag)
                {
                    Spark spark = (Spark)GenSpawn.Spawn(ThingDefOf.Spark, originalPos, map);
                    spark.Launch(f, intVec, null);
                }
                else
                {
                    FireUtility.TryStartFireIn(intVec, map, Fire.MinFireSize);
                }
            }
        }

        private static void DoComplexParentedCalcs(this Fire f)
        {
            flammabilityMax = 0f;
            Map map = f.Map;

            //TerrainDef terrainDef td = f.Position.GetTerrain(f.Map);
            if (!f.Position.GetTerrain(map).HasTag("Water"))
            {
                flammabilityMax = f.parent.GetStatValue(StatDefOf.Flammability, true);
            }

            if (flammabilityMax < 0.01f) // min flammability to continue
            {
                f.Destroy(DestroyMode.Vanish);
                return;
            }

            f.DoFireDamage(f.parent);

            if (f.Spawned)
            {
                // heat
                float num = f.fireSize * HeatPerFireSizePerInterval;
                GenTemperature.PushHeat(f.Position, map, num);

                if (Rand.Value < 0.4f)
                {
                    float radius = f.fireSize * SnowClearRadiusPerFireSize;
                    SnowUtility.AddSnowRadial(f.Position, map, radius, -(f.fireSize * 0.1f));
                }
                f.fireSize += FireBaseGrowthPerTick * flammabilityMax * 150f;

                if (f.fireSize > MaxFireSize)
                    f.fireSize = MaxFireSize;

                if ((double)map.weatherManager.RainRate <= 0.01f || !f.VulnerableToRain()) // || (double)Rand.Value >= 6.0)
                    return;

                //f.TakeDamage(new DamageInfo(DamageDefOf.Extinguish, 10, -1f, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown));

                // Verse.Thing.public void TakeDamage(DamageInfo dinfo)

                // Verse.DamageWorker.Apply(DamageInfo dinfo, Thing victim) -- ALL THAT's LEFT MUAWHAHAHAHA
                if (Rand.Value > 0.9f)
                {
                    ImpactSoundUtility.PlayImpactSound(f, DamageDefOf.Extinguish.impactSoundType, map);
                }

                f.fireSize -= 0.1f * map.weatherManager.RainRate; // FEATURE: uses rainrate when extinguishing fire
                if (f.fireSize <= MinFireSize)
                {
                    f.Destroy(DestroyMode.Vanish);
                }
            }
        }

        private static void DoComplexOrphanCalcs(this Fire f)
        {
            bool doorPresent = false;
            HighPerformanceFire.flammableList.Clear();
            flammabilityMax = 0f;
            Map map = f.Map;
            Thing thing;

            //TerrainDef terrainDef td = f.Position.GetTerrain(f.Map);
            if (!f.Position.GetTerrain(map).HasTag("Water"))
            {
                if (f.Position.TerrainFlammableNow(map))
                {
                    flammabilityMax = f.Position.GetTerrain(map).GetStatValueAbstract(StatDefOf.Flammability, null);
                }
                List<Thing> list = map.thingGrid.ThingsListAt(f.Position);
                for (int i = 0; i < list.Count; i++)
                {
                    thing = list[i];
                    if (thing is Building_Door) doorPresent = true;

                    float statValue = thing.GetStatValue(StatDefOf.Flammability, true);
                    if (statValue >= 0.01f) // min flammability to continue
                    {
                        HighPerformanceFire.flammableList.Add(list[i]);
                        if (statValue > flammabilityMax) flammabilityMax = statValue;

                        //if (f.parent == null && f.fireSize > 0.4f && list[i].def.category == ThingCategory.Pawn)
                        if (f.fireSize >= MinSizeForIgniteMovables && list[i].def.category == ThingCategory.Pawn)
                        {
                            list[i].TryAttachFire(f.fireSize * 0.2f);
                        }
                    }
                }
            }

            if (flammabilityMax < 0.01f) // min flammability to continue
            {
                f.Destroy(DestroyMode.Vanish);
                return;
            }

            if (HighPerformanceFire.flammableList.Count > 0) // NOTE: why would this be empty?
            {
                thing = HighPerformanceFire.flammableList.RandomElement<Thing>();
            }
            else
            {
                thing = null;
            }

            if (thing != null && (f.fireSize >= MinSizeForIgniteMovables || thing.def.category != ThingCategory.Pawn))
            {
                f.DoFireDamage(thing);
            }

            if (f.Spawned)
            {
                // heat
                float num = f.fireSize * HeatPerFireSizePerInterval;
                if (doorPresent) num *= HeatFactorWhenDoorPresent;
                GenTemperature.PushHeat(f.Position, map, num);

                if (Rand.Value < 0.4f)
                {
                    float radius = f.fireSize * SnowClearRadiusPerFireSize;
                    SnowUtility.AddSnowRadial(f.Position, map, radius, -(f.fireSize * 0.1f));
                }
                f.fireSize += FireBaseGrowthPerTick * flammabilityMax * 150f;

                if (f.fireSize > MaxFireSize)
                    f.fireSize = MaxFireSize;

                if ((double)map.weatherManager.RainRate <= 0.01f || !f.VulnerableToRain()) // || (double)Rand.Value >= 6.0)
                    return;

                // NOTE: COPY PASTA
                //f.TakeDamage(new DamageInfo(DamageDefOf.Extinguish, 10, -1f, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown));

                // Verse.Thing.public void TakeDamage(DamageInfo dinfo)

                // Verse.DamageWorker.Apply(DamageInfo dinfo, Thing victim) -- ALL THAT's LEFT MUAWHAHAHAHA
                if (Rand.Value > 0.9f)
                {
                    ImpactSoundUtility.PlayImpactSound(f, DamageDefOf.Extinguish.impactSoundType, map);
                }

                f.fireSize -= 0.1f * map.weatherManager.RainRate; // FEATURE: uses rainrate when extinguishing fire
                if (f.fireSize <= MinFireSize)
                {
                    f.Destroy(DestroyMode.Vanish);
                }
            }
        }

        private static void DoFireDamage(this Fire f, Thing targ)
        {
            float num = 0.0161f * f.fireSize; //float num = 0.0125f + 0.0036f * f.fireSize;
            num = Mathf.Clamp(num, 0.0125f, 0.05f);
            int num2 = GenMath.RoundRandom(num * 150f); // TODO: fix this
            if (num2 < 1) num2 = 1;

            Pawn pawn = targ as Pawn;
            if (pawn != null)
            {
                DamageInfo dinfo = new DamageInfo(DamageDefOf.Flame, num2, -1f, f, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
                dinfo.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);
                targ.TakeDamage(dinfo);

                Apparel apparel;
                if (pawn.apparel != null && pawn.apparel.WornApparel.TryRandomElement(out apparel))
                {
                    apparel.TakeDamageFast(num2);
                }
            }
            else
            {
                targ.TakeDamageFast(num2);
            }
        }

        public static void TakeDamageFast(this Thing targ, int num2)
        {
            // RISK: skipping "is destroyed" check. 
            // NOTE: ignoring damageMultipliers
            // NOTE: no PreApplyDamage
            // RISK: skipping "is spawned" check.
            // NOTE: ignoring damageWatcher
            // NOTE: fire is not filthy, it's very very pretty.
            //DamageDefOf.Flame.Worker.Apply(dinfo, targ); STILL TOO MUCH... must go deeper

            targ.HitPoints -= num2;
            if (targ.HitPoints <= 0) targ.Kill(null);

            // NOTE: no PostApplyDamage
        }

        // RimWorld.Fire.VulnerableToRain()
        private static bool VulnerableToRain(this Fire f)
        {
            if (!f.Spawned) return false; //TODO: is this needed ?

            RoofDef roofDef = f.Map.roofGrid.RoofAt(f.Position);
            if (roofDef == null) return true;
            if (roofDef.isThickRoof) return false;

            Thing edifice = f.Position.GetEdifice(f.Map);
            return edifice != null && edifice.def.holdsRoof;
        }
    }

    // custom graphic flicker to help with performance.
    public class Graphic_Flicker : Graphic_Collection
    {
        private const int BaseTicksPerFrameChange = 120;
        private const int ExtraTicksPerFrameChange = 10; // What is this doing?
        private const float MaxOffset = 0.05f;

        public override Material MatSingle
        {
            get
            {
                return this.subGraphics[Rand.Range(0, this.subGraphics.Length)].MatSingle;
            }
        }

        public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing)
        {
            if (thingDef == null)
            {
                Log.ErrorOnce("Fire DrawWorker with null thingDef: " + loc, 3427324);
                return;
            }
            if (this.subGraphics == null)
            {
                Log.ErrorOnce("Graphic_Flicker has no subgraphics " + thingDef, 358773632);
                return;
            }
            int num = Find.TickManager.TicksGame;
            int num2 = 0;
            int num3 = 0;
            float num4 = 1f;
            CompFireOverlay compFireOverlay = null;
            if (thing != null)
            {
                compFireOverlay = thing.TryGetComp<CompFireOverlay>();
                num += Mathf.Abs(thing.thingIDNumber ^ 8453458);
                num2 = num / BaseTicksPerFrameChange;
                num3 = Mathf.Abs(num2 ^ thing.thingIDNumber * 391) % this.subGraphics.Length;
                Fire fire = thing as Fire;
                if (fire != null)
                {
                    num4 = fire.fireSize;
                }
                else if (compFireOverlay != null)
                {
                    num4 = compFireOverlay.Props.fireSize;
                }
            }
            if (num3 < 0 || num3 >= this.subGraphics.Length)
            {
                Log.ErrorOnce("Fire drawing out of range: " + num3, 7453435);
                num3 = 0;
            }
            Graphic graphic = this.subGraphics[num3];
            float num5 = Mathf.Min(num4 / 1.2f, 1.2f);
            Vector3 a = GenRadial.RadialPattern[num2 % GenRadial.RadialPattern.Length].ToVector3() / GenRadial.MaxRadialPatternRadius;
            a *= MaxOffset;
            Vector3 vector = loc + a * num4;
            if (compFireOverlay != null)
            {
                vector += compFireOverlay.Props.offset;
            }
            Vector3 s = new Vector3(num5, 1f, num5);
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(vector, Quaternion.identity, s);
            Graphics.DrawMesh(MeshPool.plane10, matrix, graphic.MatSingle, 0);
        }

        public override string ToString()
        {
            return string.Concat(new object[]
            {
                "Flicker(subGraphic[0]=",
                this.subGraphics[0].ToString(),
                ", count=",
                this.subGraphics.Length,
                ")"
            });
        }
    }
}
