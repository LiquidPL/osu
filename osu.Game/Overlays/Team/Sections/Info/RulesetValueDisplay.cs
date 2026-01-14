// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Graphics.Containers;
using osu.Game.Rulesets;
using osuTK;

namespace osu.Game.Overlays.Team.Sections.Info
{
    public partial class RulesetValueDisplay : InfoValueDisplay
    {
        public new LocalisableString Content
        {
            set => throw new NotSupportedException($"Use ({nameof(Ruleset)} instead.");
        }

        public RulesetInfo? Ruleset
        {
            get;
            set
            {
                if (value != null && field != null && field.Equals(value))
                    return;

                field = value;
                Scheduler.AddOnce(updateRuleset);
            }
        }

        private void updateRuleset()
        {
            if (Ruleset == null)
                return;

            ContentTextFlow.Clear();
            ContentTextFlow.AddArbitraryDrawable(new RulesetConstrainedIconContainer
            {
                Size = new Vector2(10),
                Margin = new MarginPadding { Right = 2 },
                Icon = Ruleset.CreateInstance().CreateIcon(),
            });
            ContentTextFlow.AddText(Ruleset.Name);
        }

        private partial class RulesetConstrainedIconContainer : ConstrainedIconContainer, IHasLineBaseHeight
        {
            public float LineBaseHeight => 8; // Fake the ruleset icon's line height so that it aligns better with the label.
        }
    }
}
