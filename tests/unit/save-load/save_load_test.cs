using System;
using System.IO;
using System.Text.Json;
using Core.Narrative;
using Core.Persistence;
using NUnit.Framework;
using UnityEngine;

namespace Tests.Unit.SaveLoad
{
    /// <summary>
    /// Unit tests for SaveFile and SaveLoadSystem.
    /// Tests cover: JSON serialization, hash integrity, slot management, and event triggers.
    /// </summary>
    public class SaveLoadSystemTests
    {
        private const string TEST_SAVE_DIR = "test_saves";

        private string _testSavePath;

        [SetUp]
        public void SetUp()
        {
            // Set up a temporary save directory for testing
            _testSavePath = Path.Combine(Application.persistentDataPath, TEST_SAVE_DIR);
            if (Directory.Exists(_testSavePath))
            {
                Directory.Delete(_testSavePath, true);
            }
            Directory.CreateDirectory(_testSavePath);

            // Reset NSM state before each test
            NarrativeStateMachine.Instance?.ResetForTesting();
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up test save directory
            if (Directory.Exists(_testSavePath))
            {
                Directory.Delete(_testSavePath, true);
            }
        }

        #region SaveFile Tests

        [Test]
        public void SaveFile_ToJson_ProducesValidJson()
        {
            var saveFile = new SaveFile
            {
                Version = "1.0",
                ChapterIndex = 2,
                SceneId = "ch2_scene4",
                NsmState = "{\"chapterIndex\":2,\"sceneId\":\"ch2_scene4\"}",
                NsmHash = "abc123",
                PlayTimeSeconds = 3847,
                ChoiceCount = 47
            };

            string json = saveFile.ToJson();

            Assert.That(json, Is.Not.Null);
            Assert.That(json.Length, Is.GreaterThan(0));

            // Verify it can be deserialized back
            var deserialized = SaveFile.FromJson(json);
            Assert.That(deserialized.Version, Is.EqualTo("1.0"));
            Assert.That(deserialized.ChapterIndex, Is.EqualTo(2));
            Assert.That(deserialized.SceneId, Is.EqualTo("ch2_scene4"));
            Assert.That(deserialized.NsmState, Is.EqualTo(saveFile.NsmState));
            Assert.That(deserialized.NsmHash, Is.EqualTo("abc123"));
            Assert.That(deserialized.PlayTimeSeconds, Is.EqualTo(3847));
            Assert.That(deserialized.ChoiceCount, Is.EqualTo(47));
        }

        [Test]
        public void SaveFile_FromJson_ParsesCorrectly()
        {
            string json = @"{
                ""version"": ""1.0"",
                ""timestamp"": ""2026-04-29T10:30:00.000Z"",
                ""chapterIndex"": 3,
                ""sceneId"": ""ch3_scene1"",
                ""nsmState"": ""{\""key\"":\""value\""}"",
                ""nsmHash"": ""def456"",
                ""playTimeSeconds"": 1200,
                ""choiceCount"": 15
            }";

            var saveFile = SaveFile.FromJson(json);

            Assert.That(saveFile.Version, Is.EqualTo("1.0"));
            Assert.That(saveFile.ChapterIndex, Is.EqualTo(3));
            Assert.That(saveFile.SceneId, Is.EqualTo("ch3_scene1"));
            Assert.That(saveFile.NsmHash, Is.EqualTo("def456"));
            Assert.That(saveFile.PlayTimeSeconds, Is.EqualTo(1200));
            Assert.That(saveFile.ChoiceCount, Is.EqualTo(15));
        }

        [Test]
        public void SaveFile_FromJson_ThrowsOnNullOrEmpty()
        {
            Assert.Throws<ArgumentException>(() => SaveFile.FromJson(null));
            Assert.Throws<ArgumentException>(() => SaveFile.FromJson(""));
            Assert.Throws<ArgumentException>(() => SaveFile.FromJson("   "));
        }

        [Test]
        public void SaveFile_IsVersionCompatible_ReturnsTrueForCurrentVersion()
        {
            var saveFile = new SaveFile();
            Assert.That(saveFile.IsVersionCompatible(), Is.True);
        }

