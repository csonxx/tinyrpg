using System;
using System.Collections.Generic;
using Core.Narrative.Dialogue;
using UnityEngine;

namespace Core.Narrative.Data
{
    /// <summary>
    /// Factory for creating Chapter 3 / Finale dialogue trees.
    /// Content data for Sprint 1 climax.
    ///
    /// Narrative pillars:
    /// - Identity Is a Cage
    /// - No Choice Is Perfect
    /// - Trust Is the Most Fragile Currency
    ///
    /// This chapter demonstrates the full trust economy climax with
    /// 4 distinct endings based on accumulated trust + memory flags.
    /// </summary>
    public static class Chapter3ContentData
    {
        /// <summary>
        /// Creates the dialogue tree for Chapter 3, Scene 1: "The Reckoning".
        ///
        /// Setup: All three key characters converge. The player must navigate
        /// a tense confrontation where past choices determine available options.
        ///
        /// Flow:
        /// - 5 TEXT nodes
        /// - 1 CHOICE node with significant trust shifts
        /// - Memory flags influence the conversation (warned_liu, shared_codes)
        /// - Sets up the finale with multiple paths
        ///
        /// Trust shifts:
        /// - Choice 1: A:+10 (imperial), B:+10 (underground), C:+5 (defector)
        /// </summary>
        public static DialogueTree CreateCh3Scene1()
        {
            var nodes = new Dictionary<string, DialogueNode>
            {
                // Node 1: Introduction - The confrontation begins
                ["intro"] = DialogueNode.Text(
                    "intro",
                    "dialogue_ch3_scene1_node1",
                    "yamamoto_speaks",
                    "yamamoto"
                ),

                // Node 2: YAMAMOTO speaks first
                ["yamamoto_speaks"] = DialogueNode.Text(
                    "yamamoto_speaks",
                    "dialogue_ch3_scene1_node2",
                    "liu_speaks",
                    "yamamoto"
                ),

                // Node 3: LIU speaks
                ["liu_speaks"] = DialogueNode.Text(
                    "liu_speaks",
                    "dialogue_ch3_scene1_node3",
                    "feng_speaks",
                    "liu"
                ),

                // Node 4: FENG speaks - references past choices
                ["feng_speaks"] = DialogueNode.Text(
                    "feng_speaks",
                    "dialogue_ch3_scene1_node4",
                    "choice1",
                    "feng"
                ),

                // Node 5: Critical choice - Who to trust at the climax
                ["choice1"] = DialogueNode.Choice(
                    "choice1",
                    "dialogue_ch3_scene1_node5",
                    new ChoiceData[]
                    {
                        new ChoiceData("dialogue_ch3_scene1_choice1a", "yamamoto_response"),  // Side with YAMAMOTO
                        new ChoiceData("dialogue_ch3_scene1_choice1b", "liu_response"),      // Side with LIU
                        new ChoiceData("dialogue_ch3_scene1_choice1c", "feng_response")      // Side with FENG
                    },
                    new float[] { +10f, +10f, +5f }, // A:+10 imperial, B:+10 underground, C:+5 defector
                    "yamamoto_response",
                    "yamamoto"
                ),

                // Node 6: YAMAMOTO's response
                ["yamamoto_response"] = DialogueNode.Text(
                    "yamamoto_response",
                    "dialogue_ch3_scene1_node6",
                    "transition_to_finale",
                    "yamamoto"
                ),

                // Node 7: LIU's response
                ["liu_response"] = DialogueNode.Text(
                    "liu_response",
                    "dialogue_ch3_scene1_node7",
                    "transition_to_finale",
                    "liu"
                ),

                // Node 8: FENG's response
                ["feng_response"] = DialogueNode.Text(
                    "feng_response",
                    "dialogue_ch3_scene1_node8",
                    "transition_to_finale",
                    "feng"
                ),

                // Node 9: Transition to finale
                ["transition_to_finale"] = DialogueNode.Text(
                    "transition_to_finale",
                    "dialogue_ch3_scene1_node9",
                    "end_scene1",
                    null
                ),

                // End node for Scene 1
                ["end_scene1"] = DialogueNode.End("end_scene1")
            };

            return DialogueTree.CreateRuntime("ch3_scene1", nodes);
        }

