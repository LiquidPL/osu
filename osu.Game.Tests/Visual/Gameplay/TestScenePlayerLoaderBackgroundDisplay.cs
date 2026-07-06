// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Testing;
using osu.Game.Overlays;
using osu.Game.Rulesets.Osu;
using osu.Game.Screens.Play;

namespace osu.Game.Tests.Visual.Gameplay
{
    public partial class TestScenePlayerLoaderBackgroundDisplay : OsuTestScene
    {
        [Cached]
        private readonly OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Aquamarine);

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("create", () => Child = new PlayerLoader.BackgroundCover(CreateWorkingBeatmap(new OsuRuleset().RulesetInfo))
            {
                Anchor = Anchor.TopCentre,
                Origin = Anchor.TopCentre,
            });
        }
    }
}
