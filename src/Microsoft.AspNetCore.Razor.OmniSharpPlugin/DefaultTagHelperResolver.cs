// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    [Shared]
    [Export(typeof(TagHelperResolver))]
    internal class DefaultTagHelperResolver : TagHelperResolver
    {
        public override async Task<IReadOnlyList<TagHelperDescriptor>> GetTagHelpersAsync(
            Project project,
            RazorProjectEngine engine,
            CancellationToken cancellationToken)
        {
            var providers = engine.Engine.Features.OfType<ITagHelperDescriptorProvider>().ToArray();
            if (providers.Length == 0)
            {
                return Array.Empty<TagHelperDescriptor>();
            }

            var results = new List<TagHelperDescriptor>();
            var context = TagHelperDescriptorProviderContext.Create(results);
            context.ExcludeHidden = true;
            context.IncludeDocumentation = true;

            var compilation = await project.GetCompilationAsync().ConfigureAwait(false);
            if (IsValidCompilation(compilation))
            {
                context.SetCompilation(compilation);
            }

            for (var i = 0; i < providers.Length; i++)
            {
                var provider = providers[i];
                provider.Execute(context);
            }

            return results;
        }

        private bool IsValidCompilation(Compilation compilation)
        {
            var type = typeof(CompilationTagHelperFeature);
            var isValidCompilationMethod = type.GetMethod("IsValidCompilation", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            var result = isValidCompilationMethod.Invoke(null, new object[] { compilation });

            return (bool)result;
        }
    }
}
