﻿// © 2015-2017 Sitecore Corporation A/S. All rights reserved.

using System.Composition;
using Sitecore.Pathfinder.Projects;
using Sitecore.Pathfinder.Projects.Files;

namespace Sitecore.Pathfinder.Emitting.Emitters.NugetEmitter
{
    [Export(typeof(IEmitter)), Shared]
    public class FileEmitter : EmitterBase
    {
        public FileEmitter() : base(Constants.Emitters.ContentFiles)
        {
        }

        public override bool CanEmit(IEmitContext context, IProjectItem projectItem)
        {
            return context.ProjectEmitter is NugetProjectEmitter && projectItem is File;
        }

        public override void Emit(IEmitContext context, IProjectItem projectItem)
        {
            var contentFile = (File)projectItem;
            var projectEmitter = (NugetProjectEmitter)context.ProjectEmitter;

            projectEmitter.EmitFile(context, projectItem.Snapshot.SourceFile.AbsoluteFileName, contentFile.FilePath);
        }
    }
}