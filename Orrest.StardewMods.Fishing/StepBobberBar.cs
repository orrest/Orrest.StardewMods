using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Input;
using Orrest.StardewMods.Common.Constants;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Extensions;
using StardewValley.Menus;
using StardewValley.Tools;

namespace Orrest.StardewMods.Fishing;

/// <summary>
/// A drop-in replacement for <see cref="BobberBar"/>. Instead of the vanilla
/// accelerate-as-you-hold model, the green bar moves a fixed number of pixels each frame
/// (a step model): it cruises at a constant, slower speed with a chunky "step-function"
/// feel, and sticks to the top/bottom edge instead of rebounding. See
/// <see cref="StepSpeed"/> and <see cref="UpdateStepBarMovement"/>.
/// </summary>
public class StepBobberBar : BobberBar
{
    /// <summary>
    /// Backing field access for the private <c>sparkleText</c> field on <see cref="BobberBar"/>,
    /// which the copied <see cref="update(GameTime)"/> body needs to read and clear.
    /// </summary>
    private static readonly FieldInfo SparkleTextField =
        AccessTools.Field(typeof(BobberBar), "sparkleText")
        ?? throw new InvalidOperationException("Could not find BobberBar.sparkleText field.");

    private SparklingText? SparkleText
    {
        get => (SparklingText?)SparkleTextField.GetValue(this);
        set => SparkleTextField.SetValue(this, value);
    }

    // ── Step model (this mod's only deviation from vanilla) ─────────────────────────
    // Tunables for the fixed-per-frame bar movement. See UpdateStepBarMovement.

    /// <summary>
    /// Per-frame movement of the green bar, in pixels. The bar uses a <b>step model</b>:
    /// it moves exactly <see cref="StepSpeed"/> pixels each frame (toward the button's
    /// direction), with NO acceleration carried between frames. That yields a constant,
    /// slow cruising speed (≈ <c>StepSpeed * 60</c> px/s over the track) with a chunky,
    /// "step-function" feel. Bump this up for bigger/livelier steps.
    /// </summary>
    private const float StepSpeed = 3.0f;

    /// <summary>Step is scaled by this while the fish is inside the bar (easier when centered).</summary>
    private const float StepScaleInBar = 0.6f;

    /// <summary>Extra-tight step scale while the fish is in the bar AND a Barbed Hook is equipped.</summary>
    private const float StepScaleBarbedHook = 0.3f;

    /// <summary>Per-frame nudge toward the fish's half of the bar, primary Barbed Hook.</summary>
    private const float BarbedHookNudgePrimary = 0.2f;

    /// <summary>Per-frame nudge toward the fish's half of the bar, each additional stacked Barbed Hook.</summary>
    private const float BarbedHookNudgeStacked = 0.05f;

    // ── Vanilla constants (copied from the decompiled BobberBar.update) ─────────────
    // These are the game's own tuning values, surfaced as named constants for readability.
    // On a game update, re-diff against the official decompiled BobberBar.update.

    /// <summary>Track geometry (vanilla): full height of the bar area, in px.</summary>
    private const float BarAreaHeight = 568f;

    /// <summary>Track geometry (vanilla): height of the bobber's travel range, in px.</summary>
    private const float TrackHeight = 548f;

    /// <summary>Track geometry (vanilla): upper clamp for bobberPosition in UpdateFishMovement, in px.</summary>
    private const float BobberClampMax = 532f;

    /// <summary>Catch progress (vanilla): gained per frame while the fish is in the bar.</summary>
    private const float CatchGainPerFrame = 0.002f;

    /// <summary>Catch progress (vanilla): lost per frame when out of the bar (normal rod).</summary>
    private const float CatchLossNormal = 0.003f;

    /// <summary>Catch progress (vanilla): lost per frame when out of the bar (beginners rod).</summary>
    private const float CatchLossBeginnersRod = 0.002f;

    /// <summary>Catch progress (vanilla): minimum loss floor for the Trap Bobber stacking reduction.</summary>
    private const float CatchLossMin = 0.001f;

    /// <summary>Treasure (vanilla): scale grow/shrink step per frame.</summary>
    private const float TreasureScaleStep = 0.1f;

