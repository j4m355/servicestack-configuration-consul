﻿// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ServiceStack.Configuration.Consul
{
    using System.Collections.Generic;
    using System.Linq;
    using DTO;

    public static class KeyUtilities
    {
        public const string Prefix = "ss/";
        public static string GetDefaultLookupKey(string key) => $"{Prefix}{key}";

        /// <summary>
        /// For specified key gets a list of all possible locations from most -> least specific
        /// </summary>
        /// <param name="key">Default key</param>
        /// <returns>List of keys where value may be found (most to least specific)</returns>
        public static IEnumerable<string> GetPossibleKeys(string key)
        {
            var defaultKey = !key.StartsWith(Prefix) ? GetDefaultLookupKey(key) : key;

            var appHost = HostContext.AppHost;

            if (appHost == null)
                return new[] { defaultKey };

            var serviceName = appHost.ServiceName;
            var serviceKey = $"{defaultKey}/{serviceName}";

            if (appHost.Config == null)
                return new[] { serviceKey, defaultKey };

            var version = appHost.Config.ApiVersion;

            var instanceId = GetInstanceId(appHost.Config);

            // TODO Cache these as they'll be the same always
            return new[]
            {
                $"{serviceKey}/i/{instanceId}", // instance specific (ss/keyname/service/127.0.0.1:8080)
                $"{serviceKey}/{version}", // version specific (ss/keyname/service/v1)
                serviceKey, // service specific (ss/keyname/service)
                defaultKey // default (ss/keyname)
            };
        }

        /// <summary>
        /// Finds the most specific match for key from KeyValue candidates
        /// </summary>
        /// <param name="candidates">Collection of candidate results</param>
        /// <param name="key">Key to search for</param>
        /// <returns>Most specific matching KeyValue from collection</returns>
        public static KeyValue GetMostSpecificMatch(IEnumerable<KeyValue> candidates, string key)
        {
            if (candidates == null)
                return null;

            var keyValues = candidates as IList<KeyValue> ?? candidates.ToList();
            if (keyValues.Count == 0)
                return null;

            var possibleKeys = GetPossibleKeys(key).ToList();

            return keyValues.Reverse().FirstOrDefault(candidate => possibleKeys.Contains(candidate.Key));
        }

        private static string GetInstanceId(HostConfig config)
        {
            var hostUrl = string.IsNullOrEmpty(config.WebHostUrl)
                              ? string.Empty
                              : config.WebHostUrl.WithTrailingSlash().Replace("http://", string.Empty).Replace(
                                  "https://", string.Empty).Replace("/", "|");

            var factoryPath = config.HandlerFactoryPath;

            return $"{hostUrl}{factoryPath}";
        }
    }
}