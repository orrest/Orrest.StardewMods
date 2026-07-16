using HarmonyLib;
using Orrest.StardewMods.Pokemon.Companions;
using Orrest.StardewMods.Pokemon.Content;
using Orrest.StardewMods.Pokemon.Core;
using Orrest.StardewMods.Pokemon.Eggs;
using Orrest.StardewMods.Pokemon.Integrations;
using Orrest.StardewMods.Pokemon.Pokemon.Squirtle;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace Orrest.StardewMods.Pokemon;

/// <summary>
/// The Pokemon mod entry point. Wires up: species registration, asset injection, Harmony patches,
/// and the SMAPI event pipeline (SaveLoaded / DayStarted / Saving). This is the repo's first mod
/// to use SMAPI events, config, asset editing, and save data — all standard SMAPI patterns.
/// </summary>
public class ModEntry : Mod
{
    private ModConfig _config = null!;
    private SaveData _save = null!;
    private EggManager _eggs = null!;
    private CompanionManager _companions = null!;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        _config = helper.ReadConfig<ModConfig>();
        _save = new SaveData();

        // 1. Register species (must precede asset injection so egg items exist for all species).
        SquirtleModule.Register();

        // 2. Managers (EggManager owns hatching; CompanionManager subscribes to its Hatched event).
        _eggs = new EggManager(helper, _save);
        _companions = new CompanionManager(helper, _save, _eggs);

        // 3. Inject egg items into Data\Objects + provide sprites. Done after registration so every
        //    species' egg object id is known.
        new PokemonAssetEditor(helper).Register();

        // 4. Harmony patches (chicken → Squirtle egg chance).
        ChickenProducePatch.SquirtleEggChance = _config.SquirtleEggChance;
        new Harmony(ModManifest.UniqueID).PatchAll();

        // 5. Event pipeline.
        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.GameLoop.Saving += OnSaving;
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        // Read back per-save data into the shared _save instance (managers hold this same reference,
        // so we must populate it in place rather than reassign).
        SaveData? loaded = Helper.Data.ReadSaveData<SaveData>(ModManifest.UniqueID);
        if (loaded is not null)
        {
            _save.Companions.Clear();
            _save.Companions.AddRange(loaded.Companions);
            _save.Eggs.Clear();
            _save.Eggs.AddRange(loaded.Eggs);
        }

        _companions.OnSaveLoaded();
        _eggs.ReconcilePlacedEggs();
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        // Eggs first (may hatch → spawn companions), then refresh companion state/abilities.
        _eggs.OnDayStarted();
        _companions.OnDayStarted();
    }

    private void OnSaving(object? sender, SavingEventArgs e)
    {
        // Refresh persisted positions before writing.
        _companions.OnSaving();
        Helper.Data.WriteSaveData(ModManifest.UniqueID, _save);
    }
}
