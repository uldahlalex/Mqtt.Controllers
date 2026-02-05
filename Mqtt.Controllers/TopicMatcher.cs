using System.Text.RegularExpressions;

namespace Mqtt.Controllers;

/// <summary>
/// Utility class for matching MQTT topics against patterns.
/// </summary>
public static class TopicMatcher
{
    /// <summary>
    /// Converts an MQTT topic pattern to a regex for matching incoming topics.
    /// Supports +, #, and {param} syntax.
    /// </summary>
    /// <param name="pattern">The topic pattern (e.g., "devices/{deviceId}/telemetry")</param>
    /// <returns>A compiled regex with named capture groups for parameters.</returns>
    public static Regex PatternToRegex(string pattern)
    {
        var parts = pattern.Split('/');
        var regexParts = new List<string>();

        foreach (var part in parts)
        {
            if (part == "+")
                regexParts.Add("[^/]+");
            else if (part == "#")
                regexParts.Add(".+");
            else if (part.StartsWith("{") && part.EndsWith("}"))
            {
                var paramName = part[1..^1];
                regexParts.Add($"(?<{paramName}>[^/]+)");
            }
            else
                regexParts.Add(Regex.Escape(part));
        }

        return new Regex("^" + string.Join("/", regexParts) + "$", RegexOptions.Compiled);
    }

    /// <summary>
    /// Converts a topic pattern to MQTT subscription format (replaces {param} with +).
    /// </summary>
    /// <param name="pattern">The topic pattern (e.g., "devices/{deviceId}/telemetry")</param>
    /// <returns>MQTT subscription topic (e.g., "devices/+/telemetry")</returns>
    public static string PatternToSubscription(string pattern)
    {
        return Regex.Replace(pattern, @"\{[^}]+\}", "+");
    }

    /// <summary>
    /// Checks if a topic matches an MQTT pattern with wildcards.
    /// </summary>
    /// <param name="pattern">The pattern (supports + and # wildcards)</param>
    /// <param name="topic">The actual topic to match</param>
    /// <returns>True if the topic matches the pattern.</returns>
    public static bool Matches(string pattern, string topic)
    {
        var patternParts = pattern.Split('/');
        var topicParts = topic.Split('/');

        for (int i = 0; i < patternParts.Length; i++)
        {
            if (patternParts[i] == "#") return true;
            if (i >= topicParts.Length) return false;
            if (patternParts[i] == "+") continue;
            if (patternParts[i] != topicParts[i]) return false;
        }

        return patternParts.Length == topicParts.Length;
    }

    /// <summary>
    /// Extracts parameter values from a topic using a pattern.
    /// </summary>
    /// <param name="pattern">The pattern with {param} placeholders</param>
    /// <param name="topic">The actual topic</param>
    /// <returns>Dictionary of parameter names to values, or null if no match.</returns>
    public static Dictionary<string, string>? ExtractParameters(string pattern, string topic)
    {
        var regex = PatternToRegex(pattern);
        var match = regex.Match(topic);

        if (!match.Success) return null;

        var result = new Dictionary<string, string>();
        foreach (var groupName in regex.GetGroupNames())
        {
            if (groupName != "0" && match.Groups[groupName].Success)
            {
                result[groupName] = match.Groups[groupName].Value;
            }
        }
        return result;
    }
}
