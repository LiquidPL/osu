// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Sample;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Screens;
using osu.Game.Beatmaps;
using osu.Game.Overlays;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Screens.Footer;
using osu.Game.Screens.Menu;
using osu.Game.Users;
using osuTK;

namespace osu.Game.Screens
{
    public abstract partial class OsuScreen : Screen, IOsuScreen, IHasDescription
    {
        /// <summary>
        /// The amount of negative padding that should be applied to game background content which touches both the left and right sides of the screen.
        /// This allows for the game content to be pushed by the options/notification overlays without causing black areas to appear.
        /// </summary>
        public const float HORIZONTAL_OVERFLOW_PADDING = 50;

        /// <summary>
        /// A user-facing title for this screen.
        /// </summary>
        public virtual string Title => GetType().Name;

        public string Description => Title;

        public virtual bool AllowUserExit => true;

        public virtual bool ShowFooter => false;

        public virtual bool AllowExternalScreenChange => false;

        public virtual bool HideOverlaysOnEnter => false;

        public virtual bool HideMenuCursorOnNonMouseInput => false;

        public virtual bool RequiresPortraitOrientation => false;

        /// <summary>
        /// The initial overlay activation mode to use when this screen is entered for the first time.
        /// </summary>
        protected virtual OverlayActivation InitialOverlayActivationMode => OverlayActivation.All;

        public readonly Bindable<OverlayActivation> OverlayActivationMode;

        IBindable<OverlayActivation> IOsuScreen.OverlayActivationMode => OverlayActivationMode;

        /// <summary>
        /// The initial visibility state of the back button when this screen is entered for the first time.
        /// </summary>
        protected virtual bool InitialBackButtonVisibility => AllowUserExit;

        public readonly Bindable<bool> BackButtonVisibility;

        IBindable<bool> IOsuScreen.BackButtonVisibility => BackButtonVisibility;

        public virtual bool CursorVisible => true;

        protected new OsuGameBase Game => base.Game as OsuGameBase;

        /// <summary>
        /// The <see cref="UserActivity"/> to set the user's activity automatically to when this screen is entered.
        /// <para>This <see cref="Activity"/> will be automatically set to <see cref="InitialActivity"/> for this screen on entering for the first time
        /// unless <see cref="Activity"/> is manually set before.</para>
        /// </summary>
        protected virtual UserActivity InitialActivity => null;

        /// <summary>
        /// The current <see cref="UserActivity"/> for this screen.
        /// </summary>
        protected readonly Bindable<UserActivity> Activity = new Bindable<UserActivity>();

        Bindable<UserActivity> IOsuScreen.Activity => Activity;

        /// <summary>
        /// Whether to disallow changes to game-wise Beatmap/Ruleset bindables for this screen (and all children).
        /// </summary>
        public virtual bool DisallowExternalBeatmapRulesetChanges => false;

        private Sample sampleExit;

        protected virtual bool PlayExitSound => true;

        public virtual float BackgroundParallaxAmount => 1;

        [CanBeNull]
        protected OverlayColourProvider ColourProvider { get; init; }

        [Resolved]
        private MusicController musicController { get; set; }

        public virtual bool? ApplyModTrackAdjustments => null;

        public virtual bool? AllowGlobalTrackControl => null;

        public Bindable<WorkingBeatmap> Beatmap { get; private set; } = null!;

        public Bindable<RulesetInfo> Ruleset { get; private set; } = null!;

        public Bindable<IReadOnlyList<Mod>> Mods { get; private set; }

        private OsuScreenDependencies screenDependencies;

        private bool? globalMusicControlStateAtSuspend;

        private bool? modTrackAdjustmentStateAtSuspend;

        internal void CreateLeasedDependencies(IReadOnlyDependencyContainer dependencies) => createDependencies(dependencies);

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
        {
            if (screenDependencies == null)
            {
                if (DisallowExternalBeatmapRulesetChanges)
                    throw new InvalidOperationException($"Screens that specify {nameof(DisallowExternalBeatmapRulesetChanges)} must be pushed immediately.");

                createDependencies(parent);
            }

            return base.CreateChildDependencies(screenDependencies);
        }

