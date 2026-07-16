namespace Orrest.StardewMods.Pokemon;

/// <summary>
/// The <c>modData</c> keys used by this mod, all namespaced under the mod's UniqueID to avoid
/// collisions with other mods. Used to tag object instances (eggs) and characters (companions)
/// so the mod can find and identify them across save/load and game updates.
/// </summary>
public static class PokemonKeys
{
    /// <summary>The <c>modData</c> key on an egg <c>Object</c> whose value is the species id.</summary>
    public const string Species = "Orrest.Pokemon/Species";

    /// <summary>The <c>modData</c> key on a companion <c>NPC</c> whose value is the species id.</summary>
    public const string CompanionSpecies = "Orrest.Pokemon/CompanionSpecies";
}
