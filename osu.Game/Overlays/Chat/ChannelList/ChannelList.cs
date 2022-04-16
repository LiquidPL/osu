// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.Chat;
using osuTK;

namespace osu.Game.Overlays.Chat.ChannelList
{
    public class ChannelList : Container
    {
        public Action<Channel>? OnRequestSelect;
        public Action<Channel>? OnRequestLeave;

        public readonly BindableBool SelectorActive = new BindableBool();

        private readonly Dictionary<Channel, ChannelListItem> channelMap = new Dictionary<Channel, ChannelListItem>();

        private ChannelListItemFlow publicChannelFlow = null!;
        private ChannelListItemFlow privateChannelFlow = null!;

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider)
        {
            Children = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colourProvider.Background6,
                },
                new ChannelListScrollContainer
                {
                    Padding = new MarginPadding { Vertical = 7 },
                    RelativeSizeAxes = Axes.Both,
                    ScrollbarAnchor = Anchor.TopLeft,
                    ScrollDistance = 35f,
                    Child = new FillFlowContainer
                    {
                        Direction = FillDirection.Vertical,
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Children = new Drawable[]
                        {
                            publicChannelFlow = new ChannelListItemFlow("CHANNELS"),
                            new ChannelListSelector
                            {
                                Margin = new MarginPadding { Bottom = 10 },
                                SelectorActive = { BindTarget = SelectorActive },
                            },
                            privateChannelFlow = new ChannelListItemFlow("DIRECT MESSAGES"),
                        },
                    },
                },
            };
        }

        public void AddChannel(Channel channel)
        {
            if (channelMap.ContainsKey(channel))
                return;

            ChannelListItem item = new ChannelListItem(channel);
            item.OnRequestSelect += chan => OnRequestSelect?.Invoke(chan);
            item.OnRequestLeave += chan => OnRequestLeave?.Invoke(chan);

            ChannelListItemFlow flow = getFlowForChannel(channel);
            channelMap.Add(channel, item);
            flow.Add(item);
        }

        public void RemoveChannel(Channel channel)
        {
            if (!channelMap.ContainsKey(channel))
                return;

            ChannelListItem item = channelMap[channel];
            ChannelListItemFlow flow = getFlowForChannel(channel);

            channelMap.Remove(channel);
            flow.Remove(item);
            item.Expire();
        }

        public ChannelListItem GetItem(Channel channel)
        {
            if (!channelMap.ContainsKey(channel))
                throw new ArgumentOutOfRangeException();

            return channelMap[channel];
        }

        private ChannelListItemFlow getFlowForChannel(Channel channel)
        {
            switch (channel.Type)
            {
                case ChannelType.Public:
                    return publicChannelFlow;

                case ChannelType.PM:
                    return privateChannelFlow;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private class ChannelListItemFlow : FillFlowContainer
        {
            public ChannelListItemFlow(string label)
            {
                Direction = FillDirection.Vertical;
                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;

                Add(new OsuSpriteText
                {
                    Text = label,
                    Margin = new MarginPadding { Left = 18, Bottom = 5 },
                    Font = OsuFont.Torus.With(size: 12),
                });
            }
        }

        private class ChannelListScrollContainer : OsuScrollContainer
        {
            protected override ScrollbarContainer CreateScrollbar(Direction direction)
                => new ChannelListScrollBar(direction);

            protected class ChannelListScrollBar : OsuScrollbar
            {
                private const float bar_size = 4;
                private const float bar_margin = 7;

                public ChannelListScrollBar(Direction scrollDir)
                    : base(scrollDir)
                {
                    Size = new Vector2(bar_size);
                    Margin = new MarginPadding { Left = bar_margin };
                    CornerRadius = 2;
                }

                public override void ResizeTo(float val, int duration = 0, Easing easing = Easing.None)
                {
                    Vector2 size = new Vector2(bar_size, val);
                    this.ResizeTo(size, duration, easing);
                }
            }
        }
    }
}
