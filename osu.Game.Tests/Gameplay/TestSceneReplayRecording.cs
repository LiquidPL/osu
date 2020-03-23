// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Input.StateChanges;
using osu.Framework.Input.States;
using osu.Framework.Logging;
using osu.Game.Graphics.Sprites;
using osu.Game.Replays;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Replays;
using osu.Game.Rulesets.UI;
using osu.Game.Tests.Visual;
using osu.Game.Tests.Visual.UserInterface;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Tests.Gameplay
{
    public class TestSceneReplayRecording : OsuTestScene
    {
        private readonly TestRulesetInputManager playbackManager;

        public TestSceneReplayRecording()
        {
            Replay replay = new Replay();

            Add(new GridContainer
            {
                RelativeSizeAxes = Axes.Both,
                Content = new[]
                {
                    new Drawable[]
                    {
                        new TestRulesetInputManager(new TestSceneModSettings.TestRulesetInfo(), 0, SimultaneousBindingMode.Unique)
                        {
                            RecordTarget = replay,
                            Child = new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Children = new Drawable[]
                                {
                                    new Box
                                    {
                                        Colour = Color4.Brown,
                                        RelativeSizeAxes = Axes.Both,
                                    },
                                    new OsuSpriteText
                                    {
                                        Text = "Recording",
                                        Scale = new Vector2(3),
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                    },
                                    new TestConsumer()
                                }
                            },
                        }
                    },
                    new Drawable[]
                    {
                        playbackManager = new TestRulesetInputManager(new TestSceneModSettings.TestRulesetInfo(), 0, SimultaneousBindingMode.Unique)
                        {
                            ReplayInputHandler = new TestFramedReplayInputHandler(replay)
                            {
                                GamefieldToScreenSpace = pos => playbackManager.ToScreenSpace(pos),
                            },
                            Child = new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Children = new Drawable[]
                                {
                                    new Box
                                    {
                                        Colour = Color4.DarkBlue,
                                        RelativeSizeAxes = Axes.Both,
                                    },
                                    new OsuSpriteText
                                    {
                                        Text = "Playback",
                                        Scale = new Vector2(3),
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                    },
                                    new TestConsumer()
                                }
                            },
                        }
                    }
                }
            });
        }

        protected override void Update()
        {
            base.Update();

            playbackManager.ReplayInputHandler.SetFrameFromTime(Time.Current - 500);
        }
    }

    public class TestFramedReplayInputHandler : FramedReplayInputHandler<TestReplayFrame>
    {
        public TestFramedReplayInputHandler(Replay replay)
            : base(replay)
        {
        }

        public override List<IInput> GetPendingInputs()
        {
            return new List<IInput>
            {
                new MousePositionAbsoluteInput
                {
                    Position = GamefieldToScreenSpace(CurrentFrame?.Position ?? Vector2.Zero)
                },
                new ReplayState<TestAction>
                {
                    PressedActions = CurrentFrame?.Actions ?? new List<TestAction>()
                }
            };
        }
    }

    public class TestConsumer : CompositeDrawable, IKeyBindingHandler<TestAction>
    {
        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => Parent.ReceivePositionalInputAt(screenSpacePos);

        private readonly Box box;

        public TestConsumer()
        {
            Size = new Vector2(30);

            Origin = Anchor.Centre;

            InternalChildren = new Drawable[]
            {
                box = new Box
                {
                    Colour = Color4.Black,
                    RelativeSizeAxes = Axes.Both,
                },
            };
        }

        protected override bool OnMouseMove(MouseMoveEvent e)
        {
            Position = e.MousePosition;
            return base.OnMouseMove(e);
        }

        public bool OnPressed(TestAction action)
        {
            box.Colour = Color4.White;
            return true;
        }

        public void OnReleased(TestAction action)
        {
            box.Colour = Color4.Black;
        }
    }

    public class TestRulesetInputManager : RulesetInputManager<TestAction>
    {
        private ReplayRecorder<TestAction> recorder;

        public Replay RecordTarget
        {
            set
            {
                if (recorder != null)
                    throw new InvalidOperationException("Cannot attach more than one recorder");

                KeyBindingContainer.Add(recorder = new TestReplayRecorder(value));
            }
        }

        public TestRulesetInputManager(RulesetInfo ruleset, int variant, SimultaneousBindingMode unique)
            : base(ruleset, variant, unique)
        {
        }

        protected override KeyBindingContainer<TestAction> CreateKeyBindingContainer(RulesetInfo ruleset, int variant, SimultaneousBindingMode unique)
            => new TestKeyBindingContainer();

        internal class TestKeyBindingContainer : KeyBindingContainer<TestAction>
        {
            public override IEnumerable<KeyBinding> DefaultKeyBindings => new[]
            {
                new KeyBinding(InputKey.MouseLeft, TestAction.Down),
            };
        }
    }

    public class TestReplayFrame : ReplayFrame
    {
        public Vector2 Position;

        public List<TestAction> Actions = new List<TestAction>();

        public TestReplayFrame()
        {
        }

        public TestReplayFrame(double time, Vector2 position, params TestAction[] actions)
            : base(time)
        {
            Position = position;
            Actions.AddRange(actions);
        }
    }

    public enum TestAction
    {
        Down,
    }

    internal class TestReplayRecorder : ReplayRecorder<TestAction>
    {
        public TestReplayRecorder(Replay target)
            : base(target)
        {
        }

        protected override ReplayFrame HandleFrame(InputState state, List<TestAction> pressedActions) =>
            new TestReplayFrame(Time.Current, ToLocalSpace(state.Mouse.Position), pressedActions.ToArray());
    }

    internal abstract class ReplayRecorder<T> : Component, IKeyBindingHandler<T>
        where T : struct
    {
        private readonly Replay target;

        private readonly List<T> pressedActions = new List<T>();

        protected ReplayRecorder(Replay target)
        {
            this.target = target;

            RelativeSizeAxes = Axes.Both;

            Depth = float.MinValue;
        }

        protected override bool OnMouseMove(MouseMoveEvent e)
        {
            recordFrame();
            return base.OnMouseMove(e);
        }

        public bool OnPressed(T action)
        {
            pressedActions.Add(action);
            recordFrame();
            return false;
        }

        public void OnReleased(T action)
        {
            pressedActions.Remove(action);
            recordFrame();
        }

        private void recordFrame()
        {
            var frame = HandleFrame(GetContainingInputManager().CurrentState, pressedActions);

            if (frame != null)
                target.Frames.Add(frame);
        }

        protected abstract ReplayFrame HandleFrame(InputState state, List<T> pressedActions);
    }
}
