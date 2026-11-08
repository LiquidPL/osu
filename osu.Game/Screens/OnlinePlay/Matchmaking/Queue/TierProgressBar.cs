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
using osu.Game.Scoring;
using osuTK;

namespace osu.Game.Screens.OnlinePlay.Matchmaking.Queue
{
    public partial class TierProgressBar : Container
    {
        private readonly BindableInt currentRating = new BindableInt();
        private readonly BindableFloat currentRatingInstantaneous = new BindableFloat();

        private readonly Bindable<APIRankedPlayDivision> currentDivision = new Bindable<APIRankedPlayDivision>();

        public required IReadOnlyList<APIRankedPlayDivision> Divisions
        {
            private get;
            set
            {
                if (Transforms.Any() || segmentFlow.Transforms.Any())
                    throw new InvalidOperationException("Cannot set divisions while a transform is in progress.");

                field = value;
                currentRatingInstantaneous.Value = currentRating.Value = Math.Clamp(currentRating.Value, lowerBound, upperBound);

                createSegments();
            }
        }

        /// <summary>
        /// The initial rating value that should be displayed upon load.
        /// </summary>
        /// <remarks>
        /// This can be used when the progress bar needs to start at a certain
        /// rating, for instance on a results screen, when it should animate
        /// from the old rating to the new one after a completed match.
        /// </remarks>
        public int? InitialRating { private get; init; }

        private const int displayed_rating_range = 800;

        private int lowerBound => Divisions[0].StartRating;
        private int upperBound => Divisions[^1].EndRating;

        /// <summary>
        /// The width currently being taken up by a single rating point.
        /// </summary>
        private float widthPerRating;

        private readonly Box background;
        private readonly Container segmentWindow;
        private readonly FillFlowContainer<Segment> segmentFlow;
        private readonly RatingToNextDivisionDisplay nextDivDisplay;

        private readonly LayoutValue layout = new LayoutValue(Invalidation.DrawSize);

        public TierProgressBar()
        {
            // Set fixed height so that we don't get any extraneous layout invalidations
            // due to the `nextDivDisplay` fade in/out triggering autosizing;
            Height = 56;

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
                segmentWindow = new Container
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Masking = true,
                    Children = new Drawable[]
                    {
                        segmentFlow = new FillFlowContainer<Segment>
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
                    Anchor = Anchor.TopCentre,
                    Alpha = 0,
                },
            };

            AddLayout(layout);
        }

        private void createSegments()
        {
            segmentFlow.Clear();

            segmentFlow.Add(new Segment(lowerBound - 1));
            segmentFlow.AddRange(Divisions.Where(d => d.Tier != RankingTier.Lustrous).Select(d => new DivisionSegment(d)
            {
                CurrentRating = { BindTarget = currentRatingInstantaneous },
            }));
            // segmentFlow.Add(new DivisionSegment(new APIRankedPlayDivision
            // {
            //     Tier = RankingTier.Lustrous,
            //     StartRating = Divisions[^1].StartRating,
            //     // Increase the upper bound of the highest division beyond the
            //     EndRating = Divisions[^1].EndRating + 2000,
            // })
            // {
            //     CurrentRating = { BindTarget = currentRatingInstantaneous },
            // });

            int lastDivisionLower = Divisions[^1].StartRating;
            int lastDivisionUpper = Divisions[^1].EndRating + 2000;

            while (lastDivisionLower < lastDivisionUpper)
            {
                segmentFlow.Add(new DivisionSegment(new APIRankedPlayDivision
                {
                    Tier = RankingTier.Lustrous,
                    StartRating = lastDivisionLower,
                    EndRating = Math.Min(lastDivisionLower + 99, lastDivisionUpper),
                })
                {
                    CurrentRating = { BindTarget = currentRatingInstantaneous },
                });

                lastDivisionLower += 100;
            }

            updateSegmentSizes();
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
                // Cancel animations if screen has been resized.
                // Positioning of `segmentFlow` inside `segmentWindow` is dependent on
                // the `DrawWidth` of the latter at the moment the transform begins,
                // so we can no longer rely on it after a resize.
                FinishTransforms(true);

                updateSegmentSizes();
                layout.Validate();
            }
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (InitialRating != null)
                currentRatingInstantaneous.Value = currentRating.Value = InitialRating.Value;

            updateSegmentSizes();
        }

        private const int rating_change_duration = 1600;
        private static readonly IEasingFunction rating_change_easing = new CubicBezierEasingFunction(0, 0, 0, 1);

        private static readonly IEasingFunction flow_easing = new CubicBezierEasingFunction(0.15, 0.6, 0, 1);
        private static readonly IEasingFunction flow_easing_small_change = new CubicBezierEasingFunction(0, 0, 0, 1);

        private static readonly IEasingFunction next_div_display_fade = new CubicBezierEasingFunction(0.5, 0, 0.5, 1);

        public void SetRating(int newValue)
        {
            if (newValue < lowerBound || newValue > upperBound)
                throw new ArgumentException("Rating is outside the division bounds.");

            if (currentRating.Value == newValue)
                return;

            // Use a different, less 'jerky' easing function for small rating changes.
            IEasingFunction flowEasing = Math.Abs(currentRating.Value - newValue) <= 300
                ? flow_easing_small_change
                : flow_easing;

            currentRating.Value = newValue;

            // TODO: This is currently a bindable only so that `RatingToNextDivisionDisplay` has
            //       an up to date source of the division for the current rating. It should be
            //       refactored to fetch that data from some centralized source of division data,
            //       once it exists.
            currentDivision.Value = Divisions.First(d => d.StartRating <= currentRating.Value && d.EndRating >= currentRating.Value);

            this.TransformBindableTo(currentRatingInstantaneous, currentRating.Value, rating_change_duration, rating_change_easing);
            segmentFlow.MoveToX(calculateContainerOffset(currentRating.Value), rating_change_duration, flowEasing);

            var nextDivDisplayTransform = nextDivDisplay.FadeOut(100, next_div_display_fade);

            if (currentDivision.Value?.Tier == RankingTier.Lustrous)
                return;

            nextDivDisplayTransform
                .Then()
                .Schedule(() =>
                {
                    nextDivDisplay.UpdateLabel();
                })
                .Delay(1000)
                .FadeIn(200, next_div_display_fade);
        }

        private void updateSegmentSizes()
        {
            // We want the amount of displayed divisions/segments to remain consistent
            // regardless of screen dimensions.
            widthPerRating = segmentWindow.DrawWidth / displayed_rating_range;
            segmentFlow.X = calculateContainerOffset(currentRating.Value);

            foreach (var segment in segmentFlow)
                segment.Width = segment.RatingWidth * widthPerRating;
        }

        private float calculateContainerOffset(float rating)
        {
            float filledBarLength = rating * widthPerRating + segmentFlow.Padding.Left;

            return -filledBarLength + segmentWindow.DrawWidth / 2;
        }
    }
}
