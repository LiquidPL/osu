// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Transforms;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osuTK;

namespace osu.Game.Screens.OnlinePlay.DailyChallenge
{
    public partial class PlayFooterButton : ShearedButton
    {
        public PlayFooterButton()
            : base(width: 220)
        {
        }

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            DarkerColour = colours.Green3;
            LighterColour = colours.Green1;

            ButtonContent.Padding = new MarginPadding { Horizontal = 20 };
            ButtonContent.AutoSizeAxes = Axes.Y;
            ButtonContent.RelativeSizeAxes = Axes.X;

            ButtonContent.Children = new Drawable[]
            {
                new OsuSpriteText
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    X = -10,
                    Font = OsuFont.TorusAlternate.With(size: 17),
                    Text = "Play!",
                    UseFullGlyphHeight = false,
                },
                new SpriteIcon
                {
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    Icon = OsuIcon.Play,
                    Size = new Vector2(24),
                }
            };
        }

        public void Appear()
        {
            FinishTransforms();

            this.MoveToY(150f)
                .FadeOut()
                .MoveToY(0f, 240, Easing.OutCubic)
                .FadeIn(240, Easing.OutCubic);
        }

        public TransformSequence<PlayFooterButton> Disappear()
        {
            FinishTransforms();

            return this.FadeOut(240, Easing.InOutCubic)
                       .MoveToY(150f, 240, Easing.InOutCubic);
        }
    }
}
