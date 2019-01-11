// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class DefaultTagHelperResolver : TagHelperResolver
    {
        private readonly TagHelperStore _tagHelperStore;

        public DefaultTagHelperResolver(TagHelperStore tagHelperStore)
        {
            if (tagHelperStore == null)
            {
                throw new ArgumentNullException(nameof(tagHelperStore));
            }

            _tagHelperStore = tagHelperStore;
        }

        public override Task<TagHelperResolutionResult> GetTagHelpersAsync(ProjectSnapshot project, CancellationToken cancellationToken = default)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            var tagHelpers = _tagHelperStore.GetTagHelpers(project.FilePath);
            var resolutionResult = new TagHelperResolutionResult(tagHelpers, Array.Empty<RazorDiagnostic>());
            return Task.FromResult(resolutionResult);
        }
    }
}
