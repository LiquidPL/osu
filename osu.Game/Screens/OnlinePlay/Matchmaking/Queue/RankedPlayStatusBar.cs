// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.OnlinePlay.Matchmaking.Queue
{
    public partial class RankedPlayStatusBar : CompositeDrawable
    {
        public LocalisableString TopText
        {
            get => topText.Text;
            set => topText.Text = value.ToUpper();
        }

        public LocalisableString BottomText
        {
            get => bottomText.Text;
            set => bottomText.Text = value.ToUpper();
        }

        private readonly Circle lineLeft;
        private readonly Circle lineRight;

        private readonly Circle lineMiddle;
        private readonly Circle lineMiddleThick;

        private readonly OsuSpriteText topText;
        private readonly OsuSpriteText bottomText;

        public RankedPlayStatusBar()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            InternalChild = new GridContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                ColumnDimensions = new[]
                {
                    new Dimension(),
                    new Dimension(GridSizeMode.Absolute, size: 6),
                    new Dimension(GridSizeMode.Absolute, size: 500),
                    new Dimension(GridSizeMode.Absolute, size: 6),
                    new Dimension(),
                },
                RowDimensions = new[]
                {
                    new Dimension(GridSizeMode.AutoSize),
                },
                Content = new[]
                {
                    new Drawable?[]
                    {
                        lineLeft = new Circle
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 2,
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Colour = Color4.White.Opacity(0.25f),
                        },
                        null,
                        new Container
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Children = new Drawable[]
                            {
                                lineMiddle = new Circle
                                {
                                    RelativeSizeAxes = Axes.X,
                                    Height = 2,
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Colour = Color4.White.Opacity(0.5f),
                                },
                                lineMiddleThick = new Circle
                                {
                                    RelativeSizeAxes = Axes.X,
                                    Width = 0.33f,
                                    Height = 4,
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Colour = Color4.White,
                                },
                                topText = new OsuSpriteText
                                {
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Y = -20,
                                    Font = OsuFont.Torus.With(size: 18, weight: FontWeight.Bold),
                                    Colour = Color4.White,
                                    Spacing = new Vector2(4),
                                },
                                bottomText = new OsuSpriteText
                                {
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Y = 20,
                                    Font = OsuFont.Torus.With(size: 12, weight: FontWeight.Bold),
                                    Colour = Color4.White.Opacity(0.5f),
                                    Spacing = new Vector2(2),
                                }
                            },
                        },
                        null,
                        lineRight = new Circle
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 2,
                            Anchor = Anchor.CentreRight,
                            Origin = Anchor.CentreRight,
                            Colour = Color4.White.Opacity(0.25f),
                        },
                    },
                },
            };
        }
    }
}
