﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Build.Construction;
using Microsoft.Build.Experimental.BuildCheck;

namespace ErrorCustomCheck
{
    public sealed class ErrorWhenRegisteringActions : Check
    {
        public static CheckRule SupportedRule = new CheckRule(
            "X01238",
            "Title",
            "Description",
            "Message format: {0}",
            new CheckConfiguration());

        public override string FriendlyName => "ErrorWhenRegisteringActions";

        public override IReadOnlyList<CheckRule> SupportedRules { get; } = new List<CheckRule>() { SupportedRule };

        public override void Initialize(ConfigurationContext configurationContext)
        {
            // configurationContext to be used only if check needs external configuration data.
        }

        public override void RegisterActions(IBuildCheckRegistrationContext registrationContext)
        {
            registrationContext.RegisterEvaluatedPropertiesAction(EvaluatedPropertiesAction);
            throw new Exception("something went wrong when registering actions");
        }

        private void EvaluatedPropertiesAction(BuildCheckDataContext<EvaluatedPropertiesCheckData> context)
        {
            context.ReportResult(BuildCheckResult.Create(
                SupportedRule,
                ElementLocation.EmptyLocation,
                "This check should have been disabled"));
        }
    }
}
