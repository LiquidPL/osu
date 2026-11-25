// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Localisation;
using osu.Game.Extensions;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Resources.Localisation.Web;

namespace osu.Game.Overlays.Profile.Header.Components
{
    public partial class RankedPlayRatingGraph : UserGraph<DateTimeOffset, int>
    {
        public readonly Bindable<APIUserMatchmakingRatingHistory[]?> Statistics = new Bindable<APIUserMatchmakingRatingHistory[]?>();

        protected override void LoadComplete()
        {
            base.LoadComplete();
            Statistics.BindValueChanged(e => updateHistory(e.NewValue), true);
        }

        private void updateHistory(APIUserMatchmakingRatingHistory[]? history)
        {
            Data = history?.Reverse().Select(h => new KeyValuePair<DateTimeOffset, int>(h.CreatedAt, h.RatingAfter)).ToArray();
        }

        protected override float GetDataPointHeight(int rating) => MathF.Log(rating);

        protected override UserGraphTooltipContent GetTooltipContent(DateTimeOffset date, int rating) => new UserGraphTooltipContent
        {
            Name = UsersStrings.ShowMatchmakingRating,
            Count = rating.ToStandardFormattedString(0),
            Time = LocalisableString.Interpolate($"{date:yyyy/MM/dd}"),
        };
    }
}
