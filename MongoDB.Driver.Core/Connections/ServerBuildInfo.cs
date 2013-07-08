/* Copyright 2010-2013 10gen Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Text.RegularExpressions;
using MongoDB.Bson;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// Represents build info about a server instance.
    /// </summary>
    public sealed class ServerBuildInfo
    {
        // private fields
        private readonly int _bits;
        private readonly string _gitVersion;
        private readonly string _sysInfo;
        private readonly Version _version;
        private readonly string _versionString;

        // constructors
        /// <summary>
        /// Creates a new instance of ServerBuildInfo.
        /// </summary>
        /// <param name="bits">The number of bits (32 or 64).</param>
        /// <param name="gitVersion">The GIT version.</param>
        /// <param name="sysInfo">The sysInfo.</param>
        /// <param name="versionString">The version string.</param>
        public ServerBuildInfo(int bits, string gitVersion, string sysInfo, string versionString)
        {
            _bits = bits;
            _gitVersion = gitVersion;
            _sysInfo = sysInfo;
            _version = ParseVersion(versionString);
            _versionString = versionString;
        }

        // public properties
        /// <summary>
        /// Gets the number of bits (32 or 64).
        /// </summary>
        public int Bits
        {
            get { return _bits; }
        }

        /// <summary>
        /// Gets the GIT version.
        /// </summary>
        public string GitVersion
        {
            get { return _gitVersion; }
        }

        /// <summary>
        /// Gets the sysInfo.
        /// </summary>
        public string SysInfo
        {
            get { return _sysInfo; }
        }

        /// <summary>
        /// Gets the version.
        /// </summary>
        public Version Version
        {
            get { return _version; }
        }

        /// <summary>
        /// Gets the version string.
        /// </summary>
        public string VersionString
        {
            get { return _versionString; }
        }

        // public static methods
        /// <summary>
        /// Creates a new instance of ServerBuildInfo initialized from the result of a buildinfo command.
        /// </summary>
        /// <param name="commandResult">A CommandResult.</param>
        /// <returns>A ServerBuildInfo.</returns>
        public static ServerBuildInfo FromCommandResult(CommandResult commandResult)
        {
            var response = commandResult.Response;
            return new ServerBuildInfo(
                response["bits"].ToInt32(),
                response["gitVersion"].AsString,
                response["sysInfo"].AsString,
                response["version"].AsString
            );
        }

        // public methods
        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return _version.ToString();
        }

        // private methods
        private Version ParseVersion(string versionString)
        {
            var match = Regex.Match(versionString, @"^(?<major>\d+)\.(?<minor>\d+)\.(?<build>\d+)(\.(?<revision>\d+))?(-.*)?$");
            if (match.Success)
            {
                var majorString = match.Groups["major"].Value;
                var minorString = match.Groups["minor"].Value;
                var buildString = match.Groups["build"].Value;
                var revisionString = match.Groups["revision"].Value;
                if (revisionString == "") { revisionString = "0"; }
                int major, minor, build, revision;
                if (int.TryParse(majorString, out major) &&
                    int.TryParse(minorString, out minor) &&
                    int.TryParse(buildString, out build) &&
                    int.TryParse(revisionString, out revision))
                {
                    return new Version(major, minor, build, revision);
                }
            }
            return new Version(0, 0, 0, 0);
        }
    }
}