        private void createDependencies(IReadOnlyDependencyContainer dependencies)
        {
            screenDependencies = new OsuScreenDependencies(DisallowExternalBeatmapRulesetChanges, dependencies);

            Beatmap = screenDependencies.Beatmap;
            Ruleset = screenDependencies.Ruleset;
            Mods = screenDependencies.Mods;

            if (ColourProvider != null)
                screenDependencies.Cache(ColourProvider);
        }

        /// <summary>
        /// The background created and owned by this screen. May be null if the background didn't change.
        /// </summary>
        [CanBeNull]
        private BackgroundScreen ownedBackground;

        [CanBeNull]
        private BackgroundScreen background;

        [Resolved(canBeNull: true)]
        [CanBeNull]
        private BackgroundScreenStack backgroundStack { get; set; }

        [Resolved(canBeNull: true)]
        private OsuLogo logo { get; set; }

        protected OsuScreen()
        {
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;

            OverlayActivationMode = new Bindable<OverlayActivation>(InitialOverlayActivationMode);
            BackButtonVisibility = new Bindable<bool>(InitialBackButtonVisibility);
        }

        [BackgroundDependencyLoader(true)]
        private void load(AudioManager audio)
        {
            sampleExit = audio.Samples.Get(@"UI/screen-back");
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            Activity.Value ??= InitialActivity;
        }

        /// <summary>
        /// Apply arbitrary changes to the current background screen in a thread safe manner.
        /// </summary>
        /// <param name="action">The operation to perform.</param>
        public void ApplyToBackground(Action<BackgroundScreen> action)
        {
            if (backgroundStack == null)
                throw new InvalidOperationException("Attempted to apply to background without a background stack being available.");

            if (background == null)
                throw new InvalidOperationException("Attempted to apply to background before screen is pushed.");

            background.ApplyToBackground(action);
        }

        public override void OnResuming(ScreenTransitionEvent e)
        {
            applyArrivingDefaults(true);

            // it's feasible to resume to a screen if the target screen never loaded successfully.
            // in such a case there's no need to restore this value.
            if (modTrackAdjustmentStateAtSuspend != null)
                musicController.ApplyModTrackAdjustments = modTrackAdjustmentStateAtSuspend.Value;
            if (globalMusicControlStateAtSuspend != null)
                musicController.AllowTrackControl.Value = globalMusicControlStateAtSuspend.Value;

            base.OnResuming(e);
        }

        public override void OnSuspending(ScreenTransitionEvent e)
        {
            base.OnSuspending(e);

            modTrackAdjustmentStateAtSuspend = musicController.ApplyModTrackAdjustments;
            globalMusicControlStateAtSuspend = musicController.AllowTrackControl.Value;

            onSuspendingLogo();
        }

        public override void OnEntering(ScreenTransitionEvent e)
        {
            applyArrivingDefaults(false);

            if (ApplyModTrackAdjustments != null)
                musicController.ApplyModTrackAdjustments = ApplyModTrackAdjustments.Value;

            if (AllowGlobalTrackControl != null)
                musicController.AllowTrackControl.Value = AllowGlobalTrackControl.Value;

            if (backgroundStack?.Push(ownedBackground = CreateBackground()) != true)
            {
                // If the constructed instance was not actually pushed to the background stack, we don't want to track it unnecessarily.
                ownedBackground?.Dispose();
                ownedBackground = null;
            }

            background = backgroundStack?.CurrentScreen as BackgroundScreen;
            base.OnEntering(e);
        }

        public override bool OnExiting(ScreenExitEvent e)
        {
            // Only play the exit sound if we are the last screen in the exit sequence.
            // This stops many sample playbacks from stacking when a huge screen purge happens (ie. returning to menu via the home button
            // from a deeply nested screen).
            bool arrivingAtFinalDestination = e.Next == e.Destination;

            if (ValidForResume && PlayExitSound && arrivingAtFinalDestination)
                sampleExit?.Play();

            if (ValidForResume && logo != null)
                onExitingLogo();

            if (base.OnExiting(e))
                return true;

            if (ownedBackground != null && backgroundStack?.CurrentScreen == ownedBackground)
                backgroundStack?.Exit();

            return false;
        }

        /// <summary>
        /// Fired when this screen was entered or resumed and the logo state is required to be adjusted.
        /// </summary>
        protected virtual void LogoArriving(OsuLogo logo, bool resuming)
        {
            logo.Action = null;
            logo.FadeOut(300, Easing.OutQuint);

            logo.Origin = Anchor.Centre;

            logo.ChangeAnchor(Anchor.TopLeft);
            logo.RelativePositionAxes = Axes.Both;

            logo.Triangles = true;
            logo.Ripple = true;
        }

