// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Extensions;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Multiplayer;
using osu.Game.Online.Multiplayer.MatchTypes.RankedPlay;
using osu.Game.Online.Rooms;
using osu.Game.Overlays;
using osu.Game.Rulesets.Osu;
using osu.Game.Screens.OnlinePlay.Matchmaking.RankedPlay;
using osu.Game.Tests.Visual.Multiplayer;

namespace osu.Game.Tests.Visual.RankedPlay
{
    public partial class TestSceneBeatmapTagPrompt : MultiplayerTestScene
    {
        private DummyAPIAccess dummyAPI => (DummyAPIAccess)API;

        private APIBeatmapSet beatmapSet = null!;

        private RankedPlayScreen screen = null!;

        private int writeRequestCount;

        [Cached]
        private readonly OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Aquamarine);

        [SetUpSteps]
        public override void SetUpSteps()
        {
            base.SetUpSteps();

            AddStep("set up network requests", () =>
            {
                writeRequestCount = 0;
                Func<APIRequest, bool>? defaultRequestHandler = ((DummyAPIAccess)API).HandleRequest;

                dummyAPI.HandleRequest = request =>
                {
                    switch (request)
                    {
                        case ListTagsRequest listTagsRequest:
                        {
                            Scheduler.AddDelayed(() => listTagsRequest.TriggerSuccess(new APITagCollection
                            {
                                Tags =
                                [
                                    new APITag
                                    {
                                        Id = 1, Name = "aim/aim control", Description = "Patterns with velocity or direction changes which strongly go against a player's natural movement pattern.",
                                    },
                                    new APITag { Id = 2, Name = "tap/bursts", Description = "Patterns requiring continuous movement and alternating, typically 9 notes or less.", },
                                    new APITag { Id = 3, Name = "skillset/alt", Description = "Colloquial term for maps which use rhythms that encourage the player to alternate notes. Typically distinct from burst or stream maps.", RulesetId = 0 },
                                    new APITag { Id = 4, Name = "skillset/gimmick", Description = "Distinct or obscure gameplay elements that cannot be categorised with common skillsets.", RulesetId = 0 },
                                    new APITag { Id = 5, Name = "skillset/jumps", Description = "Focuses heavily on jumps, i.e. circles spaced far apart that require the player to move towards, slow down to hit, then speed up to move towards the next object.", RulesetId = 0 },
                                    new APITag { Id = 6, Name = "skillset/precision", Description = "Colloquial term for maps which require fine, precise movement to aim correctly. Typically refers to maps with circle sizes above and including 6.", RulesetId = 0 },
                                    new APITag { Id = 7, Name = "skillset/reading", Description = "Tests a player's reading skill, i.e. patterns that obfuscate note order and/or timing.", RulesetId = 0 },
                                    new APITag { Id = 8, Name = "skillset/streams", Description = "Patterns requiring continuous note hits, typically more than 9 notes.", RulesetId = 0 },
                                    new APITag { Id = 9, Name = "skillset/tech", Description = "Tests uncommon skills.", RulesetId = 0 },
                                ]
                            }), 500);
                            return true;
                        }

                        case GetBeatmapSetRequest getBeatmapSetRequest:
                        {
                            beatmapSet.Beatmaps.Single().TopTags =
                            [
                                new APIBeatmapTag { TagId = 3, VoteCount = 4 },
                                new APIBeatmapTag { TagId = 2, VoteCount = 3 },
                                new APIBeatmapTag { TagId = 1, VoteCount = 2 },
                            ];
                            Scheduler.AddDelayed(() => getBeatmapSetRequest.TriggerSuccess(beatmapSet), 500);
                            return true;
                        }

                        case AddBeatmapTagRequest:
                        case RemoveBeatmapTagRequest:
                        {
                            writeRequestCount++;
                            Scheduler.AddDelayed(request.TriggerSuccess, 500);
                            return true;
                        }

                        default:
                            return defaultRequestHandler?.Invoke(request) ?? false;
                    }
                };
            });
        }

        [Test]
        public void TestBasic()
        {
            var beatmap = CreateWorkingBeatmap(new OsuRuleset().RulesetInfo);
            beatmapSet = CreateAPIBeatmapSet(beatmap.BeatmapInfo);
            Beatmap.Value = beatmap;

            AddStep("join room", () => JoinRoom(CreateDefaultRoom(MatchType.RankedPlay)));
            WaitForJoined();

            AddStep("add other user", () => MultiplayerClient.AddUser(new MultiplayerRoomUser(2)));

            AddStep("load screen", () => LoadScreen(screen = new RankedPlayScreen(MultiplayerClient.ClientRoom!)));
            AddUntilStep("screen loaded", () => screen.IsLoaded);

            AddStep("set results phase", () => MultiplayerClient.RankedPlayChangeStage(RankedPlayStage.Results, state =>
            {
                int losingPlayer = state.Users.Keys.First();

                foreach (var (id, userInfo) in state.Users)
                {
                    if (id == losingPlayer)
                    {
                        userInfo.DamageInfo = new RankedPlayDamageInfo
                        {
                            RawDamage = 123_456,
                            Damage = 123_456,
                            OldLife = 500_000,
                            NewLife = 500_000 - 123_456,
                            DirectDamage = 123_456,
                        };

                        userInfo.Life = 500_000 - 123_456;
                    }
                    else
                    {
                        userInfo.DamageInfo = new RankedPlayDamageInfo
                        {
                            RawDamage = 0,
                            Damage = 0,
                            OldLife = 1_000_000,
                            NewLife = 1_000_000,
                        };
                    }
                }
            }).WaitSafely());

            AddWaitStep("wait", 5);

            AddStep("set opponent pick phase", () => MultiplayerClient.RankedPlayChangeStage(RankedPlayStage.CardPlay, state => state.ActiveUserId = 2).WaitSafely());
        }
    }
}
