using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using AspNetCore.API.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Module = AspNetCore.API.Core.Module;

namespace AspNetCore.API.Extensions
{
    public static class ServiceCollectionExtensions
    {

        public static IServiceCollection LoadInstalledModules(this IServiceCollection services, string contentRootPath)
        {
            var modules = new List<Module>();
            var moduleRootFolder = new DirectoryInfo(Path.Combine(contentRootPath, "Modules"));
            var moduleFolders = moduleRootFolder.GetDirectories();

            foreach (var moduleFolder in moduleFolders)
            {
                var binFolder = new DirectoryInfo(Path.Combine(moduleFolder.FullName, "bin"));
                if (binFolder.Exists == false)
                {
                    continue;
                }

                foreach (var file in binFolder.GetFileSystemInfos("*.dll", SearchOption.AllDirectories))
                {
                    Assembly assembly;
                    try
                    {
                        assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(file.FullName);
                    }
                    catch (FileLoadException)
                    {
                        // Get loaded assembly
                        assembly = Assembly.Load(new AssemblyName(Path.GetFileNameWithoutExtension(file.Name)));

                        if (assembly == null)
                        {
                            throw;
                        }
                    }

                    if (assembly.FullName.Contains(moduleFolder.Name))
                    {
                        modules.Add(new Module()
                        {
                            Name = moduleFolder.Name,
                            Assembly = assembly,
                            Path = moduleFolder.FullName
                        });
                    }
                }
            }

            GlobalConfiguration.Modules = modules;
            return services;
        }

        public static IServiceCollection AddCustomizedMvc(this IServiceCollection services, IList<Module> modules )
        {
            var mvcBuilder = services
                .AddMvc(option =>
                {


                })
                .AddJsonOptions(options =>
                {
                    // JSON Camel Case Property Names
                    options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();
                })

                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            foreach (var module in modules)
            {
                // Register controller from modules
                mvcBuilder.AddApplicationPart(module.Assembly);
            }

            return services;
        }
    }
}
