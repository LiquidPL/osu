// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Testing;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Screens.OnlinePlay.Matchmaking.Queue;
using osu.Game.Tests.Visual.Multiplayer;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Tests.Visual.Matchmaking
{
    public partial class TestSceneQueueUserBackground : MultiplayerTestScene
    {
        private ScreenQueue.UserBackground userBackground = null!;
        private QueueVisualisation queueVisualisation = null!;

        [SetUpSteps]
        public override void SetUpSteps()
        {
            base.SetUpSteps();

            AddStep("create", () =>
            {
                Child = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        userBackground = new ScreenQueue.UserBackground
                        {
                            State = { Value = Visibility.Visible },
                        },
                        new CircularContainer
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Size = new Vector2(100),
                            Masking = true,
                            Child = new Box { RelativeSizeAxes = Axes.Both, Colour = Color4.White },
                        },
                        queueVisualisation = new QueueVisualisation
                        {
                            RelativeSizeAxes = Axes.Both,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            SafeRadius = ScreenQueue.UserBackground.WIDTH / 2f,
                        }
                    },
                };

                queueVisualisation.OnLoadComplete += _ => queueVisualisation.Users = Enumerable.Range(1, 10).Select(i => new APIUser
                {
                    Id = i,
                    Username = $"User {i}",
                }).ToArray();
            });
        }

        [Test]
        public void TestBasic()
        {
            AddStep("set queue state to idle", () => QueueController.CurrentState.Value = ScreenQueue.MatchmakingScreenState.Idle);
            AddStep("set queue state to queueing", () => QueueController.CurrentState.Value = ScreenQueue.MatchmakingScreenState.Queueing);
        }
    }
}
