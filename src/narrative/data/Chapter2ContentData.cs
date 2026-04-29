using System;
using System.Collections.Generic;
using Core.Narrative.Dialogue;
using UnityEngine;

namespace Core.Narrative.Data
{
    /// <summary>
    /// Factory for creating Chapter 2 dialogue trees.
    /// Content data for Sprint 1 continuation.
    ///
    /// Narrative pillars:
    /// - Identity Is a Cage
    /// - No Choice Is Perfect
    /// - Trust Is the Most Fragile Currency
    /// </summary>
    public static class Chapter2ContentData
    {
        /// <summary>
        /// Creates the dialogue tree for Chapter 2, Scene 1: "The Warehouse".
        ///
        /// Setup: The player meets underground contact FENG at a warehouse.
        /// FENG tests the player's loyalty with increasingly dangerous requests.
        ///
        /// Flow:
        /// - 5 TEXT nodes
        /// - 2 CHOICE nodes with asymmetric trust shifts
        /// - Memory flag registration (warned_liu, shared_codes)
        /// - Clue registration (warehouse_location)
        ///
        /// Trust shifts:
        /// - Choice 1: A:+8 (underground), B:+3 (balanced), C:-6 (imperial)
        /// - Choice 2: A:+10 (underground), B:-4 (imperial), C:+5 (underground)
        ///
        /// Ending routes (8 possible paths):
        /// - High underground: underground >= 15 (path A+A: 8+10=18, path A+C: 8+5=13, path C+A: -6+10=4... no)
        /// - Balanced: underground >= 8 AND imperial >= 5
        /// - Low/broken trust: fallback
        /// </summary>
        public static DialogueTree CreateCh2Scene1()
        {
            var nodes = new Dictionary<string, DialogueNode>
            {
                // Node 1: Introduction - FENG greets the player at the warehouse
                ["intro"] = DialogueNode.Text(
                    "intro",
                    "dialogue_ch2_scene1_node1",
                    "context",
                    "feng"
                ),

                // Node 2: Context - FENG explains the situation and tests the player
                ["context"] = DialogueNode.Text(
                    "context",
                    "dialogue_ch2_scene1_node2",
                    "choice1",
                    "feng"
                ),

                // Node 3: First choice - How to respond to FENG's test
                ["choice1"] = DialogueNode.Choice(
                    "choice1",
                    "dialogue_ch2_scene1_node3",
                    new ChoiceData[]
                    {
                        new ChoiceData("dialogue_ch2_scene1_choice1a", "feng_response1", "warned_liu"),  // Comply fully
                        new ChoiceData("dialogue_ch2_scene1_choice1b", "feng_response1"),  // Partial compliance
                        new ChoiceData("dialogue_ch2_scene1_choice1c", "feng_response1")   // Refuse (imperial loyalist)
                    },
                    new float[] { +8f, +3f, -6f }, // A:+8 underground, B:+3 balanced, C:-6 imperial
                    "feng_response1",
                    "feng"
                ),

                // Node 4: FENG's reaction to the first choice
                ["feng_response1"] = DialogueNode.Text(
                    "feng_response1",
                    "dialogue_ch2_scene1_node4",
                    "clue_register",
                    "feng"
                ),

                // Node 5: Clue registration - player learns warehouse location
                ["clue_register"] = DialogueNode.Text(
                    "clue_register",
                    "dialogue_ch2_scene1_node5",
                    "choice2",
                    "feng",
                    registerClue: "warehouse_location",
                    clueCategory: "locations"
                ),

                // Node 6: Second choice - How to handle the exchange
                ["choice2"] = DialogueNode.Choice(
                    "choice2",
                    "dialogue_ch2_scene1_node6",
                    new ChoiceData[]
                    {
                        new ChoiceData("dialogue_ch2_scene1_choice2a", "feng_response2", "shared_codes"),  // Share codes
                        new ChoiceData("dialogue_ch2_scene1_choice2b", "feng_response2"),  // Refuse to share
                        new ChoiceData("dialogue_ch2_scene1_choice2c", "feng_response2")   // Report to Yamamoto
                    },
                    new float[] { +10f, -4f, +5f }, // A:+10 underground, B:-4 imperial, C:+5 underground (double agent)
                    "feng_response2",
                    "feng"
                ),

                // Node 7: FENG's final reaction
                ["feng_response2"] = DialogueNode.Text(
                    "feng_response2",
                    "dialogue_ch2_scene1_node7",
                    "check_ending",
                    "feng"
                ),

                // Node 8: Condition check for ending routing
                ["check_ending"] = DialogueNode.Condition(
                    "check_ending",
                    "trust.underground >= 15",
                    "ending_high_underground",
                    "check_ending_b"
                ),

                // Node 9: Check for balanced ending
                ["check_ending_b"] = DialogueNode.Condition(
                    "check_ending_b",
                    "trust.underground >= 8 AND trust.imperial >= 5",
                    "ending_balanced",
                    "ending_low_underground"
                ),

                // Node 10: Ending A - High underground trust
                ["ending_high_underground"] = DialogueNode.Text(
                    "ending_high_underground",
                    "dialogue_ch2_scene1_ending_high_underground",
                    "end_a",
                    "feng"
                ),

                // Node 11: Ending B - Balanced trust
                ["ending_balanced"] = DialogueNode.Text(
                    "ending_balanced",
                    "dialogue_ch2_scene1_ending_balanced",
                    "end_b",
                    "feng"
                ),

                // Node 12: Ending C - Low/broken underground trust
                ["ending_low_underground"] = DialogueNode.Text(
                    "ending_low_underground",
                    "dialogue_ch2_scene1_ending_low_underground",
                    "end_c",
                    "feng"
                ),

                // End nodes
                ["end_a"] = DialogueNode.End("end_a"),
                ["end_b"] = DialogueNode.End("end_b"),
                ["end_c"] = DialogueNode.End("end_c")
            };

            return DialogueTree.CreateRuntime("ch2_scene1", nodes);
        }

