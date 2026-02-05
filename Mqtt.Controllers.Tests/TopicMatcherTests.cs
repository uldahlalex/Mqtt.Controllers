namespace Mqtt.Controllers.Tests;

public class TopicMatcherTests
{
    public class MatchesTests
    {
        [Theory]
        [InlineData("weather", "weather", true)]
        [InlineData("weather", "temperature", false)]
        [InlineData("devices/sensor1", "devices/sensor1", true)]
        [InlineData("devices/sensor1", "devices/sensor2", false)]
        public void ExactMatch(string pattern, string topic, bool expected)
        {
            Assert.Equal(expected, TopicMatcher.Matches(pattern, topic));
        }

        [Theory]
        [InlineData("devices/+/telemetry", "devices/sensor1/telemetry", true)]
        [InlineData("devices/+/telemetry", "devices/sensor2/telemetry", true)]
        [InlineData("devices/+/telemetry", "devices/telemetry", false)]
        [InlineData("devices/+/telemetry", "devices/a/b/telemetry", false)]
        [InlineData("+/temperature", "living-room/temperature", true)]
        [InlineData("+/+/data", "home/kitchen/data", true)]
        public void SingleLevelWildcard(string pattern, string topic, bool expected)
        {
            Assert.Equal(expected, TopicMatcher.Matches(pattern, topic));
        }

        [Theory]
        [InlineData("devices/#", "devices/sensor1", true)]
        [InlineData("devices/#", "devices/sensor1/telemetry", true)]
        [InlineData("devices/#", "devices/a/b/c/d", true)]
        [InlineData("devices/#", "other/sensor1", false)]
        [InlineData("#", "any/topic/at/all", true)]
        public void MultiLevelWildcard(string pattern, string topic, bool expected)
        {
            Assert.Equal(expected, TopicMatcher.Matches(pattern, topic));
        }

        [Theory]
        [InlineData("devices/+/#", "devices/sensor1/a/b/c", true)]
        [InlineData("devices/+/#", "devices/sensor1/data", true)]
        [InlineData("+/+/#", "a/b/c/d/e", true)]
        public void CombinedWildcards(string pattern, string topic, bool expected)
        {
            Assert.Equal(expected, TopicMatcher.Matches(pattern, topic));
        }
    }

    public class PatternToRegexTests
    {
        [Theory]
        [InlineData("devices/{deviceId}/telemetry", "devices/sensor1/telemetry", true)]
        [InlineData("devices/{deviceId}/telemetry", "devices/my-device/telemetry", true)]
        [InlineData("devices/{deviceId}/telemetry", "devices/telemetry", false)]
        [InlineData("{room}/temperature", "kitchen/temperature", true)]
        [InlineData("{building}/{floor}/{room}", "office/3/conference", true)]
        public void MatchesWithParameters(string pattern, string topic, bool expected)
        {
            var regex = TopicMatcher.PatternToRegex(pattern);
            Assert.Equal(expected, regex.IsMatch(topic));
        }

        [Fact]
        public void ExtractsNamedParameter()
        {
            var regex = TopicMatcher.PatternToRegex("devices/{deviceId}/telemetry");
            var match = regex.Match("devices/sensor1/telemetry");

            Assert.True(match.Success);
            Assert.Equal("sensor1", match.Groups["deviceId"].Value);
        }

        [Fact]
        public void ExtractsMultipleParameters()
        {
            var regex = TopicMatcher.PatternToRegex("{building}/{floor}/{room}/temperature");
            var match = regex.Match("office/3/conference/temperature");

            Assert.True(match.Success);
            Assert.Equal("office", match.Groups["building"].Value);
            Assert.Equal("3", match.Groups["floor"].Value);
            Assert.Equal("conference", match.Groups["room"].Value);
        }

        [Theory]
        [InlineData("data/+/values", "data/any/values", true)]
        [InlineData("data/#", "data/a/b/c", true)]
        public void SupportsWildcardsInRegex(string pattern, string topic, bool expected)
        {
            var regex = TopicMatcher.PatternToRegex(pattern);
            Assert.Equal(expected, regex.IsMatch(topic));
        }
    }

    public class PatternToSubscriptionTests
    {
        [Theory]
        [InlineData("weather", "weather")]
        [InlineData("devices/{deviceId}/telemetry", "devices/+/telemetry")]
        [InlineData("{room}/temperature", "+/temperature")]
        [InlineData("{a}/{b}/{c}", "+/+/+")]
        [InlineData("devices/+/data", "devices/+/data")]
        [InlineData("devices/#", "devices/#")]
        public void ConvertsParametersToWildcards(string pattern, string expected)
        {
            Assert.Equal(expected, TopicMatcher.PatternToSubscription(pattern));
        }
    }

    public class ExtractParametersTests
    {
        [Fact]
        public void ExtractsSingleParameter()
        {
            var result = TopicMatcher.ExtractParameters("devices/{deviceId}/telemetry", "devices/sensor1/telemetry");

            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("sensor1", result["deviceId"]);
        }

        [Fact]
        public void ExtractsMultipleParameters()
        {
            var result = TopicMatcher.ExtractParameters("{building}/{floor}/{room}", "office/3/conference");

            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Equal("office", result["building"]);
            Assert.Equal("3", result["floor"]);
            Assert.Equal("conference", result["room"]);
        }

        [Fact]
        public void ReturnsNullForNonMatch()
        {
            var result = TopicMatcher.ExtractParameters("devices/{id}/data", "other/topic");

            Assert.Null(result);
        }

        [Fact]
        public void ReturnsEmptyDictionaryForPatternWithoutParameters()
        {
            var result = TopicMatcher.ExtractParameters("weather", "weather");

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void HandlesSpecialCharactersInValues()
        {
            var result = TopicMatcher.ExtractParameters("devices/{deviceId}/data", "devices/sensor-123_abc/data");

            Assert.NotNull(result);
            Assert.Equal("sensor-123_abc", result["deviceId"]);
        }
    }
}
