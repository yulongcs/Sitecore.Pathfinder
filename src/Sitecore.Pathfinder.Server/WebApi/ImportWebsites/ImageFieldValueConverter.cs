﻿// © 2015 Sitecore Corporation A/S. All rights reserved.

using System;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Pathfinder.Languages;

namespace Sitecore.Pathfinder.WebApi.ImportWebsites
{
    public class ImageFieldValueConverter : FieldValueConverterBase
    {
        public override bool CanConvert(Field field, Item item, ILanguage language, string value)
        {
            return string.Equals(field.Type, "image", StringComparison.OrdinalIgnoreCase);
        }

        public override string Convert(Field field, Item item, ILanguage language, string value)
        {
            var imageField = new ImageField(field);
            return imageField.MediaItem?.Paths.Path ?? value;
        }
    }
}