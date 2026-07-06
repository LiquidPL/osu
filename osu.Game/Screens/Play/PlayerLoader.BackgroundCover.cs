// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Game.Beatmaps;
using osu.Game.Graphics;
using osu.Game.Overlays;

namespace osu.Game.Screens.Play
{
    public partial class PlayerLoader
    {
        public partial class BackgroundCover : CompositeDrawable
        {
            public const int HEIGHT = 240;
            public const int CORNER_RADIUS = 40;

            private readonly IWorkingBeatmap beatmap;

            private IBindable<StarDifficulty> starDifficulty = new Bindable<StarDifficulty>();

            [Resolved]
            private OsuColour colours { get; set; } = null!;

            [Resolved]
            private OverlayColourProvider colourProvider { get; set; } = null!;

            public BackgroundCover(IWorkingBeatmap beatmap)
            {
                this.beatmap = beatmap;
            }

            private Box gradientBoxLarge = null!;
            private Box gradientBoxSmall = null!;

            [BackgroundDependencyLoader]
            private void load(BeatmapDifficultyCache difficultyCache)
            {
                starDifficulty = difficultyCache.GetBindableDifficulty(beatmap.BeatmapInfo);

                Width = 800;
                Height = HEIGHT + CORNER_RADIUS;
                Y = -CORNER_RADIUS;

                Masking = true;
                BorderThickness = 3;
                CornerRadius = 40;

                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colourProvider.Background5,
                    },
                    new Sprite
                    {
                        RelativeSizeAxes = Axes.Both,
                        Texture = beatmap.GetBackground(),
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        FillMode = FillMode.Fill,
                    },
                    gradientBoxLarge = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Anchor = Anchor.BottomCentre,
                        Origin = Anchor.BottomCentre,
                    },
                    gradientBoxSmall = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Height = 0.5f,
                        Anchor = Anchor.BottomCentre,
                        Origin = Anchor.BottomCentre,
                        Alpha = 0.5f,
                    },
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                starDifficulty.BindValueChanged(e =>
                {
                    var colour = colours.ForStarDifficulty(e.NewValue.Stars);

                    BorderColour = colour;
                    gradientBoxLarge.Colour = ColourInfo.GradientVertical(colour.Opacity(0.5f), colour);
                    gradientBoxSmall.Colour = ColourInfo.GradientVertical(Colour4.Transparent, colour);
                }, true);

                Schedule(() => gradientBoxLarge.FadeOut(500, Easing.Out));
            }
        }
    }
}
