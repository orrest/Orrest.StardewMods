namespace Orrest.StardewMods.Pokemon.Core;

/// <summary>
/// The elemental type of a Pokemon. Determines which abilities and hatch conditions are
/// available to a species (e.g. Water types can water crops). New types can be added here
/// as more species are implemented.
/// </summary>
public enum PokemonType
{
    /// <summary>Not yet assigned. Used as a default/sentinel.</summary>
    None = 0,

    /// <summary>Water type. Can water nearby crops; egg benefits from being near water.</summary>
    Water = 1,

    /// <summary>Fire type. Reserved for future species (e.g. Charmander).</summary>
    Fire = 2,

    /// <summary>Grass type. Reserved for future species (e.g. Bulbasaur).</summary>
    Grass = 3,
}
