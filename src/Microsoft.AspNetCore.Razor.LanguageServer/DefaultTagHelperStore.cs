// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class DefaultTagHelperStore : TagHelperStore
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly ConcurrentDictionary<string, IReadOnlyList<TagHelperDescriptor>> _tagHelperMappings;

        public DefaultTagHelperStore(ForegroundDispatcher foregroundDispatcher)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _tagHelperMappings = new ConcurrentDictionary<string, IReadOnlyList<TagHelperDescriptor>>(FilePathComparer.Instance);
        }

        public override IReadOnlyList<TagHelperDescriptor> GetTagHelpers(string projectPath)
        {
            if (projectPath == null)
            {
                throw new ArgumentNullException(nameof(projectPath));
            }

            if (_tagHelperMappings.TryGetValue(projectPath, out var tagHelpers))
            {
                return tagHelpers;
            }
            return Array.Empty<TagHelperDescriptor>();
        }

        public override void UpdateTagHelpers(string projectPath, IReadOnlyList<TagHelperDescriptor> tagHelpers)
        {
            if (projectPath == null)
            {
                throw new ArgumentNullException(nameof(projectPath));
            }

            if (tagHelpers == null)
            {
                throw new ArgumentNullException(nameof(tagHelpers));
            }

            _foregroundDispatcher.AssertForegroundThread();

            _tagHelperMappings[projectPath] = tagHelpers;
        }
    }
}
