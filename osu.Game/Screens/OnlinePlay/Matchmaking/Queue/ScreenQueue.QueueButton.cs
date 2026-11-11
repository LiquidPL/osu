// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Game.Graphics;
using osu.Game.Graphics.UserInterface;
using osu.Game.Input.Bindings;
using osu.Game.Localisation;
using osu.Game.Online.Matchmaking;
using osu.Game.Online.Multiplayer;

namespace osu.Game.Screens.OnlinePlay.Matchmaking.Queue
{
    public partial class ScreenQueue
    {
        private partial class QueueButton : ShearedButton, IKeyBindingHandler<GlobalAction>
        {
            private readonly IBindable<MatchmakingScreenState> currentState = new Bindable<MatchmakingScreenState>();
            private readonly IBindable<MatchmakingPool?> selectedPool = new Bindable<MatchmakingPool?>();

            private IBindable<bool> isConnected = null!;

            [Resolved]
            private MultiplayerClient client { get; set; } = null!;

            [Resolved]
            private QueueController queue { get; set; } = null!;

            [Resolved]
            private OsuColour colours { get; set; } = null!;

            public QueueButton()
            {
                Width = 220;
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                isConnected = client.IsConnected.GetBoundCopy();
                isConnected.BindValueChanged(_ => Scheduler.AddOnce(updateEnabledState), true);

                currentState.BindTo(queue.CurrentState);
                currentState.BindValueChanged(onStateChanged, true);

                selectedPool.BindTo(queue.SelectedPool);
                selectedPool.BindValueChanged(_ => Scheduler.AddOnce(updateEnabledState), true);

                Action = () =>
                {
                    if (currentState.Value == MatchmakingScreenState.Idle)
                    {
                        Debug.Assert(selectedPool.Value != null);
                        queue.JoinQueue(selectedPool.Value);
                    }
                    else if (currentState.Value == MatchmakingScreenState.Queueing)
                    {
                        queue.LeaveQueue();
                    }
                };
            }

            private void updateEnabledState() => Enabled.Value =
                isConnected.Value
                && selectedPool.Value != null
                && currentState.Value is MatchmakingScreenState.Idle or MatchmakingScreenState.Queueing;

            private void onStateChanged(ValueChangedEvent<MatchmakingScreenState> e)
            {
                if (e.NewValue == MatchmakingScreenState.Idle)
                {
                    DarkerColour = colours.Blue3;
                    LighterColour = colours.Blue1;
                    Text = MatchmakingQueueStrings.FindMatch;
                }
                else
                {
                    DarkerColour = colours.Red3;
                    LighterColour = colours.Red1;
                    Text = MatchmakingQueueStrings.StopSearching;
                }

                updateEnabledState();
            }

            public bool OnPressed(KeyBindingPressEvent<GlobalAction> e)
            {
                if (e.Action == GlobalAction.Select && !e.Repeat)
                {
                    TriggerClick();
                    return true;
                }

                return false;
            }

            public void OnReleased(KeyBindingReleaseEvent<GlobalAction> e)
            {
            }
        }
    }
}


