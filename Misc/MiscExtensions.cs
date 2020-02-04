using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Framework.Misc
{
    /// <summary>
    /// Miscellaneous extensions.
    /// </summary>
    public static class MiscExtensions
    {
        private static readonly Random r = new Random();

        /// <summary>
        /// Select a random element from an array.
        /// </summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="_this">The array.</param>
        /// <returns>The element.</returns>
        public static T Random<T>(this T[] _this) => _this.Any() ? _this[r.Next(0, _this.Length)] : default(T);

        /// <summary>
        /// Select a random element from a list.
        /// </summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="_this">The list.</param>
        /// <returns>The element.</returns>
        public static T Random<T>(this List<T> _this) => _this.Any() ? _this[r.Next(0, _this.Count)] : default(T);

        /// <summary>
        /// Create a list from a list and an element.
        /// </summary>
        /// <typeparam name="T">The type of elements.</typeparam>
        /// <param name="_this">The list.</param>
        /// <param name="next">the element.</param>
        /// <returns>The new list.</returns>
        public static List<T> With<T>(this List<T> _this, T next) => new List<T>(_this) { next };
        /// <summary>
        /// Create a list from a list and another list.
        /// </summary>
        /// <typeparam name="T">The type of elements.</typeparam>
        /// <param name="_this">The list.</param>
        /// <param name="next">The other list.</param>
        /// <returns>The new list.</returns>
        public static List<T> With<T>(this List<T> _this, List<T> next)
        {
            var ls = new List<T>(_this);
            ls.AddRange(next);
            return ls;
        }
        /// <summary>
        /// Adds a new tuple to a list of tuples.
        /// </summary>
        /// <typeparam name="A">The first type of tuple.</typeparam>
        /// <typeparam name="B">The second type of tuple.</typeparam>
        /// <param name="_this">The list of tuples.</param>
        /// <param name="a">The first element.</param>
        /// <param name="b">The second element.</param>
        public static void Add<A, B>(this List<Tuple<A, B>> _this, A a, B b) => _this.Add(new Tuple<A, B>(a, b));

        /// <summary>
        /// Read data from embedded resource.
        /// </summary>
        /// <param name="resourceName">the name of the resource (e.g. Framework.DialogAnalyzer.RA-Blacklist.json)</param>
        /// <param name="assembly">the assembly to be searched (can by null, we will search different assemblies)</param>
        /// <returns>the data or null iff not found</returns>
        public static string LoadEmbeddedResource(string resourceName, Assembly assembly = null)
        {
            var assemblies = assembly != null ? new List<Assembly> { assembly } : new List<Assembly> { Assembly.GetEntryAssembly(), Assembly.GetExecutingAssembly(), Assembly.GetCallingAssembly() };
            string data = null;
            foreach (var a in assemblies)
                if ((data = LoadEmbeddedResourceByAssembly(resourceName, a)) != null)
                    return data;
            return null;
        }

        public static List<string> ListEmbeddedFiles(string path, Assembly assembly = null)
        {
            var assemblies = assembly != null ? new List<Assembly> { assembly } : new List<Assembly> { Assembly.GetEntryAssembly(), Assembly.GetExecutingAssembly(), Assembly.GetCallingAssembly() };
            List<string> resources = new List<string>();
            foreach (var a in assemblies)
            {
                if (a == null)
                    continue;
                var rs = a.GetManifestResourceNames();
                resources.AddRange(rs.Where(r => r.StartsWith(path)));
            }
            return resources.Distinct().ToList();
        }

        private static string LoadEmbeddedResourceByAssembly(string resourceName, Assembly assembly)
        {
            if (assembly == null)
                return null;
            Stream stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                return null;
            using (stream)
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
