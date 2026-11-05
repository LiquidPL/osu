// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading.Tasks;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Online.RankedPlay
{
    public interface IRankedPlayDivisionsCache
    {
        public Task<APIRankedPlayDivision[]> GetDivisions();

        public Task<APIRankedPlayDivision?> GetDivisionForRating(int rating);
    }
}
