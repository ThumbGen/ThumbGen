
using System;
using System.IO;
using System.Reflection;

namespace ThumbGen
{
    public class Program
    {
        [STAThread]
        public static void Main()
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(Resolver);

            App app = new App();
            app.InitializeComponent();
            app.Run();
        }

        static System.Reflection.Assembly Resolver(object sender, ResolveEventArgs args)
        {
            if (args.Name.Contains("Newton"))
            {
                return GetIt("ThumbGen.Assemblies.Newtonsoft.Json.dll");
            }

            if (args.Name.Contains("ThumbGen.Core"))
            {
                return GetIt("ThumbGen.Assemblies.ThumbGen.Core.dll");
            }

            if (args.Name.Contains("ThumbGen.Render"))
            {
                return GetIt("ThumbGen.Assemblies.ThumbGen.Renderer.dll");
            }

            if (args.Name.Contains("Cook"))
            {
                return GetIt("ThumbGen.Assemblies.CookComputing.XmlRpcV2.dll");
            }

            if (args.Name.Contains("Diffie"))
            {
                return GetIt("ThumbGen.Assemblies.DiffieHellman.dll");
            }

            if (args.Name.Contains("Org.Mentalis"))
            {
                return GetIt("ThumbGen.Assemblies.Org.Mentalis.Security.dll");
            }

            if (args.Name.Contains("Tamir"))
            {
                return GetIt("ThumbGen.Assemblies.Tamir.SharpSSH.dll");
            }

            if (args.Name.Contains("DiscUtils.Common"))
            {
                return GetIt("ThumbGen.Assemblies.DiscUtils.Common.dll");
            }

            if (args.Name.Contains("DiscUtils"))
            {
                return GetIt("ThumbGen.Assemblies.DiscUtils.dll");
            }

            if (args.Name.Contains("RestSharp"))
            {
                return GetIt("ThumbGen.Assemblies.RestSharp.dll");
            }

            if (args.Name.Contains("WatTmdb"))
            {
                return GetIt("ThumbGen.Assemblies.WatTmdb.dll");
            }

            return null;
        }

        private static Assembly GetIt(string name)
        {
            try
            {
                Assembly a1 = Assembly.GetExecutingAssembly();
                Stream s = a1.GetManifestResourceStream(name);
                byte[] block = new byte[s.Length];
                s.Read(block, 0, block.Length);
                Assembly a2 = Assembly.Load(block);
                return a2;
            }
            catch
            {
                return null;
            }
        }
    }
}