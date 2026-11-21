// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Extensions;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Resources.Localisation.Web;

namespace osu.Game.Overlays.Profile.Header.Components
{
    public partial class RankedPlayRatingGraph : UserGraph<DateTimeOffset, int>
    {
        public readonly Bindable<APIUserMatchmakingStatistics?> Statistics = new Bindable<APIUserMatchmakingStatistics?>();

        private readonly OsuSpriteText placeholder;

        public RankedPlayRatingGraph()
        {
            Add(placeholder = new OsuSpriteText
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Text = UsersStrings.ShowExtraUnranked,
                Font = OsuFont.GetFont(size: 12, weight: FontWeight.Regular)
            });
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            Statistics.BindValueChanged(e => updateStatistics(e.NewValue), true);
        }

        private void updateStatistics(APIUserMatchmakingStatistics? statistics)
        {
            Data = statistics?.RecentHistory.Select(h => new KeyValuePair<DateTimeOffset, int>(h.CreatedAt, h.RatingAfter)).ToArray();
        }

        protected override float GetDataPointHeight(int rating) => -MathF.Log(rating);

        protected override void ShowGraph()
        {
            base.ShowGraph();
            placeholder.FadeOut(FADE_DURATION, Easing.Out);
        }

        protected override void HideGraph()
        {
            base.HideGraph();
            placeholder.FadeIn(FADE_DURATION, Easing.Out);
        }

        protected override UserGraphTooltipContent GetTooltipContent(DateTimeOffset date, int rating) => new UserGraphTooltipContent
        {
            Name = UsersStrings.ShowMatchmakingRating,
            Count = rating.ToStandardFormattedString(0),
            Time = LocalisableString.Interpolate($"{date.ToLocalTime():yyyy/MM/dd}"),
        };
    }
}
