// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Rulesets;
using osu.Game.Screens.OnlinePlay.Matchmaking.RankedPlay.Components;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.OnlinePlay.Matchmaking.Queue
{
    public partial class ScreenQueue
    {
        public partial class SideContainer : VisibilityContainer
        {
            protected override bool StartHidden => true;

            protected override Container<Drawable> Content => content;

            private readonly FillFlowContainer content;

            public SideContainer()
            {
                AutoSizeAxes = Axes.X;
                Height = 140;
                Alpha = 0;
                InternalChildren = new Drawable[]
                {
                    new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Masking = true,
                        CornerRadius = CORNER_RADIUS,
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = Color4.Black.Opacity(0.2f),
                            },
                        },
                    },
                    content = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.Y,
                        AutoSizeAxes = Axes.X,
                        Spacing = new Vector2(40),
                    },
                };
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                Padding = Anchor.HasFlag(Anchor.x0)
                    ? new MarginPadding { Left = -CORNER_RADIUS }
                    : new MarginPadding { Right = -CORNER_RADIUS };

                content.Anchor = Anchor;
                content.Origin = Origin;

                content.Padding = Anchor.HasFlag(Anchor.x0)
                    ? new MarginPadding { Left = 40 + CORNER_RADIUS, Right = 40 }
                    : new MarginPadding { Left = 40, Right = 40 + CORNER_RADIUS };
            }

            protected override void PopIn()
            {
                this.FadeIn(500);
            }

            protected override void PopOut()
            {
            }

            public partial class Statistic : CompositeDrawable
            {
                public LocalisableString Label
                {
                    get;
                    set
                    {
                        field = value;
                        labelText.Text = value;
                    }
                }

                public LocalisableString Value
                {
                    get;
                    set
                    {
                        field = value;
                        valueText.Text = value;
                    }
                }

                private readonly OsuSpriteText labelText;
                private readonly OsuSpriteText valueText;

                public Statistic()
                {
                    RelativeSizeAxes = Axes.Y;
                    AutoSizeAxes = Axes.X;
                    Alpha = 0;

                    InternalChild = new FillFlowContainer
                    {
                        AutoSizeAxes = Axes.Both,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Direction = FillDirection.Vertical,
                        AlwaysPresent = true,
                        Children = new Drawable[]
                        {
                            labelText = new OsuSpriteText
                            {
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                Font = OsuFont.Torus.With(size: 14, weight: FontWeight.SemiBold),
                                Text = Label,
                            },
                            valueText = new OsuSpriteText
                            {
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                Font = OsuFont.Torus.With(size: 42, weight: FontWeight.Light),
                                Text = Value,
                            },
                        },
                    };
                }
            }

            public partial class RatingStatistic : Statistic
            {
                public new int Value
                {
                    get;
                    set
                    {
                        field = value;
                        base.Value = value.ToLocalisableString(@"N0");
                        Scheduler.AddOnce(updateBadge);
                    }
                }

                public IBindable<RulesetInfo> Ruleset = new Bindable<RulesetInfo>();

                private readonly Container badgeContainer;

                public RatingStatistic()
                {
                    AddInternal(badgeContainer = new Container
                    {
                        AutoSizeAxes = Axes.Both,
                        Anchor = Anchor.BottomCentre,
                        Origin = Anchor.BottomCentre,
                        Y = 15,
                    });
                }

                protected override void LoadComplete()
                {
                    base.LoadComplete();

                    Ruleset.BindValueChanged(_ => Scheduler.AddOnce(updateBadge), true);
                }

                private void updateBadge()
                {
                    badgeContainer.Child = new DivisionBadge(
                        new RankedPlayDivision { Tier = Tier.Bronze, Division = Division.I, DisplayName = "Bronze I", StartRating = 600, EndRating = 699 },
                        Ruleset.Value
                    )
                    {
                        Scale = new Vector2(0.5f),
                    };
                }
            }
        }
    }
}

