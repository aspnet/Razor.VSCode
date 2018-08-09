/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as assert from 'assert';
import * as path from 'path';
import * as vscode from 'vscode';

suite('Example Tests', () => {
    test('IDE can open README.md', async () => {
        const filePath = path.join(__dirname, '..', '..', '..', 'README.md');
        const doc = await vscode.workspace.openTextDocument(filePath);
        assert.ok(doc.getText().startsWith('Razor.VSCode\r\n'));
    });
});
