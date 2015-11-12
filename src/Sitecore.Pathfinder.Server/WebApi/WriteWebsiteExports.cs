﻿// © 2015 Sitecore Corporation A/S. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Mvc;
using System.Xml;
using Microsoft.Framework.ConfigurationModel;
using NuGet;
using Sitecore.Configuration;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.IO;
using Sitecore.Pathfinder.Diagnostics;
using Sitecore.Pathfinder.Extensions;
using Sitecore.Pathfinder.IO;
using Sitecore.SecurityModel;
using Sitecore.Zip;

namespace Sitecore.Pathfinder.WebApi
{
    public class WriteWebsiteExports : IWebApi
    {
        public ActionResult Execute(IAppService app)
        {
            TempFolder.EnsureFolder();

            var tempDirectory = Path.Combine(FileUtil.MapPath(TempFolder.Folder), "Pathfinder.Exports");
            if (Directory.Exists(tempDirectory))
            {
                FileUtil.DeleteDirectory(tempDirectory, true);
            }

            Directory.CreateDirectory(tempDirectory);

            var exportFileName = Path.Combine(FileUtil.MapPath(tempDirectory), "Pathfinder.Exports.zip");
            using (var zip = new ZipWriter(exportFileName))
            {
                foreach (var index in app.Configuration.GetSubKeys("write-website-exports"))
                {
                    var entryName = app.Configuration.GetString("write-website-exports:" + index.Key + ":filename");
                    var fileKey = "write-website-exports:" + index.Key + ":";

                    var fileName = Path.Combine(tempDirectory, PathHelper.NormalizeFilePath(entryName).TrimStart('\\'));

                    Directory.CreateDirectory(Path.GetDirectoryName(fileName) ?? string.Empty);

                    WriteFile(app.Configuration, tempDirectory, fileName, fileKey);

                    zip.AddEntry(entryName, fileName);
                }
            }

            return new FilePathResult(exportFileName, "application/zip");
        }

        protected virtual void CreateNugetPackage([Diagnostics.NotNull] string tempDirectory, [Diagnostics.NotNull] string fileName, [Diagnostics.NotNull] string sourceFileName)
        {
            var packageId = Path.GetFileNameWithoutExtension(fileName);

            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\"?>");
            sb.AppendLine("<package xmlns=\"http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd\">");
            sb.AppendLine("    <metadata>");
            sb.AppendLine("        <id>" + packageId + "</id>");
            sb.AppendLine("        <title>" + packageId + "</title>");
            sb.AppendLine("        <version>1.0.0</version>");
            sb.AppendLine("        <authors>Sitecore Pathfinder</authors>");
            sb.AppendLine("        <requireLicenseAcceptance>false</requireLicenseAcceptance>");
            sb.AppendLine("        <description>Generated by Sitecore Pathfinder</description>");
            sb.AppendLine("    </metadata>");
            sb.AppendLine("    <files>");
            sb.AppendLine("        <file src=\"" + sourceFileName + "\" target=\"content\\sitecore.project\\exports.xml\" />");
            sb.AppendLine("    </files>");
            sb.AppendLine("</package>");

            var stream = new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString()));
            var packageBuilder = new PackageBuilder(stream, tempDirectory);
            using (var nupkg = new FileStream(fileName, FileMode.Create))
            {
                packageBuilder.Save(nupkg);
            }
        }

        protected virtual void WriteFile([Diagnostics.NotNull] IConfiguration configuration, [Diagnostics.NotNull] string tempDirectory, [Diagnostics.NotNull] string fileName, [Diagnostics.NotNull] string fileKey)
        {
            var sourceFileName = Path.ChangeExtension(fileName, ".xml");
            using (var writer = new StreamWriter(sourceFileName))
            {
                var output = new XmlTextWriter(writer)
                {
                    Formatting = Formatting.Indented
                };

                output.WriteStartElement("Exports");

                foreach (var index in configuration.GetSubKeys(fileKey + "queries"))
                {
                    var key = fileKey + "queries:" + index.Key;

                    var queryText = configuration.GetString(key + ":query");

                    var databaseName = configuration.GetString(key + ":database");
                    var database = Factory.GetDatabase(databaseName);

                    var fieldToWrite = configuration.GetString(key + ":fields").Split(Constants.Comma, StringSplitOptions.RemoveEmptyEntries).Select(f => f.Trim().ToLowerInvariant()).ToList();

                    using (new SecurityDisabler())
                    {
                        foreach (var item in database.Query(queryText))
                        {
                            if (item.TemplateID == TemplateIDs.Template)
                            {
                                WriteTemplateAsExport(output, item);
                            }
                            else 
                            {
                                WriteItemAsExport(output, item, fieldToWrite);
                            }
                        }
                    }
                }

                output.WriteEndElement();
            }

            CreateNugetPackage(tempDirectory, fileName, sourceFileName);
        }

        protected virtual void WriteItemAsExport([Diagnostics.NotNull] XmlTextWriter output, [Diagnostics.NotNull] Item item, [ItemNotNull] [NotNull] IEnumerable<string> fieldsToWrite)
        {
            output.WriteStartElement("Item");
            output.WriteAttributeString("Id", item.ID.ToString());
            output.WriteAttributeString("Database", item.Database.Name);
            output.WriteAttributeString("Name", item.Name);
            output.WriteAttributeString("Path", item.Paths.Path);
            output.WriteAttributeString("Template", item.Template.InnerItem.Paths.Path);

            foreach (Field field in item.Fields)
            {
                if (!fieldsToWrite.Contains(field.Name.ToLowerInvariant()))
                {
                    continue;
                }

                output.WriteStartElement("Field");
                output.WriteAttributeString("Name", field.Name);
                output.WriteAttributeString("Value", field.Value);
                output.WriteEndElement();
            }

            output.WriteEndElement();
        }

        protected virtual void WriteTemplateAsExport([Diagnostics.NotNull] XmlTextWriter output, [Diagnostics.NotNull] Item templateItem)
        {
            output.WriteStartElement("Template");
            output.WriteAttributeString("Id", templateItem.ID.ToString());
            output.WriteAttributeString("Database", templateItem.Database.Name);
            output.WriteAttributeString("Name", templateItem.Name);
            output.WriteAttributeString("Path", templateItem.Paths.Path);
            output.WriteAttributeString("BaseTemplates", templateItem[FieldIDs.BaseTemplate]);

            var template = TemplateManager.GetTemplate(templateItem.ID, templateItem.Database);

            var templateFields = template.GetFields(false).ToList();

            foreach (var section in templateFields.Select(f => f.Section).Distinct().OrderBy(i => i.Sortorder).ThenBy(i => i.Key))
            {
                output.WriteStartElement("Section");
                output.WriteAttributeString("Name", section.Name);

                foreach (var field in section.GetFields().ToList().OrderBy(i => i.Sortorder).ThenBy(i => i.Key))
                {
                    output.WriteStartElement("Field");
                    output.WriteAttributeString("Name", field.Name);
                    output.WriteAttributeString("Type", field.Type);
                    output.WriteEndElement();
                }

                output.WriteEndElement();
            }

            output.WriteEndElement();
        }
    }
}
