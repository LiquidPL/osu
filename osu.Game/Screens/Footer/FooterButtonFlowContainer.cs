// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Layout;
using osuTK;

namespace osu.Game.Screens.Footer
{
    /// <summary>
    /// A simple flow container that lays the footer button horizontally with the specified spacing,
    /// and only updates the layout when any of the button changes its size.
    /// </summary>
    public partial class FooterButtonFlowContainer : Container<ScreenFooterButton>
    {
        public Vector2 Spacing { get; init; }

        private readonly LayoutValue childLayout = new LayoutValue(Invalidation.RequiredParentSizeToFit, InvalidationSource.Child);

        public FooterButtonFlowContainer()
        {
            AddLayout(childLayout);
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            if (!childLayout.IsValid)
            {
                performLayout();
                childLayout.Validate();
            }
        }

        private void performLayout()
        {
            float pos = 0;

            foreach (var child in Children)
            {
                child.X = pos;
                pos += child.DrawWidth + Spacing.X;
            }
        }
    }
}