        /// <summary>
        /// Creates the dialogue tree for Chapter 2, Scene 2: "The Interrogation".
        ///
        /// Setup: LIU has been captured by imperial forces. The player must decide
        /// whether to help her escape or condemn her. A CONDITION node gates the
        /// rescue option based on whether the player previously warned LIU.
        ///
        /// Flow:
        /// - 5 TEXT nodes
        /// - 1 CHOICE node with asymmetric trust shifts
        /// - 1 CONDITION node gating the rescue option
        /// - Memory flag check (warned_liu)
        ///
        /// Trust shifts:
        /// - Choice 1: A:+10 (underground), B:-8 (imperial), C:-3 (imperial)
        /// - Conditional choice: If warned_liu is set, player can choose D:+15 (underground)
        ///
        /// Note: The rescue choice (D) is only available if the player warned LIU in Scene 1.
        /// </summary>
        public static DialogueTree CreateCh2Scene2()
        {
            var nodes = new Dictionary<string, DialogueNode>
            {
                // Node 1: Introduction - Player learns LIU has been captured
                ["intro"] = DialogueNode.Text(
                    "intro",
                    "dialogue_ch2_scene2_node1",
                    "context",
                    "yamamoto"
                ),

                // Node 2: Context - YAMAMOTO reveals the interrogation
                ["context"] = DialogueNode.Text(
                    "context",
                    "dialogue_ch2_scene2_node2",
                    "yamamoto_reveal",
                    "yamamoto"
                ),

                // Node 3: YAMAMOTO reveals what he knows
                ["yamamoto_reveal"] = DialogueNode.Text(
                    "yamamoto_reveal",
                    "dialogue_ch2_scene2_node3",
                    "choice1",
                    "yamamoto"
                ),

                // Node 4: Choice - What to do about LIU
                // Note: Choice D is conditionally available via the Condition node
                ["choice1"] = DialogueNode.Choice(
                    "choice1",
                    "dialogue_ch2_scene2_node4",
                    new ChoiceData[]
                    {
                        new ChoiceData("dialogue_ch2_scene2_choice1a", "yamamoto_response"),  // Condemn LIU
                        new ChoiceData("dialogue_ch2_scene2_choice1b", "yamamoto_response"),  // Stay silent
                        new ChoiceData("dialogue_ch2_scene2_choice1c", "yamamoto_response")   // Request leniency
                    },
                    new float[] { -8f, -3f, +2f }, // A:-8 (imperial), B:-3 (underground), C:+2 (balanced)
                    "yamamoto_response",
                    "yamamoto"
                ),

                // Node 5: YAMAMOTO's response and conditional reveal
                ["yamamoto_response"] = DialogueNode.Text(
                    "yamamoto_response",
                    "dialogue_ch2_scene2_node5",
                    "check_warned",
                    "yamamoto"
                ),

                // Node 6: CONDITION - Check if player warned LIU earlier
                // If warned_liu is set, player had foreknowledge and could have helped
                ["check_warned"] = DialogueNode.Condition(
                    "check_warned",
                    "memory.warned_liu == true",
                    "rescue_option",
                    "no_rescue_option"
                ),

                // Node 7: Rescue option available - player can try to free LIU
                ["rescue_option"] = DialogueNode.Choice(
                    "rescue_option",
                    "dialogue_ch2_scene2_node6",
                    new ChoiceData[]
                    {
                        new ChoiceData("dialogue_ch2_scene2_rescue", "liu_freed")  // Attempt rescue
                    },
                    new float[] { +15f }, // +15 underground trust for attempting rescue
                    "liu_freed",
                    "yamamoto"
                ),

                // Node 8: No rescue option - player cannot save LIU
                ["no_rescue_option"] = DialogueNode.Text(
                    "no_rescue_option",
                    "dialogue_ch2_scene2_node7",
                    "ending_captured",
                    "yamamoto"
                ),

                // Node 9: LIU is freed
                ["liu_freed"] = DialogueNode.Text(
                    "liu_freed",
                    "dialogue_ch2_scene2_node8",
                    "ending_freed",
                    null
                ),

                // Node 10: LIU is captured
                ["ending_captured"] = DialogueNode.Text(
                    "ending_captured",
                    "dialogue_ch2_scene2_ending_captured",
                    "end_captured",
                    "yamamoto"
                ),

                // Node 11: LIU is freed - ending
                ["ending_freed"] = DialogueNode.Text(
                    "ending_freed",
                    "dialogue_ch2_scene2_ending_freed",
                    "end_freed",
                    null
                ),

                // End nodes
                ["end_captured"] = DialogueNode.End("end_captured"),
                ["end_freed"] = DialogueNode.End("end_freed")
            };

            return DialogueTree.CreateRuntime("ch2_scene2", nodes);
        }

