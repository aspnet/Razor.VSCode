/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { ServerTextChange } from '../RPC/ServerTextChange';

export class CSharpProjectedDocument {
    private content = '';
    private preProvisionalContent: string | undefined;
    private provisionalEditAt: number | undefined;

    public constructor(
        readonly projectedUri: vscode.Uri,
        readonly hostDocumentUri: vscode.Uri,
        readonly onChange: () => void) {
    }

    public applyEdits(edits: ServerTextChange[]) {
        this.removeProvisionalDot();

        for (const edit of edits) {
            // TODO: Use a better data structure to represent the content, string concats
            // are slow.
            const before = this.content.substr(0, edit.span.start);
            const after = this.content.substr(edit.span.end);
            this.setContent(`${before}${edit.newText}${after}`);
        }
    }

    public getContent() {
        return this.content;
    }

    // A provisional dot represents a '.' that's inserted into the projected document but will be
    // removed prior to any edits that get applied. In Razor's case a provisional dot is used to
    // show completions after an expression for a dot that's usually interpreted as Html.
    public addProvisionalDotAt(index: number) {
        if (this.provisionalEditAt === index) {
            // Edits already applied.
            return;
        }

        const preEditContent = this.content;
        this.applyEdits([{
            newText: '.',
            span: {
                start: index,
                end: index,
                length: 0,
            },
        }]);
        this.preProvisionalContent = preEditContent;
        this.provisionalEditAt = index;
    }

    public removeProvisionalDot() {
        if (this.provisionalEditAt && this.preProvisionalContent) {
            // Undo provisional edit if one was applied.
            this.setContent(this.preProvisionalContent);
            this.provisionalEditAt = undefined;
            this.preProvisionalContent = undefined;
        }
    }

    private setContent(content: string) {
        this.content = content;
        this.onChange();
    }
}
