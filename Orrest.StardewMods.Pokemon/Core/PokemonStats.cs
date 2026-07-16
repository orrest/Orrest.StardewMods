namespace Orrest.StardewMods.Pokemon.Core;

/// <summary>
/// Per-individual Pokemon attributes. The MVP only models <see cref="Height"/>, but this is a
/// dedicated value-object so additional attributes (weight, friendship, level, ...) can be added
/// without changing call sites.
/// </summary>
public sealed class PokemonStats
{
    /// <summary>
    /// The species template these stats belong to. Gameplay-relevant derived values are computed
    /// from the combination of <see cref="Data"/> and the rolled attribute values below.
    /// </summary>
    public PokemonData Data { get; }

    /// <summary>
    /// The individual's body height in meters, rolled at hatch time within
    /// <c>[Data.HeightBase - Data.HeightVariance, Data.HeightBase + Data.HeightVariance]</c>.
    /// </summary>
    public float Height { get; set; }

    /// <summary>
    /// The companion's crop-watering radius in tiles (Chebyshev distance). Grows with height:
    /// every <paramref name="heightStep"/> meters above the base adds one tile. This is the
    /// primary way <see cref="Height"/> affects gameplay.
    /// </summary>
    /// <param name="baseRadius">The minimum radius (even a small individual waters this many tiles).</param>
    /// <param name="heightStep">Meters of height needed to gain one extra tile of radius.</param>
    /// <returns>The watering radius, in whole tiles.</returns>
    public int WateringRadius(int baseRadius, float heightStep)
        => baseRadius + (int)Math.Floor((Height - Data.HeightBase) / heightStep);

    /// <summary>Create stats for <paramref name="data"/> with an explicit height (e.g. from save).</summary>
    public PokemonStats(PokemonData data, float height)
    {
        Data = data;
        Height = height;
    }

    /// <summary>
    /// Create stats for <paramref name="data"/>, rolling the individual height within the species'
    /// configured range using <paramref name="random"/>.
    /// </summary>
    public PokemonStats(PokemonData data, Random random)
        : this(data, RollHeight(data, random))
    {
    }

    private static float RollHeight(PokemonData data, Random random)
    {
        float delta = (float)((random.NextDouble() * 2.0 - 1.0) * data.HeightVariance);
        return Math.Max(data.HeightBase + delta, 0.1f);
    }
}
