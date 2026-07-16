using Microsoft.Xna.Framework;
using StardewValley;

namespace Orrest.StardewMods.Pokemon.Eggs;

/// <summary>
/// Defines the daily-care rule for hatching an egg of a given species. The MVP "place + daily
/// care" hatching loop is: each day at <c>DayStarted</c>, the manager asks the condition whether
/// the egg received care; if so, <see cref="ApplyDailyCare"/> advances progress; when progress
/// reaches the threshold the egg hatches. Different species can implement different care rules
/// (water types: near water or watered; fire types: kept warm; ...).
/// </summary>
public interface IEggHatchCondition
{
    /// <summary>
    /// Whether the egg at <paramref name="tile"/> in <paramref name="location"/> received the
    /// required care on this day. Called once per day at <c>DayStarted</c>.
    /// </summary>
    /// <param name="location">The location the egg object is in.</param>
    /// <param name="tile">The tile the egg object occupies.</param>
    /// <returns><c>true</c> if the day counts toward hatching.</returns>
    bool ReceivedCare(GameLocation location, Vector2 tile);

    /// <summary>The number of care-days required to hatch.</summary>
    int RequiredCareDays { get; }
}
