using System;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace RzWork.AzureMonitor
{
    internal class Config
    {
        class Tag
        {
            public string Name = null;

            public string Value = null;
        }

        public const string MiClientIdKey = "MiClientId";

        public const string DcrIdKey = "DcrId";

        public const string DcrStreamKey = "DcrStream";

        public const string DceUrlKey = "DceUrl";

        public const string EnvPrefix = "LA_";

        public const string EnvMiClientIdKey = EnvPrefix + MiClientIdKey;

        public const string EnvDcrIdKey = EnvPrefix + DcrIdKey;

        public const string EnvDcrStreamKey = EnvPrefix + DcrStreamKey;

        public const string EnvDceUrlKey = EnvPrefix + DceUrlKey;

        public string MiClientId { get; private set; }

        public string DcrId { get; private set; }

        public string DcrStream { get; private set; }

        public string DceUrl { get; private set; }

        public bool Complete => !string.IsNullOrEmpty(MiClientId) && !string.IsNullOrEmpty(DcrId) &&
            !string.IsNullOrEmpty(DcrStream) && !string.IsNullOrEmpty(DceUrl);

        public void Merge(Config one)
        {
            if (string.IsNullOrEmpty(MiClientId))
            {
                MiClientId = one.MiClientId;
            }
            if (string.IsNullOrEmpty(DcrId))
            {
                DcrId = one.DcrId;
            }
            if (string.IsNullOrEmpty(DcrStream))
            {
                DcrStream = one.DcrStream;
            }
            if (string.IsNullOrEmpty(DceUrl))
            {
                DceUrl = one.DceUrl;
            }
        }

        public override string ToString()
        {
            var builder = new StringBuilder($"{typeof(Config)}:\n");
            builder.Append($"  MiClientId: \"{MiClientId}\"");
            builder.Append($"  DcrId: \"{DcrId}\"");
            builder.Append($"  DcrStream: \"{DcrStream}\"");
            builder.Append($"  DceUrl: \"{DceUrl}\"");
            return builder.ToString();
        }

        public static Config FromAttributes(StringDictionary attributes)
        {
            var config = new Config();
            config.MiClientId = attributes[MiClientIdKey];
            config.DcrId = attributes[DcrIdKey];
            config.DcrStream = attributes[DcrStreamKey];
            config.DceUrl = attributes[DceUrlKey];
            DebugLog.WriteInfo<Config>($"Config from attributes: {config}");
            return config;
        }

        public static Config FromEnvironmentVars()
        {
            var config = new Config();
            config.MiClientId = Environment.GetEnvironmentVariable(EnvMiClientIdKey);
            config.DcrId = Environment.GetEnvironmentVariable(EnvDcrIdKey);
            config.DcrStream = Environment.GetEnvironmentVariable(EnvDcrStreamKey);
            config.DceUrl = Environment.GetEnvironmentVariable(EnvDceUrlKey);
            DebugLog.WriteInfo<Config>($"Config from environment variables: {config}");
            return config;
        }

        private static Config FromTags(Tag[] tags)
        {
            var config = new Config();
            if (tags == null || tags.Length == 0)
            {
                return config;
            }
            config.MiClientId = tags.FirstOrDefault(tag => tag.Name.Equals(EnvMiClientIdKey, StringComparison.OrdinalIgnoreCase)).Value;
            config.DcrId = tags.FirstOrDefault(tag => tag.Name.Equals(EnvDcrIdKey, StringComparison.OrdinalIgnoreCase)).Value;
            config.DcrStream = tags.FirstOrDefault(tag => tag.Name.Equals(EnvDcrStreamKey, StringComparison.OrdinalIgnoreCase)).Value;
            config.DceUrl = tags.FirstOrDefault(tag => tag.Name.Equals(EnvDceUrlKey, StringComparison.OrdinalIgnoreCase)).Value;
            return config;
        }

        public static Config FromIMDS()
        {
            var client = new HttpClient();
            var url = "http://169.254.169.254/metadata/instance/compute/tagsList?api-version=2021-02-01";
            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                request.Headers.Add("Metadata", "true");
                try
                {
                    using (var response = client.SendAsync(request, HttpCompletionOption.ResponseContentRead).Result)
                    {
                        response.EnsureSuccessStatusCode();
                        var content = response.Content.ReadAsStringAsync().Result;
                        DebugLog.WriteInfo<Config>($"IMDS tags: {content}");
                        var jsonOptions = new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        };
                        var tags = JsonSerializer.Deserialize<Tag[]>(content);
                        var config = FromTags(tags);
                        DebugLog.WriteInfo<Config>($"Config from IMDS: {config}");
                        return config;
                    }
                }
                catch (Exception ex)
                {
                    DebugLog.WriteWarning<Config>($"Error when reading IMDS: {ex}");
                    return new Config();
                }
            }
        }

        public static Config Get(StringDictionary attributes)
        {
            var config = Config.FromAttributes(attributes);
            if (config.Complete)
            {
                return config;
            }
            config.Merge(Config.FromEnvironmentVars());
            if (config.Complete)
            {
                return config;
            }
            config.Merge(Config.FromIMDS());
            DebugLog.WriteInfo<Config>($"Merged config: {config}");
            return config;
        }
    }
}
