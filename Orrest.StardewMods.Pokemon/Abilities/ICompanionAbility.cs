using Microsoft.Xna.Framework;
using Orrest.StardewMods.Pokemon.Companions;
using StardewValley;

namespace Orrest.StardewMods.Pokemon.Abilities;

/// <summary>
/// An effect a companion can apply. Two cadences are supported:
/// <list type="bullet">
///   <item><see cref="DailyUpdate"/>: once per day at <c>DayStarted</c> (e.g. watering the day's crops).</item>
///   <item><see cref="PerFrameUpdate"/>: every animation frame (e.g. particle effects, proximity triggers).</item>
/// </list>
/// Abilities are owned by a single companion instance and read its <see cref="PokemonStats"/>
/// (so a taller Squirtle waters a wider area, etc.).
/// </summary>
public interface ICompanionAbility
{
    /// <summary>Called once per day. Master-game only (the caller guarantees this).</summary>
    /// <param name="companion">The companion owning this ability.</param>
    /// <param name="location">The companion's current location.</param>
    void DailyUpdate(PokemonCompanion companion, GameLocation location);

    /// <summary>Called every frame. Master-game only. No-op by default; override when needed.</summary>
    /// <param name="time">Frame timing.</param>
    /// <param name="companion">The companion owning this ability.</param>
    /// <param name="location">The companion's current location.</param>
    void PerFrameUpdate(GameTime time, PokemonCompanion companion, GameLocation location) { }
}
