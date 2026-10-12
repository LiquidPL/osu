// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Sample;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Audio;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Transforms;
using osu.Framework.Logging;
using osu.Framework.Screens;
using osu.Framework.Threading;
using osu.Game.Database;
using osu.Game.Extensions;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Matchmaking;
using osu.Game.Online.Matchmaking.Requests;
using osu.Game.Online.Multiplayer;
using osu.Game.Online.RankedPlay;
using osu.Game.Overlays;
using osu.Game.Overlays.Volume;
using osu.Game.Rulesets;
using osu.Game.Scoring;
using osu.Game.Screens.Footer;
using osu.Game.Screens.OnlinePlay.Matchmaking.Match;
using osu.Game.Screens.OnlinePlay.Matchmaking.RankedPlay;
using osuTK;

namespace osu.Game.Screens.OnlinePlay.Matchmaking.Queue
{
    /// <summary>
    /// The initial screen that users arrive at when preparing for a quick play session.
    /// </summary>
    public partial class ScreenQueue : OsuScreen
    {
        public const int CORNER_RADIUS = 12;

        public override bool ShowFooter => true;

        public override bool? ApplyModTrackAdjustments => false;

        [Cached]
        private readonly OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Aquamarine);

        private Container rootContainer = null!;
        private OnlinePlayScreenWaveContainer waves = null!;
        private Container centreContainer = null!;

        private PoolSelector poolSelector = null!;
        private OsuSpriteText selectedPoolLabel = null!;

        private QueueLoadingSpinner loadingSpinner = null!;
        private UserBackground userBackground = null!;
        private UserDisplay userDisplay = null!;
        private QueueVisualisation queueVisualisation = null!;

        private SideContainer leftStats = null!;
        private SideContainer.RatingStatistic currentRatingDisplay = null!;
        private SideContainer rightStats = null!;

        private TierProgressBar tierProgressBar = null!;

        private RankedPlayStatusBar statusBar = null!;

        private QueueButton queueButton = null!;

        private bool progressBarEntered;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        [Resolved]
        private MultiplayerClient client { get; set; } = null!;

        [Resolved]
        private QueueController queue { get; set; } = null!;

        [Resolved]
        private UserLookupCache userLookupCache { get; set; } = null!;

        [Resolved]
        private IBindable<RulesetInfo> ruleset { get; set; } = null!;

        [Resolved]
        private MusicController music { get; set; } = null!;

        [Resolved]
        private DashboardOverlay? dashboardOverlay { get; set; }

        private readonly IBindable<MatchmakingScreenState> currentState = new Bindable<MatchmakingScreenState>();

        private readonly Bindable<MatchmakingPool[]?> availablePools = new Bindable<MatchmakingPool[]?>();
        private readonly Bindable<MatchmakingPool?> selectedPool = new Bindable<MatchmakingPool?>();
        private readonly MatchmakingPoolType poolType;

        private CancellationTokenSource userLookupCancellation = new CancellationTokenSource();

        private Sample? enqueueSample;
        private Sample? matchFoundSample;

        private ScheduledDelegate? queueTimerDelegate;

        private SampleChannel? waitingLoopChannel;
        private ScheduledDelegate? startLoopPlaybackDelegate;
        private DrawableSample waitingLoop = null!;
        private ScheduledDelegate? pushScreenDelegate;

        private readonly Bindable<UserStats?> userStats = new Bindable<UserStats?>();
        private readonly Bindable<APIRankedPlayDivision?> userDivision = new Bindable<APIRankedPlayDivision?>();

        private IBindable<bool> isConnected = null!;

        /// <summary>
        /// Whether any changes to the lobby state by the user are allowed,
        /// for instance when the user is actively finding a match, or connection
        /// has been lost.
        /// </summary>
        private readonly Bindable<bool> changesAllowed = new Bindable<bool>();

        public ScreenQueue(MatchmakingPoolType poolType)
        {
            this.poolType = poolType;
        }

