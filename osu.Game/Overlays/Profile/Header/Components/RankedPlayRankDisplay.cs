// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.RankedPlay;
using osu.Game.Resources.Localisation.Web;

namespace osu.Game.Overlays.Profile.Header.Components
{
    public partial class RankedPlayRankDisplay : CompositeDrawable
    {
        public Bindable<APIUserMatchmakingStatistics?> Statistics = new Bindable<APIUserMatchmakingStatistics?>();

        private ProfileValueDisplay info = null!;

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        [Resolved]
        private IRankedPlayDivisionStore divisionStore { get; set; } = null!;

        public RankedPlayRankDisplay()
        {
            AutoSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChild = info = new ProfileValueDisplay(ProfileValueDisplay.ValueSize.Large)
            {
                Title = UsersStrings.ShowMatchmakingRank,
            };

            info.Content.Font = info.Content.Font.With(weight: FontWeight.Bold);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Statistics.BindValueChanged(_ => updateState());
        }

        private void updateState()
        {
            info.Content.Text = Statistics.Value?.Rank?.ToLocalisableString("\\##,##0") ?? (LocalisableString)"-";
            info.Content.Colour = colourProvider.Content2;

            if (Statistics.Value == null)
                return;

            divisionStore.GetDivisionForRating(Statistics.Value.Rating).ContinueWith(result =>
            {
                var division = result.GetResultSafely();

                if (division == null)
                    return;

                info.Content.Colour = OsuColour.ForRankingTier(division.Tier);
            });
        }
    }
}
