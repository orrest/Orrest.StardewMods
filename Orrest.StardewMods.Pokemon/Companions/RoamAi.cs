using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Pathfinding;

namespace Orrest.StardewMods.Pokemon.Companions;

/// <summary>
/// A lightweight wander AI for <see cref="PokemonCompanion"/>. Every few seconds, if the companion
/// is idle (no active <see cref="PathFindController"/>), it picks a random reachable tile within a
/// radius and pathfinds there. This mirrors <c>JunimoHarvester.pathfindToRandomSpotAroundHut</c> but
/// centered on the companion's own position.
/// </summary>
public sealed class RoamAi
{
    private readonly PokemonCompanion _companion;

    /// <summary>Max Chebyshev tile distance of a wander target from the companion.</summary>
    public int WanderRadius { get; set; } = 6;

    /// <summary>Milliseconds to wait idle before picking a new wander target.</summary>
    public int IdleDelayMs { get; set; } = 3000;

    private int _idleTimer;

    public RoamAi(PokemonCompanion companion)
    {
        _companion = companion;
    }

    /// <summary>Advance the AI. Call every frame on the master game.</summary>
    public void Update(GameTime time, GameLocation location)
    {
        // Already walking a path — let the base NPC.update drive it; the controller is cleared on arrival.
        if (_companion.controller != null) return;

        _idleTimer += time.ElapsedGameTime.Milliseconds;
        if (_idleTimer < IdleDelayMs) return;
        _idleTimer = 0;

        Point origin = _companion.TilePoint;
        for (int attempt = 0; attempt < 6; attempt++)
        {
            int dx = Game1.random.Next(-WanderRadius, WanderRadius + 1);
            int dy = Game1.random.Next(-WanderRadius, WanderRadius + 1);
            Point target = new(origin.X + dx, origin.Y + dy);

            if (!IsWalkable(location, target)) continue;

            _companion.controller = new PathFindController(
                _companion, location, target, finalFacingDirection: -1);
            return;
        }
    }

    private static bool IsWalkable(GameLocation location, Point tile)
    {
        if (!location.isTileOnMap(tile.X, tile.Y)) return false;
        // Reuse the game's own collision check used during pathfinding: a tile is reachable iff the
        // companion's bounding box wouldn't collide there.
        // Reuse the game's own collision check. The bounding box mirrors Character.GetBoundingBox's
        // default size so passability matches what the pathfinder itself would consider walkable.
        Microsoft.Xna.Framework.Rectangle box = new(tile.X * 64 + 16, tile.Y * 64 + 16, 16, 16);
        return !location.isCollidingPosition(
            box, Game1.viewport, isFarmer: false, damagesFarmer: 0, glider: false,
            character: null, pathfinding: true);
    }
}
