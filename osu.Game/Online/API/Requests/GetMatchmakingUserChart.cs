// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Online.API.Requests
{
    public class GetMatchmakingUserChart : APIRequest<APIMatchmakingUserChart>
    {
        public int PoolID { get; }

        public int UserID { get; }

        public GetMatchmakingUserChart(int poolID, int userID)
        {
            PoolID = poolID;
            UserID = userID;
        }

        protected override string Target => $"matchmaking-pools/{PoolID}/user-chart/{UserID}";
    }
}
