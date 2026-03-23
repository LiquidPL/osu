// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using Humanizer;
using osu.Framework.Allocation;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Game.Graphics;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;
using osu.Game.Online.Multiplayer;
using osuTK;

namespace osu.Game.Screens.OnlinePlay.Multiplayer.Match
{
    public partial class MultiplayerCountdownButton : ShearedButton, IHasPopover
    {
        public const int WIDTH = 40;

        private static readonly TimeSpan[] available_delays =
        {
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(30),
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(2)
        };

        public new required Action<TimeSpan> Action;
        public required Action CancelAction;

        [Resolved]
        private MultiplayerClient multiplayerClient { get; set; } = null!;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        private readonly Box flashLayer;

        public MultiplayerCountdownButton()
        {
            Width = WIDTH;

            ButtonContent.AutoSizeAxes = Axes.None;
            ButtonContent.RelativeSizeAxes = Axes.Both;

            ButtonContent.Children = new Drawable[]
            {
                flashLayer = new Box
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Shear = OsuGame.SHEAR,
                    RelativeSizeAxes = Axes.Both,
                    Colour = Colour4.White.Opacity(0.3f),
                    Blending = BlendingParameters.Additive,
                    Alpha = 0,
                },
                new SpriteIcon
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Icon = FontAwesome.Regular.Clock,
                    Size = new Vector2(18),
                    Shadow = true,
                    ShadowOffset = new Vector2(0.8f, 0.8f),
                },
            };

            base.Action = this.ShowPopover;

            TooltipText = MultiplayerMatchStrings.CountdownSettings;
        }

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            DarkerColour = colours.Green3;
            LighterColour = colours.Green1;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            multiplayerClient.RoomUpdated += onRoomUpdated;
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            multiplayerClient.RoomUpdated -= onRoomUpdated;
        }

        private void onRoomUpdated() => Scheduler.AddOnce(() =>
        {
            bool countdownActive = multiplayerClient.Room?.ActiveCountdowns.Any(c => c is MatchStartCountdown) == true;

            if (countdownActive)
            {
                DarkerColour = colours.YellowDark;
                LighterColour = colours.YellowLight;

                flashLayer.FadeOutFromOne(800, Easing.OutQuint)
                          .Then()
                          .Delay(200)
                          .Loop();
            }
            else
            {
                flashLayer.FadeOut();

                DarkerColour = colours.Green3;
                LighterColour = colours.Green1;
            }
        });

        public void Appear()
        {
            if (Width > 0)
                return;

            this.ResizeWidthTo(WIDTH, 200, Easing.OutQuint);
        }

        public void Disappear()
        {
            if (Width == 0)
                return;

            this.ResizeWidthTo(0, 200, Easing.OutQuint);
        }

        public Popover GetPopover()
        {
            var flow = new FillFlowContainer
            {
                Width = 200,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(2),
            };

            foreach (var duration in available_delays)
            {
                flow.Add(new RoundedButton
                {
                    RelativeSizeAxes = Axes.X,
                    Text = MultiplayerMatchStrings.StartMatchWithCountdown(duration.Humanize()),
                    BackgroundColour = colours.Green,
                    Action = () =>
                    {
                        Action(duration);
                        this.HidePopover();
                    }
                });
            }

            if (multiplayerClient.Room?.ActiveCountdowns.Any(c => c is MatchStartCountdown) == true && multiplayerClient.IsHost)
            {
                flow.Add(new RoundedButton
                {
                    RelativeSizeAxes = Axes.X,
                    Text = MultiplayerMatchStrings.StopCountdown,
                    BackgroundColour = colours.Red,
                    Action = () =>
                    {
                        CancelAction();
                        this.HidePopover();
                    }
                });
            }

            return new OsuPopover
            {
                Child = flow,
                AllowableAnchors = new[]
                {
                    Anchor.TopCentre,
                }
            };
        }
    }
}
