// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Drawables;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Localisation;
using osu.Game.Rulesets.Mods;
using osu.Game.Screens.Play.HUD;
using osuTK;
using CommonStrings = osu.Game.Localisation.CommonStrings;

namespace osu.Game.Screens.Play
{
    /// <summary>
    /// Displays beatmap metadata inside <see cref="PlayerLoader"/>
    /// </summary>
    public partial class BeatmapMetadataDisplay : Container
    {
        private readonly IWorkingBeatmap beatmap;
        private readonly Bindable<IReadOnlyList<Mod>> mods;
        // private readonly Drawable logoFacade;
        private LoadingSpinner loading;
        private Drawable blockingLoadLayer;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        public IBindable<IReadOnlyList<Mod>> Mods => mods;

        public bool Loading
        {
            set
            {
                // if (value)
                //     loading.Show();
                // else
                //     loading.Hide();
            }
        }

        private bool userBlocked;

        public bool UserBlocked
        {
            set
            {
                // if (value == userBlocked)
                //     return;
                //
                // userBlocked = value;
                //
                // if (userBlocked)
                // {
                //     using (BeginDelayedSequence(500))
                //     {
                //         blockingLoadLayer
                //             // Slight delay to avoid this flashing briefly during multiplayer load and other scenarios where
                //             // load may be blocked for a short period.
                //             .FadeIn(300, Easing.Out)
                //             .Then()
                //             .FadeTo(0.6f, 1000, Easing.In)
                //             .Loop();
                //     }
                // }
                // else
                //     blockingLoadLayer.FadeOut(500, Easing.OutQuint);
            }
        }

        public BeatmapMetadataDisplay(IWorkingBeatmap beatmap, Bindable<IReadOnlyList<Mod>> mods/*, Drawable logoFacade*/)
        {
            this.beatmap = beatmap;
            // this.logoFacade = logoFacade;

            this.mods = new Bindable<IReadOnlyList<Mod>>();
            this.mods.BindTo(mods);
        }

        private IBindable<StarDifficulty> starDifficulty;

        private FillFlowContainer versionFlow;
        private StarRatingDisplay starRatingDisplay;
        private OsuSpriteText title;
        private OsuSpriteText artist;
        private OsuSpriteText difficultyName;
        private OsuTextFlowContainer mappedBy;

