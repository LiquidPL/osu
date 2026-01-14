// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Localisation;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Chat;

namespace osu.Game.Overlays.Team.Sections.Info
{
    public partial class LeaderValueDisplay : InfoValueDisplay
    {
        public new LocalisableString Content
        {
            set => throw new NotSupportedException($"Use ({nameof(User)} instead.");
        }

        public APIUser? User
        {
            get;
            set
            {
                if (field == value)
                    return;

                field = value;
                Scheduler.AddOnce(updateLink);
            }
        }

        private void updateLink()
        {
            ContentTextFlow.Clear();

            if (User == null)
                return;

            ContentTextFlow.AddLink(User.Username, LinkAction.OpenUserProfile, User);
        }
    }
}
