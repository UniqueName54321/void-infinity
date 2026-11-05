// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Testing;
using osu.Framework.Utils;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Skinning.Default;
using osu.Game.Rulesets.Osu.UI;
using osu.Game.Rulesets.Scoring;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Mods
{
    public partial class TestSceneOsuModAutoplay : OsuModTestScene
    {
        protected override bool AllowFail => true;

        [SetUp]
        public void UseLocalAvatar() => Schedule(() => API.LocalUser.Value.AvatarUrl = "Online/avatar-guest");

        [Test]
        public void TestCursorPositionStoredToJudgement()
        {
            CreateModTest(new ModTestData
            {
                Autoplay = true,
                PassCondition = () =>
                    Player.ScoreProcessor.JudgedHits >= 1
                    && Player.ScoreProcessor.HitEvents.Any(e => e.Position != null)
            });
        }

        [Test]
        public void TestSpmUnaffectedByRateAdjust()
            => runSpmTest(new OsuModDaycore
            {
                SpeedChange = { Value = 0.88 }
            });

        [Test]
        public void TestSpmUnaffectedByTimeRamp()
            => runSpmTest(new ModWindUp
            {
                InitialRate = { Value = 0.7 },
                FinalRate = { Value = 1.3 }
            });

        [TestCase(6.25f)]
        [TestCase(20)]
        public void TestPerfectScoreOnShortSliderWithRepeat(float pathLength)
        {
            AddStep("set score to standardised", () => LocalConfig.SetValue(OsuSetting.ScoreDisplayMode, ScoringMode.Standardised));

            CreateModTest(new ModTestData
            {
                Autoplay = true,
                CreateBeatmap = () => new Beatmap
                {
                    HitObjects = new List<HitObject>
                    {
                        new Slider
                        {
                            StartTime = 500,
                            Position = new Vector2(256, 192),
                            Path = new SliderPath(new[]
                            {
                                new PathControlPoint(),
                                new PathControlPoint(new Vector2(0, pathLength))
                            }),
                            RepeatCount = 1,
                            SliderVelocityMultiplier = 10
                        }
                    }
                },
                PassCondition = () => Player.ScoreProcessor.TotalScore.Value == 1_000_000
            });
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestMultitapAutoplay(bool human)
        {
            var autoplay = new OsuModAutoplay();
            autoplay.HumanPlay.Value = human;
            CreateModTest(new ModTestData
            {
                Autoplay = false,
                Mod = autoplay,
                CreateBeatmap = () => new Beatmap
                {
                    HitObjects = { new MultitapCircle { StartTime = 1000, Position = new Vector2(256, 192), Multitap = new MultitapSettings { Count = 8 } } },
                },
                PassCondition = () => Player.ScoreProcessor.JudgedHits >= 8,
            });
            AddUntilStep("all eight taps judged", () => Player.ScoreProcessor.HitEvents.Count(e => e.HitObject is MultitapHitCircle) == 8);
            AddStep("all eight taps hit", () => Assert.That(Player.ScoreProcessor.HitEvents.Where(e => e.HitObject is MultitapHitCircle).Select(e => e.Result), Is.All.EqualTo(HitResult.Great)));
        }

        [Test]
        public void TestMultitapMissesIndividually()
        {
            CreateModTest(new ModTestData
            {
                Autoplay = false,
                CreateBeatmap = () => new Beatmap
                {
                    HitObjects = { new MultitapCircle { StartTime = 1000, Position = new Vector2(256, 192), Multitap = new MultitapSettings { Count = 4 } } },
                },
                PassCondition = () => Player.ScoreProcessor.HitEvents.Count(e => e.Result == HitResult.Miss && e.HitObject is MultitapHitCircle) == 4,
            });
        }

        [Test]
        public void TestOnePressDoesNotCompleteMultitap()
        {
            CreateModTest(new ModTestData
            {
                Autoplay = false,
                ReplayFrames = new List<osu.Game.Rulesets.Replays.ReplayFrame>
                {
                    new osu.Game.Rulesets.Osu.Replays.OsuReplayFrame(1000, new Vector2(256, 192), OsuAction.LeftButton),
                    new osu.Game.Rulesets.Osu.Replays.OsuReplayFrame(1020, new Vector2(256, 192)),
                    new osu.Game.Rulesets.Osu.Replays.OsuReplayFrame(3000, new Vector2(256, 192)),
                },
                CreateBeatmap = () => new Beatmap
                {
                    HitObjects = { new MultitapCircle { StartTime = 1000, Position = new Vector2(256, 192), Multitap = new MultitapSettings { Count = 4 } } },
                },
                PassCondition = () => Player.ScoreProcessor.JudgedHits >= 4,
            });
            AddUntilStep("four taps judged", () => Player.ScoreProcessor.HitEvents.Count(e => e.HitObject is MultitapHitCircle) == 4);
            AddAssert("one hit and three misses", () => Player.ScoreProcessor.HitEvents.Count(e => e.HitObject is MultitapHitCircle && e.Result.IsHit()) == 1
                                                        && Player.ScoreProcessor.HitEvents.Count(e => e.HitObject is MultitapHitCircle && e.Result == HitResult.Miss) == 3);
        }

        private void runSpmTest(Mod mod)
        {
            SpinnerSpmCalculator? spmCalculator = null;

            CreateModTest(new ModTestData
            {
                Autoplay = true,
                Mod = mod,
                CreateBeatmap = () => new Beatmap
                {
                    HitObjects =
                    {
                        new Spinner
                        {
                            Duration = 6000,
                            Position = OsuPlayfield.BASE_SIZE / 2,
                        }
                    }
                },
                PassCondition = () => Player.ScoreProcessor.JudgedHits >= 1
            });

            AddUntilStep("fetch SPM calculator", () =>
            {
                spmCalculator = this.ChildrenOfType<SpinnerSpmCalculator>().SingleOrDefault();
                return spmCalculator != null;
            });

            AddUntilStep("SPM is correct", () => Precision.AlmostEquals(spmCalculator.AsNonNull().Result.Value, 477, 5));
        }
    }
}
