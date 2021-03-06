// � 2015-2017 Sitecore Corporation A/S. All rights reserved.

using System.Collections.Generic;
using Sitecore.Pathfinder.Configuration;
using Sitecore.Pathfinder.Diagnostics;
using Sitecore.Pathfinder.Languages.Content;
using Sitecore.Pathfinder.Projects;
using Sitecore.Pathfinder.Snapshots;

namespace Sitecore.Pathfinder.Languages.Renderings
{
    public class Rendering : ContentFile
    {
        [FactoryConstructor]
        public Rendering([NotNull] IDatabase database, [NotNull] ISnapshot snapshot, [NotNull] string itemPath, [NotNull] string itemName, [NotNull] string filePath, [NotNull] string templateIdOrPath) : base(database.Project, snapshot, filePath)
        {
            Database = database;
            ItemPath = itemPath;
            ItemName = itemName;
            TemplateIdOrPath = templateIdOrPath;

            Placeholders = new LockableList<string>(this);
        }

        [NotNull]
        public IDatabase Database { get; }

        [NotNull]
        public string ItemName { get; }

        [NotNull]
        public string ItemPath { get; }

        [NotNull, ItemNotNull]
        public ICollection<string> Placeholders { get; }

        [NotNull]
        public IProjectItemUri RenderingItemUri { get; set; } = ProjectItemUri.Empty;

        [NotNull]
        public string TemplateIdOrPath { get; }
    }
}
