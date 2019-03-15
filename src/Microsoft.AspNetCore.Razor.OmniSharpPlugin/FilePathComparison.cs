﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    internal static class FilePathComparison
    {
        private static StringComparison? _instance;

        public static StringComparison Instance
        {
            get
            {
                if (_instance == null && RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    _instance = StringComparison.Ordinal;
                }
                else if (_instance == null)
                {
                    _instance = StringComparison.OrdinalIgnoreCase;
                }

                return _instance.Value;
            }
        }
    }
}
