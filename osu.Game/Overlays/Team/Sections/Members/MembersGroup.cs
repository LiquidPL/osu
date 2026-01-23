// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Layout;
using osu.Game.Extensions;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Resources.Localisation.Web;
using osu.Game.Users;
using osuTK;

namespace osu.Game.Overlays.Team.Sections.Members
{
    public partial class MembersGroup : CompositeDrawable
    {
        private const int padding = 5;
        private const int spacing = 5;

        public readonly Bindable<TeamProfileData?> TeamData = new Bindable<TeamProfileData?>();

        private OsuSpriteText memberCount = null!;
        private FillFlowContainer membersContainer = null!;

        private readonly LayoutValue layout = new LayoutValue(Invalidation.DrawSize);

        private float cardWidth;

        public MembersGroup()
        {
            AddLayout(layout);
        }

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider)
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
            Masking = true;
            CornerRadius = 10;
            InternalChildren = new Drawable[]
            {
                new Box
                {
                    Colour = colourProvider.Background2,
                    RelativeSizeAxes = Axes.Both,
                },
                new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Padding = new MarginPadding(padding),
                    Spacing = new Vector2(0, spacing),
                    Children = new Drawable[]
                    {
                        new Container
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Padding = new MarginPadding { Horizontal = 10 },
                            Children = new Drawable[]
                            {
                                new OsuSpriteText
                                {
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Font = OsuFont.GetFont(size: 14, weight: FontWeight.SemiBold),
                                    Colour = colourProvider.Content1,
                                    Shadow = false,
                                    Text = TeamsStrings.ShowMembersMembers,
                                },
                                memberCount = new OsuSpriteText
                                {
                                    Anchor = Anchor.CentreRight,
                                    Origin = Anchor.CentreRight,
                                    Font = OsuFont.GetFont(size: 14, weight: FontWeight.SemiBold),
                                    Colour = colourProvider.Content1,
                                    Shadow = false,
                                    Text = @"0",
                                },
                            }
                        },
                        membersContainer = new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Spacing = new Vector2(spacing),
                        },
                    },
                },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            TeamData.ValueChanged += data =>
            {
                if (data.OldValue?.Team.Id != data.NewValue?.Team.Id)
                    onTeamChanged(data.NewValue?.Team);
            };
            onTeamChanged(null);
        }

        protected override void Update()
        {
            base.Update();

            if (!layout.IsValid)
            {
                cardWidth = (DrawWidth - padding * 2 - spacing * 2) / 3;

                foreach (var card in membersContainer)
                    card.Width = cardWidth;

                layout.Validate();
            }
        }

        private void onTeamChanged(APITeam? team)
        {
            membersContainer.Clear();

            if (team == null)
                return;

            memberCount.Text = team.Members.Length.ToStandardFormattedString(0);
            membersContainer.AddRange(team.Members.Select(u => new UserGridPanel(u) { Width = cardWidth }));
        }
    }
}
