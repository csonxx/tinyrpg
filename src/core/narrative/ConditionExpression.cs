using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Core.Narrative;
using UnityEngine;

namespace Core.Narrative
{
    /// <summary>
    /// Evaluates compound boolean condition expressions against the Narrative State Machine.
    ///
    /// Supports:
    /// - Simple comparisons: trust.imperial >= 50, relationships.reina > 30, clues.foundKey == 1
    /// - AND logic (&&): conditions must all be true
    /// - OR logic (||): at least one condition must be true
    /// - Precedence: AND binds tighter than OR (a && b || c && d = (a && b) || (c && d))
    ///
    /// Grammar:
    /// expression := term (|| term)*
    /// term := condition (&& condition)*
    /// condition := key operator value
    /// operator := >= | &lt;= | &gt; | &lt; | == | !=
    ///
    /// Sprints: S3-4
    /// Design Doc: design/gdd/episode-structure.md (Rule 5)
    /// </summary>
    public static class ConditionExpression
    {
        /// <summary>
        /// Result of evaluating a condition expression.
        /// </summary>
        public sealed class EvaluationResult
        {
            public bool IsValid { get; }
            public bool Value { get; }
            public string ErrorMessage { get; }

            private EvaluationResult(bool isValid, bool value, string errorMessage = null)
            {
                IsValid = isValid;
                Value = value;
                ErrorMessage = errorMessage;
            }

            public static EvaluationResult Success(bool value) => new EvaluationResult(true, value);
            public static EvaluationResult Failure(string error) => new EvaluationResult(false, false, error);
        }

        private static readonly Regex ConditionPattern = new Regex(
            @"^(\w[\w\.]*)\s*(>=|<=|>|!=|==|<)\s*(-?[\d.]+)$",
            RegexOptions.Compiled);

        /// <summary>
        /// Evaluates a compound condition expression string against NSM state.
        /// </summary>
        /// <param name="expression">The condition expression (e.g., "trust.imperial >= 50 && clues.foundKey == 1").</param>
        /// <param name="nsm">The Narrative State Machine instance to query.</param>
        /// <returns>EvaluationResult indicating success/failure and the boolean value.</returns>
        public static EvaluationResult Evaluate(string expression, NarrativeStateMachine nsm)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                return EvaluationResult.Failure("Expression is null or empty");
            }

            try
            {
                // Split by OR (lowest precedence)
                string[] orTerms = SplitByToken(expression, "||");

                foreach (var orTerm in orTerms)
                {
                    // Split by AND (higher precedence than OR)
                    string[] andConditions = SplitByToken(orTerm.Trim(), "&&");

                    bool allAndConditionsTrue = true;
                    foreach (var condition in andConditions)
                    {
                        string cond = condition.Trim();
                        if (string.IsNullOrEmpty(cond))
                        {
                            continue;
                        }

                        var result = EvaluateSingleCondition(cond, nsm);
                        if (!result.IsValid)
                        {
                            return result;
                        }

                        if (!result.Value)
                        {
                            allAndConditionsTrue = false;
                            break;
                        }
                    }

                    if (allAndConditionsTrue)
                    {
                        return EvaluationResult.Success(true);
                    }
                }

                return EvaluationResult.Success(false);
            }
            catch (Exception ex)
            {
                return EvaluationResult.Failure($"Failed to parse expression '{expression}': {ex.Message}");
            }
        }

        /// <summary>
        /// Validates that an expression string is syntactically correct without evaluating it.
        /// </summary>
        /// <param name="expression">The condition expression to validate.</param>
        /// <returns>True if the expression is syntactically valid, false otherwise.</returns>
        public static bool IsValidSyntax(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                return false;
            }

            try
            {
                string[] orTerms = SplitByToken(expression, "||");
                foreach (var orTerm in orTerms)
                {
                    string[] andConditions = SplitByToken(orTerm.Trim(), "&&");
                    foreach (var condition in andConditions)
                    {
                        string cond = condition.Trim();
                        if (string.IsNullOrEmpty(cond))
                        {
                            continue;
                        }

                        if (!ConditionPattern.IsMatch(cond))
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string[] SplitByToken(string input, string token)
        {
            var result = new List<string>();
            int depth = 0;
            int start = 0;

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (c == '(' || c == '[' || c == '{')
                {
                    depth++;
                }
                else if (c == ')' || c == ']' || c == '}')
                {
                    depth--;
                }
                else if (depth == 0 && i + token.Length <= input.Length &&
                         input.Substring(i, token.Length) == token)
                {
                    result.Add(input.Substring(start, i - start));
                    i += token.Length - 1;
                    start = i + 1;
                }
            }

            result.Add(input.Substring(start));
            return result.ToArray();
        }

        private static EvaluationResult EvaluateSingleCondition(string condition, NarrativeStateMachine nsm)
        {
            var match = ConditionPattern.Match(condition.Trim());
            if (!match.Success)
            {
                return EvaluationResult.Failure($"Invalid condition syntax: '{condition}'");
            }

            string key = match.Groups[1].Value;
            string op = match.Groups[2].Value;
            if (!float.TryParse(match.Groups[3].Value, out float targetValue))
            {
                return EvaluationResult.Failure($"Invalid numeric value in condition: '{condition}'");
            }

            float actualValue = nsm.Get<float>(key);

            bool result = op switch
            {
                ">=" => actualValue >= targetValue,
                "<=" => actualValue <= targetValue,
                ">" => actualValue > targetValue,
                "<" => actualValue < targetValue,
                "==" => Mathf.Approximately(actualValue, targetValue),
                "!=" => !Mathf.Approximately(actualValue, targetValue),
                _ => false
            };

            return EvaluationResult.Success(result);
        }
    }
}
