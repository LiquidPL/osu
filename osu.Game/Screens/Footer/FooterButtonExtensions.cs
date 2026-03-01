// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Transforms;

namespace osu.Game.Screens.Footer
{
    public static class FooterButtonExtensions
    {
        public static TransformSequence<T> Appear<T>(this T button, double delay = 0)
            where T : Drawable, IFooterButton
        {
            button.ClearTransforms();

            return button.MoveToY(100f)
                         .FadeOut()
                         .Delay(delay)
                         .MoveToY(0f, 240, Easing.OutCubic)
                         .FadeIn(240, Easing.OutCubic);
        }

        public static TransformSequence<T> Disappear<T>(this T button, double delay = 0)
            where T : Drawable, IFooterButton
        {
            button.ClearTransforms();

            return button.Delay(delay)
                         .FadeOut(240, Easing.InOutCubic)
                         .MoveToY(100f, 240, Easing.InOutCubic);
        }
    }
}
