// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Transforms;
using osu.Game.Extensions;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays;
using osuTK;

namespace osu.Game.Screens.OnlinePlay.Matchmaking.Queue
{
    [Cached]
    public partial class TierProgressBar : Container
    {
        private int lowerBound;
        private int upperBound;

        private int divisionWidth;

        private readonly float currentRating = 600;

        public float CurrentRating
        {
            get => currentRating;
            set
            {
                if (!IsLoaded)
                    return;

                FinishTransforms(false, nameof(currentRating));
                this.TransformTo(nameof(currentRating), value, 1500, new CubicBezierEasingFunction(0.15, 0.6, 0, 1));
                tierContainer.MoveToX(Math.Clamp(-((value - lowerBound) / (upperBound - lowerBound)) * tierContainer.DrawWidth + DrawWidth / 2, -tierContainer.DrawWidth + DrawWidth, 0), 1500, new CubicBezierEasingFunction(0.15, 0.6, 0, 1));
            }
        }

        public required List<RankedPlayDivision> Divisions
        {
            get;
            set
            {
                field = value;

                // Lustrous is a special case where it doesn't have an end,
                // and because of that web sets its `EndRating` to INT_MAX.
                //
                // For the purposes of the progress bar, we set the end point
                // to a sane value so that it doesn't extend into infinity.
                field.Find(d => d.Tier == Tier.Lustrous)?.EndRating = 3000;

                lowerBound = field.Min(d => d.StartRating);
                upperBound = field.Max(d => d.EndRating);
            }
        }

        private float segmentWidth = 150;

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        private FillFlowContainer tierContainer = null!;

        public TierProgressBar(int divisionWidth) {
            this.divisionWidth = divisionWidth;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Height = 55;
            Masking = true;

            Children = new Drawable[]
            {
                new Container
                {
                    RelativeSizeAxes = Axes.X,
                    Height = 50,
                    Masking = true,
                    CornerRadius = 15,
                    EdgeEffect = new EdgeEffectParameters
                    {
                        Type = EdgeEffectType.Shadow,
                        Offset = new Vector2(0, 3),
                        Radius = 3,
                        Colour = Colour4.Black.Opacity(0.25f),
                    },
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = colourProvider.Background5,
                        },
                    },
                },
                tierContainer = new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Padding = new MarginPadding { Top = 10, Horizontal = 2 },
                    Direction = FillDirection.Horizontal,
                },
            };

            tierContainer.AddRange(Divisions.Select(d => new TierSegment(d)));
        }

        protected override void Update()
        {
            base.Update();

            segmentWidth = DrawWidth / 8;
        }

        private partial class TierSegment : Container
        {
            private readonly RankedPlayDivision division;

            private Container foregroundContainer = null!;

            [Resolved]
            private TierProgressBar progressBar { get; set; } = null!;

            public TierSegment(RankedPlayDivision division)
            {
                this.division = division;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                AutoSizeAxes = Axes.Y;

                Margin = new MarginPadding { Horizontal = 2 };

                Children = new Drawable[]
                {
                    new OsuSpriteText
                    {
                        Font = OsuFont.Torus.With(size: 12, weight: FontWeight.SemiBold),
                        Colour = Colour4.White,
                        Text = division.StartRating.ToStandardFormattedString(0),
                    },
                    new Container
                    {
                        Y = 15,
                        RelativeSizeAxes = Axes.X,
                        Height = 8,
                        Masking = true,
                        CornerRadius = 4,
                        Children = new Drawable[]
                        {
                            new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Child = new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = getColour(),
                                },
                                Alpha = 0.25f,
                            },
                            foregroundContainer = new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Masking = true,
                                CornerRadius = 4,
                                Child = new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = getColour(),
                                },
                            },
                        },
                    },
                };
            }

            private ColourInfo getColour() => division.Tier switch
            {
                Tier.Bronze => ColourInfo.GradientVertical(Colour4.FromHex("#B88F7A"), Colour4.FromHex("#855C47")),
                Tier.Silver => ColourInfo.GradientVertical(Colour4.FromHex("#E0E0EB"), Colour4.FromHex("#A3A3C2")),
                Tier.Gold => ColourInfo.GradientVertical(Colour4.FromHex("#FFEE99"), Colour4.FromHex("#FFDD33")),
                Tier.Platinum => ColourInfo.GradientVertical(Colour4.FromHex("#A8F0EF"), Colour4.FromHex("#52E0DF")),
                Tier.Rhodium => ColourInfo.GradientVertical(Colour4.FromHex("#ADFFB6"), Colour4.FromHex("#6DB566")),
                Tier.Radiant => ColourInfo.GradientVertical(Colour4.FromHex("#97DCFF"), Colour4.FromHex("#ED82FF")),
                Tier.Lustrous => ColourInfo.GradientVertical(Colour4.FromHex("#FFE600"), Colour4.FromHex("#ED82FF")),
                _ => throw new ArgumentOutOfRangeException()
            };

            protected override void Update()
            {
                base.Update();

                int start = division.StartRating;
                int end = division.EndRating;

                // Lustrous is a special case where it doesn't have an end,
                // and because of that web sets its `EndRating` to INT_MAX.
                //
                // For the purposes of the progress bar, we set the end point
                // to a sane value so that it doesn't extend into infinity.
                if (division.Tier == Tier.Lustrous)
                    end = 3000;

                Width = (end - start + 1) / divisionWidth * progressBar.segmentWidth - 4;

                if (progressBar.CurrentRating < start)
                    foregroundContainer.Width = 0;
                else if (progressBar.CurrentRating >= end)
                    foregroundContainer.Width = 1;
                else
                    foregroundContainer.Width = (progressBar.CurrentRating - start) / (end - start + 1);
            }
        }
    }
}
