using System.Linq;
using System.Reflection;

namespace AspNetCore.API.Core
{
    public class Module
    {
        public string Name { get; set; }

        public Assembly Assembly { get; set; }

        public string ShortName => Name.Split('.').Last();

        public string Path { get; set; }
    }
}
