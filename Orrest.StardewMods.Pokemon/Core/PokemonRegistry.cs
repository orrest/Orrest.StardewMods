using Orrest.StardewMods.Pokemon.Abilities;
using Orrest.StardewMods.Pokemon.Eggs;

namespace Orrest.StardewMods.Pokemon.Core;

/// <summary>
/// The runtime catalog of registered Pokemon species. Looked up by species id (from item
/// <c>modData</c> or save data) and by egg item id (to resolve which species an egg item belongs to).
/// </summary>
public sealed class PokemonRegistry
{
    private readonly Dictionary<string, IPokemonSpecies> _bySpeciesId = new();
    private readonly Dictionary<string, IPokemonSpecies> _byEggObjectId = new();

    /// <summary>The singleton instance owned by <c>ModEntry</c>.</summary>
    public static PokemonRegistry Instance { get; } = new();

    private PokemonRegistry() { }

    /// <summary>Register a species module. Throws if the species id or egg id is already taken.</summary>
    public void Register(IPokemonSpecies species)
    {
        if (!_bySpeciesId.TryAdd(species.Data.Id, species))
            throw new InvalidOperationException($"Pokemon species '{species.Data.Id}' is already registered.");

        if (!_byEggObjectId.TryAdd(species.Data.EggObjectId, species))
            throw new InvalidOperationException(
                $"Egg object id '{species.Data.EggObjectId}' is already registered to species " +
                $"'{_byEggObjectId[species.Data.EggObjectId].Data.Id}'.");
    }

    /// <summary>Try to get a species by its id. Returns false for unknown ids.</summary>
    public bool TryGet(string speciesId, out IPokemonSpecies species)
        => _bySpeciesId.TryGetValue(speciesId, out species!);

    /// <summary>Try to resolve a species from an egg's unqualified object id.</summary>
    public bool TryGetByEgg(string eggObjectId, out IPokemonSpecies species)
        => _byEggObjectId.TryGetValue(eggObjectId, out species!);

    /// <summary>All registered species modules, for asset injection of every egg at once.</summary>
    public IEnumerable<IPokemonSpecies> All => _bySpeciesId.Values;
}
