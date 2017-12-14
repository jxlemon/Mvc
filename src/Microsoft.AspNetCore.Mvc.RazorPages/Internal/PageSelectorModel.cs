﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public static class PageSelectorModel
    {
        private const string IndexFileName = "Index.cshtml";

        public static void PopulateDefaults(PageRouteModel model, string pageRoute, string routeTemplate)
        {
            model.RouteValues.Add("page", model.ViewEnginePath);

            if (AttributeRouteModel.IsOverridePattern(routeTemplate))
            {
                throw new InvalidOperationException(string.Format(
                    Resources.PageActionDescriptorProvider_RouteTemplateCannotBeOverrideable,
                    model.RelativePath));
            }

            var selectorModel = CreateSelectorModel(pageRoute, routeTemplate);
            model.Selectors.Add(selectorModel);

            var fileName = Path.GetFileName(model.RelativePath);
            if (string.Equals(IndexFileName, fileName, StringComparison.OrdinalIgnoreCase))
            {
                // For pages ending in /Index.cshtml, we want to allow incoming routing, but
                // force outgoing routes to match to the path sans /Index.
                selectorModel.AttributeRouteModel.SuppressLinkGeneration = true;

                string parentDirectoryPath;
                var index = pageRoute.LastIndexOf('/');
                if (index == -1)
                {
                    parentDirectoryPath = string.Empty;
                }
                else
                {
                    parentDirectoryPath = pageRoute.Substring(0, index);
                }
                model.Selectors.Add(CreateSelectorModel(parentDirectoryPath, routeTemplate));
            }
        }

        private static SelectorModel CreateSelectorModel(string prefix, string template)
        {
            return new SelectorModel
            {
                AttributeRouteModel = new AttributeRouteModel
                {
                    Template = AttributeRouteModel.CombineTemplates(prefix, template),
                }
            };
        }

        public static bool TryParseAreaPath(
            RazorPagesOptions razorPagesOptions,
            string path,
            ILogger logger,
            out (string areaName, string viewEnginePath, string pageRoute) result)
        {
            // path = "/Products/Pages/Manage/Home.cshtml"
            // Result = ("Products", "/Manage/Home", "/Products/Manage/Home")

            result = default;
            Debug.Assert(path.StartsWith("/", StringComparison.Ordinal));

            var areaEndIndex = path.IndexOf('/', startIndex: 1);
            if (areaEndIndex == -1 || areaEndIndex == path.Length)
            {
                logger?.UnsupportedAreaPath(razorPagesOptions, path);
                return false;
            }

            // Normalize the pages root directory so that it has a 
            var normalizedPagesRootDirectory = razorPagesOptions.RootDirectory.TrimStart('/');
            if (!normalizedPagesRootDirectory.EndsWith("/", StringComparison.Ordinal))
            {
                normalizedPagesRootDirectory += "/";
            }

            if (string.Compare(path, areaEndIndex + 1, normalizedPagesRootDirectory, 0, normalizedPagesRootDirectory.Length, StringComparison.OrdinalIgnoreCase) != 0)
            {

                logger?.UnsupportedAreaPath(razorPagesOptions, path);
                return false;
            }

            var areaName = path.Substring(1, areaEndIndex - 1);

            var pagePathIndex = areaEndIndex + normalizedPagesRootDirectory.Length;
            var pageExtensionIndex = path.LastIndexOf('.');
            var pageExtensionLength = pageExtensionIndex == -1 || pageExtensionIndex < pagePathIndex ?
                0 :
                path.Length - pageExtensionIndex;

            // PageName
            var pageName = path.Substring(pagePathIndex, path.Length - pagePathIndex - pageExtensionLength);

            var builder = new InplaceStringBuilder(areaEndIndex + pageName.Length);
            builder.Append(path, 0, areaEndIndex);
            builder.Append(pageName);
            var pageRoute = builder.ToString();

            result = (areaName, pageName, pageRoute);
            return true;
        }
    }
}
