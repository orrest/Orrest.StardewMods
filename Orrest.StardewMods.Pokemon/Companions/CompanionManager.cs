using Microsoft.Xna.Framework;
using Orrest.StardewMods.Pokemon.Core;
using Orrest.StardewMods.Pokemon.Eggs;
using StardewModdingAPI;
using StardewValley;

namespace Orrest.StardewMods.Pokemon.Companions;

/// <summary>
/// Owns the lifecycle of <see cref="PokemonCompanion"/> instances: spawns them when an egg hatches
/// (subscribes to <see cref="EggManager.Hatched"/>), restores them on save load, runs their daily
/// abilities, and serializes them back to <see cref="SaveData"/> on save.
/// </summary>
public sealed class CompanionManager
{
    private readonly IModHelper _helper;
    private readonly SaveData _save;

    /// <summary>Live companion instances currently in the world, keyed by their save record.</summary>
    private readonly List<PokemonCompanion> _companions = new();

    /// <summary>
    /// The pixel position of a newly-spawned companion, offset slightly from the egg tile so it
    /// doesn't sit exactly under any placed object.
    /// </summary>
    private const float SpawnOffsetPx = 32f;

    public CompanionManager(IModHelper helper, SaveData save, EggManager eggs)
    {
        _helper = helper;
        _save = save;
        eggs.Hatched += OnEggHatched;
    }

    /// <summary>
    /// Egg-hatch handler: create the companion for <paramref name="speciesId"/> at
    /// <paramref name="tile"/> in <paramref name="location"/>, add it to the world, and persist it.
    /// </summary>
    private void OnEggHatched(string speciesId, GameLocation location, Vector2 tile)
    {
        if (!PokemonRegistry.Instance.TryGet(speciesId, out var species))
            return;

        Spawn(species, location, tile * 64f + new Vector2(SpawnOffsetPx), name: species.Data.DisplayName);
    }

    /// <summary>Create a companion of <paramref name="species"/>, place it, and record it.</summary>
    private void Spawn(IPokemonSpecies species, GameLocation location, Vector2 pixelPosition, string name)
    {
        var stats = new PokemonStats(species.Data, Game1.random);
        var companion = new PokemonCompanion(
            species.Data, stats, species.CreateAbilities(), pixelPosition, name);

        location.addCharacter(companion);
        _companions.Add(companion);

        _save.Companions.Add(new SaveData.CompanionRecord
        {
            SpeciesId = species.Data.Id,
            Name = name,
            Height = stats.Height,
            LocationName = location.Name,
            Tile = companion.Tile,
        });
    }

    /// <summary>
    /// On <c>SaveLoaded</c>: rebuild live companions from <see cref="SaveData"/>. The mod loads after
    /// the world, so locations are available here.
    /// </summary>
    public void OnSaveLoaded()
    {
        // World characters don't persist across saves for our custom NPC; rebuild from records.
        _companions.Clear();

        foreach (var record in _save.Companions)
        {
            if (!PokemonRegistry.Instance.TryGet(record.SpeciesId, out var species))
                continue;
            if (Game1.getLocationFromName(record.LocationName) is not GameLocation location)
                continue;

            var stats = new PokemonStats(species.Data, record.Height);
            var companion = new PokemonCompanion(
                species.Data, stats, species.CreateAbilities(),
                record.Tile * 64f + new Vector2(SpawnOffsetPx), record.Name);

            // Avoid duplicates if a record was already re-spawned (defensive).
            location.addCharacter(companion);
            _companions.Add(companion);
        }
    }

    /// <summary>On <c>DayStarted</c>: run each companion's daily abilities. Master-game only.</summary>
    public void OnDayStarted()
    {
        if (!Game1.IsMasterGame) return;

        // Sync positions back into save records in case the companion wandered overnight.
        for (int i = 0; i < _companions.Count; i++)
        {
            var companion = _companions[i];
            if (companion.currentLocation is GameLocation loc)
                companion.DailyUpdate(loc);
        }
    }

    /// <summary>
    /// On <c>Saving</c>: refresh the tile/location of each companion in <see cref="SaveData"/> so
    /// next load places it where it last was. Call after the day update so positions are current.
    /// </summary>
    public void OnSaving()
    {
        // Rebuild records from live instances (positions may have changed since load).
        for (int i = 0; i < _companions.Count && i < _save.Companions.Count; i++)
        {
            var companion = _companions[i];
            var record = _save.Companions[i];
            record.Tile = companion.Tile;
            record.LocationName = companion.currentLocation?.Name ?? record.LocationName;
        }
    }
}
