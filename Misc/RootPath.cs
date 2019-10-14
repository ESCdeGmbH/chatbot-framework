using System.IO;
using System.Linq;
using System.Reflection;

namespace Framework.Misc
{
    /// <summary> 
    /// Class that calculates the root path of the project regardless of the computer that is used.
    /// </summary>
    public static class RootPath
    {
        /// <summary>
        /// Returns the root path of the project/ dll and concatenates it with the given folders/ files.
        /// </summary>
        /// <param name="projectName">The name of the project</param>
        /// <param name="path">The folder structure/ file.</param>
        /// <returns>The full path.</returns>
        public static string GetRootPath(string projectName, params string[] path)
        {
            string debug = GetDebug(projectName, path);
            string release = GetRelease(path);

            bool existD = File.Exists(debug) || Directory.Exists(debug);
            bool existR = File.Exists(release) || Directory.Exists(release);

            if (existD == existR)
            {
#if DEBUG
                return debug;
#else
                return release;
#endif
            }

            if (existD)
                return debug;
            return release;
        }

        private static string GetRelease(params string[] path)
        {
            string cur;
            cur = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return Path.Combine(cur, Path.Combine(path));
        }

        private static string GetDebug(string projectName, params string[] path)
        {
            string cur;
            cur = Assembly.GetExecutingAssembly().Location;
            cur = cur.Split(@"\bin\Debug")[0];
            var split = cur.Split(Path.DirectorySeparatorChar).ToList();
            split.RemoveAt(split.Count - 1);
            cur = Path.Combine(Path.Combine(split.ToArray()), projectName);
            return Path.Combine(cur, Path.Combine(path));
        }
    }
}
