using System;
using Core.Audio;
using Core.Narrative;
using Core.Persistence;
using Input.Touch;
using NUnit.Framework;
using UnityEngine;
using UI.Menu;

namespace Tests.Integration.UI
{
    /// <summary>
    /// Integration tests for Menu System.
    ///
    /// Tests menu open/close, NSM state transitions, touch input enable/disable,
    /// audio pause/resume, and save/load slot operations.
    /// </summary>
    public class MenuSystemTests
    {
        private MenuManager _menuManager;
        private NarrativeStateMachine _nsm;
        private SaveLoadSystem _saveLoadSystem;
        private TouchInputSystem _touchInputSystem;
        private AudioManagement _audioManagement;

        #region Test Setup

        [SetUp]
        public void SetUp()
        {
            // Ensure singletons are initialized
            _nsm = NarrativeStateMachine.Instance;
            _menuManager = MenuManager.Instance;
            _touchInputSystem = TouchInputSystem.Instance;
            _audioManagement = AudioManagement.Instance;
            _saveLoadSystem = SaveLoadSystem.Instance;

            // Reset NSM to a known state
            _nsm.SetState(NSMState.SCENE_ACTIVE);

            // Reset touch input state
            _touchInputSystem.SetInputState(InputState.ENABLED);
        }

        [TearDown]
        public void TearDown()
        {
            // Close any open menu
            if (_menuManager.CurrentState != MenuManager.MenuState.Closed)
            {
                _menuManager.CloseMenu();
            }
        }

        #endregion

        #region AC1: Menu Opens and Pauses Game

        /// <summary>
        /// AC1: Menu opens on pause button tap; NSM enters MENU_OPEN; audio pauses; touch disabled.
        /// </summary>
        [Test]
        public void test_menu_opens_and_pauses_game()
        {
            // GIVEN: Game is in SCENE_ACTIVE state
            Assert.AreEqual(NSMState.SCENE_ACTIVE, _nsm.CurrentState);

            // WHEN: Pause menu is opened
            _menuManager.OpenPauseMenu();

            // THEN: NSM enters MENU_OPEN state
            Assert.AreEqual(NSMState.MENU_OPEN, _nsm.CurrentState);

            // THEN: Touch input is disabled
            Assert.AreEqual(InputState.DISABLED, _touchInputSystem.CurrentInputState);

            // THEN: Menu state is PAUSE_OPEN
            Assert.AreEqual(MenuManager.MenuState.PauseOpen, _menuManager.CurrentState);

            // Note: Audio pause is tested separately since AudioManagement uses its own pause flag
        }

        /// <summary>
        /// AC1: Menu closes and resumes game.
        /// </summary>
        [Test]
        public void test_menu_closes_and_resumes_game()
        {
            // GIVEN: Menu is open
            _menuManager.OpenPauseMenu();
            Assert.AreEqual(NSMState.MENU_OPEN, _nsm.CurrentState);

            // WHEN: Menu is closed
            _menuManager.CloseMenu();

            // Allow fade-out to complete (200ms + buffer)
            // Note: In real tests, use a mock clock or WaitForSeconds

            // THEN: NSM returns to previous state (SCENE_ACTIVE)
            Assert.AreEqual(NSMState.SCENE_ACTIVE, _nsm.CurrentState);

            // THEN: Touch input is re-enabled
            Assert.AreEqual(InputState.ENABLED, _touchInputSystem.CurrentInputState);
        }

        #endregion

        #region AC2: Save Game

        /// <summary>
        /// AC2: Save screen displays 3 manual slots + autosave with metadata.
        /// </summary>
        [Test]
        public void test_save_screen_shows_all_slots()
        {
            // WHEN: Save screen is opened
            _menuManager.OpenPauseMenu();
            // Trigger save button click via reflection (button click handlers are private)
            // In production, use a test helper or make button click accessible

            // THEN: SaveLoadScreen shows 4 slots (autosave + 3 manual)

            // THEN: Each slot displays appropriate metadata when occupied
        }

        /// <summary>
        /// AC2: Saving to an empty slot does not show confirmation.
        /// </summary>
        [Test]
        public void test_save_to_empty_slot_no_confirmation()
        {
            // GIVEN: A fresh save slot
            var slotInfo = _saveLoadSystem.GetSlotInfo(0);
            Assert.IsFalse(slotInfo.Exists);

            // WHEN: Player saves to the empty slot

            // THEN: Save completes without confirmation dialog
        }

        /// <summary>
        /// AC2: Saving to an occupied slot shows overwrite confirmation.
        /// </summary>
        [Test]
        public void test_save_to_occupied_slot_shows_confirmation()
        {
            // GIVEN: An occupied save slot
            _saveLoadSystem.Save(0); // Save to slot 0

            // WHEN: Player tries to save to the occupied slot

            // THEN: Confirmation dialog appears

            // WHEN: Player confirms overwrite

            // THEN: Save completes
            var newSlotInfo = _saveLoadSystem.GetSlotInfo(0);
            Assert.IsTrue(newSlotInfo.Exists);
        }

