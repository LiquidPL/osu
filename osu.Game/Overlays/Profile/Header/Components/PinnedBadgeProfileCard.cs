// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Users;
using osu.Game.Utils;

namespace osu.Game.Overlays.Profile.Header.Components
{
    public partial class PinnedBadgeProfileCard : ProfileHeaderCardSmall
    {
        public readonly Bindable<UserProfileData?> User = new Bindable<UserProfileData?>();

        private DrawableDate dateDisplay = null!;

        public PinnedBadgeProfileCard()
        {
            Icon = OsuIcon.Tournament;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            User.BindValueChanged(_ => updateDisplay(), true);
        }

        protected override OsuSpriteText CreateLabel() => new TruncatingSpriteText { RelativeSizeAxes = Axes.X };

        protected override OsuSpriteText CreateValue() => dateDisplay = new BadgeDrawableDate(DateTimeOffset.Now);

        private void updateDisplay()
        {
            if (User.Value == null)
            {
                Hide();
                return;
            }

            Badge? badge = User.Value.User.PinnedBadge;

            if (badge == null)
            {
                Hide();
                return;
            }

            Label.Text = badge.Description;
            dateDisplay.Date = badge.AwardedAt;

            Clear();
            LoadComponentAsync(new DrawableBadge(badge), Add);

            Show();
        }

        private partial class BadgeDrawableDate : DrawableDate
        {
            public BadgeDrawableDate(DateTimeOffset date)
                : base(date)
            {
            }

            protected override LocalisableString Format() => Date.ToLocalisedMediumDate();
        }
    }
}
