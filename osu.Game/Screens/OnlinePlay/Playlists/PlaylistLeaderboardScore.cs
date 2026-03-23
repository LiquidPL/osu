// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Leaderboards;
using osu.Game.Overlays;
using osu.Game.Resources.Localisation.Web;
using osu.Game.Screens.Select;
using osu.Game.Users;
using osu.Game.Utils;
using osuTK;

namespace osu.Game.Screens.OnlinePlay.Playlists
{
    public partial class PlaylistLeaderboardScore : OsuClickableContainer
    {
        public const int HEIGHT = BeatmapLeaderboardScore.HEIGHT;

        public readonly APIUserScoreAggregate Score;
        public int? Rank { get; init; }
        public LeaderboardRankDisplay.HighlightType? Highlight { get; init; }

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        private const float right_content_width = 140;
        private const float common_min_width = 140;
        private const float statistics_regular_min_width = 220;
        private const float statistics_compact_min_width = 115;

        private const int corner_radius = BeatmapLeaderboardScore.CORNER_RADIUS;
        private const int transition_duration = 200;

        private Colour4 foregroundColour;
        private Colour4 backgroundColour;
        private ColourInfo totalScoreBackgroundGradient;

        private Box background = null!;
        private Box foreground = null!;

        private Container centreContent = null!;
        private Container rightContent = null!;

        private LeaderboardRankDisplay rankDisplay = null!;
        private LeaderboardCommonDisplay commonDisplay = null!;
        private FillFlowContainer statisticsContainer = null!;
        private Box totalScoreBackground = null!;

        private LeaderboardStatistic[] statistics = [];

