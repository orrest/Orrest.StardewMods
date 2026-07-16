using Microsoft.Xna.Framework;

namespace Orrest.StardewMods.Pokemon.Core;

/// <summary>
/// The mod's per-save data, persisted via SMAPI's <c>helper.Data.WriteSaveData</c>/
/// <c>ReadSaveData</c>. Tracks hatched companions and in-progress eggs.
/// </summary>
public sealed class SaveData
{
    /// <summary>All currently-existing companions (hatched and spawned into the world).</summary>
    public List<CompanionRecord> Companions { get; set; } = new();

    /// <summary>All placed-but-not-yet-hatched eggs.</summary>
    public List<EggRecord> Eggs { get; set; } = new();

    /// <summary>A persisted companion, in serialize-friendly form.</summary>
    public sealed class CompanionRecord
    {
        /// <summary>Species id (key into <see cref="PokemonRegistry"/>).</summary>
        public string SpeciesId { get; set; } = "";

        /// <summary>The display name the player gave the companion.</summary>
        public string Name { get; set; } = "";

        /// <summary>The rolled body height in meters.</summary>
        public float Height { get; set; }

        /// <summary>The companion's home location name (e.g. <c>"Farm"</c>).</summary>
        public string LocationName { get; set; } = "";

        /// <summary>The companion's last known tile position.</summary>
        public Vector2 Tile { get; set; }
    }

    /// <summary>A persisted egg awaiting enough care to hatch.</summary>
    public sealed class EggRecord
    {
        /// <summary>Species id this egg will hatch into.</summary>
        public string SpeciesId { get; set; } = "";

        /// <summary>The location name where the egg is placed.</summary>
        public string LocationName { get; set; } = "";

        /// <summary>The tile the egg object occupies.</summary>
        public Vector2 Tile { get; set; }

        /// <summary>How many days of care have been applied so far.</summary>
        public int CareProgress { get; set; }

        /// <summary>Whether the egg already received care today (prevents double-counting).</summary>
        public bool CaredToday { get; set; }
    }
}
