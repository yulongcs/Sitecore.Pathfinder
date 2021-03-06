// � 2015-2017 Sitecore Corporation A/S. All rights reserved.

using System;
using System.Composition;
using System.Linq;
using Sitecore.Pathfinder.Configuration;
using Sitecore.Pathfinder.Diagnostics;
using Sitecore.Pathfinder.Extensibility.Pipelines;
using Sitecore.Pathfinder.Extensions;
using Sitecore.Pathfinder.Projects;
using Sitecore.Pathfinder.Projects.Items;
using Sitecore.Pathfinder.Text;

namespace Sitecore.Pathfinder.Compiling.Pipelines.CompilePipelines
{
    [Export(typeof(IPipelineProcessor)), Shared]
    public class CreateMissingParentItems : PipelineProcessorBase<CompilePipeline>
    {
        [ImportingConstructor]
        public CreateMissingParentItems([NotNull] IFactory factory, [NotNull] ITraceService trace) : base(1600)
        {
            Factory = factory;
            Trace = trace;
        }

        [NotNull]
        protected IFactory Factory { get; }

        [NotNull]
        protected ITraceService Trace { get; }

        protected override void Process(CompilePipeline pipeline)
        {
            // todo: consider if imports should be omitted or not
            var items = pipeline.Context.Project.Items.ToList();

            foreach (var item in items)
            {
                var parent = item.GetParent();
                if (parent != null)
                {
                    continue;
                }

                var path = item.ItemIdOrPath;
                if (path.IndexOf('/') < 0)
                {
                    Trace.TraceWarning(Msg.C1134, "Item has no parent and no path", path);
                    continue;
                }

                if (path == "/")
                {
                    path = "/sitecore";
                }

                var parts = path.Split(Constants.Slash, StringSplitOptions.RemoveEmptyEntries);
                for (var index = parts.Length - 2; index >= 0; index--)
                {
                    var partialPath = "/" + string.Join("/", parts.Take(index + 1));
                    parent = item.Database.FindQualifiedItem<Item>(partialPath);

                    if (parent == null)
                    {
                        continue;
                    }

                    var newPath = partialPath;
                    for (var newIndex = index + 1; newIndex < parts.Length - 1; newIndex++)
                    {
                        newPath += "/" + parts[newIndex];
                        var guid = StringHelper.GetGuid(pipeline.Context.Project, newPath);

                        var newItem = Factory.Item(item.Database, guid, parts[newIndex], newPath, Constants.Templates.Folder.Format());
                        newItem.IsEmittable = false;
                        newItem.IsImport = item.IsImport;
                        newItem.IsSynthetic = true;
                        newItem.OverwriteWhenMerging = true;
                        pipeline.Context.Project.AddOrMerge(newItem);

                        ((ISourcePropertyBag)newItem).NewSourceProperty("__origin", item.Uri);
                        ((ISourcePropertyBag)newItem).NewSourceProperty("__origin_reason", nameof(CreateMissingParentItems));
                    }

                    break;
                }
            }
        }
    }
}
