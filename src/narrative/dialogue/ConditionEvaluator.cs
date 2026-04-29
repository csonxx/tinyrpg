using System;

namespace Core.Narrative.Dialogue
{
    /// <summary>
    /// Minimal expression evaluator for dialogue CONDITION nodes.
    ///
    /// Parses expressions of the form:  key operator value
    /// Supported operators:  >=, <=, >, <, ==, !=
    ///
    /// Examples:
    ///   trust.imperial >= 50
    ///   trust.underground < 30
    ///   flags.hasMetReina == 1
    ///
    /// The key is looked up via NarrativeStateMachine.Get[float]().
    /// </summary>
    public sealed class ConditionEvaluator
    {
        /// <summary>
        /// The set of supported comparison operators.
        /// </summary>
        public static readonly string[] SupportedOperators = { ">=", "<=", ">", "<", "==", "!=" };

        /// <summary>
        /// Result of evaluating a condition expression.
        /// </summary>
        public struct EvaluationResult
        {
            public readonly bool Success;
            public readonly bool Value;
            public readonly string Error;

            public EvaluationResult(bool success, bool value, string error = null)
            {
                Success = success;
                Value = value;
                Error = error;
            }

            public static EvaluationResult True() => new EvaluationResult(true, true);
            public static EvaluationResult False() => new EvaluationResult(true, false);
            public static EvaluationResult Error(string message) => new EvaluationResult(false, false, message);
        }

        /// <summary>
        /// Evaluates a condition expression against the provided NSM state lookup function.
        /// </summary>
        /// <param name="expression">The condition expression string (e.g. "trust.imperial >= 50")</param>
        /// <param name="getNsmValue">
        /// A function that retrieves a numeric NSM value given a key.
        /// Use (key) => nsm.Get[float](key) in production.
        /// </param>
        /// <returns>EvaluationResult indicating true/false or error.</returns>
        public EvaluationResult Evaluate(string expression, Func<string, float> getNsmValue)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return EvaluationResult.Error("Expression is null or empty.");

            expression = expression.Trim();

            // Find the operator
            string foundOp = null;
            int opIndex = -1;
            foreach (var op in SupportedOperators)
            {
                int idx = expression.IndexOf(op, StringComparison.Ordinal);
                if (idx >= 0)
                {
                    if (foundOp != null)
                        return EvaluationResult.Error($"Multiple operators found ('{foundOp}' and '{op}') in expression.");
                    foundOp = op;
                    opIndex = idx;
                }
            }

            if (foundOp == null)
                return EvaluationResult.Error($"No supported operator found in expression: '{expression}'");

            if (opIndex == 0 || opIndex >= expression.Length - foundOp.Length)
                return EvaluationResult.Error($"Operator '{foundOp}' is at invalid position in expression: '{expression}'");

            string key = expression.Substring(0, opIndex).Trim();
            string rightSide = expression.Substring(opIndex + foundOp.Length).Trim();

            if (string.IsNullOrEmpty(key))
                return EvaluationResult.Error($"Key is empty in expression: '{expression}'");

            if (string.IsNullOrEmpty(rightSide))
                return EvaluationResult.Error($"Value is empty in expression: '{expression}'");

            // Parse the right-hand side as a float
            if (!float.TryParse(rightSide, out float rhs))
                return EvaluationResult.Error($"Could not parse '{rightSide}' as a number in expression: '{expression}'");

            // Look up the key in NSM
            float lhs;
            try
            {
                lhs = getNsmValue(key);
            }
            catch (Exception ex)
            {
                return EvaluationResult.Error($"Failed to look up key '{key}': {ex.Message}");
            }

            bool result = Compare(lhs, foundOp, rhs);
            return result ? EvaluationResult.True() : EvaluationResult.False();
        }

        private static bool Compare(float lhs, string op, float rhs)
        {
            switch (op)
            {
                case ">=": return lhs >= rhs;
                case "<=": return lhs <= rhs;
                case ">":  return lhs > rhs;
                case "<":  return lhs < rhs;
                case "==": return Mathf.Approximately(lhs, rhs);
                case "!=": return !Mathf.Approximately(lhs, rhs);
                default:   return false;
            }
        }
    }

    // Unity reference for Mathf
    internal static class Mathf
    {
        public const float Epsilon = 1.17549435e-38f;
        public static bool Approximately(float a, float b) => System.Math.Abs(a - b) < 1e-5f;
    }
}
