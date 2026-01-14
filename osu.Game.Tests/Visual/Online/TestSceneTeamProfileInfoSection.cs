// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Testing;
using osu.Framework.Utils;
using osu.Game.Graphics.Containers;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays;
using osu.Game.Overlays.Team;
using osu.Game.Overlays.Team.Sections;
using osu.Game.Overlays.Team.Sections.Info;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Catch;
using osu.Game.Rulesets.Mania;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Taiko;
using osu.Game.Tests.Resources;

namespace osu.Game.Tests.Visual.Online
{
    public partial class TestSceneTeamProfileInfoSection : OsuTestScene
    {
        private InfoSection section = null!;

        [SetUpSteps]
        public void SetUp()
        {
            AddStep("create section", () =>
            {
                Child = new DependencyProvidingContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    CachedDependencies = new (Type, object)[]
                    {
                        (typeof(OverlayColourProvider), new OverlayColourProvider(OverlayColourScheme.Pink))
                    },
                    Child = new OsuScrollContainer(Direction.Vertical)
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding(20),
                        Child = section = new InfoSection(),
                    },
                };
            });
        }

        [Test]
        public void TestBasic()
        {
            AddStep("show team", () => section.TeamData.Value = GenerateTeam(new OsuRuleset().RulesetInfo));
        }

        [Test]
        public void TestEmpty()
        {
        }

        [Test]
        public void TestRulesets()
        {
            AddStep("osu", () => section.TeamData.Value = GenerateTeam(new OsuRuleset().RulesetInfo));
            AddStep("taiko", () => section.TeamData.Value = GenerateTeam(new TaikoRuleset().RulesetInfo));
            AddStep("mania", () => section.TeamData.Value = GenerateTeam(new ManiaRuleset().RulesetInfo));
            AddStep("catch", () => section.TeamData.Value = GenerateTeam(new CatchRuleset().RulesetInfo));
        }

        [Test]
        public void TestUrl()
        {
            AddStep("show team with url", () => section.TeamData.Value = GenerateTeam(new OsuRuleset().RulesetInfo));
            AddUntilStep("wait until loaded", () => section.IsLoaded, () => Is.True);
            AddAssert("url is present", () => section.ChildrenOfType<UrlValueDisplay>().Single().IsPresent, () => Is.True);
            AddStep("show team with no url", () => section.TeamData.Value = GenerateTeam(new OsuRuleset().RulesetInfo, hasUrl: false));
            AddUntilStep("wait until loaded", () => section.IsLoaded, () => Is.True);
            AddAssert("url is not present", () => section.ChildrenOfType<UrlValueDisplay>().Single().IsPresent, () => Is.False);
        }

        public static TeamProfileData GenerateTeam(RulesetInfo ruleset, bool hasUrl = true) => new TeamProfileData(new APITeam
        {
            Id = 1,
            CreatedAt = new DateTimeOffset(2026, 1, 1, 13, 6, 0, TimeSpan.Zero),
            IsOpen = true,
            Url = hasUrl ? "https://example.com" : null,
            DefaultRulesetId = ruleset.OnlineID,
            EmptySlots = 8,
            Leader = new APIUser
            {
                Id = 2,
                Username = "peppy",
                CoverUrl = TestResources.COVER_IMAGE_3,
            },
            Members = [],
            Statistics = new APITeamStatistics
            {
                Rank = RNG.Next(1, 10000),
                Performance = RNG.Next(0, 100000),
                PlayCount = RNG.Next(0, 1000000),
                RankedScore = RNG.Next(0, 1000000000),
            },
        }, ruleset);
    }
}
