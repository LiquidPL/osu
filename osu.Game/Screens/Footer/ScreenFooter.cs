// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Threading;
using osu.Game.Graphics.Containers;
using osu.Game.Input.Bindings;
using osu.Game.Overlays;
using osu.Game.Screens.Menu;
using osuTK;

namespace osu.Game.Screens.Footer
{
    public partial class ScreenFooter : OverlayContainer
    {
        public ScreenBackButton BackButton { get; }

        /// <summary>
        /// Called when logo tracking begins, intended to bring the osu! logo to the frontmost visually.
        /// </summary>
        public Action<bool>? RequestLogoInFront { private get; init; }

        /// <summary>
        /// The back button was pressed.
        /// </summary>
        public Action? BackButtonPressed { private get; init; }

        public const int HEIGHT = 50;

        private const int padding = 60;
        private const double transition_duration = 500;

        protected override Container<Drawable> Content => content;

        // Disable masking because it breaks due to the height of this container being less than the displayed content.
        // The height being set as it is required for transition purposes.
        public override bool UpdateSubTreeMasking() => false;

        private readonly Box background;
        private readonly Container content;

        private readonly LogoTrackingContainer logoTrackingContainer;
        private IDisposable? logoTracking;

        // TODO: This has some weird update logic local in this class, but it only works for overlay containers.
        // This is not what we want. The footer is to be displayed on *screens* with different colour schemes.
        // It needs to update on screen switch.
        //
        // For now it's locked to Blue to match song select (the most prominent usage).
        [Cached]
        private readonly OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Blue);

        public ScreenFooter(BackReceptor? receptor = null)
        {
            // This is needed so that the content set by screens gets properly disposed when exiting,
            // even if the footer isn't visible at the time.
            AlwaysPresent = true;

            RelativeSizeAxes = Axes.X;
            Height = HEIGHT;
            Anchor = Anchor.BottomLeft;
            Origin = Anchor.BottomLeft;

            var internalChildren = new Drawable[]
            {
                background = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                },
                BackButton = new ScreenBackButton
                {
                    Margin = new MarginPadding { Bottom = OsuGame.SCREEN_EDGE_MARGIN, Left = OsuGame.SCREEN_EDGE_MARGIN },
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                    Action = onBackPressed,
                },
                content = new Container
                {
                    Name = "Footer content",
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                    Padding = new MarginPadding { Left = OsuGame.SCREEN_EDGE_MARGIN + ScreenBackButton.BUTTON_WIDTH + padding },
                },
                (logoTrackingContainer = new LogoTrackingContainer
                {
                    RelativeSizeAxes = Axes.Both,
                }).WithChild(logoTrackingContainer.LogoFacade.With(f =>
                {
                    f.Anchor = Anchor.BottomRight;
                    f.Origin = Anchor.Centre;
                    f.Position = new Vector2(-76, -36);
                })),
            };

            if (receptor == null)
                internalChildren = [..internalChildren, receptor = new BackReceptor()];

            InternalChildren = internalChildren;

            receptor.OnBackPressed = () => BackButton.TriggerClick();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            background.Colour = colourProvider.Background5;
        }

        private ScheduledDelegate? changeLogoDepthDelegate;

        public void StartTrackingLogo(OsuLogo logo, float duration = 0, Easing easing = Easing.None)
        {
            changeLogoDepthDelegate?.Cancel();
            changeLogoDepthDelegate = null;

            logoTracking = logoTrackingContainer.StartTracking(logo, duration, easing);
            RequestLogoInFront?.Invoke(true);
        }

        public void StopTrackingLogo()
        {
            logoTracking?.Dispose();
            logoTracking = null;

            changeLogoDepthDelegate = Scheduler.AddDelayed(() => RequestLogoInFront?.Invoke(false), transition_duration);
        }

        protected override void PopIn()
        {
            content.FadeIn(transition_duration / 4, Easing.OutQuint);

            this.MoveToY(0, transition_duration, Easing.OutQuint)
                .FadeIn();
        }

        protected override void PopOut()
        {
            // Really we shouldn't need to do this, but some buttons protrude vertically more than expected
            // (see FooterButtonMods).
            content.FadeOut(transition_duration, Easing.OutQuint);

            this.MoveToY(ScreenFooterButton.HEIGHT, transition_duration, Easing.OutQuint)
                .Then()
                .FadeOut();
        }

        private void updateColourScheme(int hue)
        {
            colourProvider.ChangeColourScheme(hue);

            background.FadeColour(colourProvider.Background5, 150, Easing.OutQuint);
        }

        private void onBackPressed()
        {
            BackButtonPressed?.Invoke();
        }

        public partial class BackReceptor : Drawable, IKeyBindingHandler<GlobalAction>
        {
            public Action? OnBackPressed;

            public bool OnPressed(KeyBindingPressEvent<GlobalAction> e)
            {
                if (e.Repeat)
                    return false;

                switch (e.Action)
                {
                    case GlobalAction.Back:
                        OnBackPressed?.Invoke();
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
