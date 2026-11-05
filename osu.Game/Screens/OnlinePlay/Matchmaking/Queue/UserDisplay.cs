// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Transforms;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.RankedPlay;
using osu.Game.Screens.OnlinePlay.Matchmaking.RankedPlay.Components;
using osu.Game.Users.Drawables;
using osuTK;

namespace osu.Game.Screens.OnlinePlay.Matchmaking.Queue
{
    public partial class UserDisplay : VisibilityContainer
    {
        protected override bool StartHidden => true;

        public readonly IBindable<ScreenQueue.UserStats?> UserStats = new Bindable<ScreenQueue.UserStats?>();

        private readonly CircularContainer avatarContainer;
        private readonly OsuSpriteText username;
        private readonly Container badgeContainer;

        [Resolved]
        private IRankedPlayDivisionsCache divisionsCache { get; set; } = null!;

        public UserDisplay(APIUser user)
        {
            AutoSizeAxes = Axes.Both;
            Origin = Anchor.Centre;

            InternalChildren = new Drawable[]
            {
                avatarContainer = new CircularContainer
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Masking = true,
                    Size = new Vector2(100),
                    Child = new ClickableAvatar(user, true)
                    {
                        RelativeSizeAxes = Axes.Both,
                    },
                    Alpha = 0,
                },
                new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Y = -75,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(6),
                    Children = new Drawable[]
                    {
                        badgeContainer = new Container
                        {
                            AutoSizeAxes = Axes.Both,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Alpha = 0,
                        },
                        username = new OsuSpriteText
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Font = OsuFont.Torus.With(size: 18, weight: FontWeight.SemiBold),
                            UseFullGlyphHeight = false,
                            Text = user.Username,
                            Alpha = 0,
                        },
                    },
                },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            UserStats.BindValueChanged(_ => Scheduler.AddOnce(updateRating), true);
        }

        private void updateRating()
        {
            badgeContainer.Clear();

            if (UserStats.Value == null)
                return;

            divisionsCache.GetDivisionForRating(UserStats.Value.Rating).ContinueWith(task =>
            {
                var division = task.GetResultSafely();

                if (division == null)
                    return;

                Schedule(() => badgeContainer.Child = new CompactDivisionBadge(division, UserStats.Value.Rank)
                {
                    Scale = new Vector2(0.8f),
                });
            });
        }

        protected override void PopIn()
        {
            avatarContainer.ScaleTo(0.5f)
                           .FadeIn(500, new CubicBezierEasingFunction(0, 0, 0, 1))
                           .ScaleTo(1, 1100, new CubicBezierEasingFunction(0.2, 2.7, 0.42, 1));

            badgeContainer.Delay(1200).FadeIn(400, new CubicBezierEasingFunction(0.5, 0, 0.5, 1));
            username.Delay(1250).FadeIn(400, new CubicBezierEasingFunction(0.5, 0, 0.5, 1));
        }

        protected override void PopOut()
        {
        }
    }
}
