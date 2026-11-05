// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Testing;
using osu.Framework.Graphics;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Screens.Play;

namespace osu.Game.Tests.Visual.Gameplay
{
    public partial class TestSceneHumanPlay : TestSceneAllRulesetPlayers
    {
        private HumanPlayIndicator marker = null!;
        protected new TestReplayPlayer Player => (TestReplayPlayer)base.Player;

        protected override Player CreatePlayer(Ruleset ruleset)
        {
            var autoplay = (ModAutoplay)ruleset.GetAutoplayMod()!;
            autoplay.HumanPlay.Value = true;
            SelectedMods.Value = new[] { autoplay };
            return new TestReplayPlayer(false);
        }

        protected override void AddCheckSteps()
        {
            AddUntilStep("automation disclosure present", () => Player.ChildrenOfType<HumanPlayIndicator>().Any(i => i.IsPresent));
            AddStep("get disclosure", () => marker = Player.ChildrenOfType<HumanPlayIndicator>().Single());
            AddStep("hide HUD", () => Player.HUDOverlay.ShowHud.Value = false);
            AddAssert("disclosure survives hidden HUD", () => marker.IsPresent && marker.Alpha == 0.95f);
            AddAssert("disclosure is outside HUD", () => !Player.HUDOverlay.ChildrenOfType<HumanPlayIndicator>().Any());
            AddStep("try fading disclosure", () => marker.Hide());
            AddAssert("opacity remains fixed", () => marker.IsPresent && marker.Alpha == 0.95f);
            AddAssert("disclosure in bottom-left corner", () => marker.Anchor == Anchor.BottomLeft && marker.Origin == Anchor.BottomLeft
                                                                      && marker.X == 12 && marker.Y == -12);
            AddAssert("disclosure is compact", () => marker.Width <= 200 && marker.Height <= 32);
        }
    }
}
