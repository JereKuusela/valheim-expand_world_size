// 0.0.1
using System.Reflection.Emit;
using HarmonyLib;

namespace ExpandWorldSize;

public static class Helper
{
    /**
     * MatchForward(false, ...) does not throw on miss. It leaves the CodeMatcher
     * with Pos out of range, and the next call into Operand (or SetOperandAndAdvance,
     * SetAndAdvance, Advance, RemoveInstruction, etc.) throws
     * ArgumentOutOfRangeException from _codes[Pos]. That fired whenever a second
     * transpiler ran on already-mutated IL — i.e. whenever EWS and BC overlapped
     * on an anchor and BC ran first. The IsInvalid guard turns the hard crash
     * into a silent no-op, which is the correct semantic: if our anchor is gone,
     * the first transpiler in the chain already did the work we would have done.
     */
    public static CodeMatcher Replace(CodeMatcher instructions, double value, double newValue)
    {
        instructions.MatchForward(false, new CodeMatch(OpCodes.Ldc_R8, value));
        if (instructions.IsInvalid)
        {
            return instructions;
        }
        return instructions.SetOperandAndAdvance(newValue);
    }
    public static CodeMatcher Replace(CodeMatcher instructions, float value, float newValue)
    {
        instructions.MatchForward(false, new CodeMatch(OpCodes.Ldc_R4, value));
        if (instructions.IsInvalid)
        {
            return instructions;
        }
        return instructions.SetOperandAndAdvance(newValue);
    }

    public static CodeMatcher ReplaceSeed(CodeMatcher instructions, string name, float value)
    {
        instructions.MatchForward(false, new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(WorldGenerator), name)));
        if (instructions.IsInvalid)
        {
            return instructions;
        }
        return instructions
          .Advance(-1)
          .SetAndAdvance(OpCodes.Ldc_R4, value)
          .RemoveInstruction();
    }

    public static float HeightToBaseHeight(float altitude) => altitude / 200f;
    public static bool IsServer() => ZNet.instance && ZNet.instance.IsServer();
    // Note: Intended that is client when no Znet instance (so stuff isn't loaded in the main menu).
    public static bool IsClient() => !IsServer();
}