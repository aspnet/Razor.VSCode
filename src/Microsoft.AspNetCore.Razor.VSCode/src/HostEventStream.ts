/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

// Bits in this file are contracts defined in https://github.com/omnisharp/omnisharp-vscode

export interface HostEventStream {
    post(event: BaseEvent): void;
}

export class TelemetryEvent implements BaseEvent {
    public type = EventType.TelemetryEvent;

    constructor(
        public eventName: string,
        public properties?: { [key: string]: string },
        public measures?: { [key: string]: number }) {
    }
}

interface BaseEvent {
    type: EventType;
}

export enum EventType {
    OmnisharpStart,
    TelemetryEvent,
    TelemetryEventWithMeasures,
    OmnisharpDelayTrackerEventMeasures,
    OmnisharpInitialisation,
    OmnisharpLaunch,
    PackageInstallStart,
    PackageInstallation,
    LogPlatformInfo,
    InstallationStart,
    InstallationFailure,
    DownloadProgress,
    OmnisharpFailure,
    OmnisharpRequestMessage,
    TestExecutionCountReport,
    OmnisharpServerOnError,
    OmnisharpServerMsBuildProjectDiagnostics,
    OmnisharpServerUnresolvedDependencies,
    OmnisharpServerEnqueueRequest,
    OmnisharpServerProcessRequestStart,
    OmnisharpEventPacketReceived,
    OmnisharpServerOnServerError,
    OmnisharpOnMultipleLaunchTargets,
    WorkspaceInformationUpdated,
    EventWithMessage,
    DownloadStart,
    DownloadFallBack,
    DownloadSizeObtained,
    ZipError,
    ReportDotNetTestResults,
    DotNetTestRunStart,
    DotNetTestDebugStart,
    DotNetTestDebugProcessStart,
    DotNetTestsInClassRunStart,
    DotNetTestsInClassDebugStart,
    DocumentSynchronizationFailure,
    OpenURL,
    IntegrityCheckFailure,
    IntegrityCheckSuccess,
    RazorPluginPathSpecified,
    RazorPluginPathDoesNotExist,
    DebuggerPrerequisiteFailure,
    CommandDotNetRestoreProgress,
    DownloadValidation,
    DotNetTestDebugComplete,
    LatestBuildDownloadStart,
    ActiveTextEditorChanged,
    OmnisharpOnBeforeServerStart,
    ProjectJsonDeprecatedWarning,
    OmnisharpServerProcessRequestComplete,
    InstallationSuccess,
    CommandDotNetRestoreStart,
    DebuggerNotInstalledFailure,
    ShowOmniSharpChannel,
    ActivationFailure,
    ProjectModified,
    RazorDevModeActive,
    DotNetTestDebugStartFailure,
    DotNetTestDebugWarning,
    DotNetTestRunFailure,
    DotNetTestMessage,
    OmnisharpServerVerboseMessage,
    OmnisharpServerMessage,
    OmnisharpServerOnStdErr,
    DownloadFailure,
    DownloadSuccess,
    CommandDotNetRestoreSucceeded,
    DebuggerPrerequisiteWarning,
    CommandDotNetRestoreFailed,
    OmnisharpRestart,
    OmnisharpServerDequeueRequest,
    OmnisharpServerOnStop,
    OmnisharpServerOnStart,
    OmnisharpOnBeforeServerInstall,
    ProjectConfigurationReceived,
}
