using HarmonyLib;
using StardewModdingAPI;

namespace Orrest.StardewMods.Fishing;

public class ModEntry : Mod
{
    public override void Entry(IModHelper helper)
    {
        var harmony = new Harmony(ModManifest.UniqueID);
        harmony.PatchAll();
    }
}
