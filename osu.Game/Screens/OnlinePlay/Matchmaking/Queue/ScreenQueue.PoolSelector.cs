// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Transforms;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Online.Matchmaking;
using osu.Game.Overlays;
using osu.Game.Rulesets;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.OnlinePlay.Matchmaking.Queue
{
    public partial class ScreenQueue
    {
        public partial class PoolSelector : CompositeDrawable
        {
            private readonly Circle strip;
            private readonly CursedFlowContainer<SelectorButton> poolFlow;

            public readonly Bindable<MatchmakingPool[]?> AvailablePools = new Bindable<MatchmakingPool[]?>();
            public readonly Bindable<MatchmakingPool?> SelectedPool = new Bindable<MatchmakingPool?>();

            public PoolSelector()
            {
                RelativeSizeAxes = Axes.Y;
                AutoSizeAxes = Axes.X;

                InternalChildren = new Drawable[]
                {
                    strip = new Circle
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Height = 4,
                        Y = 8,
                        Colour = Color4.White,
                        Alpha = 0,
                    },
                    poolFlow = new CursedFlowContainer<SelectorButton>
                    {
                        RelativeSizeAxes = Axes.Y,
                        AutoSizeAxes = Axes.X,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Spacing = 8,
                    },
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                AvailablePools.BindValueChanged(onPoolsChanged, true);
                SelectedPool.BindValueChanged(_ => onSelectedPoolChanged(), true);
            }

            private void onPoolsChanged(ValueChangedEvent<MatchmakingPool[]?> e)
            {
                poolFlow.Clear();

                if (e.NewValue == null)
                    return;

                foreach ((var p, int i) in e.NewValue.Select((p, i) => (p, i)))
                {
                    SelectorButton button;

                    poolFlow.Add(button = new SelectorButton(p)
                    {
                        SelectedPool = { BindTarget = SelectedPool },
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Alpha = 0,
                        AlwaysPresent = true,
                    });

                    button.OnLoadComplete += _ => button.Appear(i * 40);
                }

                onSelectedPoolChanged();
            }

            private void onSelectedPoolChanged()
            {
                var selected = poolFlow.FirstOrDefault(b => b.IsSelected);

                if (selected == null)
                {
                    strip.ResizeWidthTo(0, 400, new CubicBezierEasingFunction(0.7, 0, 0.3, 1)).Then().FadeOut();
                    return;
                }

                Scheduler.AddDelayed(() =>
                {
                    strip.MoveToX(selected.ToSpaceOfOtherDrawable(Vector2.Zero, this).X - poolFlow.DrawWidth / 2 + selected.DrawWidth / 2, strip.IsPresent ? 800 : 0, Easing.OutElasticHalf)
                         .FadeIn()
                         .ResizeWidthTo(32, 400, new CubicBezierEasingFunction(0.7, 0, 0.3, 1));
                }, strip.IsPresent ? 0 : 100);
            }

            private partial class SelectorButton : OsuAnimatedButton
            {
                private const float icon_size = 34;

                public bool IsSelected => SelectedPool.Value?.Equals(pool) == true;

                protected override Colour4 DimColour => Color4.White;

                public readonly Bindable<MatchmakingPool?> SelectedPool = new Bindable<MatchmakingPool?>();

                private readonly MatchmakingPool pool;

                [Resolved]
                private RulesetStore rulesetStore { get; set; } = null!;

                [Resolved]
                private OverlayColourProvider colourProvider { get; set; } = null!;

                private FillFlowContainer container = null!;
                private Drawable iconSprite = null!;
                private OsuSpriteText name = null!;

                public SelectorButton(MatchmakingPool pool)
                {
                    this.pool = pool;
                    Size = new Vector2(100, 70);
                    ScaleOnMouseDown = 1;
                }

                [BackgroundDependencyLoader]
                private void load()
                {
                    Content.Masking = true;
                    Content.CornerRadius = 10;
                    Content.EdgeEffect = new EdgeEffectParameters();

                    Ruleset? rulesetInstance = rulesetStore.GetRuleset(pool.RulesetId)?.CreateInstance();

                    string rulesetName = rulesetInstance?.Description ?? string.Empty;
                    if (pool.Variant != 0)
                        rulesetName += $" {pool.Variant}K";

                    Add(container = new FillFlowContainer
                    {
                        AutoSizeAxes = Axes.Both,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(4),
                        Children = new Drawable[]
                        {
                            new Container
                            {
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                Size = new Vector2(icon_size),
                                Child = iconSprite = createIcon(),
                            },
                            name = new OsuSpriteText
                            {
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                Font = OsuFont.Torus.With(size: 18, weight: FontWeight.SemiBold),
                                Text = rulesetName,
                            }
                        },
                    });

                    Action = () => SelectedPool.Value = pool;
                }

                protected override void LoadComplete()
                {
                    base.LoadComplete();

                    SelectedPool.BindValueChanged(onSelectionChanged, true);
                }

                private void onSelectionChanged(ValueChangedEvent<MatchmakingPool?> _)
                {
                    if (IsSelected)
                    {
                        iconSprite.FadeColour(colourProvider.Highlight1, 100, Easing.OutQuint);
                        name.FadeColour(colourProvider.Highlight1, 100, Easing.OutQuint);
                    }
                    else
                    {
                        iconSprite.FadeColour(Color4.White, 100, Easing.OutQuint);
                        name.FadeColour(Color4.White, 100, Easing.OutQuint);
                    }
                }

                public void Appear(int delay)
                {
                    this.Delay(delay)
                        .MoveToY(-50)
                        .FadeOut()
                        .MoveToY(0, 400, new CubicBezierEasingFunction(0, 0, 0, 1))
                        .FadeIn(400, Easing.Out);
                }

                private Drawable createIcon()
                {
                    Ruleset? rulesetInstance = rulesetStore.GetRuleset(pool.RulesetId)?.CreateInstance();
                    if (rulesetInstance == null)
                        return Empty();

                    Drawable icon = rulesetInstance.CreateIcon().With(d => d.RelativeSizeAxes = Axes.Both);

                    if (pool.Variant == 0)
                        return icon;

                    return new BufferedContainer(pixelSnapping: true)
                    {
                        RelativeSizeAxes = Axes.Both,
                        Children = new[]
                        {
                            icon,
                            new Container
                            {
                                Anchor = Anchor.BottomRight,
                                Origin = Anchor.BottomRight,
                                Size = icon_size * new Vector2(0.4f, 0.28f),
                                Children = new Drawable[]
                                {
                                    new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                    },
                                    new OsuSpriteText
                                    {
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        Text = $"{pool.Variant}K",
                                        Font = OsuFont.Default.With(size: icon_size * 0.3f, weight: FontWeight.Bold),
                                        UseFullGlyphHeight = false,
                                        Blending = new BlendingParameters
                                        {
                                            AlphaEquation = BlendingEquation.ReverseSubtract
                                        }
                                    }
                                }
                            }
                        }
                    };
                }
            }
        }
    }
}
