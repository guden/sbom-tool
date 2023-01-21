﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Channels;
using Microsoft.Sbom.Api.Entities;
using Microsoft.Sbom.Api.Executors;
using Microsoft.Sbom.Extensions;
using Microsoft.Sbom.Common.Config;
using Serilog;
using Microsoft.Sbom.Api.Utils;
using System;

namespace Microsoft.Sbom.Api.Providers.FilesProviders
{
    /// <summary>
    /// Traverse a given folder recursively to generate a list of files to be serialized.
    /// </summary>
    public class DirectoryTraversingFileToJsonProvider : PathBasedFileToJsonProviderBase
    {
        private readonly DirectoryWalker directoryWalker;

        public DirectoryTraversingFileToJsonProvider(
            IConfiguration configuration,
            ChannelUtils channelUtils,
            ILogger log,
            FileHasher fileHasher,
            ManifestFolderFilterer fileFilterer,
            FileInfoWriter fileHashWriter,
            InternalSBOMFileInfoDeduplicator internalSBOMFileInfoDeduplicator,
            DirectoryWalker directoryWalker,
            IContext context)
            : base(configuration, channelUtils, log, fileHasher, fileFilterer, fileHashWriter, internalSBOMFileInfoDeduplicator, context)
        {
            this.directoryWalker = directoryWalker ?? throw new ArgumentNullException(nameof(directoryWalker));
        }

        public override bool IsSupported(ProviderType providerType)
        {
            if (providerType == ProviderType.Files)
            {
                // This is the last sources provider we should use, if no other sources have been provided by the user.
                // Thus, this condition should be to check that all the remaining configurations for file inputs are null.
                if (string.IsNullOrWhiteSpace(context.BuildListFile?.Value) && context.FilesList?.Value == null)
                {
                    Log.Debug($"Using the {nameof(DirectoryTraversingFileToJsonProvider)} provider for the files workflow.");
                    return true;
                }
            }

            return false;
        }

        protected override (ChannelReader<string> entities, ChannelReader<FileValidationResult> errors) GetSourceChannel()
        {
            return directoryWalker.GetFilesRecursively(context.BuildDropPath?.Value);
        }

        protected override (ChannelReader<JsonDocWithSerializer> results, ChannelReader<FileValidationResult> errors) WriteAdditionalItems(IList<ISbomConfig> requiredConfigs)
        {
            return (null, null);
        }
    }
}