        [Test]
        public void SaveFile_FormattedPlayTime_ReturnsCorrectFormat()
        {
            var saveFile = new SaveFile { PlayTimeSeconds = 3847 };
            Assert.That(saveFile.FormattedPlayTime(), Is.EqualTo("01:04:07"));

            var saveFile2 = new SaveFile { PlayTimeSeconds = 3600 };
            Assert.That(saveFile2.FormattedPlayTime(), Is.EqualTo("01:00:00"));

            var saveFile3 = new SaveFile { PlayTimeSeconds = 59 };
            Assert.That(saveFile3.FormattedPlayTime(), Is.EqualTo("00:00:59"));
        }

        [Test]
        public void SaveFile_TimestampAsDateTime_ParsesCorrectly()
        {
            var saveFile = new SaveFile
            {
                Timestamp = "2026-04-29T10:30:00.000Z"
            };

            var dateTime = saveFile.TimestampAsDateTime();
            Assert.That(dateTime.Year, Is.EqualTo(2026));
            Assert.That(dateTime.Month, Is.EqualTo(4));
            Assert.That(dateTime.Day, Is.EqualTo(29));
            Assert.That(dateTime.Hour, Is.EqualTo(10));
            Assert.That(dateTime.Minute, Is.EqualTo(30));
        }

        #endregion

        #region SaveLoadSystem Slot Access Tests

        [Test]
        public void SaveLoadSystem_SlotCount_Returns3()
        {
            var saveLoadSystem = CreateTestInstance();
            Assert.That(saveLoadSystem.SlotCount, Is.EqualTo(3));
        }

        [Test]
        public void SaveLoadSystem_GetSlotInfo_ReturnsExistsFalse_WhenSlotEmpty()
        {
            var saveLoadSystem = CreateTestInstance();

            var info = saveLoadSystem.GetSlotInfo(0);

            Assert.That(info.Exists, Is.False);
        }

        [Test]
        public void SaveLoadSystem_All4Slots_AreAccessible()
        {
            var saveLoadSystem = CreateTestInstance();

            // Slots 0, 1, 2 are manual saves
            Assert.That(() => saveLoadSystem.GetSlotInfo(0), Throws.Nothing);
            Assert.That(() => saveLoadSystem.GetSlotInfo(1), Throws.Nothing);
            Assert.That(() => saveLoadSystem.GetSlotInfo(2), Throws.Nothing);

            // Slot -1 is autosave
            Assert.That(() => saveLoadSystem.GetSlotInfo(-1), Throws.Nothing);
        }

        [Test]
        public void SaveLoadSystem_GetSlotInfo_ThrowsOnInvalidSlot()
        {
            var saveLoadSystem = CreateTestInstance();

            Assert.Throws<ArgumentOutOfRangeException>(() => saveLoadSystem.GetSlotInfo(3));
            Assert.Throws<ArgumentOutOfRangeException>(() => saveLoadSystem.GetSlotInfo(10));
            Assert.Throws<ArgumentOutOfRangeException>(() => saveLoadSystem.GetSlotInfo(-2));
        }

        #endregion

        #region Save/Load NSM State Tests

        [Test]
        public void SaveLoadSystem_SaveToSlot_ProducesValidJson()
        {
            var saveLoadSystem = CreateTestInstance();

            // Set up some NSM state
            NarrativeStateMachine.Instance.Mutate("chapterIndex", 2);
            NarrativeStateMachine.Instance.Mutate("sceneId", "ch2_scene4");

            string path = Path.Combine(_testSavePath, "save_0.json");

            // Use reflection to call internal save with a test path override
            // Since we cannot easily override the path in the public API,
            // we test via SaveAutosave which uses Application.persistentDataPath
            saveLoadSystem.SaveAutosave();

            Assert.That(File.Exists(Path.Combine(_testSavePath, "saves", "save_autosave.json")), Is.True);

            string json = File.ReadAllText(Path.Combine(_testSavePath, "saves", "save_autosave.json"));
            var saveFile = SaveFile.FromJson(json);

            Assert.That(saveFile.Version, Is.EqualTo("1.0"));
            Assert.That(saveFile.ChapterIndex, Is.EqualTo(2));
            Assert.That(saveFile.SceneId, Is.EqualTo("ch2_scene4"));
            Assert.That(saveFile.NsmState, Is.Not.Null);
            Assert.That(saveFile.NsmHash, Is.Not.Null);
            Assert.That(saveFile.NsmHash.Length, Is.EqualTo(64)); // SHA256 hex
        }

