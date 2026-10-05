// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Sample;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Audio;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Transforms;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Screens;
using osu.Framework.Threading;
using osu.Game.Database;
using osu.Game.Extensions;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Input.Bindings;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Chat;
using osu.Game.Online.Matchmaking;
using osu.Game.Online.Matchmaking.Requests;
using osu.Game.Online.Multiplayer;
using osu.Game.Online.Multiplayer.MatchTypes.RankedPlay;
using osu.Game.Overlays;
using osu.Game.Overlays.Volume;
using osu.Game.Rulesets;
using osu.Game.Screens.Footer;
using osu.Game.Users.Drawables;
using osuTK;
using osuTK.Graphics;

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

        private InverseScalingDrawSizePreservingFillContainer rootContainer = null!;
        private OnlinePlayScreenWaveContainer waves = null!;

        private PoolSelector poolSelector = null!;
        private CircularContainer avatarContainer = null!;
        private CloudVisualisation cloud = null!;
        private SideContainer leftStats = null!;
        private SideContainer rightStats = null!;
        private TierProgressBar tierProgressBar = null!;

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

        private SampleChannel? waitingLoopChannel;
        private ScheduledDelegate? startLoopPlaybackDelegate;
        private DrawableSample waitingLoop = null!;
        private ScheduledDelegate? pushScreenDelegate;

        private int? userRating;

        private IBindable<bool> isConnected = null!;

        public ScreenQueue(MatchmakingPoolType poolType)
        {
            this.poolType = poolType;
        }

        [BackgroundDependencyLoader]
        private void load(AudioManager audio, IAPIProvider api, OsuColour colours, TextureStore textures)
        {
            enqueueSample = audio.Samples.Get(@"Multiplayer/Matchmaking/enqueue");
            matchFoundSample = audio.Samples.Get(@"Multiplayer/Matchmaking/match-found");

            InternalChild = rootContainer = new InverseScalingDrawSizePreservingFillContainer
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    waitingLoop = new DrawableSample(audio.Samples.Get(@"Multiplayer/Matchmaking/waiting-loop")),
                    new GlobalScrollAdjustsVolume(),
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
                            new Container
                            {
                                RelativeSizeAxes = Axes.X,
                                Height = 100,
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
                                                    },
                                                    new OsuSpriteText
                                                    {
                                                        Anchor = Anchor.CentreRight,
                                                        Origin = Anchor.CentreRight,
                                                        Font = OsuFont.TorusAlternate.With(size: 24),
                                                        Text = "Season 1",
                                                    },
                                                },
                                            },
                                        },
                                    },
                                },
                            },
                            cloud = new CloudVisualisation
                            {
                                Size = new Vector2(400),
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Alpha = 0,
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
                                        Value = 1337,
                                        Ruleset = { BindTarget = ruleset },
                                    },
                                    new SideContainer.RatingStatistic
                                    {
                                        Label = "Peak Rating",
                                        Value = 1834,
                                        Ruleset = { BindTarget = ruleset },
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
                                        Value = 412.ToLocalisableString(@"N0"),
                                    },
                                    new SideContainer.Statistic
                                    {
                                        Label = "Ongoing Sessions",
                                        Value = 1834.ToLocalisableString(@"N0"),
                                    },
                                },
                            },
                            tierProgressBar = new TierProgressBar
                            {
                                RelativeSizeAxes = Axes.X,
                                Anchor = Anchor.BottomCentre,
                                Origin = Anchor.BottomCentre,
                                Padding = new MarginPadding { Horizontal = 20, Bottom = ScreenFooter.HEIGHT + 40 },
                                AlwaysPresent = true,
                                Divisions =
                                [
                                    new RankedPlayDivision { Tier = Tier.Bronze, Division = Division.I, DisplayName = "Bronze I", StartRating = 600, EndRating = 699 },
                                    new RankedPlayDivision { Tier = Tier.Bronze, Division = Division.II, DisplayName = "Bronze II", StartRating = 700, EndRating = 799 },
                                    new RankedPlayDivision { Tier = Tier.Bronze, Division = Division.III, DisplayName = "Bronze III", StartRating = 800, EndRating = 899 },
                                    new RankedPlayDivision { Tier = Tier.Silver, Division = Division.I, DisplayName = "Silver I", StartRating = 900, EndRating = 999 },
                                    new RankedPlayDivision { Tier = Tier.Silver, Division = Division.II, DisplayName = "Silver II", StartRating = 1000, EndRating = 1099 },
                                    new RankedPlayDivision { Tier = Tier.Silver, Division = Division.III, DisplayName = "Silver III", StartRating = 1100, EndRating = 1199 },
                                    new RankedPlayDivision { Tier = Tier.Gold, Division = Division.I, DisplayName = "Gold I", StartRating = 1200, EndRating = 1299 },
                                    new RankedPlayDivision { Tier = Tier.Gold, Division = Division.II, DisplayName = "Gold II", StartRating = 1300, EndRating = 1399 },
                                    new RankedPlayDivision { Tier = Tier.Gold, Division = Division.III, DisplayName = "Gold III", StartRating = 1400, EndRating = 1499 },
                                    new RankedPlayDivision { Tier = Tier.Platinum, Division = Division.I, DisplayName = "Platinum I", StartRating = 1500, EndRating = 1599 },
                                    new RankedPlayDivision { Tier = Tier.Platinum, Division = Division.II, DisplayName = "Platinum II", StartRating = 1600, EndRating = 1699 },
                                    new RankedPlayDivision { Tier = Tier.Platinum, Division = Division.III, DisplayName = "Platinum III", StartRating = 1700, EndRating = 1799 },
                                    new RankedPlayDivision { Tier = Tier.Rhodium, Division = Division.I, DisplayName = "Rhodium I", StartRating = 1800, EndRating = 1899 },
                                    new RankedPlayDivision { Tier = Tier.Rhodium, Division = Division.II, DisplayName = "Rhodium II", StartRating = 1900, EndRating = 1999 },
                                    new RankedPlayDivision { Tier = Tier.Rhodium, Division = Division.III, DisplayName = "Rhodium III", StartRating = 2000, EndRating = 2099 },
                                    new RankedPlayDivision { Tier = Tier.Radiant, Division = Division.I, DisplayName = "Radiant I", StartRating = 2100, EndRating = 2199 },
                                    new RankedPlayDivision { Tier = Tier.Radiant, Division = Division.II, DisplayName = "Radiant II", StartRating = 2200, EndRating = 2299 },
                                    new RankedPlayDivision { Tier = Tier.Radiant, Division = Division.III, DisplayName = "Radiant III", StartRating = 2300, EndRating = 2399 },
                                    new RankedPlayDivision { Tier = Tier.Lustrous, DisplayName = "Lustrous", StartRating = 2400, EndRating = 3000 },
                                ],
                            },
                        },
                    },
                    avatarContainer = new CircularContainer
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Masking = true,
                        Size = new Vector2(90),
                        Alpha = 0,
                        Child = new ClickableAvatar(api.LocalUser.Value, true)
                        {
                            RelativeSizeAxes = Axes.Both,
                        },
                    },
                },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // int delay = 0;

            // foreach (var a in mainGrid.Content)
            // {
            //     foreach (var d in a)
            //     {
            //         d.FadeOut()
            //          .Delay(delay)
            //          .FadeInFromZero(500, Easing.OutQuint);
            //
            //         delay += 100;
            //     }
            // }

            client.MatchmakingLobbyStatusChanged += onMatchmakingLobbyStatusChanged;

            currentState.BindTo(queue.CurrentState);
            currentState.BindValueChanged(s => SetState(s.NewValue));

            selectedPool.BindTo(queue.SelectedPool);
            selectedPool.BindValueChanged(_ => refreshLobbyData());

            isConnected = client.IsConnected.GetBoundCopy();
            isConnected.BindValueChanged(connected => Schedule(() =>
            {
                if (connected.NewValue)
                {
                    populateAvailablePools().FireAndForget();
                    refreshLobbyData();
                }
                else
                {
                    availablePools.Value = null;
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
                                   cloud.Users = users.OfType<APIUser>().ToArray();
                           }), cancellation.Token);

            // Global (incremental) updates will not contain the user rating, so keep the one we already received from initial status data.
            if (status.UserRating != null)
                userRating = status.UserRating;

            if (tierProgressBar.IsPresent && userRating != null)
                tierProgressBar.UpdateRating(userRating.Value);

            // ratingGraph.SetData(status.RatingDistribution, userRating);
            //
            // loadRecentMatches(status.RecentMatches.OfType<RankedPlayRoomState>().ToArray()).FireAndForget();
        });

        private void refreshLobbyData()
        {
            clearLobbyData();

            if (selectedPool.Value == null)
            {
                client.MatchmakingLeaveLobby().FireAndForget();
                return;
            }

            client.MatchmakingJoinLobbyWithParams(new MatchmakingJoinLobbyRequest
            {
                PoolId = selectedPool.Value.Id
            }).FireAndForget();
        }

        private void clearLobbyData()
        {
            // resultPanelContainer.Clear();
            // resultPanelContainer.LayoutDuration = 0;
            // userRating = null;
            // ratingGraph.SetData([], null);
            //
            // cloud.Users = Array.Empty<APIUser>();
        }

        public override void OnEntering(ScreenTransitionEvent e)
        {
            base.OnEntering(e);

            waves.Show();
            queue.SearchInForeground();

            tierProgressBar.MoveToY(200);

            avatarContainer
                .Delay(400)
                .ScaleTo(0.5f)
                .FadeIn(500, new CubicBezierEasingFunction(0, 0, 0, 1))
                .ScaleTo(1, 1100, new CubicBezierEasingFunction(0.2, 2.7, 0.42, 1))
                .Then()
                .Schedule(() =>
                {
                    rootContainer.Remove(avatarContainer, false);
                    waves.Add(avatarContainer);
                });

            Scheduler.AddDelayed(() =>
            {
                poolSelector.AvailablePools.BindTo(availablePools);
                poolSelector.SelectedPool.BindTo(selectedPool);

                tierProgressBar.MoveToY(0, 400, new CubicBezierEasingFunction(0, 0, 0, 1));

                leftStats.Show();
                rightStats.Show();
                cloud.Show();

                if (userRating != null)
                {
                    tierProgressBar.UpdateRating(userRating.Value, true);
                    tierProgressBar.Show();
                }

                SetState(currentState.Value);
            }, WaveContainer.APPEAR_DURATION - 200);

            Scheduler.AddDelayed(() =>
            {
                foreach ((var stat, int i) in leftStats.Select((d, i) => (d, i)))
                {
                    stat.Delay(i * 50).FadeIn(400, new CubicBezierEasingFunction(0.5, 0, 0.5, 1));
                }

                foreach ((var stat, int i) in rightStats.Select((d, i) => (d, i)))
                {
                    stat.Delay(100 + i * 50).FadeIn(400, new CubicBezierEasingFunction(0.5, 0, 0.5, 1));
                }
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

            waves.Hide();
            this.Delay(WaveContainer.DISAPPEAR_DURATION).FadeOut();

            stopWaitingLoopPlayback();

            switch (currentState.Value)
            {
                default:
                    client.MatchmakingLeaveLobby().FireAndForget();
                    queue.SearchInBackground();
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
            // mainContent.FadeInFromZero(500, Easing.OutQuint);
            // mainContent.Clear();
            //
            startLoopPlaybackDelegate?.Cancel();
            stopWaitingLoopPlayback();

            // pushScreenDelegate?.Cancel();
            // pushScreenDelegate = null;
            //
            switch (newState)
            {
                case MatchmakingScreenState.Idle:
                    // LinkFlowContainer duelHint;
                    //
                    // mainContent.Child = new FillFlowContainer
                    // {
                    //     Anchor = Anchor.Centre,
                    //     Origin = Anchor.Centre,
                    //     RelativeSizeAxes = Axes.X,
                    //     AutoSizeAxes = Axes.Y,
                    //     Direction = FillDirection.Vertical,
                    //     Spacing = new Vector2(10),
                    //     Children = new Drawable[]
                    //     {
                    //         new PoolSelector
                    //         {
                    //             Anchor = Anchor.TopCentre,
                    //             Origin = Anchor.TopCentre,
                    //             AvailablePools = { BindTarget = availablePools },
                    //             SelectedPool = { BindTarget = selectedPool }
                    //         },
                    //         new BeginQueueingButton
                    //         {
                    //             DarkerColour = colours.Blue2,
                    //             LighterColour = colours.Blue1,
                    //             Anchor = Anchor.TopCentre,
                    //             Origin = Anchor.TopCentre,
                    //             Width = 200,
                    //             Enabled = { BindTarget = isConnected },
                    //             SelectedPool = { BindTarget = selectedPool },
                    //             Action = () =>
                    //             {
                    //                 Debug.Assert(selectedPool.Value != null);
                    //                 queue.JoinQueue(selectedPool.Value);
                    //             },
                    //             Text = "Begin queueing",
                    //         },
                    //         duelHint = new LinkFlowContainer
                    //         {
                    //             TextAnchor = Anchor.TopCentre,
                    //             RelativeSizeAxes = Axes.X,
                    //             AutoSizeAxes = Axes.Y,
                    //         }
                    //     }
                    // };
                    //
                    // duelHint.AddText("Open the ");
                    // duelHint.AddLink("dashboard", () => dashboardOverlay?.Show());
                    // duelHint.AddText(" to duel another player!");

                    break;

                case MatchmakingScreenState.Queueing:
                    // mainContent.Child = new FillFlowContainer
                    // {
                    //     Anchor = Anchor.Centre,
                    //     Origin = Anchor.Centre,
                    //     AutoSizeAxes = Axes.Both,
                    //     Direction = FillDirection.Vertical,
                    //     Spacing = new Vector2(15),
                    //     Children = new Drawable[]
                    //     {
                    //         new FillFlowContainer
                    //         {
                    //             Anchor = Anchor.Centre,
                    //             Origin = Anchor.Centre,
                    //             AutoSizeAxes = Axes.Both,
                    //             Direction = FillDirection.Vertical,
                    //             Spacing = new Vector2(0, 4),
                    //             Children = new Drawable[]
                    //             {
                    //                 new OsuSpriteText
                    //                 {
                    //                     Anchor = Anchor.TopCentre,
                    //                     Origin = Anchor.TopCentre,
                    //                     Text = "Searching for a match...",
                    //                     Font = OsuFont.Style.Title,
                    //                 },
                    //                 new QueueTimerText
                    //                 {
                    //                     Anchor = Anchor.TopCentre,
                    //                     Origin = Anchor.TopCentre,
                    //                     Font = OsuFont.Style.Body,
                    //                 }
                    //             }
                    //         },
                    //         new LoadingSpinner
                    //         {
                    //             State = { Value = Visibility.Visible },
                    //         },
                    //         new ShearedButton
                    //         {
                    //             DarkerColour = colours.Red3,
                    //             LighterColour = colours.Red4,
                    //             Anchor = Anchor.Centre,
                    //             Origin = Anchor.Centre,
                    //             Width = 200,
                    //             Text = "Stop queueing",
                    //             Action = () => queue.LeaveQueue()
                    //         }
                    //     }
                    // };
                    //
                    // enqueueSample?.Play();
                    // startLoopPlaybackDelegate = Scheduler.AddDelayed(startWaitingLoopPlayback, 2000);
                    break;

                case MatchmakingScreenState.PendingAccept:
                    // client.MatchmakingAcceptInvitation().FireAndForget();
                    // SetState(MatchmakingScreenState.AcceptedWaitingForRoom);
                    //
                    // matchFoundSample?.Play();
                    // music.DuckMomentarily(1250);
                    break;

                case MatchmakingScreenState.AcceptedWaitingForRoom:
                    // mainContent.Child = new FillFlowContainer
                    // {
                    //     Anchor = Anchor.Centre,
                    //     Origin = Anchor.Centre,
                    //     AutoSizeAxes = Axes.Both,
                    //     Direction = FillDirection.Vertical,
                    //     Spacing = new Vector2(20),
                    //     Children = new Drawable[]
                    //     {
                    //         new OsuSpriteText
                    //         {
                    //             Anchor = Anchor.Centre,
                    //             Origin = Anchor.Centre,
                    //             Text = "Waiting for opponents...",
                    //             Font = OsuFont.GetFont(size: 32, weight: FontWeight.Light, typeface: Typeface.TorusAlternate),
                    //         },
                    //         new LoadingSpinner
                    //         {
                    //             State = { Value = Visibility.Visible },
                    //         },
                    //     }
                    // };
                    //
                    // startWaitingLoopPlayback();
                    break;

                case MatchmakingScreenState.InRoom:
                    // // room received, show users and transition to next screen.
                    // mainContent.Child = new FillFlowContainer
                    // {
                    //     Anchor = Anchor.Centre,
                    //     Origin = Anchor.Centre,
                    //     AutoSizeAxes = Axes.Both,
                    //     Direction = FillDirection.Vertical,
                    //     Spacing = new Vector2(20),
                    //     Children = new Drawable[]
                    //     {
                    //         new OsuSpriteText
                    //         {
                    //             Anchor = Anchor.Centre,
                    //             Origin = Anchor.Centre,
                    //             Text = "Good luck!",
                    //             Font = OsuFont.GetFont(size: 32, weight: FontWeight.Light, typeface: Typeface.TorusAlternate),
                    //         },
                    //     }
                    // };
                    //
                    // using (BeginDelayedSequence(2000))
                    // {
                    //     pushScreenDelegate = Schedule(() =>
                    //     {
                    //         if (client.Room == null)
                    //         {
                    //             Logger.Log("Room became null, returning to idle");
                    //             SetState(MatchmakingScreenState.Idle);
                    //             return;
                    //         }
                    //
                    //         switch (poolType)
                    //         {
                    //             case MatchmakingPoolType.QuickPlay:
                    //                 this.Push(new ScreenMatchmaking(client.Room));
                    //                 break;
                    //
                    //             case MatchmakingPoolType.RankedPlay:
                    //                 this.Push(new RankedPlayScreen(client.Room));
                    //                 break;
                    //         }
                    //     });
                    // }

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

        public partial class PanelBackground : CompositeDrawable
        {
            [Resolved]
            private OverlayColourProvider colourProvider { get; set; } = null!;

            [BackgroundDependencyLoader]
            private void load()
            {
                RelativeSizeAxes = Axes.Both;

                InternalChild = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colourProvider.Background3,
                    Blending = BlendingParameters.Additive,
                    Alpha = 0.3f,
                };
            }
        }

        public partial class QueueSectionHeader : SectionHeader
        {
            public QueueSectionHeader(string header)
                : base(header)
            {
                // Reduce base class padding.
                Margin = new MarginPadding { Top = 5, Bottom = 10, Horizontal = 5 };
            }
        }

        private partial class BeginQueueingButton : SelectionButton
        {
            public readonly IBindable<MatchmakingPool?> SelectedPool = new Bindable<MatchmakingPool?>();

            protected override void LoadComplete()
            {
                base.LoadComplete();

                SelectedPool.BindValueChanged(p => Enabled.Value = p.NewValue != null, true);
            }
        }

        private partial class SelectionButton : RoundedButton, IKeyBindingHandler<GlobalAction>
        {
            public bool OnPressed(KeyBindingPressEvent<GlobalAction> e)
            {
                if (e.Action == GlobalAction.Select && !e.Repeat)
                {
                    TriggerClick();
                    return true;
                }

                return false;
            }

            public void OnReleased(KeyBindingReleaseEvent<GlobalAction> e)
            {
            }
        }

        private partial class QueueTimerText : OsuSpriteText
        {
            [Resolved]
            private QueueController queue { get; set; } = null!;

            public QueueTimerText()
            {
                AlwaysPresent = true;
            }

            protected override void Update()
            {
                base.Update();

                Text = queue.QueueTimer.Elapsed.ToFormattedDuration();
            }
        }
    }
}
