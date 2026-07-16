using HarmonyLib;
using StardewValley;

namespace Orrest.StardewMods.Pokemon.Integrations;

/// <summary>
/// Harmony patch: when a chicken-family animal produces an egg, there's a configurable chance the
/// produced egg is replaced with a Squirtle egg (tagged via <see cref="PokemonKeys.Species"/>). This
/// is the primary entry point for obtaining a Squirtle egg without a custom shop.
/// </summary>
/// <remarks>
/// The patch runs as a <see cref="HarmonyPatchType.Postfix"/> on <see cref="FarmAnimal.dayUpdate"/>:
/// after vanilla resolves the day's produce into <see cref="FarmAnimal.currentProduce"/> (a
/// DropOvernight chicken stores today's egg id here — see <c>FarmAnimal.cs</c> ~line 1046), we roll
/// the chance and, on success, swap the produce id to the Squirtle egg and stamp the species tag.
/// The egg is then spawned on the coop floor by vanilla overnight logic carrying that tag.
/// </remarks>
[HarmonyPatch(typeof(FarmAnimal), nameof(FarmAnimal.dayUpdate))]
internal static class ChickenProducePatch
{
    /// <summary>
    /// The per-egg probability of getting a Squirtle egg instead of a normal egg. Set by
    /// <c>ModEntry</c> from <c>ModConfig</c> before <c>PatchAll</c>; defaults to a small chance.
    /// </summary>
    public static double SquirtleEggChance { get; set; } = 0.05;

    /// <summary>The unqualified vanilla egg ids considered "an egg a chicken lays" (incl. large/brown).</summary>
    private static readonly HashSet<string> VanillaEggIds = new()
    {
        "174", // Large Egg (white)
        "176", // Egg (white)
        "180", // Egg (brown)
        "182", // Large Egg (brown)
        "305", // Golden Egg
    };

    /// <summary>Unqualified id of the Squirtle egg, cached at patch time.</summary>
    private const string SquirtleEggId = "Orrest.Pokemon_SquirtleEgg";

    internal static void Postfix(FarmAnimal __instance)
    {
        // Only chickens (coop, egg-laying) should be eligible. We detect this by the produce being a
        // vanilla egg id. Barn animals (milk/wool) won't match, so they're unaffected.
        string? produce = __instance.currentProduce.Value;
        if (produce is null || !VanillaEggIds.Contains(produce))
            return;

        if (Game1.random.NextDouble() >= SquirtleEggChance)
            return;

        // Swap the produce to the Squirtle egg. currentProduce holds the unqualified id (no "(O)").
        __instance.currentProduce.Value = SquirtleEggId;
    }
}