        [BackgroundDependencyLoader]
        private void load(BeatmapDifficultyCache difficultyCache, OsuColour colours)
        {
            var metadata = beatmap.BeatmapInfo.Metadata;

            AutoSizeAxes = Axes.Both;
            Children = new Drawable[]
            {
                title = new OsuSpriteText
                {
                    Text = new RomanisableString(metadata.TitleUnicode, metadata.Title),
                    // Font = OsuFont.(size: 36, italics: true),
                    Font = OsuFont.TorusAlternate.With(size: 44, weight: FontWeight.Light),
                    Origin = Anchor.TopCentre,
                    Anchor = Anchor.TopCentre,
                    Margin = new MarginPadding { Bottom = 5 },
                },
                artist = new OsuSpriteText
                {
                    Text = new RomanisableString(metadata.ArtistUnicode, metadata.Artist),
                    Font = OsuFont.Torus.With(size: 24),
                    Origin = Anchor.TopCentre,
                    Anchor = Anchor.TopCentre,
                    Y = 49,
                },
                new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Origin = Anchor.TopCentre,
                    Anchor = Anchor.TopCentre,
                    Direction = FillDirection.Vertical,
                    Children = new Drawable[]
                    {
                        // artist = new OsuSpriteText
                        // {
                        //     Text = new RomanisableString(metadata.ArtistUnicode, metadata.Artist),
                        //     Font = OsuFont.Torus.With(size: 24),
                        //     Origin = Anchor.TopCentre,
                        //     Anchor = Anchor.TopCentre,
                        // },
                        // new Container
                        // {
                        //     Size = new Vector2(300, 60),
                        //     Margin = new MarginPadding(10),
                        //     Origin = Anchor.TopCentre,
                        //     Anchor = Anchor.TopCentre,
                        //     CornerRadius = 10,
                        //     Masking = true,
                        //     Alpha = 0,
                        //     Children = new[]
                        //     {
                        //         new Sprite
                        //         {
                        //             RelativeSizeAxes = Axes.Both,
                        //             Texture = beatmap.GetBackground(),
                        //             Origin = Anchor.Centre,
                        //             Anchor = Anchor.Centre,
                        //             FillMode = FillMode.Fill,
                        //         },
                        //         loading = new LoadingLayer(dimBackground: true)
                        //         {
                        //             BlockPositionalInput = false,
                        //         },
                        //         blockingLoadLayer = new Container
                        //         {
                        //             RelativeSizeAxes = Axes.Both,
                        //             Alpha = 0,
                        //             Children = new Drawable[]
                        //             {
                        //                 new Box
                        //                 {
                        //                     Colour = colours.PinkDarker,
                        //                     Alpha = 0.5f,
                        //                     RelativeSizeAxes = Axes.Both,
                        //                 },
                        //                 new OsuSpriteText
                        //                 {
                        //                     Anchor = Anchor.Centre,
                        //                     Origin = Anchor.Centre,
                        //                     Font = OsuFont.Style.Heading2,
                        //                     Text = PlayerLoaderStrings.LoadingPaused
                        //                 }
                        //             }
                        //         },
                        //     }
                        // },
                        // versionFlow = new FillFlowContainer
                        // {
                        //     AutoSizeAxes = Axes.Both,
                        //     Anchor = Anchor.TopCentre,
                        //     Origin = Anchor.TopCentre,
                        //     Direction = FillDirection.Vertical,
                        //     // Spacing = new Vector2(5f),
                        //     Margin = new MarginPadding { Top = 50, Bottom = 40 },
                        //     Children = new Drawable[]
                        //     {
                        //         starRatingDisplay = new StarRatingDisplay(default)
                        //         {
                        //             Alpha = 0f,
                        //             Anchor = Anchor.TopCentre,
                        //             Origin = Anchor.TopCentre,
                        //             Margin = new MarginPadding { Bottom = 2 },
                        //         },
                        //         difficultyName = new OsuSpriteText
                        //         {
                        //             Text = beatmap.BeatmapInfo.DifficultyName,
                        //             Font = OsuFont.GetFont(size: 18, weight: FontWeight.SemiBold),
                        //             Anchor = Anchor.TopCentre,
                        //             Origin = Anchor.TopCentre,
                        //         },
                        //         mappedBy = new OsuTextFlowContainer(p => p.Font = OsuFont.GetFont(size: 14))
                        //         {
                        //             AutoSizeAxes = Axes.Both,
                        //             Anchor = Anchor.TopCentre,
                        //             Origin = Anchor.TopCentre,
                        //         },
                        //     }
                        // },
                        // new GridContainer
                        // {
                        //     Anchor = Anchor.TopCentre,
                        //     Origin = Anchor.TopCentre,
                        //     AutoSizeAxes = Axes.Both,
                        //     RowDimensions = new[]
                        //     {
                        //         new Dimension(GridSizeMode.AutoSize),
                        //         new Dimension(GridSizeMode.AutoSize),
                        //     },
                        //     ColumnDimensions = new[]
                        //     {
                        //         new Dimension(GridSizeMode.AutoSize),
                        //         new Dimension(GridSizeMode.AutoSize),
                        //     },
                        //     Content = new[]
                        //     {
                        //         new Drawable[]
                        //         {
                        //             new MetadataLineLabel(BeatmapsetsStrings.ShowInfoSource),
                        //             new MetadataLineInfo(metadata.Source)
                        //         },
                        //         new Drawable[]
                        //         {
                        //             new MetadataLineLabel(CommonStrings.Mapper),
                        //             new MetadataLineInfo(metadata.Author.Username)
                        //         }
                        //     }
                        // },
                        // new ModDisplay
                        // {
                        //     Anchor = Anchor.TopCentre,
                        //     Origin = Anchor.TopCentre,
                        //     Margin = new MarginPadding { Top = 20 },
                        //     Current = mods
                        // },
                        // logoFacade.With(d =>
                        // {
                        //     d.Anchor = Anchor.TopCentre;
                        //     d.Origin = Anchor.TopCentre;
                        // }),
                    },
                }
            };

            starDifficulty = difficultyCache.GetBindableDifficulty(beatmap.BeatmapInfo);

            Loading = true;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // mappedBy.AddText(CommonStrings.MappedBy + " ");
            // mappedBy.AddText(beatmap.BeatmapInfo.Metadata.Author.Username, p => p.Font = OsuFont.GetFont(size: 14, weight: FontWeight.SemiBold));

            // starDifficulty.BindValueChanged(d =>
            // {
            //     starRatingDisplay.Current.Value = d.NewValue;
            //     difficultyName.Colour = mappedBy.Colour = colours.ForStarDifficulty(d.NewValue.Stars);
            //
            //     versionFlow.AutoSizeDuration = 300;
            //     versionFlow.AutoSizeEasing = Easing.OutQuint;
            //
            //     starRatingDisplay.FadeIn(300, Easing.InQuint);
            // }, true);

            Schedule(() =>
            {
                title.MoveToY(-150).MoveToY(0, 500, Easing.OutQuint);
                artist.MoveToY(0).MoveToY(49, 500, Easing.OutQuint);
            });
        }

        // private partial class MetadataLineLabel : OsuSpriteText
        // {
        //     public MetadataLineLabel(LocalisableString text)
        //     {
        //         Anchor = Anchor.TopRight;
        //         Origin = Anchor.TopRight;
        //         Margin = new MarginPadding { Right = 5 };
        //         Colour = OsuColour.Gray(0.8f);
        //         Text = text;
        //     }
        // }
        //
        // private partial class MetadataLineInfo : OsuSpriteText
        // {
        //     public MetadataLineInfo(string text)
        //     {
        //         Margin = new MarginPadding { Left = 5 };
        //         Text = string.IsNullOrEmpty(text) ? @"-" : text;
        //     }
        // }
    }
}
