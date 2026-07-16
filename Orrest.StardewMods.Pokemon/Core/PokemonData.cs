namespace Orrest.StardewMods.Pokemon.Core;

/// <summary>
/// Describes a Pokemon species (a template). This is species-level, shared data; per-instance
/// values such as the actual rolled height live on <see cref="PokemonStats"/>.
/// </summary>
/// <remarks>
/// Plain immutable class with a validating constructor (rather than a <c>record</c> with
/// <c>required</c> members) so it builds cleanly on the mod's <c>net6.0</c> target without needing
/// C# 11 BCL polyfills. The <see cref="PokemonRegistry"/> holds the canonical set of registered
/// species.
/// </remarks>
public sealed class PokemonData
{
    /// <summary>
    /// The unique, stable species id used as a key in <see cref="PokemonRegistry"/> and stored
    /// in item <c>modData</c>. Example: <c>"Squirtle"</c>.
    /// </summary>
    public string Id { get; }

    /// <summary>The display name shown in messages. Example: <c>"杰尼龟"</c>.</summary>
    public string DisplayName { get; }

    /// <summary>The elemental type, controlling available abilities and hatch conditions.</summary>
    public PokemonType Type { get; }

    /// <summary>
    /// The base body height in meters. A hatched individual rolls
    /// <c>HeightBase ± HeightVariance</c>. Height affects gameplay via
    /// <see cref="PokemonStats.WateringRadius"/> (taller Pokemon water a wider area).
    /// </summary>
    public float HeightBase { get; }

    /// <summary>
    /// The maximum deviation (in meters) from <see cref="HeightBase"/> applied when an
    /// individual is hatched. The rolled height is uniform in
    /// <c>[HeightBase - HeightVariance, HeightBase + HeightVariance]</c>.
    /// </summary>
    public float HeightVariance { get; }

    /// <summary>
    /// The companion sprite-sheet asset name (relative to the game's content tree), e.g.
    /// <c>"Mods\\Orrest.Pokemon\\Squirtle"</c>. Must be a 16x16-per-frame sheet laid out
    /// left-to-right, wrapping at the texture width.
    /// </summary>
    public string TextureAsset { get; }

    /// <summary>
    /// The unqualified item id of this species' egg in <c>Data\\Objects</c>, e.g.
    /// <c>"Orrest.Pokemon_SquirtleEgg"</c>. Used by <c>ChickenProducePatch</c> and the hatch system
    /// to link an egg item to a species.
    /// </summary>
    public string EggObjectId { get; }

    /// <summary>
    /// The message shown (via <c>Game1.showGlobalMessage</c>) when the egg hatches into this species.
    /// The token <c>{Name}</c> is replaced with the species display name by the caller.
    /// </summary>
    public string BirthText { get; }

    /// <summary>Create a fully-specified species template. All fields are required.</summary>
    public PokemonData(
        string id,
        string displayName,
        PokemonType type,
        float heightBase,
        float heightVariance,
        string textureAsset,
        string eggObjectId,
        string birthText)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        Type = type;
        HeightBase = heightBase;
        HeightVariance = heightVariance;
        TextureAsset = textureAsset ?? throw new ArgumentNullException(nameof(textureAsset));
        EggObjectId = eggObjectId ?? throw new ArgumentNullException(nameof(eggObjectId));
        BirthText = birthText ?? throw new ArgumentNullException(nameof(birthText));
    }
}
