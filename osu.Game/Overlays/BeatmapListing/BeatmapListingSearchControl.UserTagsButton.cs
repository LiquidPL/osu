// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Game.Beatmaps.Drawables;
using osu.Game.Graphics.Containers;
using osuTK;

namespace osu.Game.Overlays.BeatmapListing
{
    public partial class BeatmapListingSearchControl
    {
        private partial class UserTagsButton : OsuClickableContainer, IHasPopover
        {
            private readonly SpriteIcon icon;

            public UserTagsButton()
            {
                AutoSizeAxes = Axes.Both;
                Margin = new MarginPadding { Right = 40 };
                Child = icon = new SpriteIcon
                {
                    Icon = FontAwesome.Solid.Tag,
                    Size = new Vector2(20),
                };
                Action = this.ShowPopover;
            }

            public Popover GetPopover() => new UserTagsPopover
            {
                AllowableAnchors = new[] { Anchor.TopLeft, Anchor.BottomLeft }
            };
        }
    }
}
