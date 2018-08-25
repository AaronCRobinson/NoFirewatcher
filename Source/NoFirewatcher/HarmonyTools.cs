using System.Collections.Generic;
using System.Reflection.Emit;
using Harmony;
using Verse;

namespace NoFirewatcher
{
    public static class HarmonyTools
    {
        public static List<CodeInstruction> RemoveSequence(List<CodeInstruction> instructionList, List<CodeInstruction> target, CodeInstruction newInstruction = null)
        {
#if DEBUG
            Log.Message($"RemoveSequence");
#endif
            int seqIdx = 0;
            int i = 0;
            while (i < instructionList.Count)
            {
                if (instructionList[i].opcode == target[seqIdx].opcode && instructionList[i].operand == target[seqIdx].operand)
                {
                    seqIdx++;
                    if (seqIdx == target.Count)
                    {
                        seqIdx--;
                        i -= seqIdx;
                        // track labels
                        List<Label> labels = new List<Label>();
                        foreach (var instruction in instructionList.GetRange(i + 1, seqIdx))
                        {
#if DEBUG
                            Log.Message($"{instruction}");
#endif
                            labels.AddRange(instruction.labels);
                        }

                        instructionList.RemoveRange(i + 1, seqIdx);
                        seqIdx = 0;
                        // insert nop place holder (helps brrainz & harmony)
                        //instructionList.Insert(i++, new CodeInstruction(OpCodes.Nop));
                        // replace with nop place holder
                        instructionList[i].opcode = OpCodes.Nop;
                        instructionList[i].operand = null;
                        i++;

                        // fix labels
                        if (newInstruction != null)
                        {
                            newInstruction.labels = labels;
                            instructionList.Insert(i, newInstruction);
                        }
                        else
                            instructionList[i].labels = labels;
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
