﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Build.BackEnd.Shared;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;
using Microsoft.Build.Shared;

namespace Microsoft.Build.Experimental.BuildCheck;

/// <summary>
/// <see cref="ICheckContext"/> that uses <see cref="EventArgsDispatcher"/> to dispatch.
/// </summary>
internal class CheckDispatchingContext : ICheckContext
{
    private readonly EventArgsDispatcher _eventDispatcher;
    private readonly BuildEventContext _eventContext;

    public CheckDispatchingContext(
        EventArgsDispatcher dispatch,
        BuildEventContext eventContext)
    {
        _eventDispatcher = dispatch;
        _eventContext = eventContext;
    }

    public BuildEventContext BuildEventContext => _eventContext;

    public void DispatchBuildEvent(BuildEventArgs buildEvent)
    {
        ErrorUtilities.VerifyThrowInternalNull(buildEvent);

        _eventDispatcher.Dispatch(buildEvent);
    }

    public void DispatchAsComment(MessageImportance importance, string messageResourceName, params object?[] messageArgs)
    {
        ErrorUtilities.VerifyThrowInternalLength(messageResourceName, nameof(messageResourceName));

        DispatchAsCommentFromText(_eventContext, importance, ResourceUtilities.GetResourceString(messageResourceName), messageArgs);
    }

    public void DispatchAsCommentFromText(MessageImportance importance, string message)
        => DispatchAsCommentFromText(_eventContext, importance, message, messageArgs: null);

    private void DispatchAsCommentFromText(BuildEventContext buildEventContext, MessageImportance importance, string message, params object?[]? messageArgs)
    {
        BuildMessageEventArgs buildEvent = EventsCreatorHelper.CreateMessageEventFromText(buildEventContext, importance, message, messageArgs);

        _eventDispatcher.Dispatch(buildEvent);
    }

    public void DispatchAsErrorFromText(string? subcategoryResourceName, string? errorCode, string? helpKeyword, BuildEventFileInfo file, string message)
    {
        BuildErrorEventArgs buildEvent = EventsCreatorHelper.CreateErrorEventFromText(_eventContext, subcategoryResourceName, errorCode, helpKeyword, file, message);

        _eventDispatcher.Dispatch(buildEvent);
    }

    public void DispatchAsWarningFromText(string? subcategoryResourceName, string? errorCode, string? helpKeyword, BuildEventFileInfo file, string message)
    {
        BuildWarningEventArgs buildEvent = EventsCreatorHelper.CreateWarningEventFromText(_eventContext, subcategoryResourceName, errorCode, helpKeyword, file, message);

        _eventDispatcher.Dispatch(buildEvent);
    }

    public void DispatchFailedAcquisitionTelemetry(string assemblyName, Exception exception)
    // This is it - no action for replay mode.
    { }

    public void DispatchTelemetry(BuildCheckTracingData data)
    // This is it - no action for replay mode.
    { }
}
