using Microsoft.Xna.Framework.Graphics;
using Orrest.StardewMods.Pokemon.Core;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData.Objects;

namespace Orrest.StardewMods.Pokemon.Content;

/// <summary>
/// Injects the mod's egg items into <c>Data\\Objects</c> and (via the mod's
/// <c>helper.Events.Content.AssetRequested</c> subscription) provides their sprites. Each registered
/// species contributes one egg item keyed by <see cref="PokemonData.EggObjectId"/>.
/// </summary>
/// <remarks>
/// This is the mod's first use of asset editing (the repo's Fishing mod is Harmony-only), so it
/// follows the modern SMAPI 1.6 <c>AssetRequested</c> pattern: edit <c>Data\\Objects</c> in-place by
/// adding our entries, and serve the egg spritesheet from the mod's <c>assets/</c> folder.
/// </remarks>
public sealed class PokemonAssetEditor
{
    /// <summary>The egg spritesheet asset name the game will request for our egg items.</summary>
    public const string EggTextureAsset = @"Mods\Orrest.Pokemon\EggSprites";

    private readonly IModHelper _helper;

    public PokemonAssetEditor(IModHelper helper)
    {
        _helper = helper;
    }

    /// <summary>
    /// Wire the editor. Call from <c>ModEntry.Entry</c>. Subscribes to <c>AssetRequested</c>; the
    /// game lazily loads <c>Data\\Objects</c> on first access (during world load), so our edits are
    /// picked up without an explicit cache invalidation.
    /// </summary>
    public void Register()
    {
        _helper.Events.Content.AssetRequested += OnAssetRequested;
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.Name.IsEquivalentTo("Data\\Objects"))
        {
            e.Edit(EditObjects, AssetEditPriority.Default);
        }
        else if (e.Name.IsEquivalentTo(EggTextureAsset))
        {
            // Provide the egg spritesheet from the mod's assets folder. "assets/EggSprites.png"
            // holds one 16x16 frame per species egg, indexed by SpriteIndex (currently just 0).
            e.LoadFromModFile<Texture2D>("assets/EggSprites.png", AssetLoadPriority.Exclusive);
        }
        else
        {
            // Companion sprite sheets: each species' TextureAsset (e.g. "Mods\Orrest.Pokemon\Squirtle")
            // is served from "assets/<file>.png" derived from the asset name's last segment.
            foreach (IPokemonSpecies species in PokemonRegistry.Instance.All)
            {
                if (e.Name.IsEquivalentTo(species.Data.TextureAsset))
                {
                    string file = System.IO.Path.GetFileName(species.Data.TextureAsset) + ".png";
                    e.LoadFromModFile<Texture2D>("assets/" + file, AssetLoadPriority.Exclusive);
                    break;
                }
            }
        }
    }

    private void EditObjects(IAssetData asset)
    {
        var data = asset.GetData<Dictionary<string, ObjectData>>();

        foreach (IPokemonSpecies species in PokemonRegistry.Instance.All)
        {
            // Skip if another mod or a previous load already added this id.
            if (data.ContainsKey(species.Data.EggObjectId))
                continue;

            data[species.Data.EggObjectId] = new ObjectData
            {
                Name = species.Data.EggObjectId,
                DisplayName = $"{species.Data.DisplayName}的蛋",
                Description = "一枚散发着淡淡水汽的蛋。把它放在水边或每天浇灌周围的土地，它就有可能孵化。",
                Category = StardewValley.Object.EggCategory, // -5, so it groups with eggs
                Price = 200,
                Texture = EggTextureAsset,
                SpriteIndex = 0,
                Edibility = -300, // not edible
            };
        }
    }
}