    /// <summary>Treasure (vanilla): catch level gained per frame while the treasure is in the bar.</summary>
    private const float TreasureCatchPerFrame = 0.0135f;

    /// <summary>Treasure (vanilla): catch level decay per frame when not in the bar.</summary>
    private const float TreasureCatchDecay = 0.01f;

    /// <summary>Animation (vanilla): per-frame scale change during fade in/out.</summary>
    private const float FadeScaleStep = 0.05f;

    /// <summary>Animation (vanilla): shake duration (ms) applied on escape and on catch.</summary>
    private const float OutcomeShakeMs = 500f;

    /// <summary>Fish (vanilla): reset value (ms) for the fish-size-reduction timer.</summary>
    private const int FishSizeReductionTimerReset = 800;

    public StepBobberBar(
        string whichFish,
        float fishSize,
        bool treasure,
        List<string> bobbers,
        string setFlagOnCatch,
        bool isBossFish,
        string baitID = "",
        bool goldenTreasure = false
    )
        : base(
            whichFish,
            fishSize,
            treasure,
            bobbers,
            setFlagOnCatch,
            isBossFish,
            baitID,
            goldenTreasure
        ) { }

    /// <summary>
    /// Per-frame update. This is a near-verbatim copy of the vanilla
    /// <see cref="BobberBar.update(GameTime)"/> body, but with the bar's movement replaced
    /// by the step model (see <see cref="UpdateStepBarMovement"/>). The original single
    /// method is split into focused private helpers below to make the only deviation from
    /// the vanilla flow — the step model — easy to spot. Behavior is unchanged.
    /// </summary>
    public override void update(GameTime time)
    {
        Reposition();
        UpdateSparkleText(time);
        UpdateEverythingShake(time);

        if (fadeIn)
        {
            UpdateFadeIn();
        }
        else if (fadeOut)
        {
            // Vanilla returns out of the whole method (skipping the bobber-position clamp
            // at the end) while a shake/sparkle is still in progress. UpdateFadeOut returns
            // true in that case so we preserve that early return.
            if (UpdateFadeOut(time))
            {
                return;
            }
        }
        else
        {
            UpdateGameplay(time);
        }

        if (bobberPosition < 0f)
        {
            bobberPosition = 0f;
        }
        if (bobberPosition > TrackHeight)
        {
            bobberPosition = TrackHeight;
        }
    }

    /// <summary>
    /// Advances the catch result <see cref="SparklingText"/> and clears it once finished.
    /// </summary>
    private void UpdateSparkleText(GameTime time)
    {
        if (SparkleText != null && SparkleText.update(time))
        {
            SparkleText = null;
        }
    }

    /// <summary>
    /// Drives the screen-wide shake applied on escape/catch.
    /// </summary>
    private void UpdateEverythingShake(GameTime time)
    {
        if (everythingShakeTimer > 0f)
        {
            everythingShakeTimer -= time.ElapsedGameTime.Milliseconds;
            everythingShake = new Vector2(
                (float)Game1.random.Next(-10, 11) / 10f,
                (float)Game1.random.Next(-10, 11) / 10f
            );
            if (everythingShakeTimer <= 0f)
            {
                everythingShake = Vector2.Zero;
            }
        }
    }

    /// <summary>
    /// Handles the open-in animation (scale up to 1).
    /// </summary>
    private void UpdateFadeIn()
    {
        scale += FadeScaleStep;
        if (scale >= 1f)
        {
            scale = 1f;
            fadeIn = false;
        }
    }

