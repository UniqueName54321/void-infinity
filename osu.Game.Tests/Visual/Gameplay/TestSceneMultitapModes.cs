// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Graphics.Sprites;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Scoring;
using osu.Game.Screens.Play;
using osu.Game.Tests.Beatmaps;

namespace osu.Game.Tests.Visual.Gameplay
{
    [TestFixture(false, false)] [TestFixture(true, false)] [TestFixture(false, true)] [TestFixture(true, true)]
    public partial class TestSceneMultitapModes : TestSceneAllRulesetPlayers
    {
        private readonly bool noMultis;
        private readonly bool hidden;
        public TestSceneMultitapModes(bool noMultis, bool hidden)
        {
            this.noMultis = noMultis;
            this.hidden = hidden;
        }

        [SetUp]
        public void UseLocalAvatar() => Schedule(() => API.LocalUser.Value.AvatarUrl = "Online/avatar-guest");
        protected new TestReplayPlayer Player => (TestReplayPlayer)base.Player;
        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset) => MultitapModesTest.CreateMap(ruleset.OnlineID, 8);
        protected override Player CreatePlayer(Ruleset ruleset)
        {
            var mods = new System.Collections.Generic.List<Mod> { ruleset.GetAutoplayMod()! };
            if (noMultis) mods.Add(new ModNoMultis());
            if (hidden) mods.Add(ruleset.CreateMod<ModHidden>()!);
            SelectedMods.Value = mods.ToArray();
            return new TestReplayPlayer(false);
        }
        protected override void AddCheckSteps()
        {
            AddUntilStep("one native group retained", () => Player.ChildrenOfType<DrawableHitObject>().Any(d => d.HitObject is IHasMultitap) != noMultis);
            if (!noMultis)
                AddAssert("counter respects visibility mods", () => Player.ChildrenOfType<MultitapCounter>().Any()
                    && Player.ChildrenOfType<MultitapCounter>().All(c => c.Alpha == (hidden ? 0 : 1)));
            AddUntilStep("all taps judged", () => Player.ScoreProcessor.JudgedHits >= 8);
            AddAssert("no missed taps", () => !Player.ScoreProcessor.HitEvents.Any(e => e.Result == HitResult.Miss));
        }
    }
}
