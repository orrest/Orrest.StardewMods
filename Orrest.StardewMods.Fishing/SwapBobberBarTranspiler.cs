using HarmonyLib;
using StardewValley.Menus;
using StardewValley.Tools;
using System.Reflection;
using System.Reflection.Emit;

namespace Orrest.StardewMods.Fishing;

/// <summary>
/// Swaps the vanilla <see cref="BobberBar"/> created at the start of the fishing minigame
/// for our <see cref="StepBobberBar"/>, which has the bigger movement step and no edge bounce.
/// </summary>
[HarmonyPatch(typeof(FishingRod), nameof(FishingRod.startMinigameEndFunction))]
public static class SwapBobberBarTranspiler
{
    // The original 8-argument BobberBar constructor used by FishingRod.startMinigameEndFunction.
    private static readonly ConstructorInfo BobberBarCtor =
        AccessTools.Constructor(typeof(BobberBar), new[]
        {
            typeof(string), typeof(float), typeof(bool), typeof(List<string>),
            typeof(string), typeof(bool), typeof(string), typeof(bool)
        })
            ?? throw new InvalidOperationException("Could not find BobberBar constructor.");

    // The matching constructor on our subclass (same signature).
    private static readonly ConstructorInfo StepCtor =
        AccessTools.Constructor(typeof(StepBobberBar), new[]
        {
            typeof(string), typeof(float), typeof(bool), typeof(List<string>),
            typeof(string), typeof(bool), typeof(string), typeof(bool)
        })
            ?? throw new InvalidOperationException("Could not find StepBobberBar constructor.");

    // instructions is the IL stream of the original method; we locate the `newobj BobberBar`
    // instruction and redirect it to our subclass ctor. Because the subclass ctor has the
    // exact same signature, the arguments already on the stack are still valid as-is.
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> codes = instructions.ToList();
        bool patched = false;

        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].opcode == OpCodes.Newobj
                && codes[i].operand is ConstructorInfo ci
                && ci == BobberBarCtor)
            {
                codes[i].operand = StepCtor;
                patched = true;
            }
        }

        if (!patched)
        {
            // Fail loudly if a game update changes how/where BobberBar is constructed, so the
            // mod does not silently revert to vanilla behaviour.
            throw new InvalidOperationException(
                "SwapBobberBarTranspiler did not find the `new BobberBar(...)` instruction to patch. " +
                "The game may have been updated; verify FishingRod.startMinigameEndFunction.");
        }

        return codes;
    }
}