    /// <summary>
    /// Handles the close-out animation (scale down to 0), then resolves the catch: pulls
    /// the fish or finishes fishing and exits the menu. Returns <see langword="true"/> when
    /// the frame should bail out early because a shake/sparkle is still playing — in that
    /// case the caller must <c>return</c> from <see cref="update"/> without running the
    /// trailing bobber-position clamp (matches vanilla behavior).
    /// </summary>
    private bool UpdateFadeOut(GameTime time)
    {
        if (everythingShakeTimer > 0f || SparkleText != null)
        {
            return true;
        }
        scale -= FadeScaleStep;
        if (scale <= 0f)
        {
            scale = 0f;
            fadeOut = false;
            FishingRod? rod = Game1.player.CurrentTool as FishingRod;
            string? baitId = rod?.GetBait()?.QualifiedItemId;
            int numCaught = (
                (
                    bossFish
                    || !(baitId == Bait.WildBait)
                    || !(Game1.random.NextDouble() < 0.25 + Game1.player.DailyLuck / 2.0)
                )
                    ? 1
                    : 2
            );
            if (challengeBaitFishes > 0)
            {
                numCaught = challengeBaitFishes;
            }
            if (distanceFromCatching > 0.9f && rod != null)
            {
                rod.pullFishFromWater(
                    whichFish,
                    fishSize,
                    fishQuality,
                    (int)difficulty,
                    treasureCaught,
                    perfect,
                    fromFishPond,
                    setFlagOnCatch,
                    bossFish,
                    numCaught
                );
            }
            else
            {
                Game1.player.completelyStopAnimatingOrDoingAction();
                rod?.doneFishing(Game1.player, consumeBaitAndTackle: true);
            }
            Game1.exitActiveMenu();
            Game1.setRichPresence("location", Game1.currentLocation.Name);
        }
        return false;
    }

    /// <summary>
    /// The active gameplay branch: runs while the minigame is neither fading in nor out.
    /// Moves the fish, detects whether it is inside the bar, samples input, moves the bar
    /// via the step model, updates treasure, advances catch progress, and resolves the
    /// escape/win outcomes. All sub-steps are kept in vanilla order.
    /// </summary>
    private void UpdateGameplay(GameTime time)
    {
        UpdateFishMovement();
        DetectBobberInBar();
        UpdateButtonPress();

        UpdateStepBarMovement();

        bool treasureInBar = UpdateTreasure(time);

        UpdateCatchProgress(time, treasureInBar);
        distanceFromCatching = Math.Max(0f, Math.Min(1f, distanceFromCatching));
        if (Game1.player.CurrentTool != null)
        {
            Game1.player.CurrentTool.tickUpdate(time, Game1.player);
        }
        ResolveCatchResult();
    }

    /// <summary>
    /// Vanilla fish/bobber AI: random retargeting, motion-type drift, acceleration toward
    /// the target, and clamping the bobber to the track. Unchanged from the decompiled
    /// <see cref="BobberBar"/>.
    /// </summary>
    private void UpdateFishMovement()
    {
        if (
            Game1.random.NextDouble()
                < (double)(difficulty * (float)((motionType != 2) ? 1 : 20) / 4000f)
            && (motionType != 2 || bobberTargetPosition == -1f)
        )
        {
            float spaceBelow = TrackHeight - bobberPosition;
            float spaceAbove = bobberPosition;
            float percent = Math.Min(99f, difficulty + (float)Game1.random.Next(10, 45)) / 100f;
            bobberTargetPosition =
                bobberPosition
                + (float)
                    Game1.random.Next((int)Math.Min(0f - spaceAbove, spaceBelow), (int)spaceBelow)
                    * percent;
        }
        switch (motionType)
        {
            case 4:
                floaterSinkerAcceleration = Math.Max(floaterSinkerAcceleration - 0.01f, -1.5f);
                break;
            case 3:
                floaterSinkerAcceleration = Math.Min(floaterSinkerAcceleration + 0.01f, 1.5f);
                break;
        }
        if (Math.Abs(bobberPosition - bobberTargetPosition) > 3f && bobberTargetPosition != -1f)
        {
            bobberAcceleration =
                (bobberTargetPosition - bobberPosition)
                / ((float)Game1.random.Next(10, 30) + (100f - Math.Min(100f, difficulty)));
            bobberSpeed += (bobberAcceleration - bobberSpeed) / 5f;
        }
        else if (motionType != 2 && Game1.random.NextDouble() < (double)(difficulty / 2000f))
        {
            bobberTargetPosition =
                bobberPosition
                + (float)(
                    Game1.random.NextBool()
                        ? Game1.random.Next(-100, -51)
                        : Game1.random.Next(50, 101)
                );
        }
        else
        {
            bobberTargetPosition = -1f;
        }
        if (motionType == 1 && Game1.random.NextDouble() < (double)(difficulty / 1000f))
        {
            bobberTargetPosition =
                bobberPosition
                + (float)(
                    Game1.random.NextBool()
                        ? Game1.random.Next(-100 - (int)difficulty * 2, -51)
                        : Game1.random.Next(50, 101 + (int)difficulty * 2)
                );
        }
        bobberTargetPosition = Math.Max(-1f, Math.Min(bobberTargetPosition, TrackHeight));
        bobberPosition += bobberSpeed + floaterSinkerAcceleration;
        if (bobberPosition > BobberClampMax)
        {
            bobberPosition = BobberClampMax;
        }
        else if (bobberPosition < 0f)
        {
            bobberPosition = 0f;
        }
    }

