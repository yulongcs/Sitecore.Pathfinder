﻿// © 2015-2017 Sitecore Corporation A/S. All rights reserved.

using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using Sitecore.Pathfinder.Configuration.ConfigurationModel;
using Sitecore.Pathfinder.Diagnostics;
using Sitecore.Pathfinder.Emitting;
using Sitecore.Pathfinder.Emitting.Emitters;
using Sitecore.Pathfinder.IO;
using Sitecore.Pathfinder.Projects.Items;

namespace Sitecore.Pathfinder.Languages.Yaml
{
    [Export(typeof(IProjectEmitter)), Shared]
    public class YamlProjectEmitter : DirectoryProjectEmitterBase
    {
        [ImportingConstructor]
        public YamlProjectEmitter([NotNull] IConfiguration configuration, [NotNull] ITraceService trace, [ItemNotNull, NotNull, ImportMany] IEnumerable<IEmitter> emitters, [NotNull] IFileSystem fileSystem) : base(configuration, trace, emitters, fileSystem)
        {
        }

        public override bool CanEmit(string format)
        {
            return string.Equals(format, "yaml", StringComparison.OrdinalIgnoreCase);
        }

        public override void EmitItem(IEmitContext context, Item item)
        {
            if (!item.IsEmittable)
            {
                return;
            }

            Trace.TraceInformation(Msg.I1011, "Publishing", item.ItemIdOrPath);

            var destinationFileName = PathHelper.Combine(OutputDirectory, PathHelper.NormalizeFilePath(item.ItemIdOrPath).TrimStart('\\'));

            destinationFileName += ".content.yaml";

            FileSystem.CreateDirectoryFromFileName(destinationFileName);

            using (var stream = new FileStream(destinationFileName, FileMode.Create))
            {
                using (var writer = new StreamWriter(stream))
                {
                    item.WriteAsYaml(writer);
                }
            }
        }
    }
}
