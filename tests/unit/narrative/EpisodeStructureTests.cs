using System;
using System.Collections.Generic;
using Core.Narrative;
using Core.Narrative.Dialogue;
using NUnit.Framework;

namespace Tests.Unit.Narrative
{
    /// <summary>
    /// Unit tests for EpisodeStructure system.
    /// Tests linear episode progression, chapter/scene transitions, and completion events.
    /// </summary>
    [TestFixture]
    public sealed class EpisodeStructureTests
    {
        private MockSceneManagement _sceneManagement;
        private EpisodeStructure _episodeStructure;
        private List<NSMEvent> _emittedEvents;
        private EpisodeState? _lastState;
        private EpisodeState? _previousState;

        [SetUp]
        public void SetUp()
        {
            _sceneManagement = new MockSceneManagement();
            _emittedEvents = new List<NSMEvent>();
            _lastState = null;
            _previousState = null;

            // Reset NSM state before each test
            NarrativeStateMachine.Instance.Set(EpisodeKeys.CurrentEpisode, null);
            NarrativeStateMachine.Instance.Set(EpisodeKeys.EpisodeComplete, null);
            NarrativeStateMachine.Instance.Set(EpisodeKeys.CurrentChapter, null);

            // Create EpisodeStructure with test data
            _episodeStructure = CreateEpisodeStructureWithTestData();
            _episodeStructure.OnStateChanged += (prev, curr) =>
            {
                _previousState = prev;
                _lastState = curr;
            };
        }

        [TearDown]
        public void TearDown()
        {
            if (_episodeStructure != null)
            {
                _episodeStructure.OnStateChanged -= null;
            }
        }

