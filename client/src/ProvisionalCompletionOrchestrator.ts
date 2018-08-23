/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { RazorCSharpFeature } from './CSharp/RazorCSharpFeature';
import { ProjectionResult } from './ProjectionResult';
import { RazorCompletionItemProvider } from './RazorCompletionItemProvider';
import { RazorLanguage } from './RazorLanguage';
import { RazorLanguageServiceClient } from './RazorLanguageServiceClient';
import { LanguageKind } from './RPC/LanguageKind';

export class ProvisionalCompletionOrchestrator {
    private provisionalDotsMayBeActive = false;
    private currentActiveDocument: vscode.TextDocument | undefined;

    constructor(
        private readonly csharpFeature: RazorCSharpFeature,
        private readonly serviceClient: RazorLanguageServiceClient) {
    }

    public register() {
        if (vscode.window.activeTextEditor) {
            this.currentActiveDocument = vscode.window.activeTextEditor.document;
        }

        // There's no event in VSCode to let us know when the completion window has been dismissed.
        // Because of this restriction we do a best effort to understand when the user has gone onto
        // different actions (other than viewing completion).

        const onDidChangeSelectionRegistration = vscode.window.onDidChangeTextEditorSelection(
            args => this.tryRemoveProvisionalDot(args.textEditor.document));
        const onDidChangeRegistration = vscode.workspace.onDidChangeTextDocument(async args => {
            if (args.contentChanges.length === 1 && args.contentChanges[0].text === '.') {
                // Don't want to remove a provisional dot that we just added.
                return;
            }

            await this.tryRemoveProvisionalDot(args.document);
        });
        const onDidChangeActiveEditorRegistration = vscode.window.onDidChangeActiveTextEditor(args => {
            if (this.currentActiveDocument) {
                this.tryRemoveProvisionalDot(this.currentActiveDocument);
            }

            if (args) {
                this.currentActiveDocument = args.document;
            } else {
                this.currentActiveDocument = undefined;
            }
        });

        return vscode.Disposable.from(
            onDidChangeRegistration,
            onDidChangeSelectionRegistration,
            onDidChangeActiveEditorRegistration);
    }

    public async tryGetProvisionalCompletions(
        hostDocumentUri: vscode.Uri,
        projection: ProjectionResult,
        completionContext: vscode.CompletionContext) {
        // We expect to be called in scenarios where the user has just typed a dot after
        // some identifier.
        // Such as (cursor is pipe): "DateTime.| "
        // In this case Razor interprets after the dot as Html and before it as C#. We
        // use this criteria to provide a better completion experience for what we call
        // provisional changes.

        if (projection.languageKind !== LanguageKind.Html) {
            return null;
        }

        if (completionContext.triggerCharacter !== '.') {
            return null;
        }

        const htmlPosition = projection.position;
        if (htmlPosition.character === 0) {
            return null;
        }

        const previousCharacterPosition = new vscode.Position(
            htmlPosition.line,
            htmlPosition.character - 1,
        );
        const previousCharacterQuery = await this.serviceClient.languageQuery(
            previousCharacterPosition,
            hostDocumentUri);

        if (previousCharacterQuery.kind !== LanguageKind.CSharp) {
            return null;
        }

        const projectedDocument = await this.csharpFeature.projectionProvider.getDocument(hostDocumentUri);
        const projectedEditorDocument = await vscode.workspace.openTextDocument(projectedDocument.projectedUri);
        const absoluteIndex = projectedEditorDocument.offsetAt(previousCharacterQuery.position);

        // Edit the projected document to contain a '.'. This allows C# completion to provide valid completion items
        // for moments when a user has typed a '.' that's typically interpreted as Html.
        // This provisional dot is removed when one of the following is true:
        //  1. The user starts typing
        //  2. The user swaps active documents
        //  3. The user selects different content
        //  4. The projected document gets an update request
        projectedDocument.addProvisionalDotAt(absoluteIndex);

        const provisionalPosition = new vscode.Position(
            previousCharacterQuery.position.line,
            previousCharacterQuery.position.character + 1);
        const completionList = await RazorCompletionItemProvider.getCompletions(
            projectedDocument.projectedUri,
            htmlPosition,
            provisionalPosition,
            completionContext.triggerCharacter);

        // We track when we add provisional dots to avoid doing unnecessary work on commonly invoked events.
        this.provisionalDotsMayBeActive = true;

        return completionList;
    }

    private async tryRemoveProvisionalDot(document: vscode.TextDocument) {
        if (!this.provisionalDotsMayBeActive) {
            return;
        }

        if (document.languageId !== RazorLanguage.id) {
            return;
        }

        const projectedDocument = await this.csharpFeature.projectionProvider.getActiveDocument();

        if (!projectedDocument) {
            return;
        }

        projectedDocument.removeProvisionalDot();
        this.provisionalDotsMayBeActive = false;
    }
}
