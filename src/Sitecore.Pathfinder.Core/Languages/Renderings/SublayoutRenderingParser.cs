// � 2015-2017 Sitecore Corporation A/S. All rights reserved.

using System.Composition;
using Sitecore.Pathfinder.Configuration;
using Sitecore.Pathfinder.Configuration.ConfigurationModel;
using Sitecore.Pathfinder.Diagnostics;
using Sitecore.Pathfinder.Parsing;

namespace Sitecore.Pathfinder.Languages.Renderings
{
    [Export(typeof(IParser)), Shared]
    public class SublayoutRenderingParser : WebFormsRenderingParser
    {
        [ImportingConstructor]
        public SublayoutRenderingParser([NotNull] IConfiguration configuration, [NotNull] IFactory factory) : base(configuration, factory, ".ascx", Constants.Templates.SublayoutId)
        {
        }
    }
}
