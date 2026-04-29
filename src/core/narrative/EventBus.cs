using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Narrative
{
    /// <summary>
    /// Simple event emitter with glob pattern matching for event routing.
    /// Reusable across systems. Supports '*' (any characters) and '?' (single character) wildcards.
    /// </summary>
    public sealed class EventBus
    {
        private readonly Dictionary<string, List<Action<NSMEvent>>> _listeners = new Dictionary<string, List<Action<NSMEvent>>>();

        /// <summary>
        /// Subscribe a callback to events matching the given pattern.
        /// Pattern supports '*' (any characters) and '?' (single character) wildcards.
        /// </summary>
        /// <param name="pattern">Glob pattern, e.g. "trust.*" or "nsm.state.*"</param>
        /// <param name="callback">Action to invoke when a matching event is emitted</param>
        public void Subscribe(string pattern, Action<NSMEvent> callback)
        {
            if (string.IsNullOrEmpty(pattern))
                throw new ArgumentException("Pattern cannot be null or empty", nameof(pattern));
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            if (!_listeners.TryGetValue(pattern, out var list))
            {
                list = new List<Action<NSMEvent>>();
                _listeners[pattern] = list;
            }
            list.Add(callback);
        }

        /// <summary>
        /// Unsubscribe a specific callback from a pattern. If callback is null, removes all listeners for pattern.
        /// </summary>
        public void Unsubscribe(string pattern, Action<NSMEvent> callback = null)
        {
            if (string.IsNullOrEmpty(pattern))
                return;

            if (!_listeners.TryGetValue(pattern, out var list))
                return;

            if (callback == null)
            {
                list.Clear();
            }
            else
            {
                list.Remove(callback);
                if (list.Count == 0)
                    _listeners.Remove(pattern);
            }
        }

        /// <summary>
        /// Emit an event to all listeners whose pattern matches the event key.
        /// </summary>
        /// <param name="event">The NSMEvent to emit</param>
        public void Emit(NSMEvent e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            foreach (var kvp in _listeners)
            {
                if (PatternMatches(kvp.Key, e.Key))
                {
                    foreach (var callback in kvp.Value)
                    {
                        callback(e);
                    }
                }
            }
        }

        /// <summary>
        /// Check if a glob pattern matches a key.
        /// '*' matches zero or more characters.
        /// '?' matches exactly one character.
        /// </summary>
        public static bool PatternMatches(string pattern, string key)
        {
            if (pattern == null || key == null)
                return false;
            if (pattern == key)
                return true;
            if (pattern == "*")
                return true;

            return MatchWithWildcards(pattern, 0, key, 0);
        }

        private static bool MatchWithWildcards(string pattern, int pi, string key, int ki)
        {
            // Base cases
            if (pi == pattern.Length && ki == key.Length)
                return true;
            if (pi == pattern.Length)
                return false;
            if (ki == key.Length)
                return pattern[pi] == '*' && MatchWithWildcards(pattern, pi + 1, key, ki);

            char pc = pattern[pi];
            char kc = key[ki];

            if (pc == '*')
            {
                // '*' can match zero characters (skip '*') or one+ characters (consume key char)
                return MatchWithWildcards(pattern, pi + 1, key, ki) ||
                       MatchWithWildcards(pattern, pi, key, ki + 1);
            }
            else if (pc == '?')
            {
                // '?' matches exactly one character
                return MatchWithWildcards(pattern, pi + 1, key, ki + 1);
            }
            else
            {
                return char.ToLowerInvariant(pc) == char.ToLowerInvariant(kc) &&
                       MatchWithWildcards(pattern, pi + 1, key, ki + 1);
            }
        }

        /// <summary>
        /// Remove all listeners. Use for testing or scene cleanup.
        /// </summary>
        public void Clear()
        {
            _listeners.Clear();
        }

        /// <summary>
        /// Returns the number of unique patterns registered.
        /// </summary>
        public int PatternCount => _listeners.Count;
    }
}