        [BackgroundDependencyLoader]
        private void load(AudioManager audio, IAPIProvider api, OsuColour colours, TextureStore textures)
        {
            enqueueSample = audio.Samples.Get(@"Multiplayer/Matchmaking/enqueue");
            matchFoundSample = audio.Samples.Get(@"Multiplayer/Matchmaking/match-found");

            queueButton = new QueueButton
            {
                Anchor = Anchor.BottomRight,
                Origin = Anchor.BottomRight,
                Margin = new MarginPadding
                {
                    Bottom = OsuGame.SCREEN_EDGE_MARGIN,
                    Right = OsuGame.SCREEN_EDGE_MARGIN * 2
                },
                Width = 220,
                State = { BindTarget = currentState },
                SelectedPool = { BindTarget = selectedPool },
            };

            InternalChild = rootContainer = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    waitingLoop = new DrawableSample(audio.Samples.Get(@"Multiplayer/Matchmaking/waiting-loop")),
                    new ScrollAdjustsVolume(),
                    waves = new OnlinePlayScreenWaveContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Children = new Drawable[]
                        {
                            new Sprite
                            {
                                RelativeSizeAxes = Axes.Both,
                                Texture = textures.Get("Backgrounds/bg1"),
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                FillMode = FillMode.Fill,
                                Colour = colourProvider.Dark2
                            },
                            new GridContainer
                            {
                                RelativeSizeAxes = Axes.Both,
                                Padding = new MarginPadding { Bottom = ScreenFooter.HEIGHT + 40 },
                                RowDimensions = new[]
                                {
                                    new Dimension(GridSizeMode.Absolute, size: 100),
                                    new Dimension(),
                                    new Dimension(GridSizeMode.AutoSize),
                                },
                                Content = new[]
                                {
                                    new Drawable[]
                                    {
                                        new Container
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Anchor = Anchor.TopCentre,
                                            Origin = Anchor.TopCentre,
                                            Padding = new MarginPadding { Top = -12 },
                                            Child = new Container
                                            {
                                                RelativeSizeAxes = Axes.Both,
                                                Masking = true,
                                                CornerRadius = 12,
                                                Children = new Drawable[]
                                                {
                                                    new Box
                                                    {
                                                        RelativeSizeAxes = Axes.Both,
                                                        Colour = Colour4.Black.Opacity(0.5f),
                                                    },
                                                    new GridContainer
                                                    {
                                                        RelativeSizeAxes = Axes.Both,
                                                        Padding = new MarginPadding { Top = 12, Horizontal = 60 },
                                                        ColumnDimensions = new[]
                                                        {
                                                            new Dimension(),
                                                            new Dimension(GridSizeMode.AutoSize),
                                                            new Dimension(),
                                                        },
                                                        Content = new[]
                                                        {
                                                            new Drawable?[]
                                                            {
                                                                new FillFlowContainer
                                                                {
                                                                    AutoSizeAxes = Axes.Both,
                                                                    Anchor = Anchor.CentreLeft,
                                                                    Origin = Anchor.CentreLeft,
                                                                    Spacing = new Vector2(6),
                                                                    Direction = FillDirection.Horizontal,
                                                                    Children = new Drawable[]
                                                                    {
                                                                        new OsuSpriteText
                                                                        {
                                                                            Anchor = Anchor.CentreLeft,
                                                                            Origin = Anchor.CentreLeft,
                                                                            Font = OsuFont.TorusAlternate.With(size: 24),
                                                                            Text = "Ranked Play",
                                                                        },
                                                                        new OsuSpriteText
                                                                        {
                                                                            Anchor = Anchor.CentreLeft,
                                                                            Origin = Anchor.CentreLeft,
                                                                            Font = OsuFont.TorusAlternate.With(size: 24),
                                                                            Colour = colours.Yellow,
                                                                            Text = "·",
                                                                        },
                                                                        new OsuSpriteText
                                                                        {
                                                                            Anchor = Anchor.CentreLeft,
                                                                            Origin = Anchor.CentreLeft,
                                                                            Font = OsuFont.TorusAlternate.With(size: 24),
                                                                            Colour = colours.Yellow,
                                                                            Text = "Lobby",
                                                                        },
                                                                    },
                                                                },
                                                                poolSelector = new PoolSelector
                                                                {
                                                                    Anchor = Anchor.Centre,
                                                                    Origin = Anchor.Centre,
                                                                    Enabled = { BindTarget = changesAllowed },
                                                                },
                                                                selectedPoolLabel = new OsuSpriteText
                                                                {
                                                                    Anchor = Anchor.CentreRight,
                                                                    Origin = Anchor.CentreRight,
                                                                    Font = OsuFont.TorusAlternate.With(size: 24),
                                                                },
                                                            },
                                                        },
                                                    },
                                                },
                                            },
                                        },
                                    },
                                    new Drawable[]
                                    {
                                        centreContainer = new Container
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            Depth = 10,
                                            Children = new Drawable[]
                                            {
                                                userBackground = new UserBackground(),
                                                queueVisualisation = new QueueVisualisation
                                                {
                                                    RelativeSizeAxes = Axes.Both,
                                                    Anchor = Anchor.Centre,
                                                    Origin = Anchor.Centre,
                                                    Alpha = 0,
                                                    SafeRadius = UserBackground.WIDTH / 2f,
                                                },
                                                loadingSpinner = new QueueLoadingSpinner
                                                {
                                                    Anchor = Anchor.Centre,
                                                    Origin = Anchor.Centre,
                                                    Size = new Vector2(50),
                                                    State = { Value = Visibility.Hidden },
                                                    Depth = -int.MaxValue,
                                                },
                                                // leftStats = new SideContainer
                                                // {
                                                //     Anchor = Anchor.CentreLeft,
                                                //     Origin = Anchor.CentreLeft,
                                                //     Children = new Drawable[]
                                                //     {
                                                //         currentRatingDisplay = new SideContainer.RatingStatistic
                                                //         {
                                                //             Label = "Current Rating",
                                                //             Ruleset = { BindTarget = ruleset },
                                                //         },
                                                //     },
                                                // },
                                                // rightStats = new SideContainer
                                                // {
                                                //     Anchor = Anchor.CentreRight,
                                                //     Origin = Anchor.CentreRight,
                                                //     Children = new Drawable[]
                                                //     {
                                                //         new SideContainer.Statistic
                                                //         {
                                                //             Label = "Online Players",
                                                //             Value = 412.ToLocalisableString(@"N0"),
                                                //         },
                                                //         new SideContainer.Statistic
                                                //         {
                                                //             Label = "Ongoing Sessions",
                                                //             Value = 1834.ToLocalisableString(@"N0"),
                                                //         },
                                                //     },
                                                // },
                                            },
                                        },
                                    },
                                    new Drawable[]
                                    {
                                        new Container
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            Height = 60,
                                            Anchor = Anchor.BottomCentre,
                                            Origin = Anchor.BottomCentre,
                                            Children = new Drawable[]
                                            {
                                                tierProgressBar = new TierProgressBar
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    Anchor = Anchor.BottomCentre,
                                                    Origin = Anchor.BottomCentre,
                                                    Padding = new MarginPadding { Horizontal = 20 },
                                                    AlwaysPresent = true,
                                                    Divisions = divisions,
                                                },
                                                statusBar = new RankedPlayStatusBar
                                                {
                                                    Anchor = Anchor.BottomCentre,
                                                    Origin = Anchor.BottomCentre,
                                                    Alpha = 0,
                                                    AlwaysPresent = true,
                                                },
                                            },
                                        },
                                    },
                                }
                            },
                        },
                    },
                    userDisplay = new UserDisplay(api.LocalUser.Value)
                    {
                        UserStats = { BindTarget = userStats },
                        CurrentDivision = { BindTarget = userDivision },
                    },
                },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Footer?.Add(queueButton);

            client.MatchmakingLobbyStatusChanged += onMatchmakingLobbyStatusChanged;

            currentState.BindTo(queue.CurrentState);
            currentState.BindValueChanged(s => SetState(s.NewValue));

            selectedPool.BindTo(queue.SelectedPool);
            selectedPool.BindValueChanged(_ => refreshLobbyData());

            isConnected = client.IsConnected.GetBoundCopy();
            isConnected.BindValueChanged(connected => Schedule(() =>
            {
                updateChangesAllowedState();

                if (connected.NewValue)
                {
                    // If we've already fetched the pools during this session (i.e. a network reconnect),
                    // assume they haven't changed as they should remain unchanged during a single season.
                    if (availablePools.Value == null)
                        populateAvailablePools().FireAndForget();

                    refreshLobbyData();
                }
                else
                {
                    clearLobbyData();
                }
            }), true);
        }

        private async Task populateAvailablePools()
        {
            MatchmakingPool[] pools = await client.GetMatchmakingPoolsOfType(poolType).ConfigureAwait(false);

            Schedule(() =>
            {
                availablePools.Value = pools;

                // Default to the currently queueing pool, or fallback to the user's ruleset for the initial pool selection.
                selectedPool.Value ??= pools.FirstOrDefault(p => p.RulesetId == ruleset.Value.OnlineID) ?? pools.FirstOrDefault();
            });
        }

        private void onMatchmakingLobbyStatusChanged(MatchmakingLobbyStatus status) => Scheduler.Add(() =>
        {
            userLookupCancellation.Cancel();
            var cancellation = userLookupCancellation = new CancellationTokenSource();

            userLookupCache.GetUsersAsync(status.UsersInQueue, cancellation.Token)
                           .ContinueWith(result => Schedule(() =>
                           {
                               APIUser?[] users = result.GetResultSafely();
                               if (!cancellation.IsCancellationRequested)
                                   queueVisualisation.Users = users.OfType<APIUser>().ToArray();
                           }), cancellation.Token);

            loadingSpinner.Hide();

            // Global (incremental) updates will not contain the user rating, sigma, or rank, so keep the one we already received from initial status data.
            if (status.UserRating != null && status.UserSigma != null && status.UserRank != null)
            {
                userStats.Value = new UserStats(status.UserRating.Value, status.UserSigma.Value, status.UserRank.Value);
                userDivision.Value = divisions.FirstOrDefault(d => d.StartRating <= userStats.Value.Rating && d.EndRating >= userStats.Value.Rating);

                // Ensure that an in-flight status update won't interrupt the entering animation
                // of the progress bar (which uses a different easing function).
                if (progressBarEntered)
                    tierProgressBar.SetRating(userStats.Value.Rating);
            }
        });

        private void refreshLobbyData()
        {
            clearLobbyData();

            if (selectedPool.Value == null)
            {
                client.MatchmakingLeaveLobby().FireAndForget();
                return;
            }

            selectedPoolLabel.Text = selectedPool.Value.Name;
            loadingSpinner.Show();

            client.MatchmakingJoinLobbyWithParams(new MatchmakingJoinLobbyRequest
            {
                PoolId = selectedPool.Value.Id
            }).FireAndForget();
        }

        private void clearLobbyData()
        {
            userStats.Value = null;
            userDivision.Value = null;

            queueVisualisation.Users = Array.Empty<APIUser>();
        }

        private void updateChangesAllowedState() => changesAllowed.Value = isConnected.Value && currentState.Value == MatchmakingScreenState.Idle;

        public override void OnEntering(ScreenTransitionEvent e)
        {
            base.OnEntering(e);

            waves.Show();
            queue.SearchInForeground();

            tierProgressBar.MoveToY(200);

            centreContainer.OnLoadComplete += _ => userDisplay.Position = new Vector2(centreContainer.DrawWidth / 2, 100 + centreContainer.DrawHeight / 2);

            Scheduler.AddDelayed(() =>
            {
                userDisplay.Show();
                userBackground.Show();
            }, 400);

            Scheduler.AddDelayed(() =>
            {
                poolSelector.AvailablePools.BindTo(availablePools);
                poolSelector.SelectedPool.BindTo(selectedPool);

                if (currentState.Value == MatchmakingScreenState.Idle)
                {
                    tierProgressBar
                        .MoveToY(0, 400, new CubicBezierEasingFunction(0, 0, 0, 1))
                        .Then()
                        .Schedule(() => progressBarEntered = true);
                }
                else
                {
                    statusBar.FadeIn(400, Easing.OutQuint);
                }

                // leftStats.Show();
                // rightStats.Show();
                queueVisualisation.Show();

                if (userStats.Value != null)
                    tierProgressBar.SetRating(userStats.Value.Rating, true);

                SetState(currentState.Value);
            }, WaveContainer.APPEAR_DURATION - 200);

            Scheduler.AddDelayed(() =>
            {
                rootContainer.Remove(userDisplay, false);
                userDisplay.Position = default;
                userDisplay.Anchor = Anchor.Centre;
                userDisplay.Origin = Anchor.Centre;
                centreContainer.Add(userDisplay);
            }, WaveContainer.APPEAR_DURATION);

            // Scheduler.AddDelayed(() =>
            // {
            //     // foreach ((var stat, int i) in leftStats.Select((d, i) => (d, i)))
            //     // {
            //     //     stat.Delay(i * 50).FadeIn(400, new CubicBezierEasingFunction(0.5, 0, 0.5, 1));
            //     // }
            //
            //     // foreach ((var stat, int i) in rightStats.Select((d, i) => (d, i)))
            //     // {
            //     //     stat.Delay(100 + i * 50).FadeIn(400, new CubicBezierEasingFunction(0.5, 0, 0.5, 1));
            //     // }
            // }, WaveContainer.APPEAR_DURATION);
        }

        public override void OnResuming(ScreenTransitionEvent e)
        {
            base.OnResuming(e);

            // Rejoin the lobby.
            selectedPool.TriggerChange();

            queueButton.Appear();
        }

        public override void OnSuspending(ScreenTransitionEvent e)
        {
            base.OnSuspending(e);

            stopWaitingLoopPlayback();
            client.MatchmakingLeaveLobby().FireAndForget();

            queueButton.Disappear();
        }

        public override bool OnExiting(ScreenExitEvent e)
        {
            if (base.OnExiting(e))
                return true;

            stopWaitingLoopPlayback();

            switch (currentState.Value)
            {
                default:
                    client.MatchmakingLeaveLobby().FireAndForget();
                    queue.SearchInBackground();

                    waves.Hide();
                    queueButton.Disappear();
                    this.Delay(WaveContainer.DISAPPEAR_DURATION).FadeOut();

                    return false;

                case MatchmakingScreenState.PendingAccept:
                case MatchmakingScreenState.AcceptedWaitingForRoom:
                    queue.LeaveQueue();
                    return true;

                case MatchmakingScreenState.InRoom:
                    // Block exit until it's initiated from inside the matchmaking screen.
                    return true;
            }
        }

        public void SetState(MatchmakingScreenState newState)
        {
            startLoopPlaybackDelegate?.Cancel();
            stopWaitingLoopPlayback();

            pushScreenDelegate?.Cancel();
            pushScreenDelegate = null;

            queueTimerDelegate?.Cancel();
            queueTimerDelegate = null;

            if (newState is MatchmakingScreenState.Idle or MatchmakingScreenState.Queueing)
                queueButton.Appear();
            else
                queueButton.Disappear();

            updateChangesAllowedState();

            switch (newState)
            {
                case MatchmakingScreenState.Idle:
                    tierProgressBar.MoveToY(0, 400, new CubicBezierEasingFunction(0, 0, 0, 1));
                    statusBar.FadeOut(400, Easing.OutQuint);
                    // LinkFlowContainer duelHint;

                    // duelHint.AddText("Open the ");
                    // duelHint.AddLink("dashboard", () => dashboardOverlay?.Show());
                    // duelHint.AddText(" to duel another player!");

                    break;

                case MatchmakingScreenState.Queueing:
                    tierProgressBar.MoveToY(200, 400, new CubicBezierEasingFunction(0, 0, 0, 1));
                    statusBar.FadeIn(400, Easing.OutQuint);

                    statusBar.TopText = "Searching...";
                    statusBar.BottomText = queue.QueueTimer.Elapsed.ToFormattedDuration();

                    queueTimerDelegate = Scheduler.AddDelayed(() => statusBar.BottomText = queue.QueueTimer.Elapsed.ToFormattedDuration(), 1000, true);

                    enqueueSample?.Play();
                    startLoopPlaybackDelegate = Scheduler.AddDelayed(startWaitingLoopPlayback, 2000);
                    break;

                case MatchmakingScreenState.PendingAccept:
                    client.MatchmakingAcceptInvitation().FireAndForget();
                    SetState(MatchmakingScreenState.AcceptedWaitingForRoom);

                    matchFoundSample?.Play();
                    music.DuckMomentarily(1250);

                    break;

                case MatchmakingScreenState.AcceptedWaitingForRoom:
                    statusBar.TopText = "Match found!";
                    statusBar.BottomText = "Waiting for opponents...";

                    startWaitingLoopPlayback();
                    break;

                case MatchmakingScreenState.InRoom:
                    // room received, show users and transition to next screen.
                    statusBar.TopText = "Good luck!";
                    statusBar.BottomText = string.Empty;

                    using (BeginDelayedSequence(2000))
                    {
                        pushScreenDelegate = Schedule(() =>
                        {
                            if (client.Room == null)
                            {
                                Logger.Log("Room became null, returning to idle");
                                SetState(MatchmakingScreenState.Idle);
                                return;
                            }

                            switch (poolType)
                            {
                                case MatchmakingPoolType.QuickPlay:
                                    this.Push(new ScreenMatchmaking(client.Room));
                                    break;

                                case MatchmakingPoolType.RankedPlay:
                                    this.Push(new RankedPlayScreen(client.Room));
                                    break;
                            }
                        });
                    }

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            stopWaitingLoopPlayback();

            if (client.IsNotNull())
                client.MatchmakingLobbyStatusChanged -= onMatchmakingLobbyStatusChanged;
        }

        public enum MatchmakingScreenState
        {
            Idle,
            Queueing,
            PendingAccept,
            AcceptedWaitingForRoom,
            InRoom
        }

        private void startWaitingLoopPlayback()
        {
            stopWaitingLoopPlayback();

            waitingLoopChannel = waitingLoop.GetChannel();
            if (waitingLoopChannel == null)
                return;

            waitingLoopChannel.Looping = true;
            waitingLoopChannel?.Play();

            waitingLoop.VolumeTo(1)
                       .Delay(2000)
                       .VolumeTo(0, 12000);
        }

        private void stopWaitingLoopPlayback()
        {
            waitingLoopChannel?.Stop();
            waitingLoopChannel?.Dispose();
        }

        private static readonly APIRankedPlayDivision[] divisions =
        [
            new APIRankedPlayDivision { Key = "Bronze-I", Tier = RankingTier.Bronze, Division = RankedPlayDivision.I, DisplayName = "Bronze I", StartRating = 600, EndRating = 699 },
            new APIRankedPlayDivision { Key = "Bronze-II", Tier = RankingTier.Bronze, Division = RankedPlayDivision.II, DisplayName = "Bronze II", StartRating = 700, EndRating = 799 },
            new APIRankedPlayDivision { Key = "Bronze-III", Tier = RankingTier.Bronze, Division = RankedPlayDivision.III, DisplayName = "Bronze III", StartRating = 800, EndRating = 899 },
            new APIRankedPlayDivision { Key = "Silver-I", Tier = RankingTier.Silver, Division = RankedPlayDivision.I, DisplayName = "Silver I", StartRating = 900, EndRating = 999 },
            new APIRankedPlayDivision { Key = "Silver-II", Tier = RankingTier.Silver, Division = RankedPlayDivision.II, DisplayName = "Silver II", StartRating = 1000, EndRating = 1099 },
            new APIRankedPlayDivision { Key = "Silver-III", Tier = RankingTier.Silver, Division = RankedPlayDivision.III, DisplayName = "Silver III", StartRating = 1100, EndRating = 1199 },
            new APIRankedPlayDivision { Key = "Gold-I", Tier = RankingTier.Gold, Division = RankedPlayDivision.I, DisplayName = "Gold I", StartRating = 1200, EndRating = 1299 },
            new APIRankedPlayDivision { Key = "Gold-II", Tier = RankingTier.Gold, Division = RankedPlayDivision.II, DisplayName = "Gold II", StartRating = 1300, EndRating = 1399 },
            new APIRankedPlayDivision { Key = "Gold-III", Tier = RankingTier.Gold, Division = RankedPlayDivision.III, DisplayName = "Gold III", StartRating = 1400, EndRating = 1499 },
            new APIRankedPlayDivision { Key = "Platinum-I", Tier = RankingTier.Platinum, Division = RankedPlayDivision.I, DisplayName = "Platinum I", StartRating = 1500, EndRating = 1599 },
            new APIRankedPlayDivision { Key = "Platinum-II", Tier = RankingTier.Platinum, Division = RankedPlayDivision.II, DisplayName = "Platinum II", StartRating = 1600, EndRating = 1699 },
            new APIRankedPlayDivision { Key = "Platinum-III", Tier = RankingTier.Platinum, Division = RankedPlayDivision.III, DisplayName = "Platinum III", StartRating = 1700, EndRating = 1799 },
            new APIRankedPlayDivision { Key = "Rhodium-I", Tier = RankingTier.Rhodium, Division = RankedPlayDivision.I, DisplayName = "Rhodium I", StartRating = 1800, EndRating = 1899 },
            new APIRankedPlayDivision { Key = "Rhodium-II", Tier = RankingTier.Rhodium, Division = RankedPlayDivision.II, DisplayName = "Rhodium II", StartRating = 1900, EndRating = 1999 },
            new APIRankedPlayDivision { Key = "Rhodium-III", Tier = RankingTier.Rhodium, Division = RankedPlayDivision.III, DisplayName = "Rhodium III", StartRating = 2000, EndRating = 2099 },
            new APIRankedPlayDivision { Key = "Radiant-I", Tier = RankingTier.Radiant, Division = RankedPlayDivision.I, DisplayName = "Radiant I", StartRating = 2100, EndRating = 2199 },
            new APIRankedPlayDivision { Key = "Radiant-II", Tier = RankingTier.Radiant, Division = RankedPlayDivision.II, DisplayName = "Radiant II", StartRating = 2200, EndRating = 2299 },
            new APIRankedPlayDivision { Key = "Radiant-III", Tier = RankingTier.Radiant, Division = RankedPlayDivision.III, DisplayName = "Radiant III", StartRating = 2300, EndRating = 2399 },
            new APIRankedPlayDivision { Key = "Lustrous", Tier = RankingTier.Lustrous, DisplayName = "Lustrous", StartRating = 2400, EndRating = 3000 },
        ];

        private partial class QueueLoadingSpinner : LoadingSpinner
        {
            public QueueLoadingSpinner()
            {
                Add(new Container
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Depth = int.MaxValue,
                    Masking = true,
                    Size = Vector2.Zero,
                    EdgeEffect = new EdgeEffectParameters
                    {
                        Type = EdgeEffectType.Shadow,
                        Colour = Colour4.Black.Opacity(0.75f),
                        Hollow = false,
                        Radius = 300,
                    },
                });
            }
        }

        public class UserStats
        {
            public int Rating { get; }
            public double Sigma { get; }
            public int Rank { get; }

            public UserStats(int rating, double sigma, int rank)
            {
                Rating = rating;
                Sigma = sigma;
                Rank = rank;
            }
        }
    }
}
