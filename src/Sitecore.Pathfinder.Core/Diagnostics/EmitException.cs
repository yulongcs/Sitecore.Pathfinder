﻿// © 2015-2017 Sitecore Corporation A/S. All rights reserved.

using System;
using Sitecore.Pathfinder.Snapshots;

namespace Sitecore.Pathfinder.Diagnostics
{
    public class EmitException : Exception
    {
        public EmitException(int msg, [Localizable(true), NotNull] string text) : base(text)
        {
            Msg = msg;
            Text = text;

            Details = string.Empty;
            FileName = string.Empty;
            Span = TextSpan.Empty;
            Details = string.Empty;
        }

        public EmitException(int msg, [Localizable(true), NotNull] string text, [NotNull] ISnapshot snapshot, [NotNull] string details = "") : base(text + (string.IsNullOrEmpty(details) ? ": " + details : string.Empty))
        {
            Msg = msg;
            Text = text;
            FileName = snapshot.SourceFile.AbsoluteFileName;
            Span = TextSpan.Empty;
            Details = details;
        }

        public EmitException(int msg, [Localizable(true), NotNull] string text, [NotNull] ITextNode textNode, [NotNull] string details = "") : base(text + (string.IsNullOrEmpty(details) ? ": " + details : string.Empty))
        {
            Msg = msg;
            Text = text;
            FileName = textNode.Snapshot.SourceFile.AbsoluteFileName;
            Span = textNode.TextSpan;
            Details = details;
        }

        [NotNull]
        public string Details { get; }

        [NotNull]
        public string FileName { get; }

        public int Msg { get; }

        public TextSpan Span { get; }

        [NotNull]
        public string Text { get; }
    }
}
