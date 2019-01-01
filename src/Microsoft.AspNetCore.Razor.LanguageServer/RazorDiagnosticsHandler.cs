using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.Embedded.MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol.Client;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.AspNetCore.Razor.Language;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class RazorDiagnosticsHandler : IPublishDiagnosticsHandler
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly ProjectSnapshotManagerAccessor _projectSnapshotManagerAccessor;
        private readonly ILanguageServer _languageServer;

        public RazorDiagnosticsHandler(
            ForegroundDispatcher foregroundDispatcher,
            ProjectSnapshotManagerAccessor projectSnapshotManagerAccessor,
            ILanguageServer languageServer)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (projectSnapshotManagerAccessor == null)
            {
                throw new ArgumentNullException(nameof(projectSnapshotManagerAccessor));
            }

            if (languageServer == null)
            {
                throw new ArgumentNullException(nameof(languageServer));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _projectSnapshotManagerAccessor = projectSnapshotManagerAccessor;
            _languageServer = languageServer;
        }

        public async Task<Unit> Handle(PublishDiagnosticsParams request, CancellationToken cancellationToken)
        {
            var documents = await Task.Factory.StartNew(() =>
            {
                var openDocuments = new List<DocumentSnapshot>();
                foreach (var project in _projectSnapshotManagerAccessor.Instance.Projects)
                {
                    foreach (var documentFilePath in project.DocumentFilePaths)
                    {
                        if (_projectSnapshotManagerAccessor.Instance.IsDocumentOpen(documentFilePath))
                        {
                            var document = project.GetDocument(documentFilePath);
                            openDocuments.Add(document);
                        }
                    }
                }

                return openDocuments;
            },
            cancellationToken, TaskCreationOptions.None, _foregroundDispatcher.ForegroundScheduler);

            foreach (var document in documents)
            {
                var result = await document.GetGeneratedOutputAsync();
                var csharpDocument = result.GetCSharpDocument();
                var sourceText = await document.GetTextAsync();
                var documentDiagnostics = csharpDocument.Diagnostics
                    .Select(razorDiagnostic => ConvertDiagnostic(razorDiagnostic, sourceText));

                _languageServer.Document.PublishDiagnostics(new PublishDiagnosticsParams()
                {
                    Uri = new Uri(document.FilePath),
                    Diagnostics = new Container<Diagnostic>(documentDiagnostics),
                });
            }

            return Unit.Value;
        }

        private static Diagnostic ConvertDiagnostic(RazorDiagnostic razorDiagnostic, SourceText sourceText)
        {
            var diagnostic = new Diagnostic()
            {
                Message = razorDiagnostic.GetMessage(),
                Code = razorDiagnostic.Id,
                Severity = ConvertSeverity(razorDiagnostic.Severity),
                Range = ConvertSpanToRange(razorDiagnostic.Span, sourceText),
            };

            return diagnostic;
        }

        private static DiagnosticSeverity ConvertSeverity(RazorDiagnosticSeverity severity)
        {
            switch (severity)
            {
                case RazorDiagnosticSeverity.Error:
                    return DiagnosticSeverity.Error;
                default:
                    return DiagnosticSeverity.Information;
            }
        }

        private static Range ConvertSpanToRange(SourceSpan sourceSpan, SourceText sourceText)
        {
            var startPosition = sourceText.Lines.GetLinePosition(sourceSpan.AbsoluteIndex);
            var start = new Position()
            {
                Line = startPosition.Line,
                Character = startPosition.Character,
            };
            var endPosition = sourceText.Lines.GetLinePosition(sourceSpan.AbsoluteIndex + sourceSpan.Length);
            var end = new Position()
            {
                Line = endPosition.Line,
                Character = endPosition.Character,
            };
            var range = new Range()
            {
                Start = start,
                End = end,
            };

            return range;
        }
    }
}
