// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Layout;

namespace osu.Game.Graphics.Containers
{
    public partial class CursedFlowContainer : CursedFlowContainer<Drawable>
    {
    }

    public partial class CursedFlowContainer<T> : Container<T>
        where T : Drawable
    {
        public float Spacing { get; init; }

        private readonly LayoutValue childLayout = new LayoutValue(Invalidation.RequiredParentSizeToFit, InvalidationSource.Child);

        public CursedFlowContainer()
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
                pos += child.DrawWidth + Spacing;
            }
        }
    }
}
