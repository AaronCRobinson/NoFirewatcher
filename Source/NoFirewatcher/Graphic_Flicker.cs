using UnityEngine;
using Verse;
using RimWorld;

namespace NoFirewatcher
{
    // NOTE: look into working with this better, especially switching out the custom dynamically...
    // custom graphic flicker to help with performance.
    public class Graphic_Flicker : Verse.Graphic_Flicker
    {
        private const int BaseTicksPerFrameChange = 60;

        public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing)
        {
            // Skip some error checking.
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
                if (thing is Fire)
                {
                    num4 = (thing as Fire).fireSize;
                }
                else if (compFireOverlay != null)
                {
                    Log.ErrorOnce("compFireOverlay != null (1)", 6612326);
                    num4 = compFireOverlay.Props.fireSize;
                }
            }
            Graphic graphic = this.subGraphics[num3];
            float num5 = Mathf.Min(num4 / 1.2f, 1.2f);
            //Vector3 a = GenRadial.RadialPattern[num2 % GenRadial.RadialPattern.Length].ToVector3() / GenRadial.MaxRadialPatternRadius;
            //a *= MaxOffset;
            Vector3 vector = loc; // + a * num4;
            if (compFireOverlay != null)
            {
                Log.ErrorOnce("compFireOverlay != null (2)", 6612326);
                vector += compFireOverlay.Props.offset;
            }
            Vector3 s = new Vector3(num5, 1f, num5);
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(vector, Quaternion.identity, s);
            Graphics.DrawMesh(MeshPool.plane10, matrix, graphic.MatSingle, 0);
        }
    }
}