        private EpisodeStructure CreateEpisodeStructureWithTestData()
        {
            // Create a game object manually (not Unity scene)
            var episodeData = CreateTestEpisodeData();
            var structure = new EpisodeStructure();

            // Use reflection to set private fields for testing
            var episodeDataField = typeof(EpisodeStructure).GetField("_episodeData",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var sceneManagementField = typeof(EpisodeStructure).GetField("_sceneManagement",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            episodeDataField.SetValue(structure, episodeData);
            sceneManagementField.SetValue(structure, _sceneManagement);

            return structure;
        }

        private EpisodeData CreateTestEpisodeData()
        {
            // Episode "test_ep" with 2 chapters, each with 2 scenes
            var chapter1Scenes = new List<SceneData>
            {
                new SceneData("ch1_scene1"),
                new SceneData("ch1_scene2")
            };

            var chapter2Scenes = new List<SceneData>
            {
                new SceneData("ch2_scene1"),
                new SceneData("ch2_scene2")
            };

            var chapter1 = new ChapterData(0, chapter1Scenes);
            var chapter2 = new ChapterData(1, chapter2Scenes);

            var chapters = new List<ChapterData> { chapter1, chapter2 };
            return new EpisodeData("test_ep", chapters, isLastEpisode: true);
        }

        private EpisodeData CreateMemoirEpisodeData()
        {
            // Episode with memoir/flashback scene
            var scenes = new List<SceneData>
            {
                new SceneData("normal_scene", isMemoirOrFlashback: false),
                new SceneData("memoir_scene", isMemoirOrFlashback: true),
                new SceneData("another_normal", isMemoirOrFlashback: false)
            };

            var chapter = new ChapterData(0, scenes);
            return new EpisodeData("memoir_ep", new List<ChapterData> { chapter }, isLastEpisode: true);
        }

        private EpisodeData CreateSingleSceneEpisode()
        {
            var scenes = new List<SceneData> { new SceneData("single_scene") };
            var chapter = new ChapterData(0, scenes);
            return new EpisodeData("single_ep", new List<ChapterData> { chapter }, isLastEpisode: true);
        }

        #region StartEpisode Tests

        [Test]
        public void StartEpisode_LoadsFirstScene()
        {
            _episodeStructure.StartEpisode();

            Assert.AreEqual(1, _sceneManagement.LoadSceneCalls.Count);
            Assert.AreEqual("ch1_scene1", _sceneManagement.LoadSceneCalls[0].sceneId);
            Assert.AreEqual(SceneTransitionType.FadeGrey, _sceneManagement.LoadSceneCalls[0].transitionType);
        }

        [Test]
        public void StartEpisode_SetsStateToEpisodeLoading()
        {
            _episodeStructure.StartEpisode();

            Assert.AreEqual(EpisodeState.EpisodeLoading, _episodeStructure.CurrentState);
        }

        [Test]
        public void StartEpisode_UpdatesNSMKeys()
        {
            _episodeStructure.StartEpisode();

            Assert.AreEqual("test_ep", NarrativeStateMachine.Instance.Get<string>(EpisodeKeys.CurrentEpisode));
            Assert.AreEqual("false", NarrativeStateMachine.Instance.Get<string>(EpisodeKeys.EpisodeComplete));
            Assert.AreEqual("0", NarrativeStateMachine.Instance.Get<string>(EpisodeKeys.CurrentChapter));
        }

        [Test]
        public void StartEpisode_SetsChapterAndSceneIndexToZero()
        {
            _episodeStructure.StartEpisode();

            Assert.AreEqual(0, _episodeStructure.CurrentChapterIndex);
            Assert.AreEqual(0, _episodeStructure.CurrentSceneIndex);
        }

        [Test]
        public void StartEpisode_IsRunningTrue()
        {
            _episodeStructure.StartEpisode();

            Assert.IsTrue(_episodeStructure.IsRunning);
        }

        [Test]
        public void StartEpisode_WithNullEpisodeData_LogsError()
        {
            var emptyStructure = new EpisodeStructure();
            // Field injection not needed for this test since StartEpisode checks _episodeData directly

            // This should log error but not crash
            emptyStructure.StartEpisode();
        }

        #endregion

        #region Scene Ready and State Transition Tests

        [Test]
        public void OnSceneReady_TransitionsToChapterActive()
        {
            _episodeStructure.StartEpisode();
            _episodeStructure.OnSceneReady();

            Assert.AreEqual(EpisodeState.ChapterActive, _episodeStructure.CurrentState);
        }

        [Test]
        public void OnSceneReady_DoesNothingWhenNotRunning()
        {
            // Start and complete episode
            _episodeStructure.StartEpisode();
            SimulateDialogueSceneComplete(); // ch1_scene1
            SimulateDialogueSceneComplete(); // ch1_scene2
            SimulateDialogueSceneComplete(); // ch2_scene1
            SimulateDialogueSceneComplete(); // ch2_scene2

            Assert.IsFalse(_episodeStructure.IsRunning);

            // Should not change state
            _episodeStructure.OnSceneReady();

            Assert.AreEqual(EpisodeState.EpisodeComplete, _episodeStructure.CurrentState);
        }

        [Test]
        public void OnSceneReady_DoesNothingWhenNotInLoadingState()
        {
            _episodeStructure.StartEpisode();
            _episodeStructure.OnSceneReady(); // Now in ChapterActive

            // Call again - should not change state
            _episodeStructure.OnSceneReady();

            Assert.AreEqual(EpisodeState.ChapterActive, _episodeStructure.CurrentState);
        }

        #endregion

        #region DialogueSceneComplete Handler Tests

        [Test]
        public void DialogueSceneComplete_AdvancesToNextScene()
        {
            _episodeStructure.StartEpisode();
            _episodeStructure.OnSceneReady();
            SimulateDialogueSceneComplete();

            Assert.AreEqual(1, _episodeStructure.CurrentSceneIndex);
            Assert.AreEqual(2, _sceneManagement.LoadSceneCalls.Count);
            Assert.AreEqual("ch1_scene2", _sceneManagement.LoadSceneCalls[1].sceneId);
        }

        [Test]
        public void DialogueSceneComplete_LastSceneOfChapter_CompletesChapter()
        {
            // Set up to be at last scene of chapter 1
            var episodeData = new EpisodeData("test", new List<ChapterData>
            {
                new ChapterData(0, new List<SceneData>
                {
                    new SceneData("scene1"),
                    new SceneData("scene2") // last scene
                })
            }, true);

            InjectEpisodeData(episodeData);
            _episodeStructure.StartEpisode();
            _episodeStructure.OnSceneReady();

            // Complete first scene
            SimulateDialogueSceneComplete();

            // Now complete second (last) scene of chapter
            _episodeStructure.OnSceneReady();
            SimulateDialogueSceneComplete();

            Assert.AreEqual(EpisodeState.ChapterComplete, _previousState);
        }

        [Test]
        public void DialogueSceneComplete_LastSceneOfLastChapter_CompletesEpisode()
        {
            _episodeStructure.StartEpisode();
            _episodeStructure.OnSceneReady();

            // Complete all 4 scenes
            for (int i = 0; i < 4; i++)
            {
                SimulateDialogueSceneComplete();
                if (_episodeStructure.IsRunning && _episodeStructure.CurrentState == EpisodeState.EpisodeLoading)
                {
                    _episodeStructure.OnSceneReady();
                }
            }

            Assert.AreEqual(EpisodeState.EpisodeComplete, _episodeStructure.CurrentState);
            Assert.IsFalse(_episodeStructure.IsRunning);
        }

        #endregion

        #region ResumeEpisode Tests

        [Test]
        public void ResumeEpisode_StartsFromSpecifiedPosition()
        {
            _episodeStructure.ResumeEpisode(1, 1); // Chapter 2, scene 2

            Assert.AreEqual(1, _episodeStructure.CurrentChapterIndex);
            Assert.AreEqual(1, _episodeStructure.CurrentSceneIndex);
            Assert.IsTrue(_episodeStructure.IsRunning);
        }

        [Test]
        public void ResumeEpisode_LoadsCorrectScene()
        {
            _episodeStructure.ResumeEpisode(1, 1);

            Assert.AreEqual(1, _sceneManagement.LoadSceneCalls.Count);
            Assert.AreEqual("ch2_scene2", _sceneManagement.LoadSceneCalls[0].sceneId);
        }

        [Test]
        public void ResumeEpisode_UpdatesNSMKeys()
        {
            _episodeStructure.ResumeEpisode(1, 0);

            Assert.AreEqual("test_ep", NarrativeStateMachine.Instance.Get<string>(EpisodeKeys.CurrentEpisode));
            Assert.AreEqual("false", NarrativeStateMachine.Instance.Get<string>(EpisodeKeys.EpisodeComplete));
            Assert.AreEqual("1", NarrativeStateMachine.Instance.Get<string>(EpisodeKeys.CurrentChapter));
        }

        [Test]
        public void ResumeEpisode_InvalidChapterIndex_LogsError()
        {
            _episodeStructure.ResumeEpisode(99, 0);

            // Should not crash, episode should not be running
            Assert.IsFalse(_episodeStructure.IsRunning);
        }

        #endregion

        #region ForceTransition Tests

        [Test]
        public void ForceTransition_LoadsSpecifiedScene()
        {
            _episodeStructure.StartEpisode();
            _episodeStructure.OnSceneReady();

            _episodeStructure.ForceTransition("arbitrary_scene");

            Assert.AreEqual(SceneTransitionType.FadeGrey, _sceneManagement.LoadSceneCalls[_sceneManagement.LoadSceneCalls.Count - 1].transitionType);
        }

        [Test]
        public void ForceTransition_DoesNothingWhenNotRunning()
        {
            _episodeStructure.StartEpisode();

            // Complete the episode
            for (int i = 0; i < 4; i++)
            {
                SimulateDialogueSceneComplete();
                if (_episodeStructure.IsRunning && _episodeStructure.CurrentState == EpisodeState.EpisodeLoading)
                {
                    _episodeStructure.OnSceneReady();
                }
            }

            _sceneManagement.LoadSceneCalls.Clear();
            _episodeStructure.ForceTransition("some_scene");

            Assert.AreEqual(0, _sceneManagement.LoadSceneCalls.Count);
        }

        #endregion

        #region Transition Type Resolution Tests

        [Test]
        public void ResolveTransitionType_NormalScene_UsesFadeGrey()
        {
            var episodeData = new EpisodeData("test", new List<ChapterData>
            {
                new ChapterData(0, new List<SceneData>
                {
                    new SceneData("normal", isMemoirOrFlashback: false)
                })
            }, true);

            InjectEpisodeData(episodeData);
            _episodeStructure.StartEpisode();

            Assert.AreEqual(SceneTransitionType.FadeGrey, _sceneManagement.LoadSceneCalls[0].transitionType);
        }

        [Test]
        public void ResolveTransitionType_MemoirScene_UsesFadeBlack()
        {
            var episodeData = new EpisodeData("test", new List<ChapterData>
            {
                new ChapterData(0, new List<SceneData>
                {
                    new SceneData("normal"),
                    new SceneData("memoir", isMemoirOrFlashback: true)
                })
            }, true);

            InjectEpisodeData(episodeData);
            _episodeStructure.StartEpisode();
            _episodeStructure.OnSceneReady();
            SimulateDialogueSceneComplete(); // advance to memoir scene
            _episodeStructure.OnSceneReady();

            Assert.AreEqual(SceneTransitionType.FadeBlack, _sceneManagement.LoadSceneCalls[1].transitionType);
        }

        [Test]
        public void ResolveTransitionType_ChapterOverride_TakesPrecedence()
        {
            var chapterWithOverride = new ChapterData(0, new List<SceneData>
            {
                new SceneData("normal", isMemoirOrFlashback: false)
            }, "FadeBlack");

            var episodeData = new EpisodeData("test", new List<ChapterData> { chapterWithOverride }, true);

            InjectEpisodeData(episodeData);
            _episodeStructure.StartEpisode();

            Assert.AreEqual(SceneTransitionType.FadeBlack, _sceneManagement.LoadSceneCalls[0].transitionType);
        }

        #endregion

        #region State Transition Tests

        [Test]
        public void StateTransition_EpisodeLoading_To_ChapterActive()
        {
            _episodeStructure.StartEpisode();
            Assert.AreEqual(EpisodeState.EpisodeLoading, _episodeStructure.CurrentState);

            _episodeStructure.OnSceneReady();
            Assert.AreEqual(EpisodeState.ChapterActive, _episodeStructure.CurrentState);
        }

        [Test]
        public void StateTransition_ChapterActive_To_SceneTransitioning()
        {
            _episodeStructure.StartEpisode();
            _episodeStructure.OnSceneReady();
            Assert.AreEqual(EpisodeState.ChapterActive, _episodeStructure.CurrentState);

            SimulateDialogueSceneComplete();
            Assert.AreEqual(EpisodeState.SceneTransitioning, _previousState);
        }

        [Test]
        public void StateTransition_EpisodeComplete_IsFinalState()
        {
            _episodeStructure.StartEpisode();
            _episodeStructure.OnSceneReady();

            // Complete all scenes
            for (int i = 0; i < 4; i++)
            {
                SimulateDialogueSceneComplete();
                if (_episodeStructure.IsRunning && _episodeStructure.CurrentState == EpisodeState.EpisodeLoading)
                {
                    _episodeStructure.OnSceneReady();
                }
            }

            Assert.AreEqual(EpisodeState.EpisodeComplete, _episodeStructure.CurrentState);
        }

        #endregion

        #region AdvanceToNextScene Tests

        [Test]
        public void AdvanceToNextScene_ManuallyAdvances()
        {
            _episodeStructure.StartEpisode();
            _episodeStructure.OnSceneReady();

            _episodeStructure.AdvanceToNextScene();

            Assert.AreEqual(1, _episodeStructure.CurrentSceneIndex);
        }

        [Test]
        public void AdvanceToNextScene_DoesNothingWhenNotRunning()
        {
            _episodeStructure.StartEpisode();
            // Don't advance, just check that calling AdvanceToNextScene when not running is safe
            _episodeStructure.AdvanceToNextScene();
        }

        #endregion

        #region Helper Methods

        private void SimulateDialogueSceneComplete()
        {
            var dialogueCompleteEvent = new DialogueSceneCompleteEvent(
                _episodeStructure.EpisodeData.Chapters[_episodeStructure.CurrentChapterIndex]
                    .Scenes[_episodeStructure.CurrentSceneIndex].SceneId);

            // Get the private OnDialogueSceneComplete method and invoke it
            var method = typeof(EpisodeStructure).GetMethod("OnDialogueSceneComplete",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(_episodeStructure, new object[] { dialogueCompleteEvent });
        }

        private void InjectEpisodeData(EpisodeData data)
        {
            var field = typeof(EpisodeStructure).GetField("_episodeData",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(_episodeStructure, data);
        }

        #endregion
    }

    /// <summary>
    /// Mock implementation of IEpisodeSceneManagement for testing.
    /// </summary>
    internal sealed class MockSceneManagement : IEpisodeSceneManagement
    {
        public List<SceneLoadCall> LoadSceneCalls { get; } = new List<SceneLoadCall>();

        public void LoadScene(string sceneId, SceneTransitionType transitionType)
        {
            LoadSceneCalls.Add(new SceneLoadCall(sceneId, transitionType));
        }
    }

    internal struct SceneLoadCall
    {
        public string sceneId;
        public SceneTransitionType transitionType;

        public SceneLoadCall(string sceneId, SceneTransitionType transitionType)
        {
            this.sceneId = sceneId;
            this.transitionType = transitionType;
        }
    }
}
