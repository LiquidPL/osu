// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Overlays.Profile.Header.Components;
using osuTK;

namespace osu.Game.Overlays.Profile.Header
{
    public partial class DetailHeaderContainer : CompositeDrawable
    {
        public readonly Bindable<UserProfileData?> User = new Bindable<UserProfileData?>();

        private GridContainer cardContainer = null!;
        private FillFlowContainer rightColumn = null!;

        private SoloPlayProfileCard soloCard = null!;
        private RankedPlayProfileCard rankedCard = null!;

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider)
        {
            AutoSizeAxes = Axes.Y;

            InternalChildren = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colourProvider.Background5,
                },
                cardContainer = new GridContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Padding = new MarginPadding { Horizontal = WaveOverlayContainer.HORIZONTAL_PADDING, Vertical = 10 },
                    RowDimensions = new[]
                    {
                        new Dimension(GridSizeMode.AutoSize),
                    },
                    ColumnDimensions = new[]
                    {
                        new Dimension(),
                        new Dimension(GridSizeMode.Absolute, size: 10),
                        new Dimension(),
                        new Dimension(GridSizeMode.Absolute, size: 10),
                        new Dimension(),
                    },
                    Content = new[]
                    {
                        new Drawable?[]
                        {
                            soloCard = new SoloPlayProfileCard
                            {
                                RelativeSizeAxes = Axes.X,
                                User = { BindTarget = User },
                            },
                            null,
                            rankedCard = new RankedPlayProfileCard
                            {
                                RelativeSizeAxes = Axes.X,
                                User = { BindTarget = User },
                            },
                            null,
                            rightColumn = new FillFlowContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Direction = FillDirection.Vertical,
                                Spacing = new Vector2(5),
                                Children = new Drawable[]
                                {
                                    new PinnedBadgeProfileCard { User = { BindTarget = User } },
                                    new DailyChallengeProfileCard { User = { BindTarget = User } },
                                    new SeasonProfileCard { User = { BindTarget = User } },
                                    new ExtendedDetails
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        User = { BindTarget = User }
                                    },
                                },
                            },
                        },
                    },
                    // ColumnDimensions = new[]
                    // {
                    //     new Dimension(),
                    //     new Dimension(GridSizeMode.AutoSize),
                    //     new Dimension(GridSizeMode.AutoSize),
                    // },
                    // Content = new[]
                    // {
                    //     new Drawable[]
                    //     {
                    //         new MainDetails
                    //         {
                    //             RelativeSizeAxes = Axes.X,
                    //             User = { BindTarget = User }
                    //         },
                    //         new Box
                    //         {
                    //             RelativeSizeAxes = Axes.Y,
                    //             Width = 2,
                    //             Colour = colourProvider.Background6,
                    //             Margin = new MarginPadding { Horizontal = 15 }
                    //         },
                    //         new ExtendedDetails
                    //         {
                    //             Anchor = Anchor.CentreLeft,
                    //             Origin = Anchor.CentreLeft,
                    //             User = { BindTarget = User }
                    //         }
                    //     }
                    // }
                }
            };
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            // soloCard.Height = rankedCard.Height = Math.Max(cardContainer.DrawHeight - cardContainer.Padding.TotalHorizontal, rightColumn.DrawHeight);
            // cardContainer.Height = Math.Max(soloCard.DrawHeight, Math.Max(rankedCard.DrawHeight, rightColumn.DrawHeight)) + 20;
        }
    }
}
