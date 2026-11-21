// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Resources.Localisation.Web;
using osu.Game.Scoring;
using osuTK;

namespace osu.Game.Overlays.Profile.Header.Components
{
    public partial class SoloPlayProfileCard : ProfileHeaderCard
    {
        public readonly IBindable<UserProfileData?> User = new Bindable<UserProfileData?>();

        private GlobalRankDisplay globalRankDisplay = null!;
        private ProfileValueDisplay countryRankDisplay = null!;
        private ProfileValueDisplay ppDisplay = null!;
        private ProfileValueDisplay medalsDisplay = null!;
        private RankGraph rankGraph = null!;

        private readonly Dictionary<ScoreRank, MainDetails.ScoreRankInfo> scoreRankInfos = new Dictionary<ScoreRank, MainDetails.ScoreRankInfo>();

        public SoloPlayProfileCard()
            : base(UsersStrings.ShowSoloTitle, OsuIcon.Solo)
        {
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            TopContent.Child = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Spacing = new Vector2(10),
                Direction = FillDirection.Vertical,
                Children = new Drawable[]
                {
                    globalRankDisplay = new GlobalRankDisplay(),
                    countryRankDisplay = new ProfileValueDisplay(ProfileValueDisplay.ValueSize.Medium)
                    {
                        Title = UsersStrings.ShowRankCountrySimple
                    },
                    new GridContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                        ColumnDimensions = new[]
                        {
                            new Dimension(),
                            new Dimension(),
                        },
                        Content = new[]
                        {
                            new Drawable[]
                            {
                                ppDisplay = new ProfileValueDisplay { Title = @"pp" },
                                medalsDisplay = new ProfileValueDisplay { Title = UsersStrings.ShowStatsMedals },
                            },
                        },
                    },
                },
            };

            MiddleContent.Child = new Container
            {
                RelativeSizeAxes = Axes.X,
                Height = 60,
                Child = new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(5),
                    Children = new[]
                    {
                        scoreRankInfos[ScoreRank.XH] = new MainDetails.ScoreRankInfo(ScoreRank.XH),
                        scoreRankInfos[ScoreRank.X] = new MainDetails.ScoreRankInfo(ScoreRank.X),
                        scoreRankInfos[ScoreRank.SH] = new MainDetails.ScoreRankInfo(ScoreRank.SH),
                        scoreRankInfos[ScoreRank.S] = new MainDetails.ScoreRankInfo(ScoreRank.S),
                        scoreRankInfos[ScoreRank.A] = new MainDetails.ScoreRankInfo(ScoreRank.A),
                    },
                },
            };

            BottomContent.Child = new Container
            {
                RelativeSizeAxes = Axes.X,
                Height = 80,
                Padding = new MarginPadding { Vertical = 10, Right = 15 },
                Child = rankGraph = new RankGraph
                {
                    RelativeSizeAxes = Axes.Both,
                },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            User.BindValueChanged(e => updateDisplay(e.NewValue), true);
        }

        private void updateDisplay(UserProfileData? data)
        {
            var user = data?.User;

            ppDisplay.Content.Text = user?.Statistics?.PP?.ToLocalisableString("#,##0") ?? (LocalisableString)"0";
            medalsDisplay.Content.Text = user?.Achievements?.Length.ToString() ?? "0";

            globalRankDisplay.HighestRank.Value = user?.RankHighest;
            globalRankDisplay.UserStatistics.Value = user?.Statistics;

            countryRankDisplay.Content.Text = user?.Statistics?.CountryRank?.ToLocalisableString("\\##,##0") ?? (LocalisableString)"-";

            foreach (var scoreRankInfo in scoreRankInfos)
                scoreRankInfo.Value.RankCount = user?.Statistics?.GradesCount[scoreRankInfo.Key] ?? 0;

            rankGraph.Statistics.Value = user?.Statistics;
        }
    }
}
