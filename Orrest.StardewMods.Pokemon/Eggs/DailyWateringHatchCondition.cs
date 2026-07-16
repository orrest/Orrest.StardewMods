using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace Orrest.StardewMods.Pokemon.Eggs;

/// <summary>
/// A water-type egg hatch condition. An egg receives a day of care if either:
/// <list type="bullet">
///   <item>It is placed within <see cref="WaterProximity"/> tiles of a water tile (the egg "stays
///   cool"), OR</item>
///   <item>At least one of the dirt tiles adjacent to the egg is watered (the player "watered the
///   egg").</item>
/// </list>
/// This mirrors the rice-paddy <c>paddyWaterCheck</c> idea but re-scanned daily for our egg.
/// </summary>
public sealed class DailyWateringHatchCondition : IEggHatchCondition
{
    /// <summary>
    /// The Chebyshev radius (in tiles) around the egg within which a water tile counts as "near
    /// water". The vanilla paddy check uses a 3x3 ring; we use the same idea.
    /// </summary>
    public int WaterProximity { get; init; } = 1;

    /// <inheritdoc />
    public int RequiredCareDays { get; init; } = 3;

    /// <inheritdoc />
    public bool ReceivedCare(GameLocation location, Vector2 tile)
    {
        int tx = (int)tile.X;
        int ty = (int)tile.Y;

        // (a) Near water: scan a square ring within WaterProximity.
        for (int dx = -WaterProximity; dx <= WaterProximity; dx++)
        {
            for (int dy = -WaterProximity; dy <= WaterProximity; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                if (location.isTileOnMap(tx + dx, ty + dy) && location.isWaterTile(tx + dx, ty + dy))
                    return true;
            }
        }

        // (b) Adjacent watered dirt: the player watered around the egg yesterday.
        for (int dx = -WaterProximity; dx <= WaterProximity; dx++)
        {
            for (int dy = -WaterProximity; dy <= WaterProximity; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                Vector2 neighbor = new(tx + dx, ty + dy);
                if (location.terrainFeatures.TryGetValue(neighbor, out TerrainFeature? tf)
                    && tf is HoeDirt dirt
                    && dirt.isWatered())
                {
                    return true;
                }
            }
        }

        return false;
    }
}