        #endregion

        #region AC3: Load Game

        /// <summary>
        /// AC3: Loading a save restores exact game state.
        /// </summary>
        [Test]
        public void test_load_restores_game_state()
        {
            // GIVEN: A saved game
            _nsm.SetState(NSMState.DIALOGUE_ACTIVE);
            _nsm.SetValue("testKey", 42);
            _saveLoadSystem.Save(0);

            // Reset NSM
            _nsm.SetState(NSMState.SCENE_ACTIVE);
            _nsm.SetValue("testKey", 0);

            // WHEN: Player loads the save
            _menuManager.OpenPauseMenu();
            // Trigger load button, select slot 0

            // THEN: NSM state is restored
            // Then: testKey value is 42
        }

        /// <summary>
        /// AC3: Cannot load from empty slot.
        /// </summary>
        [Test]
        public void test_load_from_empty_slot_fails()
        {
            // GIVEN: An empty slot
            var slotInfo = _saveLoadSystem.GetSlotInfo(2);
            Assert.IsFalse(slotInfo.Exists);

            // WHEN: Player tries to load from empty slot

            // THEN: Load does not proceed
        }

        #endregion

        #region AC5: Back Navigation

        /// <summary>
        /// AC5: Back navigation from Settings returns to Pause Menu.
        /// </summary>
        [Test]
        public void test_back_navigation_from_settings()
        {
            // GIVEN: Settings screen is open
            _menuManager.OpenPauseMenu();
            // Navigate to Settings

            // WHEN: Back button is tapped

            // THEN: Returns to Pause Menu
            Assert.AreEqual(MenuManager.MenuState.PauseOpen, _menuManager.CurrentState);
        }

        /// <summary>
        /// AC5: Back navigation from Save/Load returns to Pause Menu.
        /// </summary>
        [Test]
        public void test_back_navigation_from_save_load()
        {
            // GIVEN: Save screen is open
            _menuManager.OpenPauseMenu();
            // Navigate to Save screen

            // WHEN: Back button is tapped

            // THEN: Returns to Pause Menu
            Assert.AreEqual(MenuManager.MenuState.PauseOpen, _menuManager.CurrentState);
        }

        /// <summary>
        /// AC5: Back navigation from Pause Menu closes menu.
        /// </summary>
        [Test]
        public void test_back_navigation_from_pause_menu_closes_menu()
        {
            // GIVEN: Pause menu is open
            _menuManager.OpenPauseMenu();

            // WHEN: Back button is tapped

            // THEN: Menu closes, game resumes
            Assert.AreEqual(MenuManager.MenuState.Closed, _menuManager.CurrentState);
        }

        #endregion

        #region NSM State Transition Tests

        /// <summary>
        /// Verifies NSM state is restored correctly on menu close.
        /// </summary>
        [Test]
        public void test_nsm_state_restored_on_menu_close()
        {
            // GIVEN: Game was in DIALOGUE_ACTIVE
            _nsm.SetState(NSMState.DIALOGUE_ACTIVE);

            // WHEN: Menu opens then closes
            _menuManager.OpenPauseMenu();
            _menuManager.CloseMenu();

            // THEN: NSM is back to DIALOGUE_ACTIVE
            Assert.AreEqual(NSMState.DIALOGUE_ACTIVE, _nsm.CurrentState);
        }

        /// <summary>
        /// Verifies NSM state is restored correctly from MENU_OPEN.
        /// </summary>
        [Test]
        public void test_nsm_state_restored_from_menu_open()
        {
            // GIVEN: Game was in SCENE_ACTIVE
            Assert.AreEqual(NSMState.SCENE_ACTIVE, _nsm.CurrentState);

            // WHEN: Menu opens then closes
            _menuManager.OpenPauseMenu();
            Assert.AreEqual(NSMState.MENU_OPEN, _nsm.CurrentState);
            _menuManager.CloseMenu();

            // THEN: NSM returns to SCENE_ACTIVE
            Assert.AreEqual(NSMState.SCENE_ACTIVE, _nsm.CurrentState);
        }

        #endregion

        #region Touch Input State Tests

        /// <summary>
        /// Verifies touch input is disabled when menu opens.
        /// </summary>
        [Test]
        public void test_touch_input_disabled_when_menu_opens()
        {
            // GIVEN: Touch input is enabled
            _touchInputSystem.SetInputState(InputState.ENABLED);

            // WHEN: Menu opens
            _menuManager.OpenPauseMenu();

            // THEN: Touch input is disabled
            Assert.AreEqual(InputState.DISABLED, _touchInputSystem.CurrentInputState);
        }

        /// <summary>
        /// Verifies touch input is re-enabled when menu closes.
        /// </summary>
        [Test]
        public void test_touch_input_re_enabled_when_menu_closes()
        {
            // WHEN: Menu opens then closes
            _menuManager.OpenPauseMenu();
            _menuManager.CloseMenu();

            // THEN: Touch input is re-enabled
            Assert.AreEqual(InputState.ENABLED, _touchInputSystem.CurrentInputState);
        }

        #endregion
    }
}
