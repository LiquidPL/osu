// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Game.Extensions;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.RankedPlay;
using osu.Game.Resources.Localisation.Web;
using osu.Game.Screens.OnlinePlay.Matchmaking.RankedPlay.Components;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Overlays.Profile.Header.Components
{
    public partial class RankedPlayProfileCard : ProfileHeaderCard
    {
        public readonly IBindable<UserProfileData?> User = new Bindable<UserProfileData?>();

        private Container badgeContainer = null!;

        private RankedPlayRankDisplay rankDisplay = null!;
        private ProfileValueDisplay ratingDisplay = null!;
        private ProfileValueDisplay tierDisplay = null!;
        private ProfileValueDisplay totalMatchesDisplay = null!;
        private ProfileValueDisplay winsDisplay = null!;
        private ProfileValueDisplay lossesDisplay = null!;
        private RankedPlayRatingGraph ratingGraph = null!;

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        [Resolved]
        private IRankedPlayDivisionStore divisionStore { get; set; } = null!;

        public RankedPlayProfileCard()
            : base(UsersStrings.ShowMatchmakingTitle, OsuIcon.Multi)
        {
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            TopContainer.Add(badgeContainer = new Container
            {
                AutoSizeAxes = Axes.Both,
                Anchor = Anchor.TopRight,
                Origin = Anchor.TopRight,
            });

            TopContent.Child = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Spacing = new Vector2(10),
                Direction = FillDirection.Vertical,
                Children = new Drawable[]
                {
                    rankDisplay = new RankedPlayRankDisplay(),
                    new GridContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                        ColumnDimensions = new[]
                        {
                            new Dimension(),
                            new Dimension(),
                        },
                        Content = new[]
                        {
                            new Drawable[]
                            {
                                ratingDisplay = new ProfileValueDisplay(ProfileValueDisplay.ValueSize.Medium)
                                {
                                    Title = UsersStrings.ShowMatchmakingRating,
                                },
                                tierDisplay = new ProfileValueDisplay(ProfileValueDisplay.ValueSize.Medium)
                                {
                                    Title = UsersStrings.ShowMatchmakingTier,
                                }
                            },
                        }
                    },
                    new GridContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                        ColumnDimensions = new[]
                        {
                            new Dimension(GridSizeMode.Relative, size: 0.5f),
                            new Dimension(GridSizeMode.Relative, size: 0.25f),
                            new Dimension(GridSizeMode.Relative, size: 0.25f),
                        },
                        Content = new[]
                        {
                            new Drawable[]
                            {
                                totalMatchesDisplay = new ProfileValueDisplay
                                {
                                    Title = UsersStrings.ShowMatchmakingPlays,
                                },
                                winsDisplay = new ProfileValueDisplay
                                {
                                    Title = UsersStrings.ShowMatchmakingWins,
                                },
                                lossesDisplay = new ProfileValueDisplay
                                {
                                    Title = UsersStrings.ShowMatchmakingLosses,
                                },
                            },
                        }
                    },
                },
            };

            BottomContent.Child = new Container
            {
                RelativeSizeAxes = Axes.X,
                Height = 80,
                Padding = new MarginPadding { Vertical = 10, Right = 15 },
                Child = ratingGraph = new RankedPlayRatingGraph
                {
                    RelativeSizeAxes = Axes.Both,
                },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            User.BindValueChanged(e => updateDisplay(e.NewValue), true);
        }

        private void updateDisplay(UserProfileData? data)
        {
            badgeContainer.Clear();

            if (data == null || data.User.MatchmakingStatistics.Length == 0)
                return;

            var latestPoolStats = data.User.MatchmakingStatistics.SingleOrDefault(s => s.Pool.Active);

            if (latestPoolStats == null)
                return;

            divisionStore.GetDivisionForRating(latestPoolStats.Rating).ContinueWith(result =>
            {
                var division = result.GetResultSafely();

                if (division == null)
                    return;

                Schedule(() =>
                {
                    badgeContainer.Child = new DivisionBadge(division, data.Ruleset, latestPoolStats.Rank)
                    {
                        Anchor = Anchor.TopRight,
                        Origin = Anchor.TopRight,
                        Scale = new Vector2(0.66f),
                    };

                    tierDisplay.Content.Text = division.DisplayName;
                });
            });

            rankDisplay.Statistics.Value = latestPoolStats;

            ratingDisplay.Content.Text = latestPoolStats.Rating.ToStandardFormattedString(0);
            totalMatchesDisplay.Content.Text = latestPoolStats.Plays.ToStandardFormattedString(0);
            winsDisplay.Content.Text = latestPoolStats.FirstPlacements.ToStandardFormattedString(0);
            lossesDisplay.Content.Text = (latestPoolStats.Plays - latestPoolStats.FirstPlacements).ToStandardFormattedString(0);

            MiddleContent.Child = new Container
            {
                RelativeSizeAxes = Axes.X,
                Height = 60,
                Padding = new MarginPadding { Horizontal = 10, Vertical = 12 },
                Children = new Drawable[]
                {
                    new OsuSpriteText
                    {
                        Anchor = Anchor.TopLeft,
                        Origin = Anchor.TopLeft,
                        Text = UsersStrings.ShowMatchmakingRecentHistory,
                        Font = OsuFont.Style.Caption2.With(weight: FontWeight.Bold),
                    },
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Anchor = Anchor.BottomLeft,
                        Origin = Anchor.BottomLeft,
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(4),
                        Masking = true,
                        ChildrenEnumerable = latestPoolStats.RecentHistory.Select(h => new MatchResult(h)),
                    },
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = MatchResult.HEIGHT,
                        Width = 0.4f,
                        Anchor = Anchor.BottomRight,
                        Origin = Anchor.BottomRight,
                        Padding = new MarginPadding { Right = -1 },
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = ColourInfo.GradientHorizontal(colourProvider.Background3.Opacity(0), colourProvider.Background3),
                            },
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = ColourInfo.GradientHorizontal(Color4.Black.Opacity(0), Color4.Black.Opacity(0.2f)),
                            }
                        },
                    },
                },
            };

            ratingGraph.Statistics.Value = latestPoolStats;
        }

        private partial class MatchResult : CompositeDrawable, IHasCustomTooltip<DateTimeOffset>
        {
            public const int HEIGHT = 24;

            private readonly APIUserMatchmakingRatingHistory result;

            public MatchResult(APIUserMatchmakingRatingHistory result)
            {
                this.result = result;
            }

            [BackgroundDependencyLoader]
            private void load(OsuColour colours)
            {
                Size = new Vector2(HEIGHT, 16);
                Masking = true;
                CornerRadius = 8;

                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = result.Result == APIUserMatchmakingRatingHistory.MatchResult.Win
                            ? ColourInfo.GradientVertical(colours.Green1, colours.Green3)
                            : ColourInfo.GradientVertical(colours.Red1, colours.Red3),
                    },
                    new OsuSpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Text = result.Result == APIUserMatchmakingRatingHistory.MatchResult.Win ? @"W" : @"L",
                        Font = OsuFont.Torus.With(size: 12, weight: FontWeight.Bold),
                        Colour = result.Result == APIUserMatchmakingRatingHistory.MatchResult.Win ? colours.Green4 : colours.Red4,
                        UseFullGlyphHeight = false,
                    },
                };
            }

            public ITooltip<DateTimeOffset> GetCustomTooltip() => new DateTooltip();

            public DateTimeOffset TooltipContent => result.CreatedAt;
        }
    }
}
