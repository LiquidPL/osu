// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;

namespace osu.Game.Overlays.Team.Sections.Info
{
    public partial class InfoValueDisplay : CompositeDrawable
    {
        private readonly OsuSpriteText titleText;

        protected readonly Container ContentContainer;
        protected readonly LinkFlowContainer ContentTextFlow;

        public LocalisableString Title
        {
            set => titleText.Text = value;
        }

        public LocalisableString Content
        {
            set
            {
                ContentTextFlow.Clear();
                ContentTextFlow.AddText(value);
            }
        }

        public InfoValueDisplay(bool large = false)
        {
            AutoSizeAxes = Axes.Both;
            InternalChild = new FillFlowContainer
            {
                Direction = FillDirection.Vertical,
                AutoSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    titleText = new OsuSpriteText
                    {
                        Font = OsuFont.GetFont(size: 12),
                    },
                    ContentContainer = new Container
                    {
                        AutoSizeAxes = Axes.Both,
                        Child = ContentTextFlow = new LinkFlowContainer(p => p.Font = OsuFont.GetFont(size: large ? 30 : 12))
                        {
                            AutoSizeAxes = Axes.Both,
                        }
                    },
                }
            };
        }

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider)
        {
            titleText.Colour = colourProvider.Content1;
            ContentTextFlow.Colour = colourProvider.Content2;
        }
    }
}
