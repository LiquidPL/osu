// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;

namespace osu.Game.Screens.Footer
{
    /// <summary>
    /// Marker interface for buttons to be displayed on <see cref="ScreenFooter"/>.
    /// Buttons must implement this interface in order to use the transform methods
    /// defined in <see cref="FooterButtonExtensions"/>.
    /// </summary>
    public interface IFooterButton : IDrawable
    {
    }
}