    /// <summary>
    /// Sets <see cref="bobberInBar"/>: true when the fish lies within the bar's span, or
    /// when both are pinned to the bottom of the track. Vanilla hit-test logic.
    /// </summary>
    private void DetectBobberInBar()
    {
        bobberInBar =
            bobberPosition + 12f <= bobberBarPos - 32f + (float)bobberBarHeight
            && bobberPosition - 16f >= bobberBarPos - 32f;
        if (
            bobberPosition >= (float)(548 - bobberBarHeight)
            && bobberBarPos >= (float)(568 - bobberBarHeight - 4)
        )
        {
            bobberInBar = true;
        }
    }

    /// <summary>
    /// Samples the hold-the-fish button (mouse left, tool key, or gamepad X/A), stores it in
    /// <see cref="buttonPressed"/>, and plays the press sound on a fresh press.
    /// </summary>
    private void UpdateButtonPress()
    {
        bool wasPressed = buttonPressed;
        buttonPressed =
            Game1.oldMouseState.LeftButton == ButtonState.Pressed
            || Game1.isOneOfTheseKeysDown(Game1.oldKBState, Game1.options.useToolButton)
            || (
                Game1.options.gamepadControls
                && (
                    Game1.oldPadState.IsButtonDown(Buttons.X)
                    || Game1.oldPadState.IsButtonDown(Buttons.A)
                )
            );
        if (!wasPressed && buttonPressed)
        {
            Game1.playSound("fishingRodBend");
        }
    }

    /// <summary>
    /// STEP MODEL — the only deviation from the vanilla <see cref="BobberBar"/>. The vanilla
    /// game adds <c>gravity</c> to <c>bobberBarSpeed</c> every frame, so the bar keeps
    /// accelerating (gets faster the longer you hold), which felt too fast/loose. Instead we
    /// move the bar by a FIXED step each frame (<see cref="StepSpeed"/> px), so it cruises at
    /// a constant, slower speed with a chunky step-function feel.
    /// <para>
    /// The step direction follows the button (held = up, released = down). While the fish is
    /// inside the bar we shrink the step (matches the vanilla "easier when centered" feel),
    /// and a Barbed Hook (<c>(O)691</c>) tracks the fish even more tightly. At the track edges
    /// the bar sticks instead of rebounding, unlike vanilla.
    /// </para>
    /// <para>
    /// <b>Note:</b> the original code comments mislabeled <c>(O)691</c> as "Cork bobber", but
    /// the official ID <c>(O)691</c> is the <b>Barbed Hook</b> (the real Cork Bobber is
    /// <c>(O)695</c>, which is not handled here). The behavior is unchanged — only the
    /// comments and the constant name were corrected.
    /// </para>
    /// </summary>
    private void UpdateStepBarMovement()
    {
        float step = buttonPressed ? -StepSpeed : StepSpeed;
        float oldPos = bobberBarPos;

        if (bobberInBar)
        {
            bool hasBarbedHook = bobbers.Contains(Tackle.BarbedHook);
            step *= (hasBarbedHook ? StepScaleBarbedHook : StepScaleInBar);
            if (hasBarbedHook)
            {
                for (int i = 0; i < Utility.getStringCountInList(bobbers, Tackle.BarbedHook); i++)
                {
                    // Barbed Hook: nudge the bar toward the fish's half each frame.
                    if (bobberPosition + 16f < bobberBarPos + (float)(bobberBarHeight / 2))
                    {
                        bobberBarPos -= (i > 0) ? BarbedHookNudgeStacked : BarbedHookNudgePrimary;
                    }
                    else
                    {
                        bobberBarPos += (i > 0) ? BarbedHookNudgeStacked : BarbedHookNudgePrimary;
                    }
                }
            }
        }

        // Apply the fixed step. Keep bobberBarSpeed in sync with actual movement so the
        // reel-rotation / fish-shake logic downstream still behaves naturally.
        bobberBarSpeed = step;
        bobberBarPos += step;

        if (bobberBarPos + (float)bobberBarHeight > BarAreaHeight)
        {
            bobberBarPos = BarAreaHeight - bobberBarHeight;
            // Stick at the bottom edge instead of rebounding.
            bobberBarSpeed = 0f;
            if (oldPos + (float)bobberBarHeight < BarAreaHeight)
            {
                Game1.playSound("shiny4");
            }
        }
        else if (bobberBarPos < 0f)
        {
            bobberBarPos = 0f;
            // Stick at the top edge instead of rebounding.
            bobberBarSpeed = 0f;
            if (oldPos > 0f)
            {
                Game1.playSound("shiny4");
            }
        }
    }

