using System.Composition;
using Sitecore.Pathfinder.Configuration;
using Sitecore.Pathfinder.Diagnostics;
using Sitecore.Pathfinder.Extensibility.Pipelines;
using Sitecore.Pathfinder.Parsing;
using Sitecore.Pathfinder.Parsing.Items;
using Sitecore.Pathfinder.Parsing.References;
using Sitecore.Pathfinder.Snapshots;

namespace Sitecore.Pathfinder.Languages.Json
{
    [Export(typeof(ITextNodeParser)), Shared]
    public class JsonTemplateTextNodeParser : TemplateTextNodeParserBase
    {
        [ImportingConstructor]
        public JsonTemplateTextNodeParser([NotNull] IFactory factory, [NotNull] ITraceService trace, [NotNull] IPipelineService pipelines, [NotNull] IReferenceParserService referenceParser, [NotNull] ISchemaService schemaService) : base(factory, trace, pipelines, referenceParser, schemaService, Constants.TextNodeParsers.Templates)
        {
        }

        public override bool CanParse(ItemParseContext context, ITextNode textNode)
        {
            return textNode.Snapshot is JsonTextSnapshot && textNode.Key == "Template";
        }

        protected override ITextNode GetItemNameTextNode(IParseContext context, ITextNode textNode, string attributeName = "Name")
        {
            return !string.IsNullOrEmpty(textNode.Value) ? textNode : base.GetItemNameTextNode(context, textNode, attributeName);
        }
    }
}