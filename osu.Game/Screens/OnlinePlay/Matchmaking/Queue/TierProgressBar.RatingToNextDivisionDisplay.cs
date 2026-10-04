// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Online.API.Requests.Responses;
using FontWeight = osu.Game.Graphics.FontWeight;
using OsuFont = osu.Game.Graphics.OsuFont;

namespace osu.Game.Screens.OnlinePlay.Matchmaking.Queue
{
    public partial class TierProgressBar
    {
        private partial class RatingToNextDivisionDisplay : CompositeDrawable
        {
            public bool Flipped
            {
                set
                {
                    if (value)
                    {
                        Origin = Anchor.CentreLeft;
                        background.Rotation = 180;
                        textFlow.Padding = new MarginPadding { Left = 20, Right = 15 };
                    }
                    else
                    {
                        Origin = Anchor.CentreRight;
                        background.Rotation = 0;
                        textFlow.Padding = new MarginPadding { Left = 15, Right = 20 };
                    }
                }
            }

            private readonly NineSliceSprite background;
            private readonly OsuTextFlowContainer textFlow;

            public readonly IBindable<int> CurrentRating = new BindableInt();
            public readonly IBindable<RankedPlayDivision> CurrentDivision = new Bindable<RankedPlayDivision>();

            public RatingToNextDivisionDisplay()
            {
                AutoSizeAxes = Axes.X;
                Height = 22;

                InternalChildren = new Drawable[]
                {
                    background = new NineSliceSprite
                    {
                        RelativeSizeAxes = Axes.Both,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        TextureInsetRelativeAxes = Axes.None,
                        TextureInset = new MarginPadding(8) { Horizontal = 22 },
                    },
                    textFlow = new OsuTextFlowContainer(p => p.Font = OsuFont.Torus.With(size: 12))
                    {
                        AutoSizeAxes = Axes.Both,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                    }
                };

                Flipped = false;
            }

            [BackgroundDependencyLoader]
            private void load(TextureStore textures)
            {
                background.Texture = textures.Get(@"Online/RankedPlay/remaining-rating-display");
            }

            public void UpdateLabel()
            {
                textFlow.Clear();

                int remaining = CurrentDivision.Value.EndRating - CurrentRating.Value + 1;

                textFlow.AddText(remaining.ToLocalisableString(@"N0"), p => p.Font = p.Font.With(weight: FontWeight.Bold));
                textFlow.AddText(" to next division");

                background.Colour = getColour(CurrentDivision.Value);
            }

            private ColourInfo getColour(RankedPlayDivision division) => division.Tier switch
            {
                Tier.Bronze => ColourInfo.GradientHorizontal(Colour4.FromHex(@"#242521"), Colour4.FromHex(@"#B88F7A")),
                Tier.Silver => ColourInfo.GradientHorizontal(Colour4.FromHex(@"#272C2D"), Colour4.FromHex(@"#E0E0EB")),
                Tier.Gold => ColourInfo.GradientHorizontal(Colour4.FromHex(@"#30321F"), Colour4.FromHex(@"#FFEE99")),
                Tier.Platinum => ColourInfo.GradientHorizontal(Colour4.FromHex(@"#1F3230"), Colour4.FromHex(@"#A8F0EF")),
                Tier.Rhodium => ColourInfo.GradientHorizontal(Colour4.FromHex(@"#1F2920"), Colour4.FromHex(@"#ADFFB6")),
                Tier.Radiant => ColourInfo.GradientHorizontal(Colour4.FromHex(@"#2E2933"), Colour4.FromHex(@"#97DCFF")),
                Tier.Lustrous => ColourInfo.GradientHorizontal(Colour4.FromHex(@"#2E2933"), Colour4.FromHex(@"#FFE600")),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}
