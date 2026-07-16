namespace Orrest.StardewMods.Pokemon;

/// <summary>
/// The mod's user-facing configuration, loaded via <c>helper.ReadConfig&lt;ModConfig&gt;()</c>.
/// This is the repo's first config class; it's a plain POCO with XML-documented tunables.
/// </summary>
public class ModConfig
{
    /// <summary>
    /// The chance (0–1) that any egg laid by a chicken-family animal is replaced with a Squirtle
    /// egg. Default <c>0.05</c> (1 in 20).
    /// </summary>
    public double SquirtleEggChance { get; set; } = 0.05;

    /// <summary>
    /// Whether companion roaming is enabled. When <c>false</c>, hatched companions stay still.
    /// Useful for performance or if the player finds wandering distracting.
    /// </summary>
    public bool EnableRoaming { get; set; } = true;
}
