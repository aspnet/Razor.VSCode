/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { RazorCSharpFeature } from './CSharp/RazorCSharpFeature';
import { RazorLanguageServiceClient } from './RazorLanguageServiceClient';
import { LanguageKind } from './RPC/LanguageKind';

export class RazorLanguageFeatureBase {
    constructor(
        protected readonly csharpFeature: RazorCSharpFeature,
        protected readonly serviceClient: RazorLanguageServiceClient) {
    }

    protected async getProjection(document: vscode.TextDocument, position: vscode.Position) {
        const languageResponse = await this.serviceClient.languageQuery(position, document.uri);

        if (languageResponse.kind === LanguageKind.CSharp) {
            const projectionProvider = this.csharpFeature.projectionProvider;
            const projectedDocument = await projectionProvider.getDocument(document.uri);
            const projectedUri = projectedDocument.projectedUri;

            return { uri: projectedUri, position: languageResponse.position } as ProjectionResult;
        }

        return null;
    }
}

interface ProjectionResult {
    uri: vscode.Uri;
    position: vscode.Position;
}
