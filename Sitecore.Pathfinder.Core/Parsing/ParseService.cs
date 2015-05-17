﻿namespace Sitecore.Pathfinder.Parsing
{
  using System;
  using System.Collections.Generic;
  using System.ComponentModel.Composition;
  using System.Linq;
  using Microsoft.Framework.ConfigurationModel;
  using Sitecore.Pathfinder.Diagnostics;
  using Sitecore.Pathfinder.Projects;
  using Sitecore.Pathfinder.TextDocuments;

  [Export(typeof(IParseService))]
  public class ParseService : IParseService
  {
    [ImportingConstructor]
    public ParseService([NotNull] ICompositionService compositionService, [NotNull] IConfiguration configuration, [NotNull] ITextDocumentService textDocumentService, [NotNull] ITextTokenService textTokenService)
    {
      this.CompositionService = compositionService;
      this.Configuration = configuration;
      this.TextDocumentService = textDocumentService;
      this.TextTokenService = textTokenService;
    }

    [NotNull]
    [ImportMany]
    public IEnumerable<IParser> Parsers { get; private set; }

    [NotNull]
    public ITraceService TraceService { get; set; }

    [NotNull]
    protected ICompositionService CompositionService { get; }

    [NotNull]
    protected IConfiguration Configuration { get; }

    [NotNull]
    protected ITextDocumentService TextDocumentService { get; }

    [NotNull]
    protected ITextTokenService TextTokenService { get; }

    public virtual void Parse(IProject project, ISourceFile sourceFile)
    {
      // todo: change to abstract factory pattern
      var context = new ParseContext(this.CompositionService, this.Configuration, this.TextDocumentService, this.TextTokenService).With(project, sourceFile);

      try
      {
        foreach (var parser in this.Parsers.OrderBy(c => c.Sortorder))
        {
          if (parser.CanParse(context))
          {
            parser.Parse(context);
          }
        }
      }
      catch (BuildException ex)
      {
        project.Trace.TraceError(Texts.Text3013, sourceFile.SourceFileName, ex.LineNumber, ex.LinePosition, ex.Message);
      }
      catch (Exception ex)
      {
        project.Trace.TraceError(Texts.Text3013, sourceFile.SourceFileName, 0, 0, ex.Message);
      }
    }
  }
}
