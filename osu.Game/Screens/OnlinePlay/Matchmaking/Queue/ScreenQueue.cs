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
        public const int WAITING_LOOP_DELAY = 2000;

        public override bool? ApplyModTrackAdjustments => false;

        [Cached]
        private readonly OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Aquamarine);

        private Container temporaryUserDisplayContainer = null!;
        private OnlinePlayScreenWaveContainer waves = null!;
        private Container centreContainer = null!;
        private Container bottomContainer = null!;

        private PoolSelector poolSelector = null!;
        private OsuSpriteText selectedPoolLabel = null!;

        private QueueLoadingSpinner loadingSpinner = null!;
        private InQueueAnimation inQueueAnimation = null!;
        private UserDisplay userDisplay = null!;
        private QueueVisualisation queueVisualisation = null!;

        private SideContainer leftStats = null!;
        private SideContainer rightStats = null!;

        private TierProgressBar? tierProgressBar;

        private StatusBar statusBar = null!;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        [Resolved]
        private MultiplayerClient client { get; set; } = null!;

        [Resolved]
        private QueueController queue { get; set; } = null!;

        [Resolved]
        private IRankedPlayDivisionStore divisionStore { get; set; } = null!;

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

        private bool initialLoadComplete;

        private ScheduledDelegate? initialLoadingSpinnerDelegate;
        private SampleChannel? waitingLoopChannel;
        private ScheduledDelegate? startLoopPlaybackDelegate;
        private DrawableSample waitingLoop = null!;
        private ScheduledDelegate? pushScreenDelegate;

        private APIRankedPlayDivision[]? divisions;
        private readonly Bindable<MatchmakingUserStats?> userStats = new Bindable<MatchmakingUserStats?>();

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
            FooterContent.Value = ScreenFooterContent.Argon();
        }

        [BackgroundDependencyLoader]
        private void load(AudioManager audio, IAPIProvider api, TextureStore textures)
        {
            enqueueSample = audio.Samples.Get(@"Multiplayer/Matchmaking/enqueue");
            matchFoundSample = audio.Samples.Get(@"Multiplayer/Matchmaking/match-found");

            FooterContent.Value = ScreenFooterContent.Argon(null, () => [new QueueButton()]);

            InternalChild = new Container
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
                                            Depth = 10,
                                            Children = new Drawable[]
                                            {
                                                inQueueAnimation = new InQueueAnimation(),
                                                queueVisualisation = new QueueVisualisation
                                                {
                                                    Anchor = Anchor.Centre,
                                                    Origin = Anchor.Centre,
                                                    Size = new Vector2(700),
                                                    Alpha = 0,
                                                    SafeRadius = 0.3f,
                                                },
                                                loadingSpinner = new QueueLoadingSpinner
                                                {
                                                    Anchor = Anchor.Centre,
                                                    Origin = Anchor.Centre,
                                                    Size = new Vector2(50),
                                                    State = { Value = Visibility.Hidden },
                                                    Depth = -int.MaxValue,
                                                },
                                                leftStats = new SideContainer
                                                {
                                                    Anchor = Anchor.CentreLeft,
                                                    Origin = Anchor.CentreLeft,
                                                    Children = new Drawable[]
                                                    {
                                                        new SideContainer.RatingStatistic
                                                        {
                                                            Label = "Current Rating",
                                                            Ruleset = { BindTarget = ruleset },
                                                            UserStats = { BindTarget = userStats },
                                                        },
                                                        new SideContainer.RatingStatistic
                                                        {
                                                            Label = "Peak Rating",
                                                            Ruleset = { BindTarget = ruleset },
                                                            UserStats = { BindTarget = userStats },
                                                        },
                                                    },
                                                },
                                                rightStats = new SideContainer
                                                {
                                                    Anchor = Anchor.CentreRight,
                                                    Origin = Anchor.CentreRight,
                                                    Children = new Drawable[]
                                                    {
                                                        new SideContainer.Statistic
                                                        {
                                                            Label = "Online Players",
                                                            Value = 412.ToStandardFormattedString(0),
                                                        },
                                                        new SideContainer.Statistic
                                                        {
                                                            Label = "Ongoing Sessions",
                                                            Value = 1834.ToStandardFormattedString(0),
                                                        },
                                                    },
                                                },
                                            },
                                        },
                                    },
                                    new Drawable[]
                                    {
                                        bottomContainer = new Container
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            Height = 60,
                                            Children = new Drawable[]
                                            {
                                                statusBar = new StatusBar
                                                {
                                                    Anchor = Anchor.BottomCentre,
                                                    Origin = Anchor.BottomCentre,
                                                },
                                            },
                                        },
                                    },
                                }
                            },
                        },
                    },
                    temporaryUserDisplayContainer = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding { Top = 100, Bottom = 150 },
                        Child = userDisplay = new UserDisplay(api.LocalUser.Value)
                        {
                            Anchor = Anchor.Centre,
                            UserStats = { BindTarget = userStats },
                        },
                    },
                },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            client.MatchmakingLobbyStatusChanged += onMatchmakingLobbyStatusChanged;

            currentState.BindTo(queue.CurrentState);
            currentState.BindValueChanged(_ => onStateChanged());

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

            populateDivisions().FireAndForget();
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

        private async Task populateDivisions()
        {
            divisions = await divisionStore.GetDivisions().ConfigureAwait(false);

            Schedule(createTierProgressBar);

            void createTierProgressBar()
            {
                if (userStats.Value == null)
                {
                    Schedule(createTierProgressBar);
                    return;
                }

                bottomContainer.Add(tierProgressBar = new TierProgressBar
                {
                    RelativeSizeAxes = Axes.X,
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.BottomCentre,
                    Y = 200,
                    Padding = new MarginPadding { Horizontal = 20 },
                    Divisions = divisions,
                    InitialRating = userStats.Value.Rating - 300,
                });
            }
        }

        private void attemptShowData()
        {
            initialLoadingSpinnerDelegate ??= Scheduler.AddDelayed(() => loadingSpinner.Show(), 1000);

            if (availablePools.Value == null || userStats.Value == null || divisions == null)
            {
                Schedule(attemptShowData);
                return;
            }

            initialLoadingSpinnerDelegate?.Cancel();
            initialLoadingSpinnerDelegate = null;

            initialLoadComplete = true;

            bottomContainer.Add(tierProgressBar = new TierProgressBar
            {
                RelativeSizeAxes = Axes.X,
                Anchor = Anchor.BottomCentre,
                Origin = Anchor.BottomCentre,
                Y = 200,
                Padding = new MarginPadding { Horizontal = 20 },
                Divisions = divisions,
                InitialRating = userStats.Value.Rating - 300,
            });

            tierProgressBar.OnLoadComplete += _ => Schedule(() =>
            {
                tierProgressBar?.SetRating(userStats.Value.Rating);

                poolSelector.AvailablePools.BindTo(availablePools);
                poolSelector.SelectedPool.BindTo(selectedPool);

                leftStats.Show();
                rightStats.Show();
                queueVisualisation.Show();

                using (BeginDelayedSequence(200))
                {
                    foreach ((var stat, int i) in leftStats.Select((d, i) => (d, i)))
                    {
                        stat.Delay(i * 50).FadeIn(400, new CubicBezierEasingFunction(0.5, 0, 0.5, 1));
                    }

                    foreach ((var stat, int i) in rightStats.Select((d, i) => (d, i)))
                    {
                        stat.Delay(100 + i * 50).FadeIn(400, new CubicBezierEasingFunction(0.5, 0, 0.5, 1));
                    }
                }

                onStateChanged();
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

            // Global (incremental) updates will not contain the user rating, so keep the one we already received from initial status data.
            // The rank value can be null if the rating is provisional, so we don't check for it here.
            if (status.UserRating != null && status.UserSigma != null)
            {
                userStats.Value = new MatchmakingUserStats(status.UserRating.Value, status.UserSigma.Value, status.UserRank);
                tierProgressBar?.SetRating(userStats.Value.Rating);
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

            // Don't immediately show the spinner on initial push as it would have
            // appeared before all the animations had time to complete.
            if (initialLoadComplete)
                loadingSpinner.Show();

            client.MatchmakingJoinLobbyWithParams(new MatchmakingJoinLobbyRequest
            {
                PoolId = selectedPool.Value.Id
            }).FireAndForget();
        }

        private void clearLobbyData()
        {
            userStats.Value = null;

            queueVisualisation.Users = Array.Empty<APIUser>();
        }

        private void updateChangesAllowedState() => changesAllowed.Value = isConnected.Value && currentState.Value == MatchmakingScreenState.Idle;

        public override void OnEntering(ScreenTransitionEvent e)
        {
            base.OnEntering(e);

            waves.Show();

            queue.SearchInForeground();

            Scheduler.AddDelayed(() =>
            {
                userDisplay.Show();
                inQueueAnimation.Show();
            }, 400);

            Scheduler.AddDelayed(attemptShowData, WaveContainer.APPEAR_DURATION - 200);

            Scheduler.AddDelayed(() =>
            {
                temporaryUserDisplayContainer.Remove(userDisplay, false);
                temporaryUserDisplayContainer.Expire();
                centreContainer.Add(userDisplay);
            }, WaveContainer.APPEAR_DURATION);
        }

        public override void OnResuming(ScreenTransitionEvent e)
        {
            base.OnResuming(e);

            // Rejoin the lobby.
            selectedPool.TriggerChange();
        }

        public override void OnSuspending(ScreenTransitionEvent e)
        {
            base.OnSuspending(e);

            stopWaitingLoopPlayback();
            client.MatchmakingLeaveLobby().FireAndForget();
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

        private void onStateChanged()
        {
            startLoopPlaybackDelegate?.Cancel();
            stopWaitingLoopPlayback();

            pushScreenDelegate?.Cancel();
            pushScreenDelegate = null;

            updateChangesAllowedState();

            switch (currentState.Value)
            {
                case MatchmakingScreenState.Idle:
                    tierProgressBar?.MoveToY(0, 400, new CubicBezierEasingFunction(0, 0, 0, 1));
                    statusBar.Hide();
                    // LinkFlowContainer duelHint;

                    // duelHint.AddText("Open the ");
                    // duelHint.AddLink("dashboard", () => dashboardOverlay?.Show());
                    // duelHint.AddText(" to duel another player!");

                    break;

                case MatchmakingScreenState.Queueing:
                    tierProgressBar?.MoveToY(200, 400, new CubicBezierEasingFunction(0, 0, 0, 1));
                    Scheduler.AddDelayed(() => statusBar.Show(), 50);

                    enqueueSample?.Play();
                    startLoopPlaybackDelegate = Scheduler.AddDelayed(startWaitingLoopPlayback, WAITING_LOOP_DELAY);
                    break;

                case MatchmakingScreenState.PendingAccept:
                    client.MatchmakingAcceptInvitation().FireAndForget();

                    matchFoundSample?.Play();
                    music.DuckMomentarily(1250);

                    break;

                case MatchmakingScreenState.AcceptedWaitingForRoom:
                    startWaitingLoopPlayback();
                    break;

                case MatchmakingScreenState.InRoom:
                    // room received, show users and transition to next screen.
                    pushScreenDelegate = Scheduler.AddDelayed(attemptJoinRoom, 2000);

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(currentState), currentState.Value, null);
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

        private void attemptJoinRoom()
        {
            if (currentState.Value != MatchmakingScreenState.InRoom)
                return;

            if (client.Room == null)
            {
                pushScreenDelegate = Scheduler.Add(attemptJoinRoom);
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
        }

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
    }
}
