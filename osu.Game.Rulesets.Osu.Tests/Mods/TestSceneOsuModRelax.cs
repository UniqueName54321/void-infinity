// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Testing;
using osu.Game.Rulesets.Scoring;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Replays;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Replays;
using osu.Game.Rulesets.Osu.Scoring;
using osu.Game.Rulesets.Replays;
using osu.Game.Scoring;
using osu.Game.Tests.Visual;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Mods
{
    public partial class TestSceneOsuModRelax : OsuModTestScene
    {
        protected override TestPlayer CreateModPlayer(Ruleset ruleset) => new ModRelaxTestPlayer(CurrentTestData, AllowFail);

        [SetUp]
        public void UseLocalAvatar() => Schedule(() => API.LocalUser.Value.AvatarUrl = "Online/avatar-guest");

        [Test]
        public void TestMultitapRelax()
        {
            CreateModTest(new ModTestData
            {
                Mod = new OsuModRelax(), Autoplay = false,
                CreateBeatmap = () => new Beatmap
                {
                    HitObjects = { new MultitapCircle { StartTime = 1000, Position = new Vector2(100), Multitap = new MultitapSettings { Count = 8 } } },
                },
                ReplayFrames = new List<ReplayFrame>
                {
                    new OsuReplayFrame(0, new Vector2(100)),
                    new OsuReplayFrame(4000, new Vector2(100)),
                },
                PassCondition = () => Player.ScoreProcessor.JudgedHits >= 8,
            });
            AddUntilStep("eight taps judged", () => Player.ScoreProcessor.JudgedHits >= 8);
            AddStep("all taps hit", () =>
            {
                var results = Player.ScoreProcessor.HitEvents.Where(e => e.HitObject is MultitapHitCircle).Select(e => e.Result).ToArray();
                Assert.That(results.Length, Is.EqualTo(8));
                Assert.That(results, Is.All.EqualTo(HitResult.Great));
            });
        }

        [Test]
        public void TestRelax() => CreateModTest(new ModTestData
        {
            Mod = new OsuModRelax(),
            Autoplay = false,
            CreateBeatmap = () => new Beatmap
            {
                Difficulty = { OverallDifficulty = 9 },
                HitObjects = new List<HitObject>
                {
                    new HitCircle
                    {
                        StartTime = 1000,
                        Position = new Vector2(100, 100),
                        HitWindows = new OsuHitWindows()
                    }
                }
            },
            ReplayFrames = new List<ReplayFrame>
            {
                new OsuReplayFrame(0, new Vector2()),
                new OsuReplayFrame(100, new Vector2(100)),
            },
            PassCondition = () => Player.ScoreProcessor.Combo.Value == 1
        });

        [Test]
        public void TestRelaxLeniency() => CreateModTest(new ModTestData
        {
            Mod = new OsuModRelax(),
            Autoplay = false,
            CreateBeatmap = () => new Beatmap
            {
                Difficulty = { OverallDifficulty = 9 },
                HitObjects = new List<HitObject>
                {
                    new HitCircle
                    {
                        StartTime = 1000,
                        Position = new Vector2(100, 100),
                        HitWindows = new OsuHitWindows()
                    }
                }
            },
            ReplayFrames = new List<ReplayFrame>
            {
                new OsuReplayFrame(0, new Vector2(78, 78)), // must be an edge hit for the cursor to not stay on the object for too long
                new OsuReplayFrame(1000 - OsuModRelax.RELAX_LENIENCY, new Vector2(78, 78)),
                new OsuReplayFrame(1000, new Vector2(0)),
            },
            PassCondition = () => Player.ScoreProcessor.Combo.Value == 1
        });

        protected partial class ModRelaxTestPlayer : ModTestPlayer
        {
            private readonly ModTestData currentTestData;

            public ModRelaxTestPlayer(ModTestData data, bool allowFail)
                : base(data, allowFail)
            {
                currentTestData = data;
            }

            protected override void PrepareReplay()
            {
                // We need to set IsLegacyScore to true otherwise the mod assumes that presses are already embedded into the replay
                DrawableRuleset?.SetReplayScore(new Score
                {
                    Replay = new Replay { Frames = currentTestData.ReplayFrames! },
                    ScoreInfo = new ScoreInfo { User = new APIUser { Username = @"Test" }, IsLegacyScore = true, Mods = new Mod[] { new OsuModRelax() } },
                });

                DrawableRuleset?.SetRecordTarget(Score);
            }
        }
    }
}
