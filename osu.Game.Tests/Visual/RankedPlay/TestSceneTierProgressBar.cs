// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Overlays;
using osu.Game.Screens.OnlinePlay.Matchmaking.Queue;

namespace osu.Game.Tests.Visual.RankedPlay
{
    public partial class TestSceneTierProgressBar : OsuTestScene
    {
        [Cached]
        private readonly OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Aquamarine);

        private TierProgressBar progressBar = null!;

        public TestSceneTierProgressBar()
        {
            AddStep("create", () =>
            {
                Child = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding(10),
                    Child = progressBar = new TierProgressBar
                    {
                        RelativeSizeAxes = Axes.X,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        CurrentRating = 225,
                    },
                };
            });
            AddStep("set to 1000", () => progressBar.CurrentRating = 1000);
            AddStep("set to 1400", () => progressBar.CurrentRating = 1400);
            AddStep("set to 800", () => progressBar.CurrentRating = 800);
            AddStep("set to 100", () => progressBar.CurrentRating = 100);
            AddStep("set to 375", () => progressBar.CurrentRating = 375);
        }
    }
}
