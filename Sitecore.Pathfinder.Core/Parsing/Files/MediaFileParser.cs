﻿namespace Sitecore.Pathfinder.Parsing.Files
{
  using System;
  using System.ComponentModel.Composition;
  using System.IO;
  using System.Linq;
  using Sitecore.Pathfinder.Projects.Files;
  using Sitecore.Pathfinder.Projects.Items;
  using Sitecore.Pathfinder.TreeNodes;

  [Export(typeof(IParser))]
  public class MediaFileParser : ParserBase
  {
    private static readonly string[] FileExtensions = {
      ".png", 
      ".gif", 
      ".bmp", 
      ".jpg", 
      ".jpeg", 
      ".docx", 
      ".doc", 
      ".pdf", 
      ".zip", 
    };

    public MediaFileParser() : base(Media)
    {
    }

    public override bool CanParse(IParseContext context)
    {
      var fileExtension = Path.GetExtension(context.Document.SourceFile.SourceFileName);
      return FileExtensions.Contains(fileExtension, StringComparer.OrdinalIgnoreCase);
    }

    public override void Parse(IParseContext context)
    {
      var mediaItem = new Item(context.Project, new TextSpan(context.Document));
      context.Project.Items.Add(mediaItem);

      // todo: set template
      mediaItem.ItemName = context.ItemName;
      mediaItem.DatabaseName = context.DatabaseName;
      mediaItem.ItemIdOrPath = context.ItemPath;
      mediaItem.IsEmittable = false;

      var mediaFile = new MediaFile(context.Project, new TextSpan(context.Document), mediaItem);
      context.Project.Items.Add(mediaFile);
    }
  }
}