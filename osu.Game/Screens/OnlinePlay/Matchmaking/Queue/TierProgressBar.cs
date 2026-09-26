// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
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
using osu.Game.Overlays;
using osuTK;

namespace osu.Game.Screens.OnlinePlay.Matchmaking.Queue
{
    [Cached]
    public partial class TierProgressBar : Container
    {
        private readonly float currentRating = 0;

        public float CurrentRating
        {
            get => currentRating;
            set
            {
                if (!IsLoaded)
                    return;

                FinishTransforms(false, nameof(currentRating));
                this.TransformTo(nameof(currentRating), value, 1500, new CubicBezierEasingFunction(0.15, 0.6, 0, 1));
                tierContainer.MoveToX(Math.Clamp(-(value / 1500) * tierContainer.DrawWidth + DrawWidth / 2, -tierContainer.DrawWidth + DrawWidth, 0), 1500, new CubicBezierEasingFunction(0.15, 0.6, 0, 1));
            }
        }

        private float segmentWidth = 150;

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        private FillFlowContainer tierContainer = null!;

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
                    // X = -600,
                    Children = new Drawable[]
                    {
                        new TierSegment { LowerBound = 0, UpperBound = 150 },
                        new TierSegment { LowerBound = 150, UpperBound = 300 },
                        new TierSegment { LowerBound = 300, UpperBound = 450 },
                        new TierSegment { LowerBound = 450, UpperBound = 600 },
                        new TierSegment { LowerBound = 600, UpperBound = 750 },
                        new TierSegment { LowerBound = 750, UpperBound = 900 },
                        new TierSegment { LowerBound = 900, UpperBound = 1050 },
                        new TierSegment { LowerBound = 1050, UpperBound = 1200 },
                        new TierSegment { LowerBound = 1200, UpperBound = 1350 },
                        new TierSegment { LowerBound = 1350, UpperBound = 1500 },
                    },
                },
            };
        }

        protected override void Update()
        {
            base.Update();

            segmentWidth = DrawWidth / 8;
        }

        private partial class TierSegment : Container
        {
            public float LowerBound { get; init; }
            public float UpperBound { get; init; }

            private Container foregroundContainer = null!;

            [Resolved]
            private TierProgressBar progressBar { get; set; } = null!;

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
                        Text = LowerBound.ToStandardFormattedString(0),
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
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = ColourInfo.GradientVertical(Colour4.FromHex("#FFEE997F"), Colour4.FromHex("#FFDD337F")),
                            },
                            foregroundContainer = new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Masking = true,
                                CornerRadius = 4,
                                Child = new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = ColourInfo.GradientVertical(Colour4.FromHex("#FFEE99"), Colour4.FromHex("#FFDD33")),
                                },
                            },
                        },
                    },
                };
            }

            protected override void Update()
            {
                base.Update();

                Width = progressBar.segmentWidth - 4;

                if (progressBar.CurrentRating < LowerBound)
                    foregroundContainer.Width = 0;
                else if (progressBar.CurrentRating >= UpperBound)
                    foregroundContainer.Width = 1;
                else
                    foregroundContainer.Width = (progressBar.CurrentRating - LowerBound) / DrawWidth;
            }
        }
    }
}
