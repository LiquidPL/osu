// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Transforms;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Game.Graphics;
using osu.Game.Graphics.UserInterface;
using osu.Game.Input.Bindings;
using osu.Game.Online.Matchmaking;
using osu.Game.Online.Multiplayer;

namespace osu.Game.Screens.OnlinePlay.Matchmaking.Queue
{
    public partial class ScreenQueue
    {
        private partial class QueueButton : ShearedButton, IKeyBindingHandler<GlobalAction>
        {
            public readonly IBindable<MatchmakingScreenState> State = new Bindable<MatchmakingScreenState>();
            public readonly IBindable<MatchmakingPool?> SelectedPool = new Bindable<MatchmakingPool?>();

            private IBindable<bool> isConnected = null!;

            [Resolved]
            private MultiplayerClient client { get; set; } = null!;

            [Resolved]
            private QueueController queue { get; set; } = null!;

            [Resolved]
            private OsuColour colours { get; set; } = null!;

            protected override void LoadComplete()
            {
                base.LoadComplete();

                State.BindValueChanged(onStateChanged, true);
                SelectedPool.BindValueChanged(_ => Scheduler.AddOnce(updateEnabledState), true);

                isConnected = client.IsConnected.GetBoundCopy();
                isConnected.BindValueChanged(_ => Scheduler.AddOnce(updateEnabledState), true);

                Action = () =>
                {
                    if (State.Value == MatchmakingScreenState.Idle)
                    {
                        Debug.Assert(SelectedPool.Value != null);
                        queue.JoinQueue(SelectedPool.Value);
                    }
                    else if (State.Value == MatchmakingScreenState.Queueing)
                    {
                        queue.LeaveQueue();
                    }
                };
            }

            private void updateEnabledState() => Enabled.Value = isConnected.Value && SelectedPool.Value != null;

            private void onStateChanged(ValueChangedEvent<MatchmakingScreenState> e)
            {
                if (e.NewValue == MatchmakingScreenState.Idle)
                {
                    DarkerColour = colours.Blue3;
                    LighterColour = colours.Blue1;
                    Text = "Find Match";
                }
                else
                {
                    DarkerColour = colours.Red3;
                    LighterColour = colours.Red1;
                    Text = "Stop Searching";
                }
            }

            public void Appear()
            {
                const float destination = 0;

                if (IsPresent)
                    return;

                if (Transforms.OfType<Transform<float>>().Any(t => t.TargetMember == nameof(Y) && t.EndValue == destination))
                    return;

                FinishTransforms();

                this.MoveToY(150f)
                    .FadeOut()
                    .MoveToY(destination, 240, Easing.OutCubic)
                    .FadeIn(240, Easing.OutCubic);
            }

            public TransformSequence<QueueButton> Disappear()
            {
                const float destination = 150;

                if (Transforms.OfType<Transform<float>>().Any(t => t.TargetMember == nameof(Y) && t.EndValue == destination))
                    return new TransformSequence<QueueButton>(this);

                FinishTransforms();

                return this.FadeOut(240, Easing.InOutCubic)
                           .MoveToY(destination, 240, Easing.InOutCubic);
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


