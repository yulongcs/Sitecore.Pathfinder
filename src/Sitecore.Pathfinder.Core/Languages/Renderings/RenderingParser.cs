﻿// © 2015-2017 Sitecore Corporation A/S. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using Sitecore.Pathfinder.Configuration;
using Sitecore.Pathfinder.Configuration.ConfigurationModel;
using Sitecore.Pathfinder.Diagnostics;
using Sitecore.Pathfinder.Extensions;
using Sitecore.Pathfinder.IO;
using Sitecore.Pathfinder.Parsing;

namespace Sitecore.Pathfinder.Languages.Renderings
{
    public abstract class RenderingParser : ParserBase
    {
        protected RenderingParser([NotNull] IConfiguration configuration, [NotNull] IFactory factory, [NotNull] string fileExtension, [NotNull] string templateIdOrPath) : base(Constants.Parsers.Renderings)
        {
            Configuration = configuration;
            Factory = factory;
            FileExtension = fileExtension;
            TemplateIdOrPath = templateIdOrPath;
        }

        protected RenderingParser([NotNull] IConfiguration configuration, [NotNull] IFactory factory, [NotNull] string fileExtension, [NotNull] string templateIdOrPath, double priority) : base(priority)
        {
            Configuration = configuration;
            Factory = factory;
            FileExtension = fileExtension;
            TemplateIdOrPath = templateIdOrPath;
        }

        [NotNull]
        public IFactory Factory { get; }

        [NotNull]
        public string TemplateIdOrPath { get; }

        [NotNull]
        protected IConfiguration Configuration { get; }

        [NotNull]
        protected string FileExtension { get; }

        public override bool CanParse(IParseContext context)
        {
            if (string.IsNullOrEmpty(context.FilePath))
            {
                return false;
            }

            var extension = PathHelper.GetExtension(context.Snapshot.SourceFile.AbsoluteFileName);
            return string.Equals(extension, FileExtension, StringComparison.OrdinalIgnoreCase);
        }

        public override void Parse(IParseContext context)
        {
            // check if creating items for partial views (file name starts with '_')
            if (Path.GetFileName(context.FilePath).StartsWith("_") && !Configuration.GetBool(Constants.Configuration.BuildProject.Renderings.CreateItemsForPartialViews))
            {
                return;
            }

            var rendering = Factory.Rendering(context.Database, context.Snapshot, context.ItemPath, context.ItemName, context.FilePath, TemplateIdOrPath);
            context.Project.AddOrMerge(rendering);

            var contents = context.Snapshot.SourceFile.ReadAsText();

            var placeholders = GetPlaceholders(contents);

            rendering.Placeholders.AddRange(placeholders);

            context.Project.Ducats += 100;
        }

        [NotNull, ItemNotNull]
        protected abstract IEnumerable<string> GetPlaceholders([NotNull] string contents);
    }
}
