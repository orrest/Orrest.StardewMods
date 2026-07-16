using Orrest.StardewMods.Pokemon.Abilities;
using Orrest.StardewMods.Pokemon.Core;
using Orrest.StardewMods.Pokemon.Eggs;

namespace Orrest.StardewMods.Pokemon.Pokemon.Squirtle;

/// <summary>
/// Registration module for the Squirtle species: the first (and only, for MVP) concrete Pokemon.
/// Encapsulates everything species-specific — the data template, the water-themed hatch condition,
/// and the water-crops ability — so <c>ModEntry</c> just calls <see cref="Register"/>.
/// </summary>
/// <remarks>
/// Adding a new species means writing an analogous module (e.g. <c>CharmanderModule</c>) and calling
/// its <c>Register</c> from <c>ModEntry</c>. The Core/Abilities/Eggs layers require no changes.
/// </remarks>
public static class SquirtleModule
{
    /// <summary>The species id, stable across versions; stored in item <c>modData</c> and saves.</summary>
    public const string SpeciesId = "Squirtle";

    /// <summary>The unqualified egg object id injected into <c>Data\Objects</c>.</summary>
    public const string EggObjectId = "Orrest.Pokemon_SquirtleEgg";

    /// <summary>
    /// The species template. Canon height ~0.5m with ±0.15m variance, so a tall individual can reach
    /// a noticeably wider watering radius (each 0.1m above base adds a tile via WaterCropsAbility).
    /// </summary>
    public static PokemonData Data { get; } = new(
        id: SpeciesId,
        displayName: "杰尼龟",
        type: PokemonType.Water,
        heightBase: 0.5f,
        heightVariance: 0.15f,
        textureAsset: @"Mods\Orrest.Pokemon\Squirtle",
        eggObjectId: EggObjectId,
        birthText: "一只 {Name} 从蛋里孵化出来了！它似乎愿意帮你浇地。");

    /// <summary>
    /// The water hatch condition: 3 days of being near water or having the surrounding dirt watered.
    /// </summary>
    public static IEggHatchCondition HatchCondition { get; } = new DailyWateringHatchCondition
    {
        WaterProximity = 1,
        RequiredCareDays = 3,
    };

    /// <summary>
    /// Register Squirtle with the global <see cref="PokemonRegistry"/>. Call once during
    /// <c>ModEntry.Entry</c>, before any save/world logic runs.
    /// </summary>
    public static void Register()
    {
        PokemonRegistry.Instance.Register(new SpeciesModule());
    }

    /// <summary>The <see cref="IPokemonSpecies"/> adapter bundling Squirtle's data + condition + ability.</summary>
    private sealed class SpeciesModule : IPokemonSpecies
    {
        public PokemonData Data => SquirtleModule.Data;
        public IEggHatchCondition? HatchCondition => SquirtleModule.HatchCondition;

        public IReadOnlyList<ICompanionAbility> CreateAbilities()
            => new ICompanionAbility[]
            {
                new WaterCropsAbility { BaseRadius = 1, HeightStep = 0.1f, ShowEffects = true },
            };
    }
}
