﻿// © 2015-2017 Sitecore Corporation A/S. All rights reserved.

using System.Composition;
using Sitecore.Pathfinder.Compiling.Pipelines.CompilePipelines;
using Sitecore.Pathfinder.Projects;
using Sitecore.Pathfinder.Projects.Items;

namespace Sitecore.Pathfinder.Emitting.Emitters.SitecorePackageEmitter
{
    [Export(typeof(IEmitter)), Shared]
    public class ItemEmitter : EmitterBase
    {
        public ItemEmitter() : base(Constants.Emitters.Items)
        {
        }

        public override bool CanEmit(IEmitContext context, IProjectItem projectItem)
        {
            return context.ProjectEmitter is SitecorePackageProjectEmitter && projectItem is Item;
        }

        public override void Emit(IEmitContext context, IProjectItem projectItem)
        {
            var item = (Item)projectItem;
            var sourcePropertyBag = (ISourcePropertyBag)item;
            var projectEmitter = (SitecorePackageProjectEmitter)context.ProjectEmitter;

            if (item.IsEmittable || sourcePropertyBag.GetValue<string>("__origin_reason") == nameof(CreateItemsFromTemplates))
            {
                projectEmitter.AddItem(context, item);
            }
        }
    }
}