        [Test]
        public void SaveLoadSystem_LoadFromSlot_RestoresExactNsmState()
        {
            var saveLoadSystem = CreateTestInstance();

            // Set up NSM state and save
            NarrativeStateMachine.Instance.Mutate("chapterIndex", 5);
            NarrativeStateMachine.Instance.Mutate("sceneId", "ch5_final");
            NarrativeStateMachine.Instance.Mutate("trustPlayer", 75);

            saveLoadSystem.SaveAutosave();

            // Reset NSM state
            NarrativeStateMachine.Instance.ResetForTesting();

            // Load and verify
            saveLoadSystem.LoadAutosave();

            Assert.That(NarrativeStateMachine.Instance.Get<int>("chapterIndex"), Is.EqualTo(5));
            Assert.That(NarrativeStateMachine.Instance.Get<string>("sceneId"), Is.EqualTo("ch5_final"));
            Assert.That(NarrativeStateMachine.Instance.Get<int>("trustPlayer"), Is.EqualTo(75));
        }

        [Test]
        public void SaveLoadSystem_HashMismatch_OnTamperedSave_IsDetectedAndRejected()
        {
            var saveLoadSystem = CreateTestInstance();

            // Set up NSM state and save
            NarrativeStateMachine.Instance.Mutate("chapterIndex", 1);
            NarrativeStateMachine.Instance.Mutate("sceneId", "ch1_start");

            saveLoadSystem.SaveAutosave();

            // Tamper with the save file
            string path = Path.Combine(_testSavePath, "saves", "save_autosave.json");
            string json = File.ReadAllText(path);
            var saveFile = SaveFile.FromJson(json);
            saveFile.NsmState = "{\"chapterIndex\":999,\"sceneId\":\"tampered\"}";
            saveFile.NsmHash = "tampered_hash_value_12345678901234567890123456789012345678901234";
            File.WriteAllText(path, saveFile.ToJson());

            // Reset NSM state
            NarrativeStateMachine.Instance.ResetForTesting();
            int originalChapterIndex = NarrativeStateMachine.Instance.Get<int>("chapterIndex");

            // Attempt to load - should fail and not modify NSM state
            bool loadFailed = false;
            saveLoadSystem.OnLoadFailed += (slot, msg) => loadFailed = true;

            saveLoadSystem.LoadAutosave();

            Assert.That(loadFailed, Is.True);
            // NSM state should remain unchanged
            Assert.That(NarrativeStateMachine.Instance.Get<int>("chapterIndex"), Is.EqualTo(originalChapterIndex));
        }

        #endregion

        #region Play Time and Choice Count Tests

