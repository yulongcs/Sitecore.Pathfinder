﻿// © 2015-2017 by Jakob Christensen. All rights reserved.

using System;
using System.Collections.Generic;
using System.Composition;
using Sitecore.Pathfinder.Compiling.Pipelines.CompilePipelines;
using Sitecore.Pathfinder.Configuration.ConfigurationModel;
using Sitecore.Pathfinder.Diagnostics;
using Sitecore.Pathfinder.Emitting;
using Sitecore.Pathfinder.Emitting.Emitters;
using Sitecore.Pathfinder.IO;
using Sitecore.Pathfinder.Languages.Media;
using Sitecore.Pathfinder.Projects;
using Sitecore.Pathfinder.Projects.Items;

namespace Sitecore.Pathfinder.Languages.Webdeploy
{
    [Export(typeof(IProjectEmitter)), Shared]
    public class WebdeployProjectEmitter : DirectoryProjectEmitterBase
    {
        [ImportingConstructor]
        public WebdeployProjectEmitter([NotNull] IConfiguration configuration, [NotNull] ITraceService trace, [ItemNotNull, NotNull, ImportMany] IEnumerable<IEmitter> emitters, [NotNull] IFileSystem fileSystem) : base(configuration, trace, emitters, fileSystem)
        {
        }

        public override bool CanEmit(string format)
        {
            return string.Equals(format, "webdeploy", StringComparison.OrdinalIgnoreCase);
        }


        public override void EmitItem(IEmitContext context, Item item)
        {
            var sourceBag = item as ISourcePropertyBag;

            if (!item.IsEmittable && sourceBag.GetValue<string>("__origin_reason") != nameof(CreateItemsFromTemplates))
            {
                return;
            }

            Trace.TraceInformation(Msg.I1011, "Publishing", item.ItemIdOrPath);


        }

        protected override void EmitMediaFile(IEmitContext context, MediaFile mediaFile)
        {
        }
    }
}
