using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;

namespace ThumbGen.Core
{
    public static class AssemblyResolverHelper
    {
        static AssemblyResolverHelper()
        {

        }

        public static void Init()
        {
            AppDomain.CurrentDomain.AssemblyResolve += Resolver;
        }

        static System.Reflection.Assembly Resolver(object sender, ResolveEventArgs args)
        {
            if (args.Name.Contains("NLog"))
            {
                return GenericHelpers.GetEmbeddedAssembly("ThumbGen.Core.Assemblies.NLog.dll");
            }

            if (args.Name.Contains("Ionic"))
            {
                return GenericHelpers.GetEmbeddedAssembly("ThumbGen.Core.Assemblies.Ionic.Zip.Reduced.dll");
            }

            if (args.Name.Contains("Avalon"))
            {
                return GenericHelpers.GetEmbeddedAssembly("ThumbGen.Core.Assemblies.AvalonDock.dll");
            }

            if (args.Name.Contains("Fluent"))
            {
                return GenericHelpers.GetEmbeddedAssembly("ThumbGen.Core.Assemblies.Fluent.dll");
            }

            return null;
        }
    }
}
