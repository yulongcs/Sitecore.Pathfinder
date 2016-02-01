﻿// © 2015 Sitecore Corporation A/S. All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using Microsoft.Framework.ConfigurationModel;
using Sitecore.Pathfinder.Diagnostics;
using Sitecore.Pathfinder.Extensions;
using Sitecore.Pathfinder.IO;

namespace Sitecore.Pathfinder.Configuration
{
    [Export(typeof(IConfigurationService))]
    public class ConfigurationService : IConfigurationService
    {
        [ImportingConstructor]
        public ConfigurationService([NotNull] IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public virtual void AddCommandLine([NotNull] IConfigurationSourceRoot configurationSourceRoot)
        {
            // cut off executable name
            var commandLineArgs = Environment.GetCommandLineArgs().Skip(1).ToList();
            AddCommandLine(configurationSourceRoot, commandLineArgs);
        }

        public virtual void AddCommandLine([NotNull] IConfigurationSourceRoot configurationSourceRoot, [NotNull][ItemNotNull] IEnumerable<string> commandLineArgs)
        {
            var args = new List<string>();

            var positionalArg = 0;
            for (var n = 0; n < commandLineArgs.Count(); n++)
            {
                var arg = commandLineArgs.ElementAt(n);

                // if the arg is not a switch, add it to the list of position args
                if (!arg.StartsWith("-") && !arg.StartsWith("/"))
                {
                    args.Add("/arg" + positionalArg);
                    args.Add(arg);

                    positionalArg++;

                    continue;
                }

                // if the arg is a switch, add it to the list of args to pass to the commandline configuration
                args.Add(arg);
                if (arg.IndexOf('=') >= 0)
                {
                    continue;
                }

                n++;
                if (n >= commandLineArgs.Count())
                {
                    args.Add("true");
                    continue;
                }

                arg = commandLineArgs.ElementAt(n);
                if (arg.StartsWith("-") || arg.StartsWith("/"))
                {
                    args.Add("true");
                    n--;
                    continue;
                }

                args.Add(commandLineArgs.ElementAt(n));
            }

            configurationSourceRoot.AddCommandLine(args.ToArray());
        }

        public virtual void Load(ConfigurationOptions options, string projectDirectory = null)
        {
            var configurationSourceRoot = Configuration as IConfigurationSourceRoot;
            if (configurationSourceRoot == null)
            {
                throw new ConfigurationException(Texts.Configuration_failed_spectacularly);
            }

            var toolsDirectory = configurationSourceRoot.Get(Constants.Configuration.ToolsDirectory);
            Console.WriteLine();
            Console.WriteLine("ToolsDirectory1: " + toolsDirectory);


            // add system config
            var systemConfigFileName = Path.Combine(toolsDirectory, configurationSourceRoot.Get(Constants.Configuration.SystemConfigFileName));
            if (!File.Exists(systemConfigFileName))
            {
                throw new ConfigurationException(Texts.System_configuration_file_not_found, systemConfigFileName);
            }

            Console.WriteLine(File.ReadAllText(toolsDirectory));


            configurationSourceRoot.AddJsonFile(systemConfigFileName);

            Console.WriteLine("ProjectDirectory1: " + projectDirectory);
            Console.WriteLine("SourceDirectory1: " + configurationSourceRoot.GetString(Constants.Configuration.CopyDependenciesSourceDirectory));
            Console.WriteLine("Database1: " + configurationSourceRoot.GetString(Constants.Configuration.Database));
            Console.WriteLine("ConfigFileName1: " + configurationSourceRoot.GetString(Constants.Configuration.ProjectConfigFileName));


            // add command line
            if ((options & ConfigurationOptions.IncludeCommandLine) == ConfigurationOptions.IncludeCommandLine)
            {
                AddCommandLine(configurationSourceRoot);
            }

            // add environment variables
            if ((options & ConfigurationOptions.IncludeEnvironment) == ConfigurationOptions.IncludeEnvironment)
            {
                configurationSourceRoot.AddEnvironmentVariables();
            }

            // set project directory
            if (projectDirectory != null)
            {
                configurationSourceRoot.Set(Constants.Configuration.ProjectDirectory, projectDirectory);
            }
            else
            {
                projectDirectory = configurationSourceRoot.GetString(Constants.Configuration.ProjectDirectory);
            }

            Console.WriteLine("ProjectDirectory: " + projectDirectory);
            Console.WriteLine("SourceDirectory: " + configurationSourceRoot.GetString(Constants.Configuration.CopyDependenciesSourceDirectory));
            Console.WriteLine("Database: " + configurationSourceRoot.GetString(Constants.Configuration.Database));
            Console.WriteLine("ConfigFileName: " + configurationSourceRoot.GetString(Constants.Configuration.ProjectConfigFileName));

            // add project config file - scconfig.json
            var projectConfigFileName = PathHelper.Combine(projectDirectory, configurationSourceRoot.Get(Constants.Configuration.ProjectConfigFileName));
            if (File.Exists(projectConfigFileName))
            {
                configurationSourceRoot.AddFile(projectConfigFileName);
            }
            else if (Directory.GetFiles(projectDirectory).Any() || Directory.GetDirectories(projectDirectory).Any())
            {
                // no config file, but project directory has files, so let's try the default project config file
                projectConfigFileName = PathHelper.Combine(toolsDirectory, "files\\project.noconfig\\scconfig.json");
                if (File.Exists(projectConfigFileName))
                {
                    configurationSourceRoot.AddFile(projectConfigFileName);
                }
            }

            // add project role configs 
            var projectRole = configurationSourceRoot.GetString(Constants.Configuration.ProjectRole).ToLowerInvariant();
            if (!string.IsNullOrEmpty(projectRole))
            {
                foreach (var pair in configurationSourceRoot.GetSubKeys("project-role-conventions:" + projectRole))
                {
                    var conventionsFileName = configurationSourceRoot.GetString("project-role-conventions:" + projectRole + ":" + pair.Key);

                    if (conventionsFileName.StartsWith("$tools/", StringComparison.OrdinalIgnoreCase))
                    {
                        conventionsFileName = Path.Combine(configurationSourceRoot.GetString(Constants.Configuration.ToolsDirectory), PathHelper.NormalizeFilePath(conventionsFileName.Mid(7)));
                    }

                    if (conventionsFileName.StartsWith("~/", StringComparison.OrdinalIgnoreCase))
                    {
                        conventionsFileName = Path.Combine(configurationSourceRoot.GetString(Constants.Configuration.ProjectDirectory), PathHelper.NormalizeFilePath(conventionsFileName.Mid(2)));
                    }

                    configurationSourceRoot.AddFile(conventionsFileName);
                }
            }

            var machineConfigFileName = Path.GetFileNameWithoutExtension(projectConfigFileName) + "." + Environment.MachineName + ".json";

            // add module configs (ignore machine config - it will be added last) - scconfig.[module].json 
            if ((options & ConfigurationOptions.IncludeModuleConfig) == ConfigurationOptions.IncludeModuleConfig)
            {
                foreach (var moduleFileName in Directory.GetFiles(projectDirectory, "scconfig.*.json").OrderBy(f => f))
                {
                    if (!string.Equals(moduleFileName, machineConfigFileName, StringComparison.OrdinalIgnoreCase))
                    {
                        configurationSourceRoot.AddFile(moduleFileName);
                    }
                }
            }

            // add machine level config file - scconfig.[machine name].json
            if ((options & ConfigurationOptions.IncludeMachineConfig) == ConfigurationOptions.IncludeMachineConfig)
            {
                if (File.Exists(machineConfigFileName))
                {
                    configurationSourceRoot.AddFile(machineConfigFileName);
                }
            }

            // add user config file - scconfig.json.user
            if ((options & ConfigurationOptions.IncludeUserConfig) == ConfigurationOptions.IncludeUserConfig)
            {
                var userConfigFileName = projectConfigFileName + ".user";
                if (File.Exists(userConfigFileName))
                {
                    configurationSourceRoot.AddFile(userConfigFileName, ".json");
                }
            }

            // add config file specified on the command line: /config myconfig.xml
            if ((options & ConfigurationOptions.IncludeCommandLineConfig) == ConfigurationOptions.IncludeCommandLineConfig)
            {
                var configName = configurationSourceRoot.Get(Constants.Configuration.CommandLineConfig);

                if (!string.IsNullOrEmpty(configName))
                {
                    var configFileName = PathHelper.Combine(projectDirectory, configName);
                    if (File.Exists(configFileName))
                    {
                        configurationSourceRoot.AddFile(configFileName);
                    }
                    else
                    {
                        throw new ConfigurationException(Texts.Config_file_not_found__ + configFileName);
                    }
                }
            }
        }
    }
}