        private void applyArrivingDefaults(bool isResuming)
        {
            logo?.AppendAnimatingAction(() =>
            {
                if (this.IsCurrentScreen()) LogoArriving(logo, isResuming);
            }, true);
        }

        private void onExitingLogo()
        {
            logo?.AppendAnimatingAction(() => LogoExiting(logo), false);
        }

        /// <summary>
        /// Fired when this screen was exited to add any outwards transition to the logo.
        /// </summary>
        protected virtual void LogoExiting(OsuLogo logo)
        {
        }

        private void onSuspendingLogo()
        {
            logo?.AppendAnimatingAction(() => LogoSuspending(logo), false);
        }

        /// <summary>
        /// Fired when this screen was suspended to add any outwards transition to the logo.
        /// </summary>
        protected virtual void LogoSuspending(OsuLogo logo)
        {
        }

        #region Footer handling

        [Resolved(canBeNull: true)]
        [CanBeNull]
        protected ScreenFooter Footer { get; private set; }

        [CanBeNull]
        private FooterButtonFlowContainer footerButtonContainer;

        private const int footer_button_animation_delay = 30;

        /// <summary>
        /// Called when the screen should add its footer content to the footer drawable,
        /// and animate its arrival.
        /// </summary>
        public virtual void FooterArriving()
        {
            if (Footer == null)
                return;

            if (ColourProvider != null)
                Footer.UpdateColourScheme(ColourProvider.Hue);

            var footerContent = CreateFooterContent();

            if (footerContent.Count == 0)
                return;

            LoadComponents(footerContent);
            Footer.AddRange(footerContent);

            if (footerButtonContainer == null)
                return;

            foreach ((var button, int i) in footerButtonContainer.Children.Select((b, i) => (b, i)))
            {
                // ensure transforms are added after LoadComplete to not be aborted by the FinishTransforms call.
                button.OnLoadComplete += _ => button.Appear(i * footer_button_animation_delay);
            }
        }

        /// <summary>
        /// Called when the screen should animate the exit of its footer content,
        /// and remove it from the footer drawable.
        /// </summary>
        public virtual void FooterExiting()
        {
            if (footerButtonContainer == null)
                return;

            foreach ((var button, int i) in footerButtonContainer.Children.Select((b, i) => (b, i)))
            {
                button.Enabled.Value = false;
                button.Disappear(i * footer_button_animation_delay).Expire();
            }

            double delay = footerButtonContainer.Count > 0
                ? footerButtonContainer.Max(b => b.LatestTransformEndTime) - Time.Current
                : 0;

            footerButtonContainer.Delay(delay).FadeOut().Expire();
        }

        /// <summary>
        /// Buttons to be added to the game's footer toolbar.
        /// </summary>
        protected virtual IReadOnlyList<ScreenFooterButton> CreateFooterButtons() => Array.Empty<ScreenFooterButton>();

        /// <summary>
        /// The content to be set on the game's footer toolbar.
        /// </summary>
        ///
        /// <remarks>
        /// Subclasses can override this in order to display additional custom buttons
        /// on top of the ones defined in <see cref="CreateFooterButtons"/>.
        /// </remarks>
        protected virtual IReadOnlyList<Drawable> CreateFooterContent()
        {
            return new[]
            {
                // In order to prevent the appear/disappear button transforms from messing with
                // the height of the container, it is explicitly set to the height of `ScreenFooterButton`.
                footerButtonContainer = new FooterButtonFlowContainer
                {
                    Name = "Visible footer buttons",
                    AutoSizeAxes = Axes.X,
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                    Y = ScreenFooterButton.CORNER_RADIUS,
                    Height = ScreenFooterButton.HEIGHT,
                    Spacing = new Vector2(7, 0),
                    Children = CreateFooterButtons(),
                },
            };
        }

        #endregion

        /// <summary>
        /// Override to create a BackgroundMode for the current screen.
        /// Note that the instance created may not be the used instance if it matches the BackgroundMode equality clause.
        /// </summary>
        protected virtual BackgroundScreen CreateBackground() => null;

        public virtual bool OnBackButton() => false;
    }
}
