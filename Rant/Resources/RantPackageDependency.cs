#region License

// https://github.com/TheBerkin/Rant
// 
// Copyright (c) 2017 Nicholas Fleck
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in the
// Software without restriction, including without limitation the rights to use, copy,
// modify, merge, publish, distribute, sublicense, and/or sell copies of the Software,
// and to permit persons to whom the Software is furnished to do so, subject to the
// following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
// PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE
// OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

#endregion

using System;

using Rant.Core.Utilities;

namespace Rant.Resources
{
    /// <summary>
    /// Represents a dependency for a Rant package.
    /// </summary>
    public sealed class RantPackageDependency
    {
        private string _id;
        private RantPackageVersion _version;

        /// <summary>
        /// Initializes a new RantPackageDependency object.
        /// </summary>
        /// <param name="id">The ID of the package.</param>
        /// <param name="version">The targeted version of the package.</param>
        public RantPackageDependency(string id, string version)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (version == null) throw new ArgumentNullException(nameof(version));
            ID = id;
            Version = RantPackageVersion.Parse(version);
        }

        /// <summary>
        /// Initializes a new RantPackageDependency object.
        /// </summary>
        /// <param name="id">The ID of the package.</param>
        /// <param name="version">The targeted version of the package.</param>
        public RantPackageDependency(string id, RantPackageVersion version)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (version == null) throw new ArgumentNullException(nameof(version));
            ID = id;
            Version = version;
        }

        /// <summary>
        /// The ID of the package.
        /// </summary>
        public string ID
        {
            get { return _id; }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                _id = value.Trim();
            }
        }

        /// <summary>
        /// The targeted version of the package.
        /// </summary>
        public RantPackageVersion Version
        {
            get { return _version; }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                _version = value;
            }
        }

        /// <summary>
        /// Specifies whether the dependency will accept a package newer than the one given.
        /// </summary>
        public bool AllowNewer { get; set; }

        /// <summary>
        /// Checks if the specified version is compatible with the current dependency.
        /// </summary>
        /// <param name="version">The version to check.</param>
        /// <returns></returns>
        public bool CheckVersion(RantPackageVersion version)
        {
            if (version == null) throw new ArgumentNullException(nameof(version));
            return AllowNewer ? version >= Version : version == Version;
        }

        /// <summary>
        /// Creates a dependency for the specified package.
        /// </summary>
        /// <param name="package">The package to create the dependency for.</param>
        /// <returns></returns>
        public static RantPackageDependency Create(RantPackage package)
        {
            if (Util.IsNullOrWhiteSpace(package.ID))
                throw new ArgumentException("Package ID cannot be empty.");
            return new RantPackageDependency(package.ID, package.Version);
        }

        /// <summary>
        /// Returns a string representation of the current dependency.
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"{ID} {Version}";

        /// <summary>
        /// Gets the hash code for the instance.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() => ID.GetHashCode();

        /// <summary>
        /// Determines whether the current RantPackageDependency is shares an ID with the specified object.
        /// </summary>
        /// <param name="obj">The object to compare to.</param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            var d = obj as RantPackageDependency;
            return d != null && string.Equals(ID, d.ID, StringComparison.Ordinal);
        }
    }
}