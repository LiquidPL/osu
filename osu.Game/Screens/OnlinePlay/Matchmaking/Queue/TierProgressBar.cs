// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Transforms;
using osu.Framework.Layout;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays;
using osuTK;

namespace osu.Game.Screens.OnlinePlay.Matchmaking.Queue
{
    public partial class TierProgressBar : VisibilityContainer
    {
        protected override bool StartHidden => true;

        private readonly BindableInt currentRating = new BindableInt();
        private readonly BindableFloat currentRatingInstantaneous = new BindableFloat();

        private readonly Bindable<RankedPlayDivision> currentDivision = new Bindable<RankedPlayDivision>();

        public required List<RankedPlayDivision> Divisions
        {
            private get;
            set
            {
                if (Transforms.Any() || segmentContainer.Transforms.Any())
                    throw new InvalidOperationException("Cannot set divisions while a transform is in progress.");

                field = value;

                lowerBound = field.Min(d => d.StartRating);
                upperBound = field.Max(d => d.EndRating);

                currentRatingInstantaneous.Value = currentRating.Value = Math.Clamp(currentRating.Value, lowerBound, upperBound);

                segmentContainer.Clear();
                segmentContainer.AddRange(field.Select(d => new DivisionSegment(d)
                {
                    CurrentRating = { BindTarget = currentRatingInstantaneous },
                }));

                updateSegmentSizes();
            }
        }

        private const int displayed_rating_range = 800;

        private int lowerBound;
        private int upperBound;

        /// <summary>
        /// The width currently being taken up by a single rating point.
        /// </summary>
        private float widthPerRating;

        private readonly Box background;
        private readonly FillFlowContainer<DivisionSegment> segmentContainer;
        private readonly RatingToNextDivisionDisplay nextDivDisplay;

        private readonly LayoutValue layout = new LayoutValue(Invalidation.DrawSize);

        public TierProgressBar()
        {
            AutoSizeAxes = Axes.Y;

            Children = new Drawable[]
            {
                new Container
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Masking = true,
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
                            Child = background = new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                            },
                        },
                        segmentContainer = new FillFlowContainer<DivisionSegment>
                        {
                            AutoSizeAxes = Axes.Both,
                            Padding = new MarginPadding { Top = 10, Horizontal = 2 },
                            Direction = FillDirection.Horizontal,
                        },
                    },
                },
                nextDivDisplay = new RatingToNextDivisionDisplay
                {
                    CurrentRating = { BindTarget = currentRating },
                    CurrentDivision = { BindTarget = currentDivision },
                    Alpha = 0,
                    Flipped = true,
                },
            };

            AddLayout(layout);
        }

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider)
        {
            background.Colour = colourProvider.Background5;
        }

        protected override void Update()
        {
            base.Update();

            if (!layout.IsValid)
            {
                updateSegmentSizes();
                layout.Validate();
            }
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            updateSegmentSizes();
        }

        public void UpdateRating(int rating, bool entering = false)
        {
            if (currentRating.Value == rating) return;

            if (rating < lowerBound || rating > upperBound)
                throw new ArgumentException("Rating is outside the division bounds.");

            currentRating.Value = rating;
            currentDivision.Value = Divisions.First(d => d.StartRating <= rating && d.EndRating >= rating);

            if (IsLoaded)
            {
                int duration = entering ? 1500 : 1600;
                IEasingFunction easingFunction = entering
                    ? new CubicBezierEasingFunction(0, 0, 0, 1)
                    : new CubicBezierEasingFunction(0.15, 0.6, 0, 1);

                this.TransformBindableTo(currentRatingInstantaneous, rating, duration, easingFunction);
                segmentContainer.MoveToX(calculateContainerOffset(rating), duration, easingFunction);

                var transform = nextDivDisplay.FadeOut(entering ? 0 : 100, new CubicBezierEasingFunction(0.5, 0, 0.5, 1));

                if (currentDivision.Value?.Tier != Tier.Lustrous)
                {
                    transform
                        .Then()
                        .Schedule(() =>
                        {
                            updateToNextDivPosition();
                            nextDivDisplay.UpdateLabel();
                        })
                        .Delay(duration - 600)
                        .FadeIn(200, new CubicBezierEasingFunction(0.5, 0, 0.5, 1));
                }
            }
            else
            {
                currentRatingInstantaneous.Value = rating;
            }
        }

        private void updateSegmentSizes()
        {
            // We want the amount of displayed divisions/segments to remain consistent
            // regardless of screen dimensions.
            widthPerRating = DrawWidth / displayed_rating_range;
            segmentContainer.X = calculateContainerOffset(currentRatingInstantaneous.Value);

            foreach (var segment in segmentContainer)
                segment.Width = segment.Division.Width * widthPerRating;
        }

        private void updateToNextDivPosition()
        {
            if (currentRating.Value - lowerBound > displayed_rating_range / 2)
            {
                nextDivDisplay.Flipped = false;
                nextDivDisplay.Anchor = Anchor.TopCentre;
                nextDivDisplay.X = 0;
                return;
            }

            nextDivDisplay.Flipped = true;
            nextDivDisplay.Anchor = Anchor.TopLeft;
            nextDivDisplay.X = (currentRating.Value - lowerBound) * widthPerRating + segmentContainer.Padding.Left;
        }

        protected override void PopIn()
        {
            int rating = currentRating.Value;

            currentRatingInstantaneous.Value = currentRating.Value = Math.Clamp(rating - 300, lowerBound, upperBound);
            UpdateRating(rating, true);
        }

        protected override void PopOut()
        {
        }

        private float calculateContainerOffset(float rating)
        {
            float filledBarLength = (rating - lowerBound) * widthPerRating;
            float totalBarLength = (upperBound - lowerBound) * widthPerRating + segmentContainer.Padding.TotalHorizontal;

            return Math.Clamp(-filledBarLength + DrawWidth / 2, -totalBarLength + DrawWidth, 0);
        }
    }
}
