using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Input;
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
/// feel, and sticks to the top/bottom edge instead of rebounding. See <see cref="StepSpeed"/>.
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

    /// <summary>
    /// Per-frame movement of the green bar, in pixels. The bar uses a <b>step model</b>:
    /// it moves exactly <see cref="StepSpeed"/> pixels each frame (toward the button's
    /// direction), with NO acceleration carried between frames. That yields a constant,
    /// slow cruising speed (≈ <c>StepSpeed * 60</c> px/s over a 568 px track) with a
    /// chunky, "step-function" feel. Bump this up for bigger/livelier steps.
    /// </summary>
    private const float StepSpeed = 2.0f;

    public StepBobberBar(string whichFish, float fishSize, bool treasure, List<string> bobbers, string setFlagOnCatch, bool isBossFish, string baitID = "", bool goldenTreasure = false)
        : base(whichFish, fishSize, treasure, bobbers, setFlagOnCatch, isBossFish, baitID, goldenTreasure)
    {
    }

    public override void update(GameTime time)
    {
        Reposition();
        if (SparkleText != null && SparkleText.update(time))
        {
            SparkleText = null;
        }
        if (everythingShakeTimer > 0f)
        {
            everythingShakeTimer -= time.ElapsedGameTime.Milliseconds;
            everythingShake = new Vector2((float)Game1.random.Next(-10, 11) / 10f, (float)Game1.random.Next(-10, 11) / 10f);
            if (everythingShakeTimer <= 0f)
            {
                everythingShake = Vector2.Zero;
            }
        }
        if (fadeIn)
        {
            scale += 0.05f;
            if (scale >= 1f)
            {
                scale = 1f;
                fadeIn = false;
            }
        }
        else if (fadeOut)
        {
            if (everythingShakeTimer > 0f || SparkleText != null)
            {
                return;
            }
            scale -= 0.05f;
            if (scale <= 0f)
            {
                scale = 0f;
                fadeOut = false;
                FishingRod? rod = Game1.player.CurrentTool as FishingRod;
                string? baitId = rod?.GetBait()?.QualifiedItemId;
                int numCaught = ((bossFish || !(baitId == "(O)774") || !(Game1.random.NextDouble() < 0.25 + Game1.player.DailyLuck / 2.0)) ? 1 : 2);
                if (challengeBaitFishes > 0)
                {
                    numCaught = challengeBaitFishes;
                }
                if (distanceFromCatching > 0.9f && rod != null)
                {
                    rod.pullFishFromWater(whichFish, fishSize, fishQuality, (int)difficulty, treasureCaught, perfect, fromFishPond, setFlagOnCatch, bossFish, numCaught);
                }
                else
                {
                    Game1.player.completelyStopAnimatingOrDoingAction();
                    rod?.doneFishing(Game1.player, consumeBaitAndTackle: true);
                }
                Game1.exitActiveMenu();
                Game1.setRichPresence("location", Game1.currentLocation.Name);
            }
        }
        else
        {
            if (Game1.random.NextDouble() < (double)(difficulty * (float)((motionType != 2) ? 1 : 20) / 4000f) && (motionType != 2 || bobberTargetPosition == -1f))
            {
                float spaceBelow = 548f - bobberPosition;
                float spaceAbove = bobberPosition;
                float percent = Math.Min(99f, difficulty + (float)Game1.random.Next(10, 45)) / 100f;
                bobberTargetPosition = bobberPosition + (float)Game1.random.Next((int)Math.Min(0f - spaceAbove, spaceBelow), (int)spaceBelow) * percent;
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
                bobberAcceleration = (bobberTargetPosition - bobberPosition) / ((float)Game1.random.Next(10, 30) + (100f - Math.Min(100f, difficulty)));
                bobberSpeed += (bobberAcceleration - bobberSpeed) / 5f;
            }
            else if (motionType != 2 && Game1.random.NextDouble() < (double)(difficulty / 2000f))
            {
                bobberTargetPosition = bobberPosition + (float)(Game1.random.NextBool() ? Game1.random.Next(-100, -51) : Game1.random.Next(50, 101));
            }
            else
            {
                bobberTargetPosition = -1f;
            }
            if (motionType == 1 && Game1.random.NextDouble() < (double)(difficulty / 1000f))
            {
                bobberTargetPosition = bobberPosition + (float)(Game1.random.NextBool() ? Game1.random.Next(-100 - (int)difficulty * 2, -51) : Game1.random.Next(50, 101 + (int)difficulty * 2));
            }
            bobberTargetPosition = Math.Max(-1f, Math.Min(bobberTargetPosition, 548f));
            bobberPosition += bobberSpeed + floaterSinkerAcceleration;
            if (bobberPosition > 532f)
            {
                bobberPosition = 532f;
            }
            else if (bobberPosition < 0f)
            {
                bobberPosition = 0f;
            }
            bobberInBar = bobberPosition + 12f <= bobberBarPos - 32f + (float)bobberBarHeight && bobberPosition - 16f >= bobberBarPos - 32f;
            if (bobberPosition >= (float)(548 - bobberBarHeight) && bobberBarPos >= (float)(568 - bobberBarHeight - 4))
            {
                bobberInBar = true;
            }
            bool num = buttonPressed;
            buttonPressed = Game1.oldMouseState.LeftButton == ButtonState.Pressed || Game1.isOneOfTheseKeysDown(Game1.oldKBState, Game1.options.useToolButton) || (Game1.options.gamepadControls && (Game1.oldPadState.IsButtonDown(Buttons.X) || Game1.oldPadState.IsButtonDown(Buttons.A)));
            if (!num && buttonPressed)
            {
                Game1.playSound("fishingRodBend");
            }
            // CHANGE A/B — STEP MODEL. The vanilla game adds `gravity` to `bobberBarSpeed`
            // every frame, so the bar keeps accelerating (gets faster the longer you hold),
            // which felt too fast/loose. Instead we move the bar by a FIXED step each frame,
            // so it cruises at a constant, slower speed with a chunky step-function feel.
            //
            // The step direction follows the button (held = up, released = down). While the
            // fish is inside the bar we shrink the step (matches the vanilla "easier when
            // centered" feel, and cork bobber "(O)691" tracks the fish even more tightly).
            float step = buttonPressed ? -StepSpeed : StepSpeed;
            float oldPos = bobberBarPos;

            if (bobberInBar)
            {
                step *= (bobbers.Contains("(O)691") ? 0.3f : 0.6f);
                if (bobbers.Contains("(O)691"))
                {
                    for (int i = 0; i < Utility.getStringCountInList(bobbers, "(O)691"); i++)
                    {
                        // Cork bobber: nudge the bar toward the fish's half each frame.
                        if (bobberPosition + 16f < bobberBarPos + (float)(bobberBarHeight / 2))
                        {
                            bobberBarPos -= (i > 0) ? 0.05f : 0.2f;
                        }
                        else
                        {
                            bobberBarPos += (i > 0) ? 0.05f : 0.2f;
                        }
                    }
                }
            }

            // Apply the fixed step. Keep bobberBarSpeed in sync with actual movement so the
            // reel-rotation / fish-shake logic downstream still behaves naturally.
            bobberBarSpeed = step;
            bobberBarPos += step;

            if (bobberBarPos + (float)bobberBarHeight > 568f)
            {
                bobberBarPos = 568 - bobberBarHeight;
                // Stick at the bottom edge instead of rebounding.
                bobberBarSpeed = 0f;
                if (oldPos + (float)bobberBarHeight < 568f)
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
            bool treasureInBar = false;
            if (treasure)
            {
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
                        treasureScale = Math.Min(1f, treasureScale + 0.1f);
                    }
                    treasureInBar = treasurePosition + 12f <= bobberBarPos - 32f + (float)bobberBarHeight && treasurePosition - 16f >= bobberBarPos - 32f;
                    if (treasureInBar && !treasureCaught)
                    {
                        treasureCatchLevel += 0.0135f;
                        treasureShake = new Vector2(Game1.random.Next(-2, 3), Game1.random.Next(-2, 3));
                        if (treasureCatchLevel >= 1f)
                        {
                            Game1.playSound("newArtifact");
                            treasureCaught = true;
                        }
                    }
                    else if (treasureCaught)
                    {
                        treasureScale = Math.Max(0f, treasureScale - 0.1f);
                    }
                    else
                    {
                        treasureShake = Vector2.Zero;
                        treasureCatchLevel = Math.Max(0f, treasureCatchLevel - 0.01f);
                    }
                }
            }
            if (bobberInBar)
            {
                distanceFromCatching += 0.002f;
                reelRotation += (float)Math.PI / 8f;
                fishShake.X = (float)Game1.random.Next(-10, 11) / 10f;
                fishShake.Y = (float)Game1.random.Next(-10, 11) / 10f;
                barShake = Vector2.Zero;
                Rumble.rumble(0.1f, 1000f);
                unReelSound?.Stop(AudioStopOptions.Immediate);
                if (reelSound == null || reelSound.IsStopped || reelSound.IsStopping || !reelSound.IsPlaying)
                {
                    Game1.playSound("fastReel", out reelSound);
                }
            }
            else if (!treasureInBar || treasureCaught || !bobbers.Contains("(O)693"))
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
                    fishSizeReductionTimer = 800;
                }
                if ((Game1.player.fishCaught != null && Game1.player.fishCaught.Length != 0) || Game1.currentMinigame != null)
                {
                    if (bobbers.Contains("(O)694"))
                    {
                        float reduction = 0.003f;
                        float amount = 0.001f;
                        for (int i = 0; i < Utility.getStringCountInList(bobbers, "(O)694"); i++)
                        {
                            reduction -= amount;
                            amount /= 2f;
                        }
                        reduction = Math.Max(0.001f, reduction);
                        distanceFromCatching -= reduction * distanceFromCatchPenaltyModifier;
                    }
                    else
                    {
                        distanceFromCatching -= (beginnersRod ? 0.002f : 0.003f) * distanceFromCatchPenaltyModifier;
                    }
                }
                float distanceAway = Math.Abs(bobberPosition - (bobberBarPos + (float)(bobberBarHeight / 2)));
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
            distanceFromCatching = Math.Max(0f, Math.Min(1f, distanceFromCatching));
            if (Game1.player.CurrentTool != null)
            {
                Game1.player.CurrentTool.tickUpdate(time, Game1.player);
            }
            if (distanceFromCatching <= 0f)
            {
                fadeOut = true;
                everythingShakeTimer = 500f;
                Game1.playSound("fishEscape");
                handledFishResult = true;
                unReelSound?.Stop(AudioStopOptions.Immediate);
                reelSound?.Stop(AudioStopOptions.Immediate);
            }
            else if (distanceFromCatching >= 1f)
            {
                everythingShakeTimer = 500f;
                Game1.playSound("jingle1");
                fadeOut = true;
                handledFishResult = true;
                unReelSound?.Stop(AudioStopOptions.Immediate);
                reelSound?.Stop(AudioStopOptions.Immediate);
                if (perfect)
                {
                    SparkleText = new SparklingText(Game1.dialogueFont, Game1.content.LoadString("Strings\\UI:BobberBar_Perfect"), Color.Yellow, Color.White, rainbow: false, 0.1, 1500);
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
        if (bobberPosition < 0f)
        {
            bobberPosition = 0f;
        }
        if (bobberPosition > 548f)
        {
            bobberPosition = 548f;
        }
    }
}
