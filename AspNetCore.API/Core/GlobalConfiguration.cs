using System.Collections.Generic;

namespace AspNetCore.API.Core
{
    public static class GlobalConfiguration
    {
        static GlobalConfiguration()
        {
            Modules = new List<Module>();
        }

        public static IList<Module> Modules { get; set; }

        public static IList<Module> Areas { get; set; }

        public static string WebRootPath { get; set; }

        public static string ContentRootPath { get; set; }

        public static string DefaultConnectionString { get; set; }
    }
}
