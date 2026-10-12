// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Transforms;
using osu.Framework.Threading;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.OnlinePlay.Matchmaking.Queue
{
    public partial class ScreenQueue
    {
        public partial class UserBackground : VisibilityContainer
        {
            public const int WIDTH = 250;

            protected override bool StartHidden => true;

            private readonly Container background;
            private CircularContainer outerRing = null!;
            private CircularContainer innerRing = null!;

            private readonly List<CircularContainer> ringsInMotion = new List<CircularContainer>();

            private ScheduledDelegate? queueAnimationDelegate = null;

            private readonly Bindable<MatchmakingScreenState> currentState = new Bindable<MatchmakingScreenState>();

            [Resolved]
            private QueueController queue { get; set; } = null!;

            public UserBackground()
            {
                Anchor = Anchor.Centre;
                Origin = Anchor.Centre;

                Children = new Drawable[]
                {
                    background = new Container
                    {
                        Size = new Vector2(WIDTH),
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Alpha = 0,
                        Masking = true,
                        CornerRadius = WIDTH / 2f,
                        Children = new Drawable[]
                        {
                            new BufferedContainer
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Size = new Vector2(WIDTH),
                                Child = new Container
                                {
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Size = new Vector2(0),
                                    Masking = true,
                                    EdgeEffect = new EdgeEffectParameters
                                    {
                                        Type = EdgeEffectType.Shadow,
                                        Colour = Color4.Black.Opacity(0.75f),
                                        Radius = 180,
                                    },
                                },
                            },
                        },
                    },
                    outerRing = createOuterRing(),
                    innerRing = createInnerRing(),
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                currentState.BindTo(queue.CurrentState);
                currentState.BindValueChanged(onStateChanged, true);
            }

            private void onStateChanged(ValueChangedEvent<MatchmakingScreenState> e)
            {
                if (e.OldValue == MatchmakingScreenState.Queueing)
                {
                    queueAnimationDelegate?.Cancel();
                    queueAnimationDelegate = null;

                    foreach (var ring in ringsInMotion)
                    {
                        ring.FadeOut(200).Expire();
                    }

                    AddRange(new Drawable[]
                    {
                        outerRing = createOuterRing().With(r => r.OnLoadComplete += _ => r.FadeIn(500)),
                        innerRing = createInnerRing().With(r => r.OnLoadComplete += _ => r.Delay(250).FadeIn(500)),
                    });

                    return;
                }

                if (e.NewValue == MatchmakingScreenState.Queueing)
                {
                    using (BeginDelayedSequence(1000))
                    {
                        innerRing.ResizeTo(new Vector2(100), 500, Easing.InQuint)
                                 .FadeOut(500, Easing.InQuint);

                        outerRing.Delay(500)
                                 .ResizeTo(new Vector2(100), 500, Easing.InQuint)
                                 .FadeOut(500, Easing.InQuint);
                    }

                    queueAnimationDelegate = new ScheduledDelegate(() =>
                    {
                        var ring1 = createOuterRing().With(r => r.OnLoadComplete += _ =>
                        {
                            r.FadeIn(270)
                             .ResizeTo(r.Size + new Vector2(500), 5270, new CubicBezierEasingFunction(0, 0.2, 0.15, 0.3))
                             .Delay(270)
                             .FadeOut(5000)
                             .Expire();
                        });

                        var ring2 = createInnerRing().With(r => r.OnLoadComplete += _ =>
                        {
                            r.Delay(468)
                             .FadeIn(270)
                             .ResizeTo(r.Size + new Vector2(500), 5270, new CubicBezierEasingFunction(0, 0.2, 0.15, 0.3))
                             .Delay(270)
                             .FadeOut(5000)
                             .Expire();
                        });

                        AddRange([ring1, ring2]);
                        ringsInMotion.AddRange([ring1, ring2]);

                        // outerRing.ResizeTo(outerRing.Size + new Vector2(500), 5270, new CubicBezierEasingFunction(0, 0.2, 0.15, 0.3))
                        //          .Delay(270).FadeOut(5000)
                        //          .Expire();
                        //
                        // innerRing.Delay(468)
                        //          .ResizeTo(innerRing.Size + new Vector2(500), 5270, new CubicBezierEasingFunction(0, 0.2, 0.15, 0.3))
                        //          .Delay(270).FadeOut(5000)
                        //          .Expire();
                        //
                        // ringsInMotion.AddRange(outerRing, innerRing);
                        // // createRings();
                    }, Clock.CurrentTime + 2000, 2881);

                    Scheduler.Add(queueAnimationDelegate);
                }
            }

            private CircularContainer createOuterRing() => new CircularContainer
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(200),
                Alpha = 0,
                Masking = true,
                BorderColour = Color4.White.Opacity(0.05f),
                BorderThickness = 2,
                Child = new Box { RelativeSizeAxes = Axes.Both, Colour = Color4.Transparent },
            };

            private CircularContainer createInnerRing() => new CircularContainer
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(120),
                Alpha = 0,
                Masking = true,
                BorderColour = Color4.White.Opacity(0.2f),
                BorderThickness = 2,
                Child = new Box { RelativeSizeAxes = Axes.Both, Colour = Color4.Transparent },
            };

            protected override void PopIn()
            {
                background.FadeIn(2000);
                innerRing.Delay(500).FadeIn(500);
                outerRing.Delay(750).FadeIn(500);
            }

            protected override void PopOut()
            {
            }
        }
    }
}
