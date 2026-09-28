// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays;
using osu.Game.Screens.OnlinePlay.Matchmaking.Queue;

namespace osu.Game.Tests.Visual.RankedPlay
{
    public partial class TestSceneTierProgressBar : OsuTestScene
    {
        [Cached]
        private readonly OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Aquamarine);

        private TierProgressBar progressBar = null!;

        private int customRating = 1000;

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
                        CurrentRating = 800,
                        Divisions =
                        [
                            new RankedPlayDivision { Key = "Bronze-I", Tier = Tier.Bronze, DisplayName = "Bronze I", StartRating = 600, EndRating = 699 },
                            new RankedPlayDivision { Key = "Bronze-II", Tier = Tier.Bronze, DisplayName = "Bronze II", StartRating = 700, EndRating = 799 },
                            new RankedPlayDivision { Key = "Bronze-III", Tier = Tier.Bronze, DisplayName = "Bronze III", StartRating = 800, EndRating = 899 },
                            new RankedPlayDivision { Key = "Silver-I", Tier = Tier.Silver, DisplayName = "Silver I", StartRating = 900, EndRating = 999 },
                            new RankedPlayDivision { Key = "Silver-II", Tier = Tier.Silver, DisplayName = "Silver II", StartRating = 1000, EndRating = 1099 },
                            new RankedPlayDivision { Key = "Silver-III", Tier = Tier.Silver, DisplayName = "Silver III", StartRating = 1100, EndRating = 1199 },
                            new RankedPlayDivision { Key = "Gold-I", Tier = Tier.Gold, DisplayName = "Gold I", StartRating = 1200, EndRating = 1299 },
                            new RankedPlayDivision { Key = "Gold-II", Tier = Tier.Gold, DisplayName = "Gold II", StartRating = 1300, EndRating = 1399 },
                            new RankedPlayDivision { Key = "Gold-III", Tier = Tier.Gold, DisplayName = "Gold III", StartRating = 1400, EndRating = 1499 },
                            new RankedPlayDivision { Key = "Platinum-I", Tier = Tier.Platinum, DisplayName = "Platinum I", StartRating = 1500, EndRating = 1599 },
                            new RankedPlayDivision { Key = "Platinum-II", Tier = Tier.Platinum, DisplayName = "Platinum II", StartRating = 1600, EndRating = 1699 },
                            new RankedPlayDivision { Key = "Platinum-III", Tier = Tier.Platinum, DisplayName = "Platinum III", StartRating = 1700, EndRating = 1799 },
                            new RankedPlayDivision { Key = "Rhodium-I", Tier = Tier.Rhodium, DisplayName = "Rhodium I", StartRating = 1800, EndRating = 1899 },
                            new RankedPlayDivision { Key = "Rhodium-II", Tier = Tier.Rhodium, DisplayName = "Rhodium II", StartRating = 1900, EndRating = 1999 },
                            new RankedPlayDivision { Key = "Rhodium-III", Tier = Tier.Rhodium, DisplayName = "Rhodium III", StartRating = 2000, EndRating = 2099 },
                            new RankedPlayDivision { Key = "Radiant-I", Tier = Tier.Radiant, DisplayName = "Radiant I", StartRating = 2100, EndRating = 2199 },
                            new RankedPlayDivision { Key = "Radiant-II", Tier = Tier.Radiant, DisplayName = "Radiant II", StartRating = 2200, EndRating = 2299 },
                            new RankedPlayDivision { Key = "Radiant-III", Tier = Tier.Radiant, DisplayName = "Radiant III", StartRating = 2300, EndRating = 2399 },
                            new RankedPlayDivision { Key = "Lustrous", Tier = Tier.Lustrous, DisplayName = "Lustrous", StartRating = 2400, EndRating = int.MaxValue },
                        ],
                    },
                };
            });
            AddStep("set to 650", () => progressBar.CurrentRating = 650);
            AddStep("set to 900", () => progressBar.CurrentRating = 900);
            AddStep("set to 1800", () => progressBar.CurrentRating = 1800);
            AddStep("set to 2000", () => progressBar.CurrentRating = 2000);
            AddStep("set to 2200", () => progressBar.CurrentRating = 2200);
            AddStep("set to 2900", () => progressBar.CurrentRating = 2900);
            AddSliderStep("custom rating", 600, 3000, 1000, value => customRating = value);
            AddStep("set custom rating", () => progressBar.CurrentRating = customRating);
        }
    }
}