        /// <summary>
        /// Creates the dialogue tree for Chapter 2, Scene 3: "The Betrayal".
        ///
        /// Setup: The double game is exposed. Yamamoto confronts the player,
        /// having known all along. The player must make a final choice that
        /// determines their path forward.
        ///
        /// Flow:
        /// - 4 TEXT nodes
        /// - 1 CHOICE node with dramatic trust shifts
        /// - CONDITION nodes for ending routing
        /// - Multiple endings based on accumulated trust
        ///
        /// Trust shifts:
        /// - Choice 1: A:+12 (imperial), B:+10 (underground), C:+6 (defector)
        ///
        /// Ending routes (6 possible paths, 3 endings):
        /// - Ending A (imperial victory): imperial >= 20
        /// - Ending B (underground victory): underground >= 18
        /// - Ending C (defector/neutral): balanced
        /// </summary>
        public static DialogueTree CreateCh2Scene3()
        {
            var nodes = new Dictionary<string, DialogueNode>
            {
                // Node 1: Introduction - YAMAMOTO confronts the player
                ["intro"] = DialogueNode.Text(
                    "intro",
                    "dialogue_ch2_scene3_node1",
                    "yamamoto_accusation",
                    "yamamoto"
                ),

                // Node 2: YAMAMOTO reveals he knew about the double game
                ["yamamoto_accusation"] = DialogueNode.Text(
                    "yamamoto_accusation",
                    "dialogue_ch2_scene3_node2",
                    "choice1",
                    "yamamoto"
                ),

                // Node 3: Final choice - Who to betray
                ["choice1"] = DialogueNode.Choice(
                    "choice1",
                    "dialogue_ch2_scene3_node3",
                    new ChoiceData[]
                    {
                        new ChoiceData("dialogue_ch2_scene3_choice1a", "yamamoto_final"),  // Choose imperial
                        new ChoiceData("dialogue_ch2_scene3_choice1b", "yamamoto_final"),  // Choose underground
                        new ChoiceData("dialogue_ch2_scene3_choice1c", "yamamoto_final")   // Choose neither (defect)
                    },
                    new float[] { +12f, +10f, +6f }, // A:+12 imperial, B:+10 underground, C:+6 defector
                    "yamamoto_final",
                    "yamamoto"
                ),

                // Node 4: YAMAMOTO's final reaction
                ["yamamoto_final"] = DialogueNode.Text(
                    "yamamoto_final",
                    "dialogue_ch2_scene3_node4",
                    "check_ending",
                    "yamamoto"
                ),

                // Node 5: Condition check - imperial victory
                ["check_ending"] = DialogueNode.Condition(
                    "check_ending",
                    "trust.imperial >= 20",
                    "ending_imperial",
                    "check_ending_b"
                ),

                // Node 6: Condition check - underground victory
                ["check_ending_b"] = DialogueNode.Condition(
                    "check_ending_b",
                    "trust.underground >= 18",
                    "ending_underground",
                    "ending_defector"
                ),

                // Node 7: Ending A - Imperial victory
                ["ending_imperial"] = DialogueNode.Text(
                    "ending_imperial",
                    "dialogue_ch2_scene3_ending_imperial",
                    "end_imperial",
                    "yamamoto"
                ),

                // Node 8: Ending B - Underground victory
                ["ending_underground"] = DialogueNode.Text(
                    "ending_underground",
                    "dialogue_ch2_scene3_ending_underground",
                    "end_underground",
                    "yamamoto"
                ),

                // Node 9: Ending C - Defector/neutral
                ["ending_defector"] = DialogueNode.Text(
                    "ending_defector",
                    "dialogue_ch2_scene3_ending_defector",
                    "end_defector",
                    "yamamoto"
                ),

                // End nodes
                ["end_imperial"] = DialogueNode.End("end_imperial"),
                ["end_underground"] = DialogueNode.End("end_underground"),
                ["end_defector"] = DialogueNode.End("end_defector")
            };

            return DialogueTree.CreateRuntime("ch2_scene3", nodes);
        }
    }
}
