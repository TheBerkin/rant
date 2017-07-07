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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Rant.Resources
{
    /// <summary>
    /// Default class for package depdendency resolving.
    /// </summary>
    public class RantDependencyResolver
    {
        private static readonly Regex FallbackRegex = new Regex(@"[\s\-_.]*[vV]?(?<version>\d+(\.\d+){0,2})$",
            RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Compiled);

        /// <summary>
        /// Attempts to resolve a depdendency to the appropriate package.
        /// </summary>
        /// <param name="depdendency">The depdendency to resolve.</param>
        /// <param name="package">The package loaded from the depdendency.</param>
        /// <returns></returns>
        public virtual bool TryResolvePackage(RantPackageDependency depdendency, out RantPackage package)
        {
            var currentDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            package = null;
            string path = Path.Combine(currentDirectory, $"{depdendency.ID}.rantpkg");
            if (!File.Exists(path))
            {
                RantPackageVersion version;
                // Fallback to name with version appended
#if UNITY
				path = Directory.GetFiles(Environment.CurrentDirectory, $"{depdendency.ID}*.rantpkg", SearchOption.AllDirectories).FirstOrDefault(p =>
#else
                path =
                    Directory.EnumerateFiles(currentDirectory, "*.rantpkg", SearchOption.AllDirectories)
                        .FirstOrDefault(p =>
#endif

                        {
                            var match = FallbackRegex.Match(Path.GetFileNameWithoutExtension(p));
                            if (!match.Success) return false;
                            version = RantPackageVersion.Parse(match.Groups["version"].Value);
                            return depdendency.AllowNewer && version >= depdendency.Version || depdendency.Version == version;
                        });
                if (path == null) return false;
            }
            try
            {
                var pkg = RantPackage.Load(path);
                if (pkg.ID != depdendency.ID) return false;
                if (depdendency.AllowNewer && pkg.Version >= depdendency.Version || pkg.Version == depdendency.Version)
                {
                    package = pkg;
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}