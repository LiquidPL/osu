// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.ComponentModel;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Game.Graphics;
using osu.Game.Graphics.UserInterface;
using osu.Game.Localisation;
using osu.Game.Online.API;
using osu.Game.Online.Rooms;
using osu.Game.Screens.Footer;

namespace osu.Game.Screens.OnlinePlay.Playlists
{
    public partial class PlaylistsCloseButton : ShearedButton, IFooterButton
    {
        public const int WIDTH = 120;

        private readonly Room room;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        public PlaylistsCloseButton(Room room)
        {
            Width = WIDTH;
            Text = OnlinePlayStrings.FooterButtonPlaylistClose;

            this.room = room;
        }

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            DarkerColour = colours.Pink3;
            LighterColour = colours.Pink1;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            room.PropertyChanged += onRoomPropertyChanged;

            updateSetupState();
        }

        private void onRoomPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Room.RoomID):
                    updateSetupState();
                    break;

                case nameof(Room.Status):
                case nameof(Room.Host):
                case nameof(Room.StartDate):
                    UpdateState();
                    break;
            }
        }

        private void updateSetupState()
        {
            if (room.RoomID == null)
                return;

            UpdateState();
        }

        public void UpdateState()
        {
            if (room.Host?.Id != api.LocalUser.Value.Id)
            {
                this.Disappear().Expire();
                return;
            }

            TimeSpan? deletionGracePeriodRemaining = room.StartDate?.AddMinutes(5) - DateTimeOffset.Now;

            if (deletionGracePeriodRemaining > TimeSpan.Zero && !room.HasEnded)
            {
                this.Appear().Then().Schedule(() =>
                {
                    using (BeginDelayedSequence(deletionGracePeriodRemaining.Value.TotalMilliseconds))
                        this.Disappear().Expire();
                });
            }
            else
                this.Disappear().Expire();
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            room.PropertyChanged -= onRoomPropertyChanged;
        }
    }
}
