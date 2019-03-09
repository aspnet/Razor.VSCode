/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { IRazorProject } from './IRazorProject';
import { RazorLanguage } from './RazorLanguage';
import { RazorLanguageServerClient } from './RazorLanguageServerClient';
import { RazorLogger } from './RazorLogger';
import { AddDocumentRequest } from './RPC/AddDocumentRequest';
import { AddProjectRequest } from './RPC/AddProjectRequest';
import { LanguageQueryRequest } from './RPC/LanguageQueryRequest';
import { LanguageQueryResponse } from './RPC/LanguageQueryResponse';
import { RazorTextDocumentItem } from './RPC/RazorTextDocumentItem';
import { RemoveDocumentRequest } from './RPC/RemoveDocumentRequest';
import { RemoveProjectRequest } from './RPC/RemoveProjectRequest';
import { UpdateProjectRequest } from './RPC/UpdateProjectRequest';

export class RazorLanguageServiceClient {
    constructor(
        private readonly serverClient: RazorLanguageServerClient,
        private readonly logger: RazorLogger) {
        serverClient.onStart(() => {
            // Once the server starts we need to attach to all of the request handlers

            serverClient.onRequest('getTextDocument', filePath => this.getTextDocument(filePath));
        });
    }

    public async addDocument(documentUri: vscode.Uri) {
        await this.ensureStarted();

        const request = new AddDocumentRequest(documentUri.fsPath);
        await this.serverClient.sendRequest<AddDocumentRequest>('projects/addDocument', request);
    }

    public async removeDocument(documentUri: vscode.Uri) {
        await this.ensureStarted();

        const request = new RemoveDocumentRequest(documentUri.fsPath);
        await this.serverClient.sendRequest<RemoveDocumentRequest>('projects/removeDocument', request);
    }

    public async addProject(projectFileUri: vscode.Uri) {
        await this.ensureStarted();

        const request = new AddProjectRequest(projectFileUri.fsPath);
        await this.serverClient.sendRequest<AddProjectRequest>('projects/addProject', request);
    }

    public async removeProject(projectFileUri: vscode.Uri) {
        await this.ensureStarted();

        const request = new RemoveProjectRequest(projectFileUri.fsPath);
        await this.serverClient.sendRequest<RemoveProjectRequest>('projects/removeProject', request);
    }

    public async updateProject(project: IRazorProject) {
        await this.ensureStarted();

        const request: UpdateProjectRequest = {
            filePath: project.uri.fsPath,
            projectWorkspaceState: project.configuration ? project.configuration.projectWorkspaceState : null,
            configuration: project.configuration ? project.configuration.configuration : undefined,
        };
        await this.serverClient.sendRequest<UpdateProjectRequest>('projects/updateProject', request);
    }

    public async languageQuery(position: vscode.Position, uri: vscode.Uri) {
        await this.ensureStarted();

        const request = new LanguageQueryRequest(position, uri);
        const response = await this.serverClient.sendRequest<LanguageQueryResponse>('razor/languageQuery', request);
        response.position = new vscode.Position(response.position.line, response.position.character);
        return response;
    }

    private async getTextDocument(filePath: string) {
        const clientUri = vscode.Uri.file(filePath);
        try {
            const document = await vscode.workspace.openTextDocument(clientUri);
            return new RazorTextDocumentItem(document);
        } catch {
            this.logger.logVerbose(`Failed to open text document ${filePath}. Returning an empty document to the server.`);

            // We were asked for a document that no longer exists. Return an empty text document as a response.
            return {
                languageId: RazorLanguage.id,
                version: -1,
                text: '',
                uri: clientUri.toString(),
            };
        }
    }

    private async ensureStarted() {
        // If the server is already started this will instantly return.
        await this.serverClient.start();
    }
}
