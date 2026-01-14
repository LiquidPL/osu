// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays.Profile.Header.Components;
using osu.Game.Resources.Localisation.Web;

namespace osu.Game.Overlays.Team.Header.Components
{
    public partial class TeamActionsButton : ProfileActionsButton
    {
        public readonly Bindable<TeamProfileData?> TeamData = new Bindable<TeamProfileData?>();

        public override Popover GetPopover() => new TeamActionPopover(TeamData.Value!.Team);

        private partial class TeamActionPopover : ProfileActionPopover
        {
            private readonly APITeam team;

            public TeamActionPopover(APITeam team)
            {
                this.team = team;
            }

            [BackgroundDependencyLoader]
            private void load(IDialogOverlay? dialogOverlay)
            {
                Actions = new[]
                {
                    new ProfilePopoverAction(FontAwesome.Solid.ExclamationTriangle, ReportStrings.TeamButton)
                    {
                        Action = () =>
                        {
                            dialogOverlay?.Push(new ReportTeamDialog(team));
                            this.HidePopover();
                        }
                    }
                };
            }
        }
    }
}