    /// <summary>
    /// Advances treasure appearance and capture when treasure is enabled. Returns whether the
    /// treasure currently lies within the bar this frame (false when treasure is disabled or
    /// not yet visible), which the catch-progress logic needs.
    /// </summary>
    private bool UpdateTreasure(GameTime time)
    {
        if (!treasure)
        {
            return false;
        }
        bool treasureInBar = false;
        float oldTreasureAppearTimer = treasureAppearTimer;
        treasureAppearTimer -= time.ElapsedGameTime.Milliseconds;
        if (treasureAppearTimer <= 0f)
        {
            if (treasureScale < 1f && !treasureCaught)
            {
                if (oldTreasureAppearTimer > 0f)
                {
                    if (bobberBarPos > 274f)
                    {
                        treasurePosition = Game1.random.Next(8, (int)bobberBarPos - 20);
                    }
                    else
                    {
                        int min = Math.Min(528, (int)bobberBarPos + bobberBarHeight);
                        int max = 500;
                        treasurePosition = ((min > max) ? (max - 1) : Game1.random.Next(min, max));
                    }
                    Game1.playSound("dwop");
                }
                treasureScale = Math.Min(1f, treasureScale + TreasureScaleStep);
            }
            treasureInBar =
                treasurePosition + 12f <= bobberBarPos - 32f + (float)bobberBarHeight
                && treasurePosition - 16f >= bobberBarPos - 32f;
            if (treasureInBar && !treasureCaught)
            {
                treasureCatchLevel += TreasureCatchPerFrame;
                treasureShake = new Vector2(Game1.random.Next(-2, 3), Game1.random.Next(-2, 3));
                if (treasureCatchLevel >= 1f)
                {
                    Game1.playSound("newArtifact");
                    treasureCaught = true;
                }
            }
            else if (treasureCaught)
            {
                treasureScale = Math.Max(0f, treasureScale - TreasureScaleStep);
            }
            else
            {
                treasureShake = Vector2.Zero;
                treasureCatchLevel = Math.Max(0f, treasureCatchLevel - TreasureCatchDecay);
            }
        }
        return treasureInBar;
    }

