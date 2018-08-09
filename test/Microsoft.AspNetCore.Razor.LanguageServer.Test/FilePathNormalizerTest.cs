﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public class FilePathNormalizerTest
    {
        [Fact]
        public void Normalize_NullFilePath_ReturnsEmptyString()
        {
            // Arrange
            var filePathNormalizer = new FilePathNormalizer();

            // Act
            var normalized = filePathNormalizer.Normalize(null);

            // Assert
            Assert.Equal(string.Empty, normalized);
        }

        [Fact]
        public void Normalize_EmptyFilePath_ReturnsEmptyString()
        {
            // Arrange
            var filePathNormalizer = new FilePathNormalizer();

            // Act
            var normalized = filePathNormalizer.Normalize(null);

            // Assert
            Assert.Equal(string.Empty, normalized);
        }

        [Fact]
        public void Normalize_RemovesLeadingForwardSlash()
        {
            // Arrange
            var filePathNormalizer = new FilePathNormalizer();
            var filePath = "/path/to/document.cshtml";

            // Act
            var normalized = filePathNormalizer.Normalize(filePath);

            // Assert
            Assert.Equal("path/to/document.cshtml", normalized);
        }

        [Fact]
        public void Normalize_UrlDecodesFilePath()
        {
            // Arrange
            var filePathNormalizer = new FilePathNormalizer();
            var filePath = "C:/path%20to/document.cshtml";

            // Act
            var normalized = filePathNormalizer.Normalize(filePath);

            // Assert
            Assert.Equal("C:/path to/document.cshtml", normalized);
        }

        [Fact]
        public void Normalize_ReplacesBackSlashesWithForwardSlashes()
        {
            // Arrange
            var filePathNormalizer = new FilePathNormalizer();
            var filePath = "\\path\\to\\document.cshtml";

            // Act
            var normalized = filePathNormalizer.Normalize(filePath);

            // Assert
            Assert.Equal("path/to/document.cshtml", normalized);
        }
    }
}
