// � 2015-2017 Sitecore Corporation A/S. All rights reserved.

using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Linq;
using Sitecore.Pathfinder.Configuration;
using Sitecore.Pathfinder.Configuration.ConfigurationModel;
using Sitecore.Pathfinder.Diagnostics;
using Sitecore.Pathfinder.Extensibility.Pipelines;
using Sitecore.Pathfinder.Extensions;
using Sitecore.Pathfinder.IO;
using Sitecore.Pathfinder.Parsing.Pipelines.ReferenceParserPipelines;
using Sitecore.Pathfinder.Projects;
using Sitecore.Pathfinder.Projects.Items;
using Sitecore.Pathfinder.Projects.References;
using Sitecore.Pathfinder.Snapshots;
using Sitecore.Pathfinder.Text;

namespace Sitecore.Pathfinder.Parsing.References
{
    [Export(typeof(IReferenceParserService))]
    public class ReferenceParserService : IReferenceParserService
    {
        [CanBeNull, ItemNotNull]
        private List<Tuple<string, string>> _ignoreReferences;

        [ImportingConstructor]
        public ReferenceParserService([NotNull] IConfiguration configuration, [NotNull] IFactory factory, [NotNull] IPipelineService pipelines)
        {
            Configuration = configuration;
            Factory = factory;
            Pipelines = pipelines;
        }

        [NotNull]
        protected IConfiguration Configuration { get; }

        [NotNull]
        protected IFactory Factory { get; }

        [NotNull]
        protected IPipelineService Pipelines { get; }

