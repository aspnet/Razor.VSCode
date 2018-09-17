/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as path from 'path';
import * as vscode from 'vscode';
import * as razorExtensionPackage from 'microsoft.aspnetcore.razor.vscode';

let activationResolver: (value?: any) => void;
export const extensionActivated = new Promise(resolve => {
    activationResolver = resolve;
});

export async function activate(context: vscode.ExtensionContext) {
    // Because this extension is only used for local development and tests in CI,
    // we know the Razor Language Server is at a specific path within this repo
    const languageServerDir = path.join(
        __dirname, '..', '..', '..', 'src', 'Microsoft.AspNetCore.Razor.LanguageServer', 'bin', 'Debug', 'net461');

    await razorExtensionPackage.activate(context, languageServerDir);
    activationResolver();
}
