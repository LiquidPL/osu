// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;
using osu.Game.Online.Chat;

namespace osu.Game.Overlays.Team.Sections.Info
{
    public partial class UrlValueDisplay : InfoValueDisplay
    {
        public new LocalisableString Content
        {
            set
            {
                ContentTextFlow.Clear();
                ContentTextFlow.AddLink(value, LinkAction.External, value);
            }
        }
    }
}
