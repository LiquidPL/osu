// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Database;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.Multiplayer;
using osu.Game.Overlays;
using osu.Game.Screens.OnlinePlay.Matchmaking.Match.BeatmapSelect;
using osu.Game.Screens.Ranking;
using osuTK;

namespace osu.Game.Screens.OnlinePlay.Matchmaking.RankedPlay
{
    public partial class BeatmapTagPrompt : CompositeDrawable
    {
        private readonly BeatmapInfo beatmap;

        [Resolved]
        private BeatmapLookupCache beatmapLookupCache { get; set; } = null!;

        private Container beatmapCardContainer = null!;

        public BeatmapTagPrompt(BeatmapInfo beatmap)
        {
            this.beatmap = beatmap;
        }

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider, SessionStatics sessionStatics)
        {
            AutoSizeAxes = Axes.Both;

            Masking = true;
            CornerRadius = 10;
            EdgeEffect = new EdgeEffectParameters
            {
                Type = EdgeEffectType.Shadow,
                Radius = 3,
                Colour = Colour4.Black.Opacity(0.1f),
            };

            InternalChildren = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colourProvider.Background5,
                },
                new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Vertical,
                    Margin = new MarginPadding(20),
                    Children = new Drawable[]
                    {
                        new OsuTextFlowContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Text = "Which of the following skillsets do you think is the most appropriate for the previous beatmap?",
                            Margin = new MarginPadding { Bottom = 10 },
                        },
                        beatmapCardContainer = new Container
                        {
                            Anchor = Anchor.TopRight,
                            Origin = Anchor.BottomRight,
                            Margin = new MarginPadding { Bottom = 10 },
                            Scale = new Vector2(1.1f),
                            Size = new Vector2(MatchmakingSelectPanel.WIDTH, MatchmakingSelectPanel.HEIGHT),
                            // Child = new MatchmakingSelectPanel.CardContentBeatmap(beatmap.BeatmapSet, []),
                        },
                        new UserTagControl(beatmap)
                        {
                            RelativeSizeAxes = Axes.X,
                            Margin = new MarginPadding { Bottom = 10 },
                            Writable = true,
                            ShowAddButton = false,
                            FilterOverride = t => t.FullName.StartsWith("skillset", StringComparison.Ordinal),
                        },
                        new OsuSpriteText
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Text = "Tagging beatmaps will help create more balanced mappools.",
                            Font = OsuFont.Style.Caption1,
                        },
                    },
                },
            };
        }

        private async Task fetchBeatmap()
        {
            var apiBeatmap = (await beatmapLookupCache.GetBeatmapAsync(beatmap.OnlineID).ConfigureAwait(false))!;

            Schedule(() => beatmapCardContainer.Child = new MatchmakingSelectPanel.CardContentBeatmap(apiBeatmap, []));
        }

        protected override void LoadComplete()
        {
            base.LoadAsyncComplete();

            fetchBeatmap().FireAndForget();
        }
    }
}