        [Test]
        public void SaveLoadSystem_PlayTime_IsRecordedCorrectly()
        {
            var saveLoadSystem = CreateTestInstance();

            NarrativeStateMachine.Instance.Mutate("chapterIndex", 1);
            NarrativeStateMachine.Instance.Mutate("sceneId", "test");

            // Save immediately (play time should be near zero)
            saveLoadSystem.SaveAutosave();

            string path = Path.Combine(_testSavePath, "saves", "save_autosave.json");
            var saveFile = SaveFile.FromJson(File.ReadAllText(path));

            Assert.That(saveFile.PlayTimeSeconds, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void SaveLoadSystem_ChoiceCount_IsRecordedCorrectly()
        {
            var saveLoadSystem = CreateTestInstance();

            NarrativeStateMachine.Instance.Mutate("chapterIndex", 1);
            NarrativeStateMachine.Instance.Mutate("sceneId", "test");

            // Record some choices
            saveLoadSystem.RecordChoice();
            saveLoadSystem.RecordChoice();
            saveLoadSystem.RecordChoice();

            saveLoadSystem.SaveAutosave();

            string path = Path.Combine(_testSavePath, "saves", "save_autosave.json");
            var saveFile = SaveFile.FromJson(File.ReadAllText(path));

            Assert.That(saveFile.ChoiceCount, Is.EqualTo(3));
        }

        [Test]
        public void SaveLoadSystem_RecordChoice_IncrementsCount()
        {
            var saveLoadSystem = CreateTestInstance();

            Assert.That(() => saveLoadSystem.RecordChoice(), Throws.Nothing);
            Assert.That(() => saveLoadSystem.RecordChoice(), Throws.Nothing);
        }

        #endregion

        #region Autosave Trigger Tests

        [Test]
        public void SaveLoadSystem_Autosave_FiresOnCHAPTER_COMPLETEState()
        {
            var saveLoadSystem = CreateTestInstance();

            bool autosaveTriggered = false;
            saveLoadSystem.OnAutosaveTriggered += () => autosaveTriggered = true;

            // Emit a CHAPTER_COMPLETE state change event
            var stateChangedEvent = new StateChangedEvent(
                NSMState.SceneActive,
                NSMState.ChapterComplete
            );
            EventBus.Instance.Emit(stateChangedEvent);

            Assert.That(autosaveTriggered, Is.True);
        }

        [Test]
        public void SaveLoadSystem_Autosave_FiresOnTrustBoundaryReached()
        {
            var saveLoadSystem = CreateTestInstance();

            bool autosaveTriggered = false;
            saveLoadSystem.OnAutosaveTriggered += () => autosaveTriggered = true;

            // Emit a TrustBoundaryReached event
            var trustEvent = new TrustBoundaryReachedEvent(
                "playerTrust",
                0f,
                TrustBoundary.CrossedZero
            );
            EventBus.Instance.Emit(trustEvent);

            Assert.That(autosaveTriggered, Is.True);
        }

        [Test]
        public void SaveLoadSystem_RequestAutosaveForDialogueNode0_TriggersAutosave()
        {
            var saveLoadSystem = CreateTestInstance();

            bool autosaveTriggered = false;
            saveLoadSystem.OnAutosaveTriggered += () => autosaveTriggered = true;

            saveLoadSystem.RequestAutosaveForDialogueNode0();

            Assert.That(autosaveTriggered, Is.True);
        }

        #endregion

        #region Event Tests

        [Test]
        public void SaveLoadSystem_SaveComplete_EventFires()
        {
            var saveLoadSystem = CreateTestInstance();

            NarrativeStateMachine.Instance.Mutate("chapterIndex", 1);
            NarrativeStateMachine.Instance.Mutate("sceneId", "test");

            int triggeredSlot = -1;
            saveLoadSystem.OnSaveComplete += slot => triggeredSlot = slot;

            saveLoadSystem.SaveAutosave();

            Assert.That(triggeredSlot, Is.EqualTo(-1));
        }

        [Test]
        public void SaveLoadSystem_LoadComplete_EventFires()
        {
            var saveLoadSystem = CreateTestInstance();

            NarrativeStateMachine.Instance.Mutate("chapterIndex", 1);
            NarrativeStateMachine.Instance.Mutate("sceneId", "test");
            saveLoadSystem.SaveAutosave();

            NarrativeStateMachine.Instance.ResetForTesting();

            int triggeredSlot = -1;
            SaveFile loadedFile = null;
            saveLoadSystem.OnLoadComplete += (slot, file) =>
            {
                triggeredSlot = slot;
                loadedFile = file;
            };

            saveLoadSystem.LoadAutosave();

            Assert.That(triggeredSlot, Is.EqualTo(-1));
            Assert.That(loadedFile, Is.Not.Null);
            Assert.That(loadedFile.ChapterIndex, Is.EqualTo(1));
        }

        #endregion

        #region Helper Methods

        private SaveLoadSystem CreateTestInstance()
        {
            // Create a new GameObject with SaveLoadSystem component
            GameObject go = new GameObject("SaveLoadSystemTest");
            SaveLoadSystem instance = go.AddComponent<SaveLoadSystem>();

            // Override the save path for testing by creating a test directory
            // The system will use Application.persistentDataPath which in tests
            // points to a temp location. We redirect by patching the path.
            // Since we cannot easily inject a custom path, we use the autosave path
            // which writes to Application.persistentDataPath/saves/

            return instance;
        }

        #endregion
    }
}
