// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays;
using osu.Game.Screens.OnlinePlay.Matchmaking.Queue;

namespace osu.Game.Tests.Visual.RankedPlay
{
    public partial class TestSceneTierProgressBar : OsuTestScene
    {
        [Cached]
        private readonly OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Aquamarine);

        private readonly Container content;
        protected override Container<Drawable> Content => content;

        private TierProgressBar progressBar = null!;

        private int customRating = 1000;

        public TestSceneTierProgressBar()
        {
            base.Content.Child = content = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Padding = new MarginPadding(10),
            };
        }

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("create", () => Child = createProgressBar());
        }

        [Test]
        public void TestRatingChange()
        {
            AddStep("set to 650", () => progressBar.UpdateRating(650));
            AddStep("set to 900", () => progressBar.UpdateRating(900));
            AddStep("set to 1800", () => progressBar.UpdateRating(1800));
            AddStep("set to 1975", () => progressBar.UpdateRating(1975));
            AddStep("set to 2200", () => progressBar.UpdateRating(2200));
            AddStep("set to 2750", () => progressBar.UpdateRating(2750));
            AddSliderStep("custom rating", 600, 3000, 1000, value => customRating = value);
            AddStep("set custom rating", () => progressBar.UpdateRating(customRating));
        }

        [Test]
        public void TestPopIn()
        {
            AddStep("hide", () => progressBar.Hide());
            AddStep("set to 1250", () => progressBar.UpdateRating(1250));
            AddStep("show", () => progressBar.Show());
        }

        [Test]
        public void TestUpdateDivisions()
        {
            float originalLength = 0;

            AddStep("set to 2500", () => progressBar.UpdateRating(2500));
            AddStep("skip animation", () =>
            {
                progressBar.FinishTransforms();
                progressBar.ChildrenOfType<FillFlowContainer<TierProgressBar.DivisionSegment>>().Single().FinishTransforms();
            });
            AddStep("save length", () => originalLength = getLastDivisionLength());
            AddStep("update divisions", () => progressBar.Divisions =
            [
                new RankedPlayDivision { Tier = Tier.Bronze, Division = Division.I, DisplayName = "Bronze I", StartRating = 600, EndRating = 699 },
                new RankedPlayDivision { Tier = Tier.Bronze, Division = Division.II, DisplayName = "Bronze II", StartRating = 700, EndRating = 799 },
                new RankedPlayDivision { Tier = Tier.Bronze, Division = Division.III, DisplayName = "Bronze III", StartRating = 800, EndRating = 899 },
                new RankedPlayDivision { Tier = Tier.Silver, Division = Division.I, DisplayName = "Silver I", StartRating = 900, EndRating = 999 },
                new RankedPlayDivision { Tier = Tier.Silver, Division = Division.II, DisplayName = "Silver II", StartRating = 1000, EndRating = 1099 },
                new RankedPlayDivision { Tier = Tier.Silver, Division = Division.III, DisplayName = "Silver III", StartRating = 1100, EndRating = 1199 },
                new RankedPlayDivision { Tier = Tier.Gold, Division = Division.I, DisplayName = "Gold I", StartRating = 1200, EndRating = 1299 },
                new RankedPlayDivision { Tier = Tier.Gold, Division = Division.II, DisplayName = "Gold II", StartRating = 1300, EndRating = 1399 },
                new RankedPlayDivision { Tier = Tier.Gold, Division = Division.III, DisplayName = "Gold III", StartRating = 1400, EndRating = 1499 },
                new RankedPlayDivision { Tier = Tier.Platinum, Division = Division.I, DisplayName = "Platinum I", StartRating = 1500, EndRating = 1599 },
                new RankedPlayDivision { Tier = Tier.Platinum, Division = Division.II, DisplayName = "Platinum II", StartRating = 1600, EndRating = 1699 },
                new RankedPlayDivision { Tier = Tier.Platinum, Division = Division.III, DisplayName = "Platinum III", StartRating = 1700, EndRating = 1799 },
                new RankedPlayDivision { Tier = Tier.Rhodium, Division = Division.I, DisplayName = "Rhodium I", StartRating = 1800, EndRating = 1899 },
                new RankedPlayDivision { Tier = Tier.Rhodium, Division = Division.II, DisplayName = "Rhodium II", StartRating = 1900, EndRating = 1999 },
                new RankedPlayDivision { Tier = Tier.Rhodium, Division = Division.III, DisplayName = "Rhodium III", StartRating = 2000, EndRating = 2099 },
                new RankedPlayDivision { Tier = Tier.Radiant, Division = Division.I, DisplayName = "Radiant I", StartRating = 2100, EndRating = 2199 },
                new RankedPlayDivision { Tier = Tier.Radiant, Division = Division.II, DisplayName = "Radiant II", StartRating = 2200, EndRating = 2299 },
                new RankedPlayDivision { Tier = Tier.Radiant, Division = Division.III, DisplayName = "Radiant III", StartRating = 2300, EndRating = 2399 },
                new RankedPlayDivision { Tier = Tier.Lustrous, DisplayName = "Lustrous", StartRating = 2400, EndRating = 2700 },
            ]);
            AddAssert("last division is shorter", () => getLastDivisionLength() / originalLength, () => Is.EqualTo(0.5).Within(0.001));

            float getLastDivisionLength() => this.ChildrenOfType<TierProgressBar.DivisionSegment>().Last().DrawWidth;
        }

        private TierProgressBar createProgressBar() => progressBar = new TierProgressBar
        {
            RelativeSizeAxes = Axes.X,
            Anchor = Anchor.Centre,
            Origin = Anchor.Centre,
            Divisions =
            [
                new RankedPlayDivision { Tier = Tier.Bronze, Division = Division.I, DisplayName = "Bronze I", StartRating = 600, EndRating = 699 },
                new RankedPlayDivision { Tier = Tier.Bronze, Division = Division.II, DisplayName = "Bronze II", StartRating = 700, EndRating = 799 },
                new RankedPlayDivision { Tier = Tier.Bronze, Division = Division.III, DisplayName = "Bronze III", StartRating = 800, EndRating = 899 },
                new RankedPlayDivision { Tier = Tier.Silver, Division = Division.I, DisplayName = "Silver I", StartRating = 900, EndRating = 999 },
                new RankedPlayDivision { Tier = Tier.Silver, Division = Division.II, DisplayName = "Silver II", StartRating = 1000, EndRating = 1099 },
                new RankedPlayDivision { Tier = Tier.Silver, Division = Division.III, DisplayName = "Silver III", StartRating = 1100, EndRating = 1199 },
                new RankedPlayDivision { Tier = Tier.Gold, Division = Division.I, DisplayName = "Gold I", StartRating = 1200, EndRating = 1299 },
                new RankedPlayDivision { Tier = Tier.Gold, Division = Division.II, DisplayName = "Gold II", StartRating = 1300, EndRating = 1399 },
                new RankedPlayDivision { Tier = Tier.Gold, Division = Division.III, DisplayName = "Gold III", StartRating = 1400, EndRating = 1499 },
                new RankedPlayDivision { Tier = Tier.Platinum, Division = Division.I, DisplayName = "Platinum I", StartRating = 1500, EndRating = 1599 },
                new RankedPlayDivision { Tier = Tier.Platinum, Division = Division.II, DisplayName = "Platinum II", StartRating = 1600, EndRating = 1699 },
                new RankedPlayDivision { Tier = Tier.Platinum, Division = Division.III, DisplayName = "Platinum III", StartRating = 1700, EndRating = 1799 },
                new RankedPlayDivision { Tier = Tier.Rhodium, Division = Division.I, DisplayName = "Rhodium I", StartRating = 1800, EndRating = 1899 },
                new RankedPlayDivision { Tier = Tier.Rhodium, Division = Division.II, DisplayName = "Rhodium II", StartRating = 1900, EndRating = 1999 },
                new RankedPlayDivision { Tier = Tier.Rhodium, Division = Division.III, DisplayName = "Rhodium III", StartRating = 2000, EndRating = 2099 },
                new RankedPlayDivision { Tier = Tier.Radiant, Division = Division.I, DisplayName = "Radiant I", StartRating = 2100, EndRating = 2199 },
                new RankedPlayDivision { Tier = Tier.Radiant, Division = Division.II, DisplayName = "Radiant II", StartRating = 2200, EndRating = 2299 },
                new RankedPlayDivision { Tier = Tier.Radiant, Division = Division.III, DisplayName = "Radiant III", StartRating = 2300, EndRating = 2399 },
                new RankedPlayDivision { Tier = Tier.Lustrous, DisplayName = "Lustrous", StartRating = 2400, EndRating = 3000 },
            ],
        };
    }
}
