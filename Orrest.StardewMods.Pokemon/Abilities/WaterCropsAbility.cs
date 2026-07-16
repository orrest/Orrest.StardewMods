using Microsoft.Xna.Framework;
using Orrest.StardewMods.Pokemon.Companions;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace Orrest.StardewMods.Pokemon.Abilities;

/// <summary>
/// A water-type ability: each morning, water every dry <see cref="HoeDirt"/> crop tile within a
/// Chebyshev radius of the companion. The radius scales with the companion's height via
/// <see cref="PokemonStats.WateringRadius"/>, so taller individuals cover more tiles — this is the
/// primary gameplay effect of the "height" attribute.
/// </summary>
/// <remarks>
/// Watering is done by setting <c>HoeDirt.state.Value = 1</c>, exactly the path the watering can
/// (<c>HoeDirt.performToolAction</c>) and the spouse helper (<c>NPC.cs</c> ~line 6462) use. Splash
/// sprites and a sound are emitted for feedback. Master-game only.
/// </remarks>
public sealed class WaterCropsAbility : ICompanionAbility
{
    /// <summary>The minimum radius in tiles, regardless of height.</summary>
    public int BaseRadius { get; init; } = 1;

    /// <summary>Meters of height (above the species base) needed to add one tile of radius.</summary>
    public float HeightStep { get; init; } = 0.1f;

    /// <summary>Whether to spawn the water splash + sound for feedback.</summary>
    public bool ShowEffects { get; init; } = true;

    /// <inheritdoc />
    public void DailyUpdate(PokemonCompanion companion, GameLocation location)
    {
        int radius = companion.Stats.WateringRadius(BaseRadius, HeightStep);
        Vector2 origin = companion.Tile;
        bool anyWatered = false;

        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                Vector2 t = new(origin.X + dx, origin.Y + dy);
                if (!location.terrainFeatures.TryGetValue(t, out TerrainFeature? tf))
                    continue;
                if (tf is not HoeDirt dirt || !dirt.needsWatering() || dirt.isWatered())
                    continue;

                dirt.state.Value = 1;
                anyWatered = true;

                if (ShowEffects)
                {
                    // Water-splash sprite (asset id 13), same id the watering can emits.
                    Game1.Multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(
                        13, new Vector2(t.X * 64f, t.Y * 64f), Color.White, animationLength: 8,
                        flipped: Game1.random.NextDouble() < 0.5, animationInterval: 70f));
                }
            }
        }

        if (ShowEffects && anyWatered)
            location.playSound("wateringCan");
    }

    /// <inheritdoc />
    /// <remarks>Watering is daily only; no per-frame work.</remarks>
    public void PerFrameUpdate(GameTime time, PokemonCompanion companion, GameLocation location)
    {
        // Intentionally empty: watering happens once per day in DailyUpdate.
    }
}
