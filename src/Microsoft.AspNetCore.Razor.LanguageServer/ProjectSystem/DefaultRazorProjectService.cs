﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem
{
    internal class DefaultRazorProjectService : RazorProjectService
    {
        private readonly ProjectSnapshotManagerAccessor _projectSnapshotManagerAccessor;
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly HostDocumentFactory _hostDocumentFactory;
        private readonly ProjectResolver _projectResolver;
        private readonly DocumentVersionCache _documentVersionCache;
        private readonly FilePathNormalizer _filePathNormalizer;
        private readonly DocumentResolver _documentResolver;
        private readonly ILogger _logger;

        public DefaultRazorProjectService(
            ForegroundDispatcher foregroundDispatcher,
            HostDocumentFactory hostDocumentFactory,
            DocumentResolver documentResolver,
            ProjectResolver projectResolver,
            DocumentVersionCache documentVersionCache,
            FilePathNormalizer filePathNormalizer,
            ProjectSnapshotManagerAccessor projectSnapshotManagerAccessor,
            ILoggerFactory loggerFactory)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (hostDocumentFactory == null)
            {
                throw new ArgumentNullException(nameof(hostDocumentFactory));
            }

            if (documentResolver == null)
            {
                throw new ArgumentNullException(nameof(documentResolver));
            }

            if (projectResolver == null)
            {
                throw new ArgumentNullException(nameof(projectResolver));
            }

            if (documentVersionCache == null)
            {
                throw new ArgumentNullException(nameof(documentVersionCache));
            }

            if (filePathNormalizer == null)
            {
                throw new ArgumentNullException(nameof(filePathNormalizer));
            }

            if (projectSnapshotManagerAccessor == null)
            {
                throw new ArgumentNullException(nameof(projectSnapshotManagerAccessor));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _hostDocumentFactory = hostDocumentFactory;
            _documentResolver = documentResolver;
            _projectResolver = projectResolver;
            _documentVersionCache = documentVersionCache;
            _filePathNormalizer = filePathNormalizer;
            _projectSnapshotManagerAccessor = projectSnapshotManagerAccessor;
            _logger = loggerFactory.CreateLogger<DefaultRazorProjectService>();
        }

        public override void AddDocument(string filePath, TextLoader textLoader)
        {
            _foregroundDispatcher.AssertForegroundThread();

            var textDocumentPath = _filePathNormalizer.Normalize(filePath);
            if (_documentResolver.TryResolveDocument(textDocumentPath, out var _))
            {
                // Document already added. This usually occurs when VSCode has already pre-initialized
                // open documents and then we try to manually add all known razor documents.
                return;
            }

            if (!_projectResolver.TryResolvePotentialProject(textDocumentPath, out var projectSnapshot))
            {
                projectSnapshot = _projectResolver.GetMiscellaneousProject();
            }

            var hostDocument = _hostDocumentFactory.Create(textDocumentPath, projectSnapshot);
            var defaultProject = (DefaultProjectSnapshot)projectSnapshot;

            _logger.LogInformation($"Adding document '{textDocumentPath}' to project '{projectSnapshot.FilePath}'.");
            _projectSnapshotManagerAccessor.Instance.DocumentAdded(defaultProject.HostProject, hostDocument, textLoader);
        }

        public override void OpenDocument(string filePath, SourceText sourceText, long version)
        {
            _foregroundDispatcher.AssertForegroundThread();

            var textDocumentPath = _filePathNormalizer.Normalize(filePath);
            if (!_documentResolver.TryResolveDocument(textDocumentPath, out var _))
            {
                // Document hasn't been added. This usually occurs when VSCode trumps all other initialization 
                // processes and pre-initializes already open documents.
                AddDocument(filePath, textLoader: null);
            }

            if (!_projectResolver.TryResolvePotentialProject(textDocumentPath, out var projectSnapshot))
            {
                projectSnapshot = _projectResolver.GetMiscellaneousProject();
            }

            var defaultProject = (DefaultProjectSnapshot)projectSnapshot;

            _logger.LogInformation($"Opening document '{textDocumentPath}' in project '{projectSnapshot.FilePath}'.");
            _projectSnapshotManagerAccessor.Instance.DocumentOpened(defaultProject.HostProject.FilePath, textDocumentPath, sourceText);
            TrackDocumentVersion(textDocumentPath, version);

        }

        public override void CloseDocument(string filePath, TextLoader textLoader)
        {
            _foregroundDispatcher.AssertForegroundThread();

            var textDocumentPath = _filePathNormalizer.Normalize(filePath);
            if (!_projectResolver.TryResolvePotentialProject(textDocumentPath, out var projectSnapshot))
            {
                projectSnapshot = _projectResolver.GetMiscellaneousProject();
            }

            var defaultProject = (DefaultProjectSnapshot)projectSnapshot;
            _logger.LogInformation($"Closing document '{textDocumentPath}' in project '{projectSnapshot.FilePath}'.");
            _projectSnapshotManagerAccessor.Instance.DocumentClosed(defaultProject.HostProject.FilePath, textDocumentPath, textLoader);
        }

        public override void RemoveDocument(string filePath)
        {
            _foregroundDispatcher.AssertForegroundThread();

            var textDocumentPath = _filePathNormalizer.Normalize(filePath);
            if (!_projectResolver.TryResolvePotentialProject(textDocumentPath, out var projectSnapshot))
            {
                projectSnapshot = _projectResolver.GetMiscellaneousProject();
            }

            if (!projectSnapshot.DocumentFilePaths.Contains(textDocumentPath, FilePathComparer.Instance))
            {
                _logger.LogInformation($"Containing project is not tracking document '{filePath}");
                return;
            }

            var document = (DefaultDocumentSnapshot)projectSnapshot.GetDocument(textDocumentPath);
            var defaultProject = (DefaultProjectSnapshot)projectSnapshot;
            _logger.LogInformation($"Removing document '{textDocumentPath}' from project '{projectSnapshot.FilePath}'.");
            _projectSnapshotManagerAccessor.Instance.DocumentRemoved(defaultProject.HostProject, document.State.HostDocument);
        }

        public override void UpdateDocument(string filePath, SourceText sourceText, long version)
        {
            _foregroundDispatcher.AssertForegroundThread();

            var textDocumentPath = _filePathNormalizer.Normalize(filePath);
            if (!_projectResolver.TryResolvePotentialProject(textDocumentPath, out var projectSnapshot))
            {
                projectSnapshot = _projectResolver.GetMiscellaneousProject();
            }

            var defaultProject = (DefaultProjectSnapshot)projectSnapshot;
            _logger.LogTrace($"Updating document '{textDocumentPath}'.");
            _projectSnapshotManagerAccessor.Instance.DocumentChanged(defaultProject.HostProject.FilePath, textDocumentPath, sourceText);

            TrackDocumentVersion(textDocumentPath, version);
        }

        public override void AddProject(string filePath)
        {
            _foregroundDispatcher.AssertForegroundThread();

            var normalizedPath = _filePathNormalizer.Normalize(filePath);
            var hostProject = new HostProject(normalizedPath, RazorDefaults.Configuration, RazorDefaults.RootNamespace);
            _projectSnapshotManagerAccessor.Instance.ProjectAdded(hostProject);
            _logger.LogInformation($"Added project '{filePath}' to project system.");

            TryMigrateMiscellaneousDocumentsToProject();
        }

        public override void RemoveProject(string filePath)
        {
            _foregroundDispatcher.AssertForegroundThread();

            var normalizedPath = _filePathNormalizer.Normalize(filePath);
            var project = (DefaultProjectSnapshot)_projectSnapshotManagerAccessor.Instance.GetLoadedProject(normalizedPath);

            if (project == null)
            {
                // Never tracked the project to begin with, noop.
                return;
            }

            _logger.LogInformation($"Removing project '{filePath}' from project system.");
            _projectSnapshotManagerAccessor.Instance.ProjectRemoved(project.HostProject);

            TryMigrateDocumentsFromRemovedProject(project);
        }

        public override void UpdateProject(
            string filePath,
            RazorConfiguration configuration,
            string rootNamespace,
            ProjectWorkspaceState projectWorkspaceState)
        {
            _foregroundDispatcher.AssertForegroundThread();

            var normalizedPath = _filePathNormalizer.Normalize(filePath);
            var project = (DefaultProjectSnapshot)_projectSnapshotManagerAccessor.Instance.GetLoadedProject(normalizedPath);

            if (project == null)
            {
                // Never tracked the project to begin with, noop.
                _logger.LogInformation($"Failed to update untracked project '{filePath}'.");
                return;
            }

            if (!projectWorkspaceState.Equals(ProjectWorkspaceState.Default))
            {
                _logger.LogInformation($"Updating project '{filePath}' TagHelpers ({projectWorkspaceState.TagHelpers.Count}).");
            }

            _projectSnapshotManagerAccessor.Instance.ProjectWorkspaceStateChanged(project.FilePath, projectWorkspaceState);

            var currentHostProject = project.HostProject;
            var currentConfiguration = currentHostProject.Configuration;
            if (currentConfiguration.ConfigurationName == configuration?.ConfigurationName &&
                currentHostProject.RootNamespace == rootNamespace)
            {
                _logger.LogTrace($"Updating project '{filePath}'. The project is already using configuration '{configuration.ConfigurationName}' and root namespace '{rootNamespace}'.");
                return;
            }

            if (configuration == null)
            {
                configuration = RazorDefaults.Configuration;
                _logger.LogInformation($"Updating project '{filePath}' to use Razor's default configuration ('{configuration.ConfigurationName}')'.");
            }
            else if (currentConfiguration.ConfigurationName != configuration.ConfigurationName)
            {
                _logger.LogInformation($"Updating project '{filePath}' to Razor configuration '{configuration.ConfigurationName}' with language version '{configuration.LanguageVersion}'.");
            }

            if (currentHostProject.RootNamespace != rootNamespace)
            {
                _logger.LogInformation($"Updating project '{filePath}''s root namespace to '{rootNamespace}'.");
            }

            var hostProject = new HostProject(project.FilePath, configuration, rootNamespace);
            _projectSnapshotManagerAccessor.Instance.ProjectConfigurationChanged(hostProject);
        }

        // Internal for testing
        internal void TryMigrateDocumentsFromRemovedProject(ProjectSnapshot project)
        {
            _foregroundDispatcher.AssertForegroundThread();

            var miscellaneousProject = _projectResolver.GetMiscellaneousProject();

            foreach (var documentFilePath in project.DocumentFilePaths)
            {
                var documentSnapshot = (DefaultDocumentSnapshot)project.GetDocument(documentFilePath);

                if (!_projectResolver.TryResolvePotentialProject(documentFilePath, out var toProject))
                {
                    // This is the common case. It'd be rare for a project to be nested but we need to protect against it anyhow.
                    toProject = miscellaneousProject;
                }

                var textLoader = new DocumentSnapshotTextLoader(documentSnapshot);
                var defaultToProject = (DefaultProjectSnapshot)toProject;
                var oldHostDocument = documentSnapshot.State.HostDocument;
                var newHostDocument = _hostDocumentFactory.Create(documentSnapshot.FilePath, defaultToProject);

                _logger.LogInformation($"Migrating '{documentFilePath}' from the '{project.FilePath}' project to '{toProject.FilePath}' project.");
                _projectSnapshotManagerAccessor.Instance.DocumentAdded(defaultToProject.HostProject, newHostDocument, textLoader);
            }
        }

        // Internal for testing
        internal void TryMigrateMiscellaneousDocumentsToProject()
        {
            _foregroundDispatcher.AssertForegroundThread();

            var miscellaneousProject = _projectResolver.GetMiscellaneousProject();

            foreach (var documentFilePath in miscellaneousProject.DocumentFilePaths)
            {
                if (!_projectResolver.TryResolvePotentialProject(documentFilePath, out var projectSnapshot))
                {
                    continue;
                }

                var documentSnapshot = (DefaultDocumentSnapshot)miscellaneousProject.GetDocument(documentFilePath);

                // Remove from miscellaneous project
                var defaultMiscProject = (DefaultProjectSnapshot)miscellaneousProject;
                _projectSnapshotManagerAccessor.Instance.DocumentRemoved(defaultMiscProject.HostProject, documentSnapshot.State.HostDocument);

                // Add to new project

                var textLoader = new DocumentSnapshotTextLoader(documentSnapshot);
                var defaultProject = (DefaultProjectSnapshot)projectSnapshot;
                var newHostDocument = _hostDocumentFactory.Create(documentSnapshot.FilePath, defaultProject);
                _logger.LogInformation($"Migrating '{documentFilePath}' from the '{miscellaneousProject.FilePath}' project to '{projectSnapshot.FilePath}' project.");
                _projectSnapshotManagerAccessor.Instance.DocumentAdded(defaultProject.HostProject, newHostDocument, textLoader);
            }
        }

        private void TrackDocumentVersion(string textDocumentPath, long version)
        {
            if (!_documentResolver.TryResolveDocument(textDocumentPath, out var documentSnapshot))
            {
                return;
            }

            _documentVersionCache.TrackDocumentVersion(documentSnapshot, version);
        }

        private class DelegatingTextLoader : TextLoader
        {
            private readonly DocumentSnapshot _fromDocument;
            public DelegatingTextLoader(DocumentSnapshot fromDocument)
            {
                _fromDocument = fromDocument ?? throw new ArgumentNullException(nameof(fromDocument));
            }
            public override async Task<TextAndVersion> LoadTextAndVersionAsync(
               Workspace workspace,
               DocumentId documentId,
               CancellationToken cancellationToken)
            {
                var sourceText = await _fromDocument.GetTextAsync();
                var version = await _fromDocument.GetTextVersionAsync();
                var textAndVersion = TextAndVersion.Create(sourceText, version.GetNewerVersion());
                return textAndVersion;
            }
        }
    }
}
