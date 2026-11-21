// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics;
using osu.Game.Overlays;
using osu.Game.Overlays.Profile.Header.Components;
using osuTK;

namespace osu.Game.Tests.Visual.Online
{
    public partial class TestSceneProfileHeaderCard : OsuTestScene
    {
        [Cached]
        private readonly OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Plum);

        public TestSceneProfileHeaderCard()
        {
            Child = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(10),
                Children = new Drawable[]
                {
                    new ProfileHeaderCard("Solo Play", OsuIcon.Solo)
                    {
                        Size = new Vector2(300, 400),
                    },
                    new ProfileHeaderCard("Ranked Play", OsuIcon.Multi)
                    {
                        Size = new Vector2(300, 400),
                    },
                },
            };
        }
    }
}