        /// <summary>
        /// Creates the dialogue tree for Chapter 3, Scene 2: "The Finale".
        ///
        /// Setup: The final confrontation. CONDITION nodes route to 4 different
        /// endings based on accumulated trust + relationship values + memory flags.
        ///
        /// Flow:
        /// - 3 TEXT nodes
        /// - 1 CHOICE node (final declaration)
        /// - Multiple CONDITION nodes for ending routing
        ///
        /// Trust shifts:
        /// - Choice 1: A:+15 (imperial), B:+15 (underground), C:+8 (defector)
        ///
        /// Ending routes (4 endings):
        /// - Ending A (Imperial Triumph): trust.imperial >= 25
        /// - Ending B (Underground Victory): trust.underground >= 25 AND memory.shared_codes == true
        /// - Ending C (Exile): trust.imperial < 10 AND trust.underground < 10
        /// - Ending D (Unexpected Alliance): trust.imperial >= 15 AND trust.underground >= 15 AND memory.warned_liu == true
        ///
        /// This demonstrates the full trust economy climax with meaningful
        /// consequences for every choice made throughout the narrative.
        /// </summary>
        public static DialogueTree CreateCh3Scene2()
        {
            var nodes = new Dictionary<string, DialogueNode>
            {
                // Node 1: Introduction - The final choice
                ["intro"] = DialogueNode.Text(
                    "intro",
                    "dialogue_ch3_scene2_node1",
                    "yamamoto_offers",
                    "yamamoto"
                ),

                // Node 2: YAMAMOTO makes a final offer
                ["yamamoto_offers"] = DialogueNode.Text(
                    "yamamoto_offers",
                    "dialogue_ch3_scene2_node2",
                    "liu_counteroffers",
                    "yamamoto"
                ),

                // Node 3: LIU counteroffers
                ["liu_counteroffers"] = DialogueNode.Text(
                    "liu_counteroffers",
                    "dialogue_ch3_scene2_node3",
                    "feng_waits",
                    "liu"
                ),

                // Node 4: FENG waits - observing
                ["feng_waits"] = DialogueNode.Text(
                    "feng_waits",
                    "dialogue_ch3_scene2_node4",
                    "choice1",
                    "feng"
                ),

                // Node 5: Final choice - The ultimate declaration
                ["choice1"] = DialogueNode.Choice(
                    "choice1",
                    "dialogue_ch3_scene2_node5",
                    new ChoiceData[]
                    {
                        new ChoiceData("dialogue_ch3_scene2_choice1a", "yamamoto_accepts"),  // Accept YAMAMOTO's offer
                        new ChoiceData("dialogue_ch3_scene2_choice1b", "liu_accepts"),      // Accept LIU's offer
                        new ChoiceData("dialogue_ch3_scene2_choice1c", "feng_accepts")      // Reject both, walk away
                    },
                    new float[] { +15f, +15f, +8f }, // A:+15 imperial, B:+15 underground, C:+8 defector
                    "yamamoto_accepts",
                    null
                ),

                // Node 6: YAMAMOTO accepts the player's choice
                ["yamamoto_accepts"] = DialogueNode.Text(
                    "yamamoto_accepts",
                    "dialogue_ch3_scene2_node6",
                    "check_ending",
                    "yamamoto"
                ),

                // Node 7: LIU accepts the player's choice
                ["liu_accepts"] = DialogueNode.Text(
                    "liu_accepts",
                    "dialogue_ch3_scene2_node7",
                    "check_ending",
                    "liu"
                ),

                // Node 8: FENG accepts the player's choice
                ["feng_accepts"] = DialogueNode.Text(
                    "feng_accepts",
                    "dialogue_ch3_scene2_node8",
                    "check_ending",
                    "feng"
                ),

                // Node 9: First condition check - Imperial Triumph
                // Requires high imperial trust accumulated throughout
                ["check_ending"] = DialogueNode.Condition(
                    "check_ending",
                    "trust.imperial >= 25",
                    "ending_imperial_triumph",
                    "check_ending_b"
                ),

                // Node 10: Second condition - Underground Victory
                // Requires high underground trust AND having shared codes (proved loyalty)
                ["check_ending_b"] = DialogueNode.Condition(
                    "check_ending_b",
                    "trust.underground >= 25 AND memory.shared_codes == true",
                    "ending_underground_victory",
                    "check_ending_c"
                ),

                // Node 11: Third condition - Unexpected Alliance
                // Requires balanced trust AND having warned LIU (proved integrity)
                ["check_ending_c"] = DialogueNode.Condition(
                    "check_ending_c",
                    "trust.imperial >= 15 AND trust.underground >= 15 AND memory.warned_liu == true",
                    "ending_unexpected_alliance",
                    "check_ending_d"
                ),

                // Node 12: Fourth condition - Exile (default)
                // Neither side trusts the player enough
                ["check_ending_d"] = DialogueNode.Condition(
                    "check_ending_d",
                    "trust.imperial < 10 AND trust.underground < 10",
                    "ending_exile",
                    "ending_pyrrhic"
                ),

                // Node 13: Ending A - Imperial Triumph
                ["ending_imperial_triumph"] = DialogueNode.Text(
                    "ending_imperial_triumph",
                    "dialogue_ch3_scene2_ending_imperial_triumph",
                    "end_a",
                    "yamamoto"
                ),

                // Node 14: Ending B - Underground Victory
                ["ending_underground_victory"] = DialogueNode.Text(
                    "ending_underground_victory",
                    "dialogue_ch3_scene2_ending_underground_victory",
                    "end_b",
                    "liu"
                ),

                // Node 15: Ending C - Unexpected Alliance
                ["ending_unexpected_alliance"] = DialogueNode.Text(
                    "ending_unexpected_alliance",
                    "dialogue_ch3_scene2_ending_unexpected_alliance",
                    "end_c",
                    "feng"
                ),

                // Node 16: Ending D - Exile
                ["ending_exile"] = DialogueNode.Text(
                    "ending_exile",
                    "dialogue_ch3_scene2_ending_exile",
                    "end_d",
                    null
                ),

                // Node 17: Ending E - Pyrrhic Victory (fallback)
                ["ending_pyrrhic"] = DialogueNode.Text(
                    "ending_pyrrhic",
                    "dialogue_ch3_scene2_ending_pyrrhic",
                    "end_e",
                    "yamamoto"
                ),

                // End nodes
                ["end_a"] = DialogueNode.End("end_a"),
                ["end_b"] = DialogueNode.End("end_b"),
                ["end_c"] = DialogueNode.End("end_c"),
                ["end_d"] = DialogueNode.End("end_d"),
                ["end_e"] = DialogueNode.End("end_e")
            };

            return DialogueTree.CreateRuntime("ch3_scene2", nodes);
        }
    }
}
