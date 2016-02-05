﻿using Sitecore.Pathfinder.Extensibility;
using Sitecore.Pathfinder.Building;
using Sitecore.Pathfinder.Diagnostics;

namespace Sitecore.Pathfinder.Markdown
{
    public class MarkdownExtension : ExtensionBase
    {
        public override void RemoveWebsiteFiles([NotNull] IBuildContext context)
        {
            RemoveWebsiteAssembly(context, "Sitecore.Pathfinder.Markdown.dll");
            RemoveWebsiteAssembly(context, "MarkdownSharp.dll");
        }

        public override bool UpdateWebsiteFiles([NotNull] IBuildContext context)
        {
            var updated = false;

            updated |= CopyToolsFileToWebsiteBinDirectory(context, "Sitecore.Pathfinder.Markdown.dll");
            updated |= CopyToolsFileToWebsiteBinDirectory(context, "MarkdownSharp.dll");

            return updated;
        }
    }
}
