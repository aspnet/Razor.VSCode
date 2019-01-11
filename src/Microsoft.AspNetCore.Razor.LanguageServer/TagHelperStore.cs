// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal abstract class TagHelperStore
    {
        public abstract void UpdateTagHelpers(string projectPath, IReadOnlyList<TagHelperDescriptor> tagHelpers);

        public abstract IReadOnlyList<TagHelperDescriptor> GetTagHelpers(string projectPath);
    }
}
