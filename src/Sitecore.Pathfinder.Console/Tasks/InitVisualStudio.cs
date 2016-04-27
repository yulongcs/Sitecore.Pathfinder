// � 2015 Sitecore Corporation A/S. All rights reserved.

using System.IO;
using Sitecore.Pathfinder.Tasks.Building;

namespace Sitecore.Pathfinder.Tasks
{
    public class InitVisualStudio : BuildTaskBase
    {
        public InitVisualStudio() : base("init-visualstudio")
        {
        }

        public override void Run(IBuildContext context)
        {
            var zipFileName = Path.Combine(context.ToolsDirectory, "files\\editors\\VisualStudio.Website.zip");

            if (!Directory.Exists(Path.Combine(context.ProjectDirectory, "node_modules\\grunt")))
            {
                context.Trace.WriteLine(Texts.Hey__GruntJS_has_not_yet_been_installed__Run_the_install_grunt_cmd_file_to_install_it_);
            }

            context.FileSystem.Unzip(zipFileName, context.ProjectDirectory);
        }
    }
}
