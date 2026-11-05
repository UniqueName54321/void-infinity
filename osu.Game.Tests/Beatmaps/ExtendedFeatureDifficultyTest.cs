// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Objects;
using osuTK;

namespace osu.Game.Tests.Beatmaps
{
    [TestFixture]
    public class ExtendedFeatureDifficultyTest
    {
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void TestCalculationDoesNotFlattenCachedGameplayMap(int mode)
        {
            var ruleset = MultitapModesTest.RulesetFor(mode);
            var working = new CachedWorkingBeatmap(MultitapModesTest.CreateMap(mode));
            var playable = working.GetPlayableBeatmap(ruleset.RulesetInfo);
            ruleset.CreateDifficultyCalculator(working).Calculate();
            Assert.That(playable.HitObjects.Count, Is.EqualTo(1));
            Assert.That(playable.HitObjects.Single(), Is.InstanceOf<IHasMultitap>());
            Assert.That(playable.HitObjects.Single().NestedHitObjects.Count, Is.EqualTo(12));
        }

        [TestCase(false)] [TestCase(true)]
        public void TestNativeSliderTrackingRaisesRating(bool movement)
        {
            var uniform = createMap(false, false);
            var extended = createMap(movement, !movement);
            var ruleset = new OsuRuleset();
            var easy = ruleset.CreateDifficultyCalculator(new TestWorkingBeatmap(uniform)).Calculate();
            var hard = ruleset.CreateDifficultyCalculator(new TestWorkingBeatmap(extended)).Calculate();
            Assert.That(extended.HitObjects.OfType<Slider>().Single().Duration,
                Is.EqualTo(uniform.HitObjects.OfType<Slider>().Single().Duration).Within(0.001));
            Assert.That(hard.StarRating, Is.GreaterThan(easy.StarRating));
        }

        [Test]
        public void TestFirstAndOnlyMovingSliderHasTrackingDifficulty()
        {
            var map = createMap(true, false);
            var slider = map.HitObjects.OfType<Slider>().Single();
            map.HitObjects.Clear();
            map.HitObjects.Add(slider);
            var difficulty = new OsuRuleset().CreateDifficultyCalculator(new TestWorkingBeatmap(map)).Calculate();
            Assert.That(difficulty.StarRating, Is.GreaterThan(0));
        }

        private static Beatmap createMap(bool movement, bool variableVelocity)
        {
            var map = new Beatmap();
            map.BeatmapInfo.Ruleset = new OsuRuleset().RulesetInfo;
            var slider = new Slider
            {
                StartTime = 1000, Position = new Vector2(100),
                Path = new SliderPath(new[]
                {
                    new PathControlPoint(Vector2.Zero, PathType.LINEAR),
                    new PathControlPoint(new Vector2(400, 0)),
                }),
            };
            slider.ApplyDefaults(map.ControlPointInfo, map.Difficulty);
            double baseDuration = slider.Duration;
            if (movement) slider.Path.ControlPoints[1].Movement = new Vector2(0, 400);
            if (variableVelocity)
            {
                slider.Path.ControlPoints[1].Velocity = 0.2;
                slider.ApplyDefaults(map.ControlPointInfo, map.Difficulty);
                slider.SliderVelocityMultiplier *= slider.Duration / baseDuration;
            }
            slider.ApplyDefaults(map.ControlPointInfo, map.Difficulty);
            map.HitObjects.Add(new HitCircle { StartTime = 0, Position = new Vector2(100) });
            map.HitObjects.Add(slider);
            map.HitObjects.Add(new HitCircle { StartTime = 5000, Position = new Vector2(100) });
            map.HitObjects.Add(new HitCircle { StartTime = 6000, Position = new Vector2(400, 100) });
            return map;
        }

        private class CachedWorkingBeatmap : TestWorkingBeatmap
        {
            private IBeatmap? cached;
            public CachedWorkingBeatmap(IBeatmap beatmap) : base(beatmap) { }
            public override IBeatmap GetPlayableBeatmap(IRulesetInfo ruleset, IReadOnlyList<Mod> mods, CancellationToken token)
                => cached ??= base.GetPlayableBeatmap(ruleset, mods, token);
        }
    }
}
