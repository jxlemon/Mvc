// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class RazorProjectPageRouteModelProvider : IPageRouteModelProvider
    {
        private readonly RazorProject _project;
        private readonly RazorPagesOptions _pagesOptions;
        private readonly ILogger _logger;

        public RazorProjectPageRouteModelProvider(
            RazorProject razorProject,
            IOptions<RazorPagesOptions> pagesOptionsAccessor,
            ILoggerFactory loggerFactory)
        {
            _project = razorProject;
            _pagesOptions = pagesOptionsAccessor.Value;
            _logger = loggerFactory.CreateLogger<RazorProjectPageRouteModelProvider>();
        }
        
        /// <remarks>
        /// Ordered to execute after <see cref="CompiledPageRouteModelProvider"/>.
        /// </remarks>
        public int Order => -1000 + 10; 

        public void OnProvidersExecuted(PageRouteModelProviderContext context)
        {
        }

        public void OnProvidersExecuting(PageRouteModelProviderContext context)
        {
            foreach (var item in _project.EnumerateItems(_pagesOptions.RootDirectory))
            {
                if (item.FileName.StartsWith("_"))
                {
                    // Pages like _ViewImports should not be routable.
                    continue;
                }

                if (!PageDirectiveFeature.TryGetPageDirective(_logger, item, out var routeTemplate))
                {
                    // .cshtml pages without @page are not RazorPages.
                    continue;
                }

                if (IsAlreadyRegistered(context, item))
                {
                    // The CompiledPageRouteModelProvider (or another provider) already registered a PageRoute for this path.
                    // Don't register a duplicate entry for this route.
                    continue;
                }

                var routeModel = new PageRouteModel(
                    relativePath: item.CombinedPath,
                    viewEnginePath: item.FilePathWithoutExtension);
                PageSelectorModel.PopulateDefaults(routeModel, routeTemplate);

                context.RouteModels.Add(routeModel);
            }

            var areaRootDirectory = _pagesOptions.AreasRootDirectory;
            if (!areaRootDirectory.EndsWith("/", StringComparison.Ordinal))
            {
                areaRootDirectory = areaRootDirectory + "/";
            }
            foreach (var item in _project.EnumerateItems(_pagesOptions.AreasRootDirectory))
            {
                if (item.FileName.StartsWith("_"))
                {
                    // Pages like _ViewImports should not be routable.
                    continue;
                }

                if (!PageDirectiveFeature.TryGetPageDirective(_logger, item, out var routeTemplate))
                {
                    // .cshtml pages without @page are not RazorPages.
                    continue;
                }

                if (!TryParseAreaPath(item.FilePath, out var areaResult))
                {
                    continue;
                }

                var routeModel = new PageRouteModel(
                    relativePath: item.CombinedPath,
                    viewEnginePath: areaResult.viewEnginePath)
                {
                    RouteValues =
                    {
                        ["area"] = areaResult.areaName,
                    },
                };
                PageSelectorModel.PopulateDefaults(routeModel, routeTemplate);

                context.RouteModels.Add(routeModel);
            }
        }

        private bool IsAlreadyRegistered(PageRouteModelProviderContext context, RazorProjectItem projectItem)
        {
            for (var i = 0; i < context.RouteModels.Count; i++)
            {
                var routeModel = context.RouteModels[i];
                if (string.Equals(routeModel.ViewEnginePath, projectItem.FilePathWithoutExtension, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(routeModel.RelativePath, projectItem.CombinedPath, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryParseAreaPath(string path, out (string areaName, string viewEnginePath) result)
        {
            // rootDirectory = "/Areas/"
            // path = "/Products/Pages/Manage/Home.cshtml"
            // Result = ("Products", "/Products/Manage/Home")

            result = default;
            string areaName;
            var tokenizer = new StringTokenizer(new StringSegment(path, 1, path.Length - 1), new[] { '/' });
            using (var enumerator = tokenizer.GetEnumerator())
            {
                // Parse the area name
                if (!enumerator.MoveNext() || enumerator.Current.Length == 0)
                {
                    return false;
                }

                areaName = enumerator.Current.ToString();

                // Look for the "Pages" directory
                if (!enumerator.MoveNext() || enumerator.Current.Length == 0)
                {
                    return false;
                }

                if (!enumerator.Current.Equals("Pages", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning($"Page {path} is being ignored because it does not follow the pattern [AreaName]/Pages/[Path]");
                    return false;
                }

                // Parse the page path
                if (!enumerator.MoveNext() || enumerator.Current.Length == 0)
                {
                    return false;
                }
            }

            // "/Products/Pages/Manage/Home.cshtml".Length -> "/Products/Manage/Home"
            var length = path.Length;
            var endIndex = path.IndexOf('.');
            if (endIndex != -1)
            {
                length = endIndex;
            }

            var builder = new InplaceStringBuilder(length - "Pages/".Length);
            builder.Append('/');
            builder.Append(areaName);
            var offset = areaName.Length + "/Pages/".Length;
            builder.Append(path, offset, length - offset);

            result = (areaName, builder.ToString());
            return true;
        }
    }
}
