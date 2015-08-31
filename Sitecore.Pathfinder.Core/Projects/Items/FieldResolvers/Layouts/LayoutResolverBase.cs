﻿// © 2015 Sitecore Corporation A/S. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Sitecore.Pathfinder.Extensions;
using Sitecore.Pathfinder.Projects.Layouts;
using Sitecore.Pathfinder.Projects.Templates;
using Sitecore.Pathfinder.Snapshots;
using Sitecore.Pathfinder.Text;

namespace Sitecore.Pathfinder.Projects.Items.FieldResolvers.Layouts
{
    public abstract class LayoutResolverBase
    {
        private static readonly List<string> IgnoreAttributes = new List<string>
        {
            "Cacheable",
            "DataSource",
            "Placeholder",
            "RenderingName",
            "VaryByData",
            "VaryByDevice",
            "VaryByLogin",
            "VaryByParameters",
            "VaryByQueryString",
            "VaryByUser"
        };

        [Diagnostics.NotNull]
        public virtual string Resolve([Diagnostics.NotNull] LayoutResolveContext context, [Diagnostics.NotNull] ITextNode textNode)
        {
            var writer = new StringWriter();
            var output = new XmlTextWriter(writer)
            {
                Formatting = Formatting.Indented
            };

            WriteLayout(context, output, textNode);

            return writer.ToString();
        }

        [Diagnostics.NotNull]
        protected virtual Rendering[] FindRenderingItems([Diagnostics.NotNull] IEnumerable<Rendering> renderingItems, [Diagnostics.NotNull] string renderingItemId)
        {
            var n = renderingItemId.LastIndexOf('.');
            if (n < 0)
            {
                return new Rendering[0];
            }

            renderingItemId = renderingItemId.Mid(n + 1);

            return renderingItems.Where(r => r.ShortName == renderingItemId).ToArray();
        }

        [Diagnostics.NotNull]
        protected virtual string GetPlaceholders([Diagnostics.NotNull] ITextNode renderingTextNode, [Diagnostics.NotNull] IProjectItem renderingItem)
        {
            return string.Empty;
        }

        protected virtual bool IsContentProperty([Diagnostics.NotNull] ITextNode renderingTextNode, [Diagnostics.NotNull] ITextNode childTextNode)
        {
            return childTextNode.Name.StartsWith(renderingTextNode.Name + ".");
        }

        [Diagnostics.NotNull]
        protected virtual Rendering[] ResolveRenderingItem([Diagnostics.NotNull] IEnumerable<Rendering> renderingItems, [Diagnostics.NotNull] string renderingItemId)
        {
            var path = "/" + renderingItemId.Replace(".", "/");

            return renderingItems.Where(r => r.Item.ItemIdOrPath.EndsWith(path, StringComparison.InvariantCultureIgnoreCase)).ToArray();
        }

        [Diagnostics.NotNull]
        protected virtual Rendering[] ResolveRenderingItemId([Diagnostics.NotNull] IEnumerable<Rendering> renderingItems, [Diagnostics.NotNull] string renderingItemId)
        {
            var matches = renderingItems.Where(r => r.ShortName == renderingItemId).ToArray();

            if (matches.Length == 0)
            {
                matches = FindRenderingItems(renderingItems, renderingItemId);
            }

            if (matches.Length > 1)
            {
                matches = ResolveRenderingItem(matches, renderingItemId);
            }

            return matches;
        }

        protected virtual void WriteBool([Diagnostics.NotNull] LayoutResolveContext context, [Diagnostics.NotNull] XmlTextWriter output, [Diagnostics.NotNull] ITextNode renderingTextNode, [Diagnostics.NotNull] string id, [Diagnostics.NotNull] string attributeName, [Diagnostics.NotNull] string name, bool ignoreValue = false)
        {
            var value = renderingTextNode.GetAttributeValue(attributeName);
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            if (value != "True" && value != "False")
            {
                context.Trace.TraceError(id + Texts.__Boolean_parameter_must_have_value__True__or__False_, renderingTextNode, attributeName);
                value = "False";
            }

            var b = string.Compare(value, "True", StringComparison.OrdinalIgnoreCase) == 0;
            if (b == ignoreValue)
            {
                return;
            }

            output.WriteAttributeString(name, b ? "1" : "0");
        }

