// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Overlays.Profile.Header.Components
{
    public partial class ProfileHeaderCard : CompositeDrawable
    {
        private readonly LocalisableString title;
        private readonly IconUsage icon;

        protected Container TopContainer { get; private set; } = null!;
        protected Container TopContent { get; private set; } = null!;
        protected Container MiddleContent { get; private set; } = null!;
        protected Container BottomContent { get; private set; } = null!;

        public ProfileHeaderCard(LocalisableString title, IconUsage icon)
        {
            this.title = title;
            this.icon = icon;
        }

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider)
        {
            Masking = true;
            CornerRadius = 10;
            EdgeEffect = new EdgeEffectParameters
            {
                Type = EdgeEffectType.Shadow,
            };

            AutoSizeAxes = Axes.Y;

            InternalChildren = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colourProvider.Background3,
                },
                new GridContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    RowDimensions = new[]
                    {
                        new Dimension(GridSizeMode.AutoSize),
                        new Dimension(GridSizeMode.AutoSize),
                        new Dimension(GridSizeMode.AutoSize),
                    },
                    ColumnDimensions = new[]
                    {
                        new Dimension(),
                    },
                    Content = new[]
                    {
                        new Drawable[]
                        {
                            new Container
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Children = new Drawable[]
                                {
                                    new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Height = 0.7f,
                                        Anchor = Anchor.BottomLeft,
                                        Origin = Anchor.BottomLeft,
                                        Colour = ColourInfo.GradientVertical(Color4.Black.Opacity(0), Color4.Black.Opacity(0.1f)),
                                    },
                                    TopContainer = new Container
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Padding = new MarginPadding(10),
                                        Children = new Drawable[]
                                        {
                                            new FillFlowContainer
                                            {
                                                RelativeSizeAxes = Axes.X,
                                                AutoSizeAxes = Axes.Y,
                                                Direction = FillDirection.Vertical,
                                                Spacing = new Vector2(10),
                                                Children = new Drawable[]
                                                {
                                                    new FillFlowContainer
                                                    {
                                                        RelativeSizeAxes = Axes.X,
                                                        AutoSizeAxes = Axes.Y,
                                                        Direction = FillDirection.Horizontal,
                                                        Spacing = new Vector2(5),
                                                        Children = new Drawable[]
                                                        {
                                                            new SpriteIcon
                                                            {
                                                                Anchor = Anchor.CentreLeft,
                                                                Origin = Anchor.CentreLeft,
                                                                Size = new Vector2(24),
                                                                Icon = icon,
                                                                Colour = Color4.White,
                                                            },
                                                            new OsuSpriteText
                                                            {
                                                                Anchor = Anchor.CentreLeft,
                                                                Origin = Anchor.CentreLeft,
                                                                Text = title,
                                                                Font = OsuFont.Torus.With(size: 16, weight: FontWeight.Bold),
                                                                Colour = Color4.White,
                                                            },
                                                        },
                                                    },
                                                    TopContent = new Container
                                                    {
                                                        RelativeSizeAxes = Axes.X,
                                                        AutoSizeAxes = Axes.Y,
                                                    },
                                                },
                                            },
                                        },
                                    },
                                },
                            },
                        },
                        new Drawable[]
                        {
                            new Container
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Children = new Drawable[]
                                {
                                    new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = Color4.Black.Opacity(0.2f),
                                    },
                                    MiddleContent = new Container
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                    },
                                }
                            },
                        },
                        new Drawable[]
                        {
                            new Container
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Children = new Drawable[]
                                {
                                    new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = ColourInfo.GradientVertical(Color4.Black.Opacity(0.2f), Color4.Black.Opacity(0.4f)),
                                    },
                                    new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = Color4.Black.Opacity(0.2f),
                                    },
                                    BottomContent = new Container
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                    },
                                }
                            },
                        },
                    },
                },
            };
        }
    }
}
