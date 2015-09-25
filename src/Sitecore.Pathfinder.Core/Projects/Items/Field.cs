// � 2015 Sitecore Corporation A/S. All rights reserved.

using Sitecore.Pathfinder.Diagnostics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Sitecore.Pathfinder.Compiling.FieldCompilers;
using Sitecore.Pathfinder.Projects.Templates;
using Sitecore.Pathfinder.Snapshots;

namespace Sitecore.Pathfinder.Projects.Items
{
    // todo: consider basing this on ProjectElement
    [DebuggerDisplay("{GetType().Name,nq}: {FieldName,nq} = {Value}")]
    public class Field : IHasSourceTextNodes
    {
        public Field([NotNull] Item item, [NotNull] ITextNode textNode)
        {
            Item = item;

            SourceTextNodes.Add(textNode);

            ValueProperty.PropertyChanged += HandlePropertyChanged;
        }

        [NotNull]
        public string FieldName
        {
            get { return FieldNameProperty.GetValue(); }
            set { FieldNameProperty.SetValue(value); }
        }

        [NotNull]
        public SourceProperty<string> FieldNameProperty { get; } = new SourceProperty<string>("Name", string.Empty);

        public bool IsCompiled { get; set; }

        public bool IsTestable { get; set; } = true;

        [NotNull]
        public Item Item { get; set; }

        [NotNull]
        public string Language
        {
            get { return LanguageProperty.GetValue(); }
            set { LanguageProperty.SetValue(value); }
        }

        [NotNull]
        public SourceProperty<string> LanguageProperty { get; } = new SourceProperty<string>("Language", string.Empty);

        [NotNull]
        public string CompiledValue { get; private set; }

        [NotNull]
        [ItemNotNull]
        public ICollection<ITextNode> SourceTextNodes { get; } = new List<ITextNode>();

        [NotNull]
        public TemplateField TemplateField => Item.Template.Sections.SelectMany(s => s.Fields).FirstOrDefault(f => string.Compare(f.FieldName, FieldName, StringComparison.OrdinalIgnoreCase) == 0) ?? TemplateField.Empty;

        [NotNull]
        public string Value
        {
            get { return ValueProperty.GetValue(); }
            set { ValueProperty.SetValue(value); }
        }

        [NotNull]
        public string ValueHint
        {
            get { return ValueHintProperty.GetValue(); }
            set { ValueHintProperty.SetValue(value); }
        }

        [NotNull]
        public SourceProperty<string> ValueHintProperty { get; } = new SourceProperty<string>("Value.Hint", string.Empty);

        [NotNull]
        public SourceProperty<string> ValueProperty { get; } = new SourceProperty<string>("Value", string.Empty);

        public int Version
        {
            get { return VersionProperty.GetValue(); }
            set { VersionProperty.SetValue(value); }
        }

        [NotNull]
        public SourceProperty<int> VersionProperty { get; } = new SourceProperty<int>("Version", 0);

        public void Invalidate()
        {
            IsCompiled = false;
        }

        public void Compile([NotNull] IFieldCompileContext context)
        {
            if (IsCompiled)
            {
                return;
            }

            IsCompiled = true;
            CompiledValue = Value;

            foreach (var compiler in context.FieldCompilers.OrderBy(r => r.Priority))
            {
                if (compiler.CanCompile(context, this))
                {
                    CompiledValue = compiler.Compile(context, this);
                    break;
                }
            }
        }

        private void HandlePropertyChanged([NotNull] object sender, [NotNull] PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Value")
            {
                Invalidate();
            }
        }
    }
}