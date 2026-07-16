# assets/

These PNGs are **placeholder art** — solid-color fills generated so the mod builds and loads.
Replace them with real sprites before release.

## Files

### `Squirtle.png` — companion sprite sheet
- Used by `PokemonCompanion` via `AnimatedSprite` with **16×16** frames.
- Layout (matches `AnimatedSprite.GetSourceRect`): frames laid out left-to-right, wrapping at the
  texture width. For a standard 4-direction walk cycle use a **64×16** sheet (4 frames × 16px):
  - Row: `down` (frames 0–3), with subsequent rows `right`, `up`, `left` as the sheet grows
    (e.g. a full 64×64 sheet = 4 rows × 4 frames).
- For the MVP single-frame placeholder it's 16×16; replace with at least a 64×16 (4 frames, down)
  or 64×64 (all four directions).

### `EggSprites.png` — egg item icon
- 16×16 single frame, indexed by `SpriteIndex` (currently `0`) in the injected `Data\Objects`
  entry for the Squirtle egg (`Orrest.Pokemon_SquirtleEgg`).
- This is the icon shown in the inventory / on the ground. One 16×16 frame per species egg,
  laid out left-to-right if more species are added.

## Replacing
Just drop new PNGs with the same filenames here. No code changes needed — the asset editor
(`PokemonAssetEditor`) serves them by path.
