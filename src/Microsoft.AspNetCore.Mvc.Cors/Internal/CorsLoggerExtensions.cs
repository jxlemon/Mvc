// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Cors.Internal
{
    public static class CorsLoggerExtensions
    {
        private static readonly Action<ILogger, string, Exception> _notMostEffectiveFilter;

        static CorsLoggerExtensions()
        {
            _notMostEffectiveFilter = LoggerMessage.Define<string>(
               LogLevel.Trace,
               1,
               "Skipping the execution of current filter as its not the most effective filter implementing the policy {FilterPolicy}.");
        }

        public static void NotMostEffectiveFilter(this ILogger logger, string policyName)
        {
            _notMostEffectiveFilter(logger, policyName, null);
        }
    }
}
