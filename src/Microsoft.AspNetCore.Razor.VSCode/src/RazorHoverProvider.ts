/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { RazorLanguageFeatureBase } from './RazorLanguageFeatureBase';

export class RazorHoverProvider
    extends RazorLanguageFeatureBase
    implements vscode.HoverProvider {

    public async provideHover(
        document: vscode.TextDocument, position: vscode.Position,
        token: vscode.CancellationToken) {

        const projection = await this.getProjection(document, position, token);
        if (!projection) {
            return;
        }
        const results = await vscode.commands.executeCommand<vscode.Hover[]>(
            'vscode.executeHoverProvider',
            projection.uri,
            projection.position);

        if (!results || results.length === 0) {
            return;
        }

        const applicableHover = results.filter(item => item.range)[0];
        if (!applicableHover) {
            return;
        }

        // Re-map the projected hover range to the host document range
        const remappedResponse = await this.serviceClient.mapToDocumentRange(
            projection.languageKind,
            applicableHover.range!,
            document.uri);

        if (document.version !== remappedResponse.hostDocumentVersion) {
            // This hover result is for a different version of the text document, bail.
            return;
        }

        const remappedResult = new vscode.Hover(
            applicableHover.contents,
            remappedResponse.range);
        return remappedResult;
    }
}