        public virtual bool IsIgnoredReference(string referenceText)
        {
            if (_ignoreReferences == null)
            {
                _ignoreReferences = GetIgnoredReferences();
            }

            foreach (var pair in _ignoreReferences)
            {
                switch (pair.Item2)
                {
                    case "starts-with":
                        if (referenceText.StartsWith(pair.Item1, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }

                        break;
                    case "ends-with":
                        if (referenceText.EndsWith(pair.Item1, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }

                        break;
                    case "contains":
                        if (referenceText.IndexOf(pair.Item1, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return true;
                        }

                        break;

                    default:
                        if (string.Equals(referenceText, pair.Item1, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }

                        break;
                }
            }

            return false;
        }

        public IEnumerable<IReference> ParseReferences(Field field)
        {
            var databaseName = field.DatabaseName;
            var value = field.Value;

            // todo: templates may not be loaded at this point

            // look for database name
            if (field.TemplateField.Source.IndexOf("database", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var parameters = new UrlString(field.TemplateField.Source);

                databaseName = parameters["databasename"] ?? string.Empty;
                if (string.IsNullOrEmpty(databaseName))
                {
                    databaseName = parameters["database"] ?? string.Empty;
                }
            }

            // check for fields that contains paths
            var pathFields = Configuration.GetArray(Constants.Configuration.CheckProject.PathFields);
            if (pathFields.Contains(field.FieldId.Format()))
            {
                var sourceProperty = field.ValueProperty;
                if (sourceProperty.SourceTextNode == TextNode.Empty)
                {
                    sourceProperty = field.FieldNameProperty;
                }

                var referenceText = PathHelper.NormalizeItemPath(value);
                if (!referenceText.StartsWith("~/"))
                {
                    referenceText = "~/" + referenceText.TrimStart('/');
                }

                yield return Factory.FileReference(field.Item, sourceProperty, referenceText);

                yield break;
            }

            var database = field.Item.Project.GetDatabase(databaseName);
            var textNode = TraceHelper.GetTextNode(field.ValueProperty, field.FieldNameProperty, field);
            foreach (var reference in ParseReferences(field.Item, textNode, field.Value, database))
            {
                yield return reference;
            }
        }

        public virtual IEnumerable<IReference> ParseReferences<T>(IProjectItem projectItem, SourceProperty<T> sourceProperty)
        {
            var sourceTextNode = sourceProperty.SourceTextNode;
            return ParseReferences(projectItem, sourceTextNode, sourceTextNode.Value, Database.Empty);
        }

        public virtual IEnumerable<IReference> ParseReferences(IProjectItem projectItem, ITextNode textNode)
        {
            var referenceText = textNode.Value.Trim();

            return ParseReferences(projectItem, textNode, referenceText, Database.Empty);
        }

        protected virtual int EndOfFilePath([NotNull] string text, int start)
        {
            var chars = text.ToCharArray();
            var invalidChars = Path.GetInvalidPathChars();

            for (var i = start; i < text.Length; i++)
            {
                var c = chars[i];
                if (invalidChars.Contains(c))
                {
                    return i;
                }
            }

            return text.Length;
        }

        protected virtual int EndOfItemPath([NotNull] string text, int start)
        {
            var chars = text.ToCharArray();

            for (var i = start; i < text.Length; i++)
            {
                var c = chars[i];
                if (!char.IsDigit(c) && !char.IsLetter(c) && c != '/' && c != ' ' && c != '-' && c != '.' && c != '_' && c != '~')
                {
                    return i;
                }
            }

            return text.Length;
        }

        [NotNull, ItemNotNull]
        protected virtual List<Tuple<string, string>> GetIgnoredReferences()
        {
            var ignoreReferences = new List<Tuple<string, string>>();

            foreach (var pair in Configuration.GetSubKeys(Constants.Configuration.CheckProject.IgnoredReferences))
            {
                var op = Configuration.Get(Constants.Configuration.CheckProject.IgnoredReferences + ":" + pair.Key);
                ignoreReferences.Add(new Tuple<string, string>(pair.Key, op));
            }

            return ignoreReferences;
        }

        [ItemNotNull, NotNull]
        protected virtual IEnumerable<IReference> ParseFilePaths([NotNull] IProjectItem projectItem, [NotNull] ITextNode textNode, [NotNull] string referenceText, [NotNull] IDatabase database)
        {
            var s = 0;
            while (true)
            {
                var n = referenceText.IndexOf("~/", s, StringComparison.Ordinal);
                if (n < 0)
                {
                    break;
                }

                var e = EndOfFilePath(referenceText, n);
                var text = referenceText.Mid(n, e - n);

                var reference = ParseReference(projectItem, textNode, text, database);
                if (reference != null)
                {
                    yield return reference;
                }

                s = e;
            }
        }

        [ItemNotNull, NotNull]
        protected virtual IEnumerable<IReference> ParseGuids([NotNull] IProjectItem projectItem, [NotNull] ITextNode textNode, [NotNull] string referenceText, [NotNull] IDatabase database)
        {
            var s = 0;
            while (true)
            {
                var n = referenceText.IndexOf('{', s);
                if (n < 0)
                {
                    break;
                }

                // ignore uids
                if (n > 4 && referenceText.Mid(n - 5, 5) == "uid=\"")
                {
                    s = n + 1;
                    continue;
                }

                // ignore {{
                if (referenceText.Mid(n, 2) == "{{")
                {
                    s = n + 2;
                    continue;
                }

                // ignore \{
                if (referenceText.Mid(n, 2) == "\\{")
                {
                    s = n + 2;
                    continue;
                }

                var e = referenceText.IndexOf('}', n);
                if (e < 0)
                {
                    break;
                }

                e++;

                var text = referenceText.Mid(n, e - n);

                var reference = ParseReference(projectItem, textNode, text, database);
                if (reference != null)
                {
                    yield return reference;
                }

                s = e;
            }
        }

        [ItemNotNull, NotNull]
        protected virtual IEnumerable<IReference> ParseItemPaths([NotNull] IProjectItem projectItem, [NotNull] ITextNode textNode, [NotNull] string referenceText, [NotNull] IDatabase database)
        {
            var s = 0;
            while (true)
            {
                var n = referenceText.IndexOf("/sitecore", s, StringComparison.OrdinalIgnoreCase);
                if (n < 0)
                {
                    break;
                }

                var e = EndOfItemPath(referenceText, n);
                var text = referenceText.Mid(n, e - n);

                var reference = ParseReference(projectItem, textNode, text, database);
                if (reference != null)
                {
                    yield return reference;
                }

                s = e;
            }
        }

        [CanBeNull]
        protected virtual IReference ParseReference([NotNull] IProjectItem projectItem, [NotNull] ITextNode sourceTextNode, [NotNull] string referenceText, [NotNull] IDatabase database)
        {
            if (IsIgnoredReference(referenceText))
            {
                return null;
            }

            var pipeline = Pipelines.GetPipeline<ReferenceParserPipeline>().Execute(projectItem, sourceTextNode, referenceText, database);
            return pipeline.Reference;
        }

        [ItemNotNull, NotNull]
        protected virtual IEnumerable<IReference> ParseReferences([NotNull] IProjectItem projectItem, [NotNull] ITextNode textNode, [NotNull] string referenceText, [NotNull] IDatabase database)
        {
            // query string: ignore
            if (referenceText.StartsWith("query:"))
            {
                yield break;
            }

            // todo: process media links 
            if (referenceText.StartsWith("/~/media") || referenceText.StartsWith("~/media"))
            {
                yield break;
            }

            // todo: process icon links 
            if (referenceText.StartsWith("/~/icon") || referenceText.StartsWith("~/icon"))
            {
                yield break;
            }

            foreach (var reference in ParseItemPaths(projectItem, textNode, referenceText, database))
            {
                yield return reference;
            }

            foreach (var reference in ParseGuids(projectItem, textNode, referenceText, database))
            {
                yield return reference;
            }

            foreach (var reference in ParseFilePaths(projectItem, textNode, referenceText, database))
            {
                yield return reference;
            }
        }
    }
}
