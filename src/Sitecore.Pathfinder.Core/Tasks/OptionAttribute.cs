﻿// © 2015-2017 by Jakob Christensen. All rights reserved.

using System;
using Sitecore.Pathfinder.Diagnostics;

namespace Sitecore.Pathfinder.Tasks
{
    [AttributeUsage(AttributeTargets.Property)]
    public class OptionAttribute : Attribute
    {
        public OptionAttribute([NotNull] string name)
        {
            Name = name;
        }

        [NotNull]
        public string Alias { get; set; } = string.Empty;

        [CanBeNull]
        public string ConfigurationName { get; set; } = string.Empty;

        [CanBeNull]
        public object DefaultValue { get; set; }

        public bool HasOptions { get; set; }

        [NotNull]
        public string HelpText { get; set; } = string.Empty;

        public bool IsRequired { get; set; } = false;

        [NotNull]
        public string Name { get; }

        public int PositionalArg { get; set; } = -1;

        [NotNull]
        public string PromptText { get; set; } = string.Empty;
    }
}
