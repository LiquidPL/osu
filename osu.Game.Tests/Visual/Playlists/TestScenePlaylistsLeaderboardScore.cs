// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Utils;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Leaderboards;
using osu.Game.Overlays;
using osu.Game.Screens.OnlinePlay.Playlists;
using osu.Game.Tests.Resources;
using osu.Game.Tests.Visual.SongSelect;
using osu.Game.Users;
using osuTK;

namespace osu.Game.Tests.Visual.Playlists
{
    public partial class TestScenePlaylistsLeaderboardScore : SongSelectComponentsTestScene
    {
        [Cached]
        private OverlayColourProvider colourProvider { get; set; } = new OverlayColourProvider(OverlayColourScheme.Plum);

        private FillFlowContainer? fillFlowContainer;
        private OsuSpriteText? drawWidthText;

        [Test]
        public void TestBasic()
        {
            AddStep("create content", () =>
            {
                Children = new Drawable[]
                {
                    fillFlowContainer = new FillFlowContainer
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Spacing = new Vector2(0, 2),
                    },
                    drawWidthText = new OsuSpriteText(),
                };

                foreach (var score in getTestScores())
                {
                    LeaderboardRankDisplay.HighlightType? highlightType = null;

                    switch (score.User.Id)
                    {
                        case 2:
                            highlightType = LeaderboardRankDisplay.HighlightType.Own;
                            break;

                        case 1541390:
                            highlightType = LeaderboardRankDisplay.HighlightType.Friend;
                            break;
                    }

                    fillFlowContainer.Add(new PlaylistLeaderboardScore(score)
                    {
                        Rank = score.Position,
                        Highlight = highlightType,
                    });
                }
            });
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            if (drawWidthText != null) drawWidthText.Text = $"DrawWidth: {fillFlowContainer?.DrawWidth}";
        }

        private static APIUserScoreAggregate[] getTestScores() => new[]
        {
            new APIUserScoreAggregate
            {
                Position = 999,
                User = new APIUser
                {
                    Id = 6602580,
                    Username = @"waaiiru",
                    CountryCode = CountryCode.ES,
                    CoverUrl = TestResources.COVER_IMAGE_1,
                },
                Accuracy = 1,
                TotalAttempts = RNG.Next(10, 20),
                CompletedBeatmaps = RNG.Next(2, 5),
                TotalScore = RNG.Next(1_800_000, 8_900_000),
            },
            new APIUserScoreAggregate
            {
                Position = 22333,
                User = new APIUser
                {
                    Id = 1541390,
                    Username = @"Toukai",
                    CountryCode = CountryCode.CA,
                    CoverUrl = TestResources.COVER_IMAGE_2,
                },
                Accuracy = 0.1f,
                TotalAttempts = 2,
                CompletedBeatmaps = 1,
                TotalScore = RNG.Next(50_000, 200_000),
            },
            new APIUserScoreAggregate
            {
                Position = 1,
                User = new APIUser
                {
                    Id = 2,
                    Username = "peppy",
                    CountryCode = CountryCode.AU,
                    CoverUrl = TestResources.COVER_IMAGE_1,
                },
                TotalAttempts = RNG.Next(50, 100),
                Accuracy = 0.9727,
                CompletedBeatmaps = RNG.Next(10, 20),
                TotalScore = RNG.Next(10_000_000, 20_000_000),
            },
            new APIUserScoreAggregate
            {
                Position = 110_000,
                User = new APIUser
                {
                    Username = @"No cover",
                    CountryCode = CountryCode.BR,
                },
                Accuracy = RNG.NextDouble(),
                TotalAttempts = RNG.Next(10, 20),
                CompletedBeatmaps = RNG.Next(2, 5),
                TotalScore = RNG.Next(1_800_000, 8_900_000),
            },
            new APIUserScoreAggregate
            {
                Position = 2233,
                User = new APIUser
                {
                    Id = 226597,
                    Username = @"WWWWWWWWWWWWWWWWWWWW",
                    CountryCode = CountryCode.US,
                },
                Accuracy = RNG.NextDouble(),
                TotalAttempts = RNG.Next(10, 20),
                CompletedBeatmaps = RNG.Next(2, 5),
                TotalScore = RNG.Next(1_800_000, 8_900_000),
            }
        };
    }
}
