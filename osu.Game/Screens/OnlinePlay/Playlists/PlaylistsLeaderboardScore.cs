// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Game.Extensions;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays;
using osu.Game.Screens.Select;
using osu.Game.Users;
using osu.Game.Users.Drawables;
using osu.Game.Utils;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.OnlinePlay.Playlists
{
    public partial class PlaylistsLeaderboardScore : OsuClickableContainer
    {
        public const int HEIGHT = BeatmapLeaderboardScore.HEIGHT;

        public readonly APIUserScoreAggregate Score;
        public int? Rank { get; init; }
        public BeatmapLeaderboardScore.HighlightType? Highlight { get; init; }

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        private const float right_content_width = 140;
        private const float common_min_width = 140;
        private const float statistics_regular_min_width = 165;
        private const float statistics_compact_min_width = 90;
        private const float rank_label_width = 40;

        private const int corner_radius = BeatmapLeaderboardScore.CORNER_RADIUS;
        private const int transition_duration = 200;

        private static readonly Color4 personal_best_gradient_left = Color4Extensions.FromHex("#66FFCC");
        private static readonly Color4 personal_best_gradient_right = Color4Extensions.FromHex("#51A388");

        private Colour4 foregroundColour;
        private Colour4 backgroundColour;
        private ColourInfo totalScoreBackgroundGradient;

        private Box background = null!;
        private Box foreground = null!;

        private ClickableAvatar innerAvatar = null!;

        private Container centreContent = null!;
        private Container rightContent = null!;

        private Box totalScoreBackground = null!;

        private FillFlowContainer<BeatmapLeaderboardScore.ScoreComponentLabel> statisticsContainer = null!;
        private Container highlightGradient = null!;
        private Container rankLabelStandalone = null!;
        private Container rankLabelOverlay = null!;

        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos)
        {
            var inputRectangle = DrawRectangle;

            inputRectangle = inputRectangle.Inflate(new MarginPadding { Vertical = BeatmapLeaderboardWedge.SPACING_BETWEEN_SCORES / 2 });

            return inputRectangle.Contains(ToLocalSpace(screenSpacePos));
        }

        public PlaylistsLeaderboardScore(APIUserScoreAggregate score)
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
                    rankLabelStandalone = new Container
                    {
                        Width = rank_label_width,
                        RelativeSizeAxes = Axes.Y,
                        Children = new Drawable[]
                        {
                            highlightGradient = new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Padding = new MarginPadding { Right = -10f },
                                Alpha = Highlight != null ? 1 : 0,
                                Colour = getHighlightColour(Highlight),
                                Child = new Box { RelativeSizeAxes = Axes.Both },
                            },
                            new BeatmapLeaderboardScore.RankLabel(Rank, false, darkText: Highlight == BeatmapLeaderboardScore.HighlightType.Own)
                            {
                                RelativeSizeAxes = Axes.Both,
                            }
                        },
                    },
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
                                    Colour = foregroundColour
                                },
                                new CoverBackground
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Model = Score.User,
                                    Anchor = Anchor.BottomLeft,
                                    Origin = Anchor.BottomLeft,
                                    Colour = ColourInfo.GradientHorizontal(Colour4.White.Opacity(0.5f), Colour4.FromHex(@"222A27").Opacity(1)),
                                },
                                new GridContainer
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    ColumnDimensions = new[]
                                    {
                                        new Dimension(GridSizeMode.AutoSize),
                                        new Dimension(),
                                        new Dimension(GridSizeMode.AutoSize),
                                    },
                                    Content = new[]
                                    {
                                        new Drawable[]
                                        {
                                            new Container
                                            {
                                                AutoSizeAxes = Axes.Both,
                                                CornerRadius = corner_radius,
                                                Masking = true,
                                                Children = new Drawable[]
                                                {
                                                    new DelayedLoadWrapper(innerAvatar = new ClickableAvatar(Score.User)
                                                    {
                                                        Anchor = Anchor.Centre,
                                                        Origin = Anchor.Centre,
                                                        Scale = new Vector2(1.1f),
                                                        RelativeSizeAxes = Axes.Both,
                                                    })
                                                    {
                                                        RelativeSizeAxes = Axes.None,
                                                        Size = new Vector2(HEIGHT)
                                                    },
                                                    rankLabelOverlay = new Container
                                                    {
                                                        RelativeSizeAxes = Axes.Both,
                                                        Alpha = 0,
                                                        Children = new Drawable[]
                                                        {
                                                            new Box
                                                            {
                                                                RelativeSizeAxes = Axes.Both,
                                                                Colour = Colour4.Black.Opacity(0.5f),
                                                            },
                                                            new BeatmapLeaderboardScore.RankLabel(Rank, false, false)
                                                            {
                                                                AutoSizeAxes = Axes.Both,
                                                                Anchor = Anchor.Centre,
                                                                Origin = Anchor.Centre,
                                                            },
                                                        }
                                                    }
                                                },
                                            },
                                            new FillFlowContainer
                                            {
                                                Anchor = Anchor.CentreLeft,
                                                Origin = Anchor.CentreLeft,
                                                RelativeSizeAxes = Axes.X,
                                                AutoSizeAxes = Axes.Y,
                                                Direction = FillDirection.Vertical,
                                                Padding = new MarginPadding { Horizontal = corner_radius },
                                                Children = new Drawable[]
                                                {
                                                    new FillFlowContainer
                                                    {
                                                        Direction = FillDirection.Horizontal,
                                                        Spacing = new Vector2(5),
                                                        AutoSizeAxes = Axes.Both,
                                                        Masking = true,
                                                        Children = new Drawable[]
                                                        {
                                                            new UpdateableFlag(Score.User.CountryCode)
                                                            {
                                                                Anchor = Anchor.CentreLeft,
                                                                Origin = Anchor.CentreLeft,
                                                                Size = new Vector2(20, 14),
                                                            },
                                                            new UpdateableTeamFlag(Score.User.Team)
                                                            {
                                                                Anchor = Anchor.CentreLeft,
                                                                Origin = Anchor.CentreLeft,
                                                                Size = new Vector2(30, 15),
                                                            },
                                                        }
                                                    },
                                                    new TruncatingSpriteText
                                                    {
                                                        RelativeSizeAxes = Axes.X,
                                                        Text = Score.User.Username,
                                                        Font = OsuFont.Style.Heading2,
                                                    }
                                                }
                                            },
                                            new Container
                                            {
                                                AutoSizeAxes = Axes.Both,
                                                Anchor = Anchor.CentreRight,
                                                Origin = Anchor.CentreRight,
                                                Child = statisticsContainer = new FillFlowContainer<BeatmapLeaderboardScore.ScoreComponentLabel>
                                                {
                                                    Name = @"Statistics container",
                                                    Padding = new MarginPadding { Right = 10 },
                                                    Spacing = new Vector2(20, 0),
                                                    Anchor = Anchor.CentreRight,
                                                    Origin = Anchor.CentreRight,
                                                    AutoSizeAxes = Axes.Both,
                                                    Direction = FillDirection.Horizontal,
                                                    Children = new[]
                                                    {
                                                        new BeatmapLeaderboardScore.ScoreComponentLabel("ATTEMPTS", Score.TotalAttempts.ToString(), false, 50),
                                                        new BeatmapLeaderboardScore.ScoreComponentLabel("COMPLETED", Score.CompletedBeatmaps.ToString(), false, 55),
                                                    },
                                                    Alpha = 0,
                                                }
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
                                                Spacing = new Vector2(0, 6),
                                                Children = new Drawable[]
                                                {
                                                    new OsuSpriteText
                                                    {
                                                        Anchor = Anchor.TopRight,
                                                        Origin = Anchor.TopRight,
                                                        UseFullGlyphHeight = false,
                                                        Current = { Value = Score.TotalScore.ToStandardFormattedString(0) },
                                                        Spacing = new Vector2(-1.5f),
                                                        Font = OsuFont.Style.Subtitle.With(weight: FontWeight.Light, fixedWidth: true),
                                                    },
                                                    new OsuSpriteText
                                                    {
                                                        Anchor = Anchor.TopRight,
                                                        Origin = Anchor.TopRight,
                                                        UseFullGlyphHeight = false,
                                                        Text = Score.Accuracy.FormatAccuracy(),
                                                        Font = OsuFont.Style.Caption1.With(weight: FontWeight.Light),
                                                    },
                                                }
                                            }
                                        }
                                    }
                                }
                            },
                        }
                    },
                },
            };
            innerAvatar.OnLoadComplete += d => d.FadeInFromZero(200);
        }

        private ColourInfo getHighlightColour(BeatmapLeaderboardScore.HighlightType? highlightType, float lightenAmount = 0)
        {
            switch (highlightType)
            {
                case BeatmapLeaderboardScore.HighlightType.Own:
                    return ColourInfo.GradientHorizontal(personal_best_gradient_left.Lighten(lightenAmount), personal_best_gradient_right.Lighten(lightenAmount));

                case BeatmapLeaderboardScore.HighlightType.Friend:
                    return ColourInfo.GradientHorizontal(colours.Pink1.Lighten(lightenAmount), colours.Pink3.Lighten(lightenAmount));

                default:
                    return Colour4.White;
            }
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            rightContent.Width = right_content_width;
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
            highlightGradient.FadeColour(getHighlightColour(Highlight, IsHovered ? 0.2f : 0), transition_duration, Easing.OutQuint);

            if (IsHovered && currentMode != DisplayMode.Full)
                rankLabelOverlay.FadeIn(transition_duration, Easing.OutQuint);
            else
                rankLabelOverlay.FadeOut(transition_duration, Easing.OutQuint);
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
                Left = rankLabelStandalone.DrawWidth,
                Right = rightContent.DrawWidth,
            };
        }

        private void updateDisplayMode(DisplayMode mode)
        {
            double duration = currentMode == null ? 0 : transition_duration;
            if (mode >= DisplayMode.Full)
                rankLabelStandalone.FadeIn(duration, Easing.OutQuint).ResizeWidthTo(rank_label_width, duration, Easing.OutQuint);
            else
                rankLabelStandalone.FadeOut(duration, Easing.OutQuint).ResizeWidthTo(0, duration, Easing.OutQuint);

            if (mode >= DisplayMode.Regular)
            {
                statisticsContainer.FadeIn(duration, Easing.OutQuint).MoveToX(0, duration, Easing.OutQuint);
                statisticsContainer.Direction = FillDirection.Horizontal;
                statisticsContainer.ScaleTo(1, duration, Easing.OutQuint);
            }
            else if (mode >= DisplayMode.Compact)
            {
                statisticsContainer.FadeIn(duration, Easing.OutQuint).MoveToX(0, duration, Easing.OutQuint);
                statisticsContainer.Direction = FillDirection.Vertical;
                statisticsContainer.ScaleTo(0.8f, duration, Easing.OutQuint);
            }
            else
                statisticsContainer.FadeOut(duration, Easing.OutQuint).MoveToX(statisticsContainer.DrawWidth, duration, Easing.OutQuint);

            currentMode = mode;
        }

        private DisplayMode getCurrentDisplayMode()
        {
            if (DrawWidth >= common_min_width + statistics_regular_min_width + right_content_width + rank_label_width)
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
