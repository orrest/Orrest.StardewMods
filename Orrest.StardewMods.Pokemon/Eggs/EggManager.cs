using Microsoft.Xna.Framework;
using Orrest.StardewMods.Pokemon.Core;
using StardewModdingAPI;
using StardewValley;

namespace Orrest.StardewMods.Pokemon.Eggs;

/// <summary>
/// Owns the "place + daily care" hatching loop. Tracks placed eggs in <see cref="SaveData"/>,
/// evaluates their <see cref="IEggHatchCondition"/> each day at <c>DayStarted</c>, and — when an
/// egg's progress reaches the threshold — removes the egg object and raises <see cref="Hatched"/>
/// so the <see cref="Companions.CompanionManager"/> can spawn the hatched companion.
/// </summary>
public sealed class EggManager
{
    private readonly IModHelper _helper;
    private readonly SaveData _save;

    /// <summary>
    /// Raised when an egg hatches. Subscribed to by <c>CompanionManager</c> to spawn the companion.
    /// Decoupled via an event so the Eggs layer has no compile-time dependency on Companions.
    /// </summary>
    /// <param name="speciesId">The species id that hatched.</param>
    /// <param name="location">The location the egg was in (where the companion should spawn).</param>
    /// <param name="tile">The tile the egg occupied.</param>
    public delegate void HatchedHandler(string speciesId, GameLocation location, Vector2 tile);
    public event HatchedHandler? Hatched;

    public EggManager(IModHelper helper, SaveData save)
    {
        _helper = helper;
        _save = save;
    }

    /// <summary>
    /// Scan all active locations for placed Pokemon egg objects that we don't yet track, and
    /// register them in <see cref="SaveData.Eggs"/>. An egg is recognized if EITHER:
    /// <list type="bullet">
    ///   <item>it carries the <see cref="PokemonKeys.Species"/> modData tag (set when the mod spawns
    ///   the egg), OR</item>
    ///   <item>its object id matches a registered species' egg id (e.g. a vanilla chicken laid a
    ///   Squirtle egg via <c>ChickenProducePatch</c> and it was spawned on the coop floor without a
    ///   tag).</item>
    /// </list>
    /// Safe to call repeatedly; idempotent per (location, tile). Called on <c>SaveLoaded</c> and at
    /// the start of each <c>DayStarted</c>.
    /// </summary>
    public void ReconcilePlacedEggs()
    {
        var known = new HashSet<(string loc, Vector2 tile)>();
        foreach (var egg in _save.Eggs)
            known.Add((egg.LocationName, egg.Tile));

        foreach (GameLocation location in _helper.Multiplayer.GetActiveLocations())
        {
            foreach (var (tile, obj) in location.objects.Pairs)
            {
                // Resolve species from the modData tag first, else from the egg object id.
                string? speciesId = null;
                if (obj.modData.TryGetValue(PokemonKeys.Species, out string? tagged))
                    speciesId = tagged;
                else if (PokemonRegistry.Instance.TryGetByEgg(obj.ItemId, out var byId))
                    speciesId = byId.Data.Id;

                if (speciesId is null
                    || known.Contains((location.Name, tile))
                    || !PokemonRegistry.Instance.TryGet(speciesId, out _))
                {
                    continue;
                }

                // Backfill the species tag if missing so the egg survives a save/load round-trip and
                // stays recognizable even if its object id later changes form.
                obj.modData[PokemonKeys.Species] = speciesId;

                _save.Eggs.Add(new SaveData.EggRecord
                {
                    SpeciesId = speciesId,
                    LocationName = location.Name,
                    Tile = tile,
                    CareProgress = 0,
                    CaredToday = false,
                });
                known.Add((location.Name, tile));
            }
        }
    }

    /// <summary>
    /// Apply one day of care to every tracked egg and hatch those that have reached the threshold.
    /// Called on <c>GameLoop.DayStarted</c>. No-ops on non-master clients (egg state is host-owned).
    /// </summary>
    public void OnDayStarted()
    {
        if (!Game1.IsMasterGame) return;

        ReconcilePlacedEggs();

        for (int i = _save.Eggs.Count - 1; i >= 0; i--)
        {
            var record = _save.Eggs[i];
            record.CaredToday = false;

            if (Game1.getLocationFromName(record.LocationName) is not GameLocation location
                || !PokemonRegistry.Instance.TryGet(record.SpeciesId, out var species)
                || !location.objects.TryGetValue(record.Tile, out var eggObj)
                || !eggObj.modData.TryGetValue(PokemonKeys.Species, out string? tagged)
                || tagged != record.SpeciesId)
            {
                // Egg or its location is gone — drop the record.
                _save.Eggs.RemoveAt(i);
                continue;
            }

            IEggHatchCondition condition = species.HatchCondition ?? DefaultCondition.Instance;
            if (condition.ReceivedCare(location, record.Tile))
            {
                record.CareProgress++;
                record.CaredToday = true;
            }

            if (record.CareProgress >= condition.RequiredCareDays)
            {
                Hatch(record, species, location);
                _save.Eggs.RemoveAt(i);
            }
        }
    }

    private void Hatch(SaveData.EggRecord record, IPokemonSpecies species, GameLocation location)
    {
        location.objects.Remove(record.Tile);

        string name = species.Data.DisplayName;
        Game1.showGlobalMessage(species.Data.BirthText.Replace("{Name}", name));

        Hatched?.Invoke(record.SpeciesId, location, record.Tile);
    }

    /// <summary>
    /// The fallback hatch condition when a species doesn't define one: time-only, counts every day.
    /// </summary>
    private sealed class DefaultCondition : IEggHatchCondition
    {
        public static readonly DefaultCondition Instance = new();
        public int RequiredCareDays => 3;
        public bool ReceivedCare(GameLocation location, Vector2 tile) => true;
        private DefaultCondition() { }
    }
}
