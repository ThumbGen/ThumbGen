using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace ThumbGen.Core
{
    public static class AssemblyExtensions
    {
        public static IEnumerable<Type> FindDerivedTypesFromAssembly(this Assembly assembly, Type baseType, bool classOnly)
        {
            if (assembly == null)
                throw new ArgumentNullException("assembly", "Assembly must be defined");
            if (baseType == null)
                throw new ArgumentNullException("baseType", "Parent Type must be defined");

            // get all the types
            var types = assembly.GetTypes();

            // works out the derived types
            foreach (var type in types)
            {
                // if classOnly, it must be a class
                // useful when you want to create instance
                if (classOnly && !type.IsClass && type.IsAbstract)
                    continue;

                if (baseType.IsInterface)
                {
                    var it = type.GetInterface(baseType.FullName);

                    if (it != null)
                        // add it to result list
                        yield return type;
                }
                else if (type.IsSubclassOf(baseType))
                {
                    // add it to result list
                    yield return type;
                }
            }
        }

        public static IEnumerable<Type> GetImplementations(this Assembly assembly, Type interfaceType)
        {
            // this will load the types for all of the currently loaded assemblies in the
            // current domain.

            return GetImplementations(interfaceType, new List<Assembly>() { assembly });
        }

        public static IEnumerable<Type> GetImplementations(Type interfaceType)
        {
            // this will load the types for all of the currently loaded assemblies in the
            // current domain.

            return GetImplementations(interfaceType, AppDomain.CurrentDomain.GetAssemblies());
        }

        public static IEnumerable<Type> GetImplementations(Type interfaceType, IEnumerable<Assembly> assemblies)
        {
            return assemblies.SelectMany(
                assembly => assembly.GetExportedTypes()).Where(
                    t => interfaceType.IsAssignableFrom(t)
                );
        }
    }


}