    /// <summary>
    /// Advances catch progress: gains while the fish is in the bar (reeling up, fish
    /// shake, rumble), loses when out of the bar — unless the treasure is still being
    /// collected with a Treasure Hunter (<c>(O)693</c>). Vanilla progression, including
    /// challenge bait, beginners rod, and Trap Bobber (<c>(O)694</c>) handling.
    /// </summary>
    private void UpdateCatchProgress(GameTime time, bool treasureInBar)
    {
        if (bobberInBar)
        {
            distanceFromCatching += CatchGainPerFrame;
            reelRotation += (float)Math.PI / 8f;
            fishShake.X = (float)Game1.random.Next(-10, 11) / 10f;
            fishShake.Y = (float)Game1.random.Next(-10, 11) / 10f;
            barShake = Vector2.Zero;
            Rumble.rumble(0.1f, 1000f);
            unReelSound?.Stop(AudioStopOptions.Immediate);
            if (
                reelSound == null
                || reelSound.IsStopped
                || reelSound.IsStopping
                || !reelSound.IsPlaying
            )
            {
                Game1.playSound(Sound.FastReel, out reelSound);
            }
        }
        else if (!treasureInBar || treasureCaught || !bobbers.Contains(Tackle.TreasureHunter))
        {
            if (!fishShake.Equals(Vector2.Zero))
            {
                Game1.playSound("tinyWhip");
                perfect = false;
                Rumble.stopRumbling();
                if (challengeBaitFishes > 0)
                {
                    challengeBaitFishes--;
                    if (challengeBaitFishes <= 0)
                    {
                        distanceFromCatching = 0f;
                    }
                }
            }
            fishSizeReductionTimer -= time.ElapsedGameTime.Milliseconds;
            if (fishSizeReductionTimer <= 0)
            {
                fishSize = Math.Max(minFishSize, fishSize - 1);
                fishSizeReductionTimer = FishSizeReductionTimerReset;
            }
            if (
                (Game1.player.fishCaught != null && Game1.player.fishCaught.Length != 0)
                || Game1.currentMinigame != null
            )
            {
                if (bobbers.Contains(Tackle.TrapBobber))
                {
                    // Trap Bobber: each stacked tackle halves the per-frame catch loss.
                    // Base 0.003, subtract 0.001 then 0.0005 … floored at 0.001.
                    float reduction = 0.003f;
                    float amount = 0.001f;
                    for (
                        int i = 0;
                        i < Utility.getStringCountInList(bobbers, Tackle.TrapBobber);
                        i++
                    )
                    {
                        reduction -= amount;
                        amount /= 2f;
                    }
                    reduction = Math.Max(0.001f, reduction);
                    distanceFromCatching -= reduction * distanceFromCatchPenaltyModifier;
                }
                else
                {
                    distanceFromCatching -=
                        (beginnersRod ? CatchLossBeginnersRod : CatchLossNormal)
                        * distanceFromCatchPenaltyModifier;
                }
            }
            float distanceAway = Math.Abs(
                bobberPosition - (bobberBarPos + (float)(bobberBarHeight / 2))
            );
            reelRotation -= (float)Math.PI / Math.Max(10f, 200f - distanceAway);
            barShake.X = (float)Game1.random.Next(-10, 11) / 10f;
            barShake.Y = (float)Game1.random.Next(-10, 11) / 10f;
            fishShake = Vector2.Zero;
            reelSound?.Stop(AudioStopOptions.Immediate);
            if (unReelSound == null || unReelSound.IsStopped)
            {
                Game1.playSound("slowReel", 600, out unReelSound);
            }
        }
    }

    /// <summary>
    /// Resolves the terminal outcomes: fish escapes when progress hits 0, or the catch jingle
    /// and (perfect) sparkle text when it hits 1. Sets <see cref="fadeOut"/> to begin the
    /// close-out animation. Vanilla terminal logic.
    /// </summary>
    private void ResolveCatchResult()
    {
        if (distanceFromCatching <= 0f || distanceFromCatching >= 1f)
        {
            fadeOut = true;
            everythingShakeTimer = OutcomeShakeMs;
            handledFishResult = true;
            unReelSound?.Stop(AudioStopOptions.Immediate);
            reelSound?.Stop(AudioStopOptions.Immediate);
        }

        if (distanceFromCatching <= 0f)
        {
            Game1.playSound(Sound.FishEscape);
        }
        else if (distanceFromCatching >= 1f)
        {
            Game1.playSound(Sound.Jingle1);
            if (perfect)
            {
                SparkleText = new SparklingText(
                    Game1.dialogueFont,
                    Game1.content.LoadString(Text.BobberBarPerfect),
                    Color.Yellow,
                    Color.White,
                    rainbow: false,
                    sparkleFrequency: 0.1,
                    millisecondsDuration: 1500
                );
                if (Game1.isFestival())
                {
                    Game1.CurrentEvent.perfectFishing();
                }
            }
            else if (fishSize == maxFishSize)
            {
                fishSize--;
            }
        }
    }
}