        protected virtual void WriteDataSource(LayoutResolveContext context, [Diagnostics.NotNull] XmlTextWriter output, [Diagnostics.NotNull] ITextNode renderingTextNode)
        {
            var dataSource = renderingTextNode.GetAttributeValue("DataSource");
            if (string.IsNullOrEmpty(dataSource))
            {
                return;
            }

            var item = context.Project.FindQualifiedItem(dataSource);
            if (item == null)
            {
                context.Trace.Writeline(Texts.Datasource_not_found, dataSource);
                return;
            }

            output.WriteAttributeString("ds", item.Guid.Format());
        }

        protected virtual void WriteDevice([Diagnostics.NotNull] LayoutResolveContext context, [Diagnostics.NotNull] XmlTextWriter output, [Diagnostics.NotNull] IEnumerable<Rendering> renderingItems, [Diagnostics.NotNull] ITextNode deviceTextNode)
        {
            output.WriteStartElement("d");

            var deviceName = deviceTextNode.GetAttributeValue("Name");
            if (string.IsNullOrEmpty(deviceName))
            {
                context.Trace.TraceError(Texts.Device_element_is_missing__Name__attribute_, deviceTextNode);
            }
            else
            {
                // todo: get device from project
                var devices = context.Project.Items.OfType<ExternalReferenceItem>().Where(i => i.ItemIdOrPath.StartsWith("/sitecore/layout/Devices/")).ToList();
                if (!devices.Any())
                {
                    context.Trace.TraceError(Texts.Device_item_not_found, deviceTextNode);
                }
                else
                {
                    var device = devices.FirstOrDefault(d => string.Compare(d.ItemName, deviceName, StringComparison.OrdinalIgnoreCase) == 0);
                    if (device == null)
                    {
                        context.Trace.TraceError(Texts.Device_not_found, deviceTextNode, deviceName);
                    }
                    else
                    {
                        output.WriteAttributeString("id", device.Guid.Format());
                    }
                }
            }

            var layoutPlaceholders = string.Empty;
            var layoutPath = deviceTextNode.GetAttributeTextNode("Layout");
            if (layoutPath != null && !string.IsNullOrEmpty(layoutPath.Value))
            {
                var l = context.Project.FindQualifiedItem(layoutPath.Value) as ItemBase;
                if (l == null)
                {
                    context.Trace.Writeline(Texts.Layout_not_found_, layoutPath.Value);
                    return;
                }

                output.WriteAttributeString("l", l.Guid.Format());
                layoutPlaceholders = GetPlaceholders(deviceTextNode, l);
            }

            var renderings = context.Snapshot.GetJsonChildTextNode(deviceTextNode, "Renderings");
            if (renderings == null)
            {
                // silent
                return;
            }

            foreach (var renderingTextNode in renderings.ChildNodes)
            {
                WriteRendering(context, output, renderingItems, renderingTextNode, layoutPlaceholders);
            }

            output.WriteEndElement();
        }

        protected virtual void WriteLayout([Diagnostics.NotNull] LayoutResolveContext context, [Diagnostics.NotNull] XmlTextWriter output, [Diagnostics.NotNull] ITextNode layoutTextNode)
        {
            // todo: cache this in the build context
            // todo: use better search
            // todo: add renderings from project
            var renderingItems = context.Project.Items.OfType<Rendering>().ToList();

            output.WriteStartElement("r");

            var devices = context.Snapshot.GetJsonChildTextNode(layoutTextNode, "Devices");
            if (devices == null)
            {
                // silent
                return;
            }

            foreach (var deviceTextNode in devices.ChildNodes)
            {
                WriteDevice(context, output, renderingItems, deviceTextNode);
            }

            output.WriteEndElement();
        }

