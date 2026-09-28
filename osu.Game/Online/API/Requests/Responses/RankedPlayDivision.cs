// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace osu.Game.Online.API.Requests.Responses
{
    public class RankedPlayDivision
    {
        [JsonProperty(@"key")]
        public string Key = string.Empty;

        [JsonProperty(@"tier")]
        public Tier Tier;

        [JsonProperty(@"display_name")]
        public string DisplayName = string.Empty;

        [JsonProperty(@"start_rating")]
        public int StartRating;

        [JsonProperty("@end_rating")]
        public int EndRating;
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum Tier
    {
        Bronze = 1,
        Silver = 2,
        Gold = 3,
        Platinum = 4,
        Rhodium = 5,
        Radiant = 6,
        Lustrous = 7,
    }
}
