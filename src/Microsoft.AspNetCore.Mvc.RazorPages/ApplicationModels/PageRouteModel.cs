// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    /// <summary>
    /// A model component for routing RazorPages.
    /// </summary>
    public class PageRouteModel
    {
        /// <summary>
        /// Initializes a new instance of <see cref="PageRouteModel"/>.
        /// </summary>
        /// <param name="relativePath">The application relative path of the page.</param>
        /// <param name="viewEnginePath">The path relative to the base path for page discovery.</param>
        public PageRouteModel(string relativePath, string viewEnginePath)
        {
            RelativePath = relativePath ?? throw new ArgumentNullException(nameof(relativePath));
            ViewEnginePath = viewEnginePath ?? throw new ArgumentNullException(nameof(viewEnginePath));

            Properties = new Dictionary<object, object>();
            Selectors = new List<SelectorModel>();
            RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// A copy constructor for <see cref="PageRouteModel"/>.
        /// </summary>
        /// <param name="other">The <see cref="PageRouteModel"/> to copy from.</param>
        public PageRouteModel(PageRouteModel other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            RelativePath = other.RelativePath;
            ViewEnginePath = other.ViewEnginePath;

            Properties = new Dictionary<object, object>(other.Properties);
            Selectors = new List<SelectorModel>(other.Selectors.Select(m => new SelectorModel(m)));
            RouteValues = new Dictionary<string, string>(other.RouteValues, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets the application root relative path for the page.
        /// </summary>
        public string RelativePath { get; }

        /// <summary>
        /// Gets the path relative to the base path for page discovery.
        /// </summary>
        public string ViewEnginePath { get; }

        /// <summary>
        /// Stores arbitrary metadata properties associated with the <see cref="PageRouteModel"/>.
        /// </summary>
        public IDictionary<object, object> Properties { get; }

        /// <summary>
        /// Gets the <see cref="SelectorModel"/> instances.
        /// </summary>
        public IList<SelectorModel> Selectors { get; }

        public IDictionary<string, string> RouteValues { get; }
    }
}