        protected virtual void WritePlaceholder([Diagnostics.NotNull] LayoutResolveContext context, [Diagnostics.NotNull] XmlTextWriter output, [Diagnostics.NotNull] ITextNode renderingTextNode, [Diagnostics.NotNull] string id, [Diagnostics.NotNull] string placeholders)
        {
            var placeholder = renderingTextNode.GetAttributeValue("Placeholder");

            if (string.IsNullOrEmpty(placeholder) && !string.IsNullOrEmpty(placeholders))
            {
                var n = placeholders.IndexOf(",", 1, StringComparison.InvariantCultureIgnoreCase);
                if (n >= 0)
                {
                    placeholder = placeholders.Mid(1, n - 1);
                }
            }

            if (string.IsNullOrEmpty(placeholder))
            {
                return;
            }

            if (!string.IsNullOrEmpty(placeholders))
            {
                if (placeholders.IndexOf("," + placeholder + ",", StringComparison.InvariantCultureIgnoreCase) < 0)
                {
                    context.Trace.TraceWarning(string.Format(Texts._2___Placeholder___0___is_not_defined_in_the_parent_rendering__Parent_rendering_has_these_placeholders___1__, placeholder, placeholders.Mid(1, placeholders.Length - 2), id), renderingTextNode, "Placeholder");
                }
            }

            output.WriteAttributeString("ph", placeholder);
        }

        protected virtual void WriteRendering([Diagnostics.NotNull] LayoutResolveContext context, [Diagnostics.NotNull] XmlTextWriter output, [Diagnostics.NotNull] IEnumerable<Rendering> renderingItems, [Diagnostics.NotNull] ITextNode renderingTextNode, [Diagnostics.NotNull] string placeholders)
        {
            string renderingItemId;

            if (renderingTextNode.Name == "r")
            {
                renderingItemId = renderingTextNode.GetAttributeValue("id");
            }
            else if (renderingTextNode.Name == "Rendering")
            {
                renderingItemId = renderingTextNode.GetAttributeValue("RenderingName");
            }
            else
            {
                renderingItemId = renderingTextNode.Name;
            }

            var id = renderingTextNode.GetAttributeValue("Id");
            if (string.IsNullOrEmpty(id))
            {
                id = renderingItemId;
            }

            if (string.IsNullOrEmpty(renderingItemId))
            {
                context.Trace.TraceError($"Unknown element \"{id}\".", renderingTextNode);
                return;
            }

            Rendering renderingItem;
            if (renderingItemId.IsGuid())
            {
                renderingItem = context.Project.Items.OfType<Rendering>().FirstOrDefault(i => i.Guid.Format() == renderingItemId);
            }
            else
            {
                var matches = ResolveRenderingItemId(renderingItems, renderingItemId);

                if (matches.Length == 0)
                {
                    context.Trace.TraceError($"Rendering \"{renderingItemId}\" not found.", renderingTextNode);
                    return;
                }

                if (matches.Length > 1)
                {
                    context.Trace.TraceError($"Ambiguous rendering match. {matches.Length} renderings match \"{renderingItemId}\".", renderingTextNode);
                    return;
                }

                renderingItem = matches[0];
            }

            if (renderingItem == null)
            {
                context.Trace.TraceError($"Rendering \"{renderingItemId}\" not found.", renderingTextNode);
                return;
            }

            output.WriteStartElement("r");

            WriteBool(context, output, renderingTextNode, id, "Cacheable", "cac");
            output.WriteAttributeString("id", renderingItem.Guid.Format());
            WriteDataSource(context, output, renderingTextNode);
            WriteParameters(context, output, renderingTextNode, renderingItem, id);
            WritePlaceholder(context, output, renderingTextNode, id, placeholders);

            // WriteAttributeStringNotEmpty(@"uid", this.UniqueId);
            WriteBool(context, output, renderingTextNode, id, "VaryByData", "vbd");
            WriteBool(context, output, renderingTextNode, id, "VaryByDevice", "vbdev");
            WriteBool(context, output, renderingTextNode, id, "VaryByLogin", "vbl");
            WriteBool(context, output, renderingTextNode, id, "VaryByParameters", "vbp");
            WriteBool(context, output, renderingTextNode, id, "VaryByQueryString", "vbqs");
            WriteBool(context, output, renderingTextNode, id, "VaryByUser", "vbu");

            output.WriteEndElement();

            /*
            if (renderingTextNode.ChildNodes.Any(child => !IsContentProperty(renderingTextNode, child)))
            {
                var placeHolders = renderingItem["Place Holders"];

                if (string.IsNullOrEmpty(placeHolders))
                {
                    context.Trace.TraceError($"The \"{renderingTextNode.Name}\" element cannot have any child elements as it does not define any placeholders in its 'Place Holders' field.", renderingTextNode);
                }
                else if (placeHolders.IndexOf("$Id", StringComparison.InvariantCulture) >= 0 && string.IsNullOrEmpty(renderingTextNode.GetAttributeValue("Id")))
                {
                    context.Trace.TraceError($"The \"{renderingTextNode.Name}\" element must have an ID as it has child elements.", renderingTextNode);
                }
            }
            */

            foreach (var child in renderingTextNode.ChildNodes)
            {
                if (IsContentProperty(renderingTextNode, child))
                {
                    continue;
                }

                WriteRendering(context, output, renderingItems, child, GetPlaceholders(renderingTextNode, renderingItem));
            }
        }