        public PlaylistLeaderboardScore(APIUserScoreAggregate score)
        {
            Score = score;

            RelativeSizeAxes = Axes.X;
            Height = HEIGHT;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            foregroundColour = colourProvider.Background5;
            backgroundColour = colourProvider.Background3;
            totalScoreBackgroundGradient = ColourInfo.GradientHorizontal(backgroundColour.Opacity(0), backgroundColour);

            Child = new Container
            {
                Masking = true,
                CornerRadius = corner_radius,
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    background = new Box
                    {
                        Alpha = 0.4f,
                        RelativeSizeAxes = Axes.Both,
                        Colour = backgroundColour,
                    },
                    rankDisplay = new LeaderboardRankDisplay(Rank, false, Highlight),
                    centreContent = new Container
                    {
                        Name = @"Centre container",
                        RelativeSizeAxes = Axes.Both,
                        Child = new Container
                        {
                            Masking = true,
                            CornerRadius = corner_radius,
                            RelativeSizeAxes = Axes.Both,
                            Children = new Drawable[]
                            {
                                foreground = new Box
                                {
                                    Alpha = 0.4f,
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = foregroundColour,
                                },
                                new UserCoverBackground
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    User = Score.User,
                                    Anchor = Anchor.BottomLeft,
                                    Origin = Anchor.BottomLeft,
                                    Colour = ColourInfo.GradientHorizontal(Colour4.White.Opacity(0.5f), Colour4.FromHex(@"222A27").Opacity(1)),
                                },
                                new GridContainer
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    ColumnDimensions = new[]
                                    {
                                        new Dimension(),
                                        new Dimension(GridSizeMode.AutoSize),
                                    },
                                    Content = new[]
                                    {
                                        new Drawable[]
                                        {
                                            commonDisplay = new LeaderboardCommonDisplay(Score.User, null, Rank),
                                            new Container
                                            {
                                                AutoSizeAxes = Axes.Both,
                                                Anchor = Anchor.CentreRight,
                                                Origin = Anchor.CentreRight,
                                                Child = statisticsContainer = new FillFlowContainer
                                                {
                                                    Name = @"Statistics container",
                                                    Padding = new MarginPadding { Right = 10 },
                                                    Spacing = new Vector2(10, 0),
                                                    Anchor = Anchor.CentreRight,
                                                    Origin = Anchor.CentreRight,
                                                    AutoSizeAxes = Axes.Both,
                                                    Direction = FillDirection.Horizontal,
                                                    Children = statistics = new[]
                                                    {
                                                        new LeaderboardStatistic(RankingsStrings.StatAccuracy.ToUpper(), Score.Accuracy.FormatAccuracy(), Score.Accuracy == 1,
                                                            55),
                                                        new LeaderboardStatistic("ATTEMPTS", Score.TotalAttempts.ToString(),
                                                            false, 55),
                                                        new LeaderboardStatistic("COMPLETED", Score.CompletedBeatmaps.ToString(),
                                                            false, 60),
                                                    },
                                                    Alpha = 0,
                                                },
                                            },
                                        },
                                    },
                                },
                            },
                        },
                    },
                    rightContent = new Container
                    {
                        Anchor = Anchor.TopRight,
                        Origin = Anchor.TopRight,
                        Name = @"Right content",
                        RelativeSizeAxes = Axes.Y,
                        Width = right_content_width,
                        Child = new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Anchor = Anchor.TopRight,
                            Origin = Anchor.TopRight,
                            Children = new Drawable[]
                            {
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Child = new Container
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Masking = true,
                                        CornerRadius = corner_radius,
                                        Children = new Drawable[]
                                        {
                                            totalScoreBackground = new Box
                                            {
                                                RelativeSizeAxes = Axes.Both,
                                                Colour = totalScoreBackgroundGradient,
                                            },
                                            new FillFlowContainer
                                            {
                                                AutoSizeAxes = Axes.Both,
                                                Anchor = Anchor.CentreRight,
                                                Origin = Anchor.CentreRight,
                                                Direction = FillDirection.Vertical,
                                                Padding = new MarginPadding { Horizontal = corner_radius },
                                                Spacing = new Vector2(0f, -2f),
                                                Children = new Drawable[]
                                                {
                                                    new OsuSpriteText
                                                    {
                                                        Anchor = Anchor.TopRight,
                                                        Origin = Anchor.TopRight,
                                                        UseFullGlyphHeight = false,
                                                        Current = { Value = Score.TotalScore.ToString("N0") },
                                                        Spacing = new Vector2(-1.5f),
                                                        Font = OsuFont.Style.Subtitle.With(weight: FontWeight.Light, fixedWidth: true),
                                                    },
                                                }
                                            }
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
            };
        }

        protected override bool OnHover(HoverEvent e)
        {
            updateState();
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            updateState();
            base.OnHoverLost(e);
        }

        private void updateState()
        {
            var lightenedGradient = ColourInfo.GradientHorizontal(backgroundColour.Opacity(0).Lighten(0.2f), backgroundColour.Lighten(0.2f));

            foreground.FadeColour(IsHovered ? foregroundColour.Lighten(0.2f) : foregroundColour, transition_duration, Easing.OutQuint);
            background.FadeColour(IsHovered ? backgroundColour.Lighten(0.2f) : backgroundColour, transition_duration, Easing.OutQuint);
            totalScoreBackground.FadeColour(IsHovered ? lightenedGradient : totalScoreBackgroundGradient, transition_duration, Easing.OutQuint);
            rankDisplay.UpdateHighlightState(IsHovered, transition_duration);
            commonDisplay.UpdateRankOverlayState(IsHovered && currentMode != DisplayMode.Full, transition_duration);
        }

        private DisplayMode? currentMode;

        protected override void Update()
        {
            base.Update();

            DisplayMode mode = getCurrentDisplayMode();

            if (currentMode != mode)
                updateDisplayMode(mode);

            centreContent.Padding = new MarginPadding
            {
                Left = rankDisplay.DrawWidth,
                Right = rightContent.DrawWidth,
            };
        }

        private void updateDisplayMode(DisplayMode mode)
        {
            double duration = currentMode == null ? 0 : transition_duration;

            if (mode >= DisplayMode.Full)
                rankDisplay.Appear(duration);
            else
                rankDisplay.Disappear(duration);

            if (mode >= DisplayMode.Regular)
            {
                statisticsContainer.FadeIn(duration, Easing.OutQuint).MoveToX(0, duration, Easing.OutQuint);
                statisticsContainer.Direction = FillDirection.Horizontal;
                statisticsContainer.ScaleTo(1, duration, Easing.OutQuint);

                foreach (var statistic in statistics)
                    statistic.Direction = Direction.Horizontal;
            }
            else if (mode >= DisplayMode.Compact)
            {
                statisticsContainer.FadeIn(duration, Easing.OutQuint).MoveToX(0, duration, Easing.OutQuint);
                statisticsContainer.Direction = FillDirection.Vertical;
                statisticsContainer.ScaleTo(0.8f, duration, Easing.OutQuint);

                foreach (var statistic in statistics)
                    statistic.Direction = Direction.Vertical;
            }
            else
                statisticsContainer.FadeOut(duration, Easing.OutQuint).MoveToX(statisticsContainer.DrawWidth, duration, Easing.OutQuint);

            currentMode = mode;
        }

        private DisplayMode getCurrentDisplayMode()
        {
            if (DrawWidth >= common_min_width + statistics_regular_min_width + right_content_width + LeaderboardRankDisplay.WIDTH)
                return DisplayMode.Full;

            if (DrawWidth >= common_min_width + statistics_regular_min_width + right_content_width)
                return DisplayMode.Regular;

            if (DrawWidth >= common_min_width + statistics_compact_min_width + right_content_width)
                return DisplayMode.Compact;

            return DisplayMode.Minimal;
        }

        private enum DisplayMode
        {
            Minimal,
            Compact,
            Regular,
            Full
        }
    }
}
