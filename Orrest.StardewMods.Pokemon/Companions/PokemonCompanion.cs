using Microsoft.Xna.Framework;
using Orrest.StardewMods.Pokemon.Abilities;
using Orrest.StardewMods.Pokemon.Core;
using StardewValley;

namespace Orrest.StardewMods.Pokemon.Companions;

/// <summary>
/// A roaming Pokemon companion that lives in the world as an NPC. Modeled on <c>Pet</c> /
/// <c>JunimoHarvester</c>: it doesn't block the player, doesn't destroy objects underfoot, and is
/// driven by a <see cref="RoamAi"/> that periodically picks a new wander target. Each instance
/// carries its own <see cref="Stats"/> (the rolled attributes, e.g. height) and
/// <see cref="Abilities"/> (daily/per-frame effects like watering crops).
/// </summary>
/// <remarks>
/// All world-state-mutating logic (movement, ability effects) is gated on <see cref="Game1.IsMasterGame"/>;
/// non-host clients just animate from synced net fields. This mirrors the vanilla <c>Pet</c>/<c>Junimo</c>
/// convention.
/// </remarks>
public sealed class PokemonCompanion : NPC
{
    /// <summary>The per-individual stats (height, etc.). Persisted via <see cref="SaveData"/>.</summary>
    public PokemonStats Stats { get; }

    /// <summary>The abilities granted to this companion (watering, ...). Per-instance, not shared.</summary>
    public IReadOnlyList<ICompanionAbility> Abilities { get; }

    private readonly RoamAi _roam;

    /// <summary>Construct a companion. Used both at hatch time and when restoring from save.</summary>
    /// <param name="data">The species template.</param>
    /// <param name="stats">The rolled individual stats.</param>
    /// <param name="abilities">The abilities to run (daily/per-frame).</param>
    /// <param name="position">Pixel position (tile * 64).</param>
    /// <param name="name">Display name.</param>
    public PokemonCompanion(PokemonData data, PokemonStats stats, IReadOnlyList<ICompanionAbility> abilities,
        Vector2 position, string name)
        : base(new AnimatedSprite(data.TextureAsset, 0, 16, 16), position, 2, name)
    {
        Stats = stats;
        Abilities = abilities;

        // Don't obstruct the player or trample their stuff — same flags as Pet/Junimo.
        Breather = false;
        willDestroyObjectsUnderfoot = false;
        farmerPassesThrough = true;
        HideShadow = false;

        // Tag this NPC so it can be re-identified after save/load.
        modData[PokemonKeys.CompanionSpecies] = data.Id;

        _roam = new RoamAi(this);
    }

    /// <summary>
    /// Per-frame update. Calls the base NPC update (which advances movement/pathfinding), then the
    /// roam AI, then per-frame abilities. Non-master clients skip the AI/ability logic.
    /// </summary>
    public override void update(GameTime time, GameLocation location)
    {
        base.update(time, location);
        if (!Game1.IsMasterGame) return;

        _roam.Update(time, location);

        foreach (ICompanionAbility ability in Abilities)
            ability.PerFrameUpdate(time, this, location);
    }

    /// <summary>
    /// Run all daily abilities (called from <c>CompanionManager.OnDayStarted</c>). Master-game only.
    /// </summary>
    public void DailyUpdate(GameLocation location)
    {
        if (!Game1.IsMasterGame) return;
        foreach (ICompanionAbility ability in Abilities)
            ability.DailyUpdate(this, location);
    }
}
