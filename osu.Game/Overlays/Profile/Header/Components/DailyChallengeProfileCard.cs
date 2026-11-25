// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Localisation;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Resources.Localisation.Web;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Overlays.Profile.Header.Components
{
    public partial class DailyChallengeProfileCard : ProfileHeaderCardSmall, IHasCustomTooltip<DailyChallengeTooltipData>
    {
        public readonly Bindable<UserProfileData?> User = new Bindable<UserProfileData?>();

        public DailyChallengeTooltipData? TooltipContent { get; private set; }

        private Container playCountContainer = null!;
        private OsuSpriteText playCount = null!;
        private CircularContainer completionMark = null!;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            Icon = OsuIcon.DailyChallenge;

            // this is somewhat unfortunate but that's how the web-side strings are
            Label.Text = UsersStrings.ShowDailyChallengeTitle.ToString().Replace("\\n", " ");
            Value.Text = UsersStrings.ShowDailyChallengePlaycount;

            Children = new Drawable[]
            {
                playCountContainer = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Masking = true,
                    CornerRadius = 4,
                    BorderThickness = 2,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Color4.Black.Opacity(0.25f),
                        },
                        playCount = new OsuSpriteText
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Font = OsuFont.Torus.With(size: 16, weight: FontWeight.Bold),
                        },
                    },
                },
                completionMark = new CircularContainer
                {
                    Alpha = 0,
                    Size = new Vector2(16),
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.Centre,
                    Masking = true,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = colours.Lime1,
                        },
                        new SpriteIcon
                        {
                            Size = new Vector2(8),
                            Colour = colourProvider.Background6,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Icon = FontAwesome.Solid.Check,
                        },
                    },
                },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            User.BindValueChanged(_ => updateDisplay(), true);
        }

        private void updateDisplay()
        {
            if (User.Value == null)
            {
                Hide();
                return;
            }

            APIUserDailyChallengeStatistics stats = User.Value.User.DailyChallengeStatistics;

            if (stats.PlayCount == 0)
            {
                Hide();
                return;
            }

            playCount.Text = DailyChallengeStatsDisplayStrings.UnitDay(stats.PlayCount.ToLocalisableString("N0"));
            playCount.Colour = OsuColour.ForRankingTier(DailyChallengeStatsTooltip.TierForPlayCount(stats.PlayCount));

            bool playedToday = stats.LastUpdate?.Date == DateTimeOffset.UtcNow.Date;
            bool userIsOnOwnProfile = stats.UserID == api.LocalUser.Value.Id;

            if (playedToday && userIsOnOwnProfile)
            {
                if (completionMark.Alpha > 0.8f)
                {
                    completionMark.ScaleTo(1.2f).ScaleTo(1, 800, Easing.OutElastic);
                }
                else
                {
                    completionMark.FadeIn(500, Easing.OutExpo);
                    completionMark.ScaleTo(1.6f).ScaleTo(1, 500, Easing.OutExpo);
                }

                playCountContainer.BorderColour = colours.Lime1;
            }
            else
            {
                completionMark.FadeOut(50);
                playCountContainer.BorderColour = default;
            }

            TooltipContent = new DailyChallengeTooltipData(colourProvider, stats);

            Show();
        }

        public ITooltip<DailyChallengeTooltipData> GetCustomTooltip() => new DailyChallengeStatsTooltip();
    }
}
