// � 2015 Sitecore Corporation A/S. All rights reserved.

using System.Linq;
using Sitecore.Pathfinder.Tasks.Building;

namespace Sitecore.Pathfinder.Tasks
{
    public class InstallProject : WebBuildTaskBase
    {
        public InstallProject() : base("install-project")
        {
        }

        public override void Run(IBuildContext context)
        {
            if (context.Project.HasErrors)
            {
                context.Trace.TraceInformation(Msg.D1010, Texts.Package_contains_errors_and_will_not_be_deployed);
                context.IsAborted = true;
                return;
            }

            context.Trace.TraceInformation(Msg.D1011, Texts.Installing_project___);

            var webRequest = GetWebRequest(context).AsTask("InstallProject");

            var success = Post(context, webRequest);
            if (!success)
            {
                return;
            }

            foreach (var snapshot in context.Project.ProjectItems.SelectMany(i => i.Snapshots))
            {
                snapshot.SourceFile.IsModified = false;
            }
        }
    }
}