        private void WriteParameters([Diagnostics.NotNull] LayoutResolveContext context, [Diagnostics.NotNull] XmlTextWriter output, [Diagnostics.NotNull] ITextNode renderingTextNode, [Diagnostics.NotNull] Rendering renderingItem, [Diagnostics.NotNull] string id)
        {
            var fields = new Dictionary<string, string>();

            var parametersTemplateItemId = renderingItem.Item.Fields.FirstOrDefault(f => f.FieldName == "Parameters Template")?.Value.Value ?? string.Empty;
            var parametersTemplateItem = context.Project.FindQualifiedItem(parametersTemplateItemId) as Template;
            if (parametersTemplateItem != null)
            {
                foreach (var field in parametersTemplateItem.Sections.SelectMany(s => s.Fields))
                {
                    fields[field.FieldName.Value.ToLowerInvariant()] = field.Type.Value;
                }
            }

            var properties = new Dictionary<string, string>();

            foreach (var attribute in renderingTextNode.Attributes)
            {
                properties[attribute.Name] = attribute.Value;
            }

            foreach (var child in renderingTextNode.ChildNodes)
            {
                if (IsContentProperty(renderingTextNode, child))
                {
                    var name = child.Name.Mid(renderingTextNode.Name.Length + 1);
                    var value = string.Join(string.Empty, child.ChildNodes.Select(n => n.ToString()).ToArray());

                    properties[name] = value;
                }
            }

            var par = new UrlString();
            foreach (var pair in properties)
            {
                var attributeName = pair.Key;
                if (IgnoreAttributes.Contains(attributeName))
                {
                    continue;
                }

                var value = pair.Value;

                string type;
                if (fields.TryGetValue(attributeName.ToLowerInvariant(), out type))
                {
                    switch (type.ToLowerInvariant())
                    {
                        case "checkbox":
                            if (!value.StartsWith("{Binding") && !value.StartsWith("{@"))
                            {
                                if (value != "True" && value != "False")
                                {
                                    context.Trace.TraceError($"{id}: Boolean parameter must have value \"True\", \"False\", \"{{Binding ... }}\" or \"{{@ ... }}\".", renderingTextNode, attributeName);
                                }

                                value = value == "1" || string.Compare(value, "true", StringComparison.OrdinalIgnoreCase) == 0 ? "1" : "0";
                            }

                            break;
                    }
                }
                else
                {
                    context.Trace.TraceWarning(string.Format(Texts._1___Parameter___0___is_not_defined_in_the_parameters_template_, attributeName, id), renderingTextNode, attributeName);
                }

                if (value.StartsWith("/sitecore", StringComparison.InvariantCultureIgnoreCase))
                {
                    var item = context.Project.FindQualifiedItem(value);
                    if (item != null)
                    {
                        value = item.Guid.Format();
                    }
                }

                par[attributeName] = value;
            }

            output.WriteAttributeString("par", par.ToString());
        }
    }
}
