﻿// © 2015-2016 Sitecore Corporation A/S. All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.Framework.ConfigurationModel;
using Sitecore.Pathfinder.Diagnostics;
using Sitecore.Pathfinder.Extensions;
using Sitecore.Pathfinder.Projects;

namespace Sitecore.Pathfinder.Checking
{
    [Export(typeof(ICheckerService))]
    public class CheckerService : ICheckerService
    {
        [ImportingConstructor]
        public CheckerService([NotNull] IConfiguration configuration, [NotNull] ICompositionService compositionService, [NotNull, ItemNotNull, ImportMany("Check")] IEnumerable<Func<ICheckerContext, IEnumerable<Diagnostic>>> checkers)
        {
            Configuration = configuration;
            CompositionService = compositionService;
            Checkers = checkers;
        }

        public int EnabledCheckersCount { get; protected set; }

        public IEnumerable<Func<ICheckerContext, IEnumerable<Diagnostic>>> Checkers { get; }

        [NotNull]
        protected ICompositionService CompositionService { get; }

        [NotNull]
        protected IConfiguration Configuration { get; }

        public virtual void CheckProject(IProject project)
        {
            var context = CompositionService.Resolve<ICheckerContext>().With(project);

            var checkers = GetEnabledCheckers();
            foreach (var checker in checkers)
            {
                var diagnostics = checker(context);

                foreach (var diagnostic in diagnostics)
                {
                    switch (diagnostic.Severity)
                    {
                        case Severity.Error:
                            context.Trace.TraceError(diagnostic.Msg, diagnostic.Text, diagnostic.FileName, diagnostic.Span);
                            break;
                        case Severity.Warning:
                            context.Trace.TraceWarning(diagnostic.Msg, diagnostic.Text, diagnostic.FileName, diagnostic.Span);
                            break;
                        case Severity.Information:
                            context.Trace.TraceInformation(diagnostic.Msg, diagnostic.Text, diagnostic.FileName, diagnostic.Span);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                EnabledCheckersCount++;

                if (context.IsAborted)
                {
                    break;
                }
            }
        }

        private void EnableCheckers([NotNull, ItemNotNull] IEnumerable<CheckerInfo> checkers, [NotNull] string configurationKey)
        {
            foreach (var pair in Configuration.GetSubKeys(configurationKey))
            {
                if (pair.Key == "based-on")
                {
                    var baseName = Constants.Configuration.ProjectRoleCheckers + ":" + pair.Key;
                    EnableCheckers(checkers, baseName);
                }
            }

            foreach (var pair in Configuration.GetSubKeys(configurationKey))
            {
                var key = pair.Key;
                var isEnabled = Configuration.GetString(configurationKey + ":" + key) == "enabled";

                if (key == "based-on")
                {
                    continue;
                }

                if (key == "*")
                {
                    foreach (var c in checkers)
                    {
                        c.IsEnabled = isEnabled;
                    }

                    continue;
                }

                if (!checkers.Any(c => c.Name == key || c.Category == key))
                {
                  throw new ConfigurationException("Checker not found: " + key);
                }

                foreach (var checker in checkers)
                {
                    if (checker.Name == key || checker.Category == key)
                    {
                        checker.IsEnabled = isEnabled;
                    }
                }
            }
        }

        [NotNull, ItemNotNull]
        private IEnumerable<Func<ICheckerContext, IEnumerable<Diagnostic>>> GetEnabledCheckers()
        {
            var checkers = Checkers.Select(c => new CheckerInfo(c)).ToList();

            // apply project roles
            var projectRoles = Configuration.GetCommaSeparatedStringList(Constants.Configuration.ProjectRole);
            foreach (var projectRole in projectRoles)
            {
                EnableCheckers(checkers, Constants.Configuration.ProjectRoleCheckers + ":" + projectRole);
            }

            // apply check-project:checkers
            EnableCheckers(checkers, Constants.Configuration.CheckProjectCheckers);

            // apply checkers
            EnableCheckers(checkers, Constants.Configuration.Checkers);

            return checkers.Where(c => c.IsEnabled).Select(c => c.Checker).ToList();
        }

        private class CheckerInfo
        {
            public CheckerInfo([NotNull] Func<ICheckerContext, IEnumerable<Diagnostic>> checker)
            {
                Checker = checker;

                Name = checker.Method.Name;
                Category = checker.Method.DeclaringType?.Name ?? string.Empty;

                if (Category.EndsWith("Checker"))
                {
                    Category = Category.Left(Category.Length - 7);
                }

                if (Category.EndsWith("Conventions"))
                {
                    Category = Category.Left(Category.Length - 11);
                }
            }

            [NotNull]
            public string Category { get; }

            [NotNull]
            public Func<ICheckerContext, IEnumerable<Diagnostic>> Checker { get; }

            public bool IsEnabled { get; set; } = true;

            [NotNull]
            public string Name { get; }
        }
    }
}
