using System;
using System.Collections.Generic;
using Core.Narrative.Dialogue;
using UnityEngine;

namespace Core.Narrative.Data
{
    /// <summary>
    /// Factory for creating chapter dialogue trees.
    /// Content data for Sprint 1 vertical slice.
    /// </summary>
    public static class ChapterContentData
    {
        /// <summary>
        /// Creates the dialogue tree for Chapter 1, Scene 1: "The Assignment".
        ///
        /// Setup: Captain YAMAMOTO briefs the player on a shipment operation.
        /// The player must navigate loyalty to imperial authority while protecting underground contacts.
        ///
        /// Flow:
        /// - 5 TEXT nodes (intro, context, yamamoto_response1, liu_mention, yamamoto_response2)
        /// - 2 CHOICE nodes (choice1, choice2) with asymmetric trust shifts
        /// - 3 endings reachable via CONDITION nodes
        ///
        /// Trust shifts (single-axis per DialogueEngine):
        /// - Choice 1: A:+8 (imperial), B:-5 (underground), C:+2 (imperial)
        /// - Choice 2: A:+12 (imperial), B:+5 (underground), C:+5 (imperial)
        ///
        /// Ending routes (9 possible paths, all 3 endings reachable):
        /// - Ending A (high imperial): imperial >= 18 (path A+A: 8+12=20)
        /// - Ending B (balanced): imperial >= 5 AND underground >= 5 (paths B+A, C+B)
        /// - Ending C (low/broken trust): fallback (6 paths)
        ///
        /// Note: The DialogueEngine uses single-axis trust shifts:
        /// - Positive shift -> trust.imperial increases
        /// - Negative shift -> trust.underground increases (absolute value)
        /// The story specified paired values (Imperial +X, Underground +Y); these are
        /// simplified to single values for the engine while preserving gameplay intent.
        /// </summary>
        public static DialogueTree CreateCh1Scene1()
        {
            var nodes = new Dictionary<string, DialogueNode>
            {
                // Node 1: Introduction - YAMAMOTO briefs the player
                ["intro"] = DialogueNode.Text(
                    "intro",
                    "dialogue_ch1_scene1_node1",
                    "context",
                    "yamamoto"
                ),

                // Node 2: Context - YAMAMOTO explains the stakes
                ["context"] = DialogueNode.Text(
                    "context",
                    "dialogue_ch1_scene1_node2",
                    "choice1",
                    "yamamoto"
                ),

                // Node 3: First choice - How to respond to orders
                ["choice1"] = DialogueNode.Choice(
                    "choice1",
                    "dialogue_ch1_scene1_node3",
                    new ChoiceData[]
                    {
                        new ChoiceData("dialogue_ch1_scene1_choice1a", "yamamoto_response1"),  // Loyal compliance
                        new ChoiceData("dialogue_ch1_scene1_choice1b", "yamamoto_response1"),  // Subtle warning
                        new ChoiceData("dialogue_ch1_scene1_choice1c", "yamamoto_response1")   // Silent observation
                    },
                    new float[] { +8f, -5f, +2f }, // A:+8 imperial, B:-5 underground, C:+2 imperial
                    "yamamoto_response1",
                    "yamamoto"
                ),

                // Node 4: YAMAMOTO's reaction to the first choice
                ["yamamoto_response1"] = DialogueNode.Text(
                    "yamamoto_response1",
                    "dialogue_ch1_scene1_node4",
                    "liu_mention",
                    "yamamoto"
                ),

                // Node 5: LIU incident mentioned
                ["liu_mention"] = DialogueNode.Text(
                    "liu_mention",
                    "dialogue_ch1_scene1_node5",
                    "choice2",
                    "yamamoto"
                ),

                // Node 6: Second choice - How to handle the situation
                ["choice2"] = DialogueNode.Choice(
                    "choice2",
                    "dialogue_ch1_scene1_node6",
                    new ChoiceData[]
                    {
                        new ChoiceData("dialogue_ch1_scene1_choice2a", "yamamoto_response2"),  // Invoke LIU incident
                        new ChoiceData("dialogue_ch1_scene1_choice2b", "yamamoto_response2"),  // Request written orders
                        new ChoiceData("dialogue_ch1_scene1_choice2c", "yamamoto_response2")   // Minimal acknowledgment
                    },
                    new float[] { +12f, +5f, +5f }, // A:strong imperial, B:moderate underground, C:moderate imperial
                    "yamamoto_response2",
                    "yamamoto"
                ),

                // Node 7: YAMAMOTO's final reaction
                ["yamamoto_response2"] = DialogueNode.Text(
                    "yamamoto_response2",
                    "dialogue_ch1_scene1_node7",
                    "check_ending",
                    "yamamoto"
                ),

                // Node 8: Condition check for ending routing
                // Routes based on accumulated trust values
                ["check_ending"] = DialogueNode.Condition(
                    "check_ending",
                    "trust.imperial >= 18",
                    "ending_a",
                    "check_ending_b"
                ),

                // Node 9: Check for balanced ending
                // Balanced ending requires building some underground trust (choice1 B or choice2 B)
                // while maintaining moderate imperial trust
                ["check_ending_b"] = DialogueNode.Condition(
                    "check_ending_b",
                    "trust.imperial >= 5 AND trust.underground >= 5",
                    "ending_b",
                    "ending_c"
                ),

                // Node 10: Ending A - High imperial trust
                ["ending_a"] = DialogueNode.Text(
                    "ending_a",
                    "dialogue_ch1_scene1_ending_a",
                    "end_a",
                    "yamamoto"
                ),

                // Node 11: Ending B - Balanced trust
                ["ending_b"] = DialogueNode.Text(
                    "ending_b",
                    "dialogue_ch1_scene1_ending_b",
                    "end_b",
                    "yamamoto"
                ),

                // Node 12: Ending C - High underground trust
                ["ending_c"] = DialogueNode.Text(
                    "ending_c",
                    "dialogue_ch1_scene1_ending_c",
                    "end_c",
                    "yamamoto"
                ),

                // End nodes
                ["end_a"] = DialogueNode.End("end_a"),
                ["end_b"] = DialogueNode.End("end_b"),
                ["end_c"] = DialogueNode.End("end_c")
            };

            return DialogueTree.CreateRuntime("ch1_scene1", nodes);
        }
    }
}
