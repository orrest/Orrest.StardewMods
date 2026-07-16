using Orrest.StardewMods.Pokemon.Abilities;
using Orrest.StardewMods.Pokemon.Eggs;

namespace Orrest.StardewMods.Pokemon.Core;

/// <summary>
/// Implemented by a "species module" (e.g. <c>SquirtleModule</c>) to register a single Pokemon
/// species with the mod: its data template, the hatch condition for its egg, and the abilities
/// granted to its companion. New species are added by implementing this and calling
/// <see cref="PokemonRegistry.Register(IPokemonSpecies)"/> during <c>ModEntry.Entry</c>.
/// </summary>
public interface IPokemonSpecies
{
    /// <summary>The species template (id, type, height, textures, egg id, ...).</summary>
    PokemonData Data { get; }

    /// <summary>
    /// The hatch condition applied to eggs of this species, or <c>null</c> to fall back to the
    /// default (time-based) condition. Water species typically return a
    /// <c>DailyWateringHatchCondition</c>.
    /// </summary>
    IEggHatchCondition? HatchCondition { get; }

    /// <summary>
    /// Factory for the companion abilities granted to a freshly hatched individual of this species.
    /// Returns a new list each call so instances don't share mutable state. May be empty.
    /// </summary>
    IReadOnlyList<ICompanionAbility> CreateAbilities();
}
