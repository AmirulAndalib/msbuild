﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System;

namespace Microsoft.Build.Experimental.BuildCop;

/// <summary>
/// Configuration for a build analyzer.
/// Default values can be specified by the Analyzer in code.
/// Users can overwrite the defaults by explicit settings in the .editorconfig file.
/// Each rule can have its own configuration, which can differ per each project.
/// The <see cref="EvaluationAnalysisScope"/> setting must be same for all rules in the same analyzer (but can differ between projects)
/// </summary>
public class BuildAnalyzerConfiguration
{
    // Defaults to be used if any configuration property is not specified neither as default
    //  nor in the editorconfig configuration file.
    public static BuildAnalyzerConfiguration Default { get; } = new()
    {
        EvaluationAnalysisScope = BuildCop.EvaluationAnalysisScope.AnalyzedProjectOnly,
        Severity = BuildAnalyzerResultSeverity.Info,
        IsEnabled = false,
    };

    public static BuildAnalyzerConfiguration Null { get; } = new();

    /// <summary>
    /// This applies only to specific events, that can distinguish whether they are directly inferred from
    ///  the current project, or from some import. If supported it can help tuning the level of detail or noise from analysis.
    ///
    /// If not supported by the data source - then the setting is ignored
    /// </summary>
    public EvaluationAnalysisScope? EvaluationAnalysisScope { get; internal init; }

    /// <summary>
    /// The severity of the result for the rule.
    /// </summary>
    public BuildAnalyzerResultSeverity? Severity { get; internal init; }

    /// <summary>
    /// Whether the analyzer rule is enabled.
    /// If all rules within the analyzer are not enabled, it will not be run.
    /// If some rules are enabled and some are not, the analyzer will be run and reports will be post-filtered.
    /// </summary>
    public bool? IsEnabled { get; internal init; }

    public static BuildAnalyzerConfiguration Create(Dictionary<string, string> configDictionary)
    {
        return new()
        {
            EvaluationAnalysisScope = TryExtractValue("EvaluationAnalysisScope", configDictionary, out EvaluationAnalysisScope evaluationAnalysisScope) ? evaluationAnalysisScope : null,
            Severity = TryExtractValue("severity", configDictionary, out BuildAnalyzerResultSeverity severity) ? severity : null,
            IsEnabled = TryExtractValue("IsEnabled", configDictionary, out bool test) ? test : null,
        };
    }

    private static bool TryExtractValue<T>(string key, Dictionary<string, string> config, out T value) where T : struct
    {
        value = default;
        if (!config.ContainsKey(key))
        {
            return false;
        }

        if (typeof(T) == typeof(bool))
        {
            if (bool.TryParse(config[key], out bool boolValue))
            {
                value = (T)(object)boolValue;
                return true;
            }
        }
        else if(typeof(T).IsEnum)
        {
            return Enum.TryParse(config[key], true, out value);
        }
        return false;
    }
}
