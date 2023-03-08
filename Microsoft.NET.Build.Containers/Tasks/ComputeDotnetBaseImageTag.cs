// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Framework;
using NuGet.Versioning;

namespace Microsoft.NET.Build.Containers.Tasks;

/// <summary>
/// Computes the base image Tag for a Microsoft-authored container image based on the tagging scheme from various SDK versions.
/// </summary>
public sealed class ComputeDotnetBaseImageTag : Microsoft.Build.Utilities.Task
{
    // starting in .NET 8, the container tagging scheme started incorporating the
    // 'channel' (rc/preview) and the channel increment (the numeric value after the channel name)
    // into the container tags.
    private const int FirstVersionWithNewTaggingScheme = 8;

    [Required]
    public string SdkVersion { get; set; }

    [Required]
    public string TargetFrameworkVersion { get; set; }

    [Output]
    public string? ComputedBaseImageTag { get; private set; }

    public ComputeDotnetBaseImageTag()
    {
        SdkVersion = "";
        TargetFrameworkVersion = "";
    }

    public override bool Execute()
    {
        if (SemanticVersion.TryParse(TargetFrameworkVersion, out var tfm) && tfm.Major < FirstVersionWithNewTaggingScheme)
        {
            ComputedBaseImageTag = $"{tfm.Major}.{tfm.Minor}";
            return true;
        }

        if (SemanticVersion.TryParse(SdkVersion, out var version))
        {
            ComputedBaseImageTag = ComputeVersionInternal(version, tfm);
            return true;
        }
        else
        {
            Log.LogError(Resources.Strings.InvalidSdkVersion, SdkVersion);
            return false;
        }
    }


    private string? ComputeVersionInternal(SemanticVersion version, SemanticVersion tfm)
    {
        if (tfm.Major < version.Major || tfm.Minor < version.Minor)
        {
            // in this case the TFM is earlier, so we are assumed to be in a stable scenario
            return $"{tfm.Major}.{tfm.Minor}";
        }
        // otherwise if we're in a scenario where we're using the TFM for the given SDK version,
        // and that SDK version may be a prerelease, so we need to handle
        var baseImageTag = (version) switch
        {
            { IsPrerelease: false } or { Major: < FirstVersionWithNewTaggingScheme } => $"{version.Major}.{version.Minor}",
            { Major: >= FirstVersionWithNewTaggingScheme } => DetermineLabelBasedOnChannel(version.Major, version.Minor, version.ReleaseLabels.ToArray())
        };
        return baseImageTag;
    }

    private string? DetermineLabelBasedOnChannel(int major, int minor, string[] releaseLabels) =>
        (releaseLabels) switch
        {
            [var channel, var bump, ..] when channel is ("rc" or "preview") => $"{major}.{minor}-{channel}.{bump}",
            [var channel, ..] => LogInvalidPrereleaseError(channel),
            [] => $"{major}.{minor}"
        };

    private string? LogInvalidPrereleaseError(string channel)
    {
        Log.LogError(Resources.Strings.InvalidSdkPrereleaseVersion, channel);
        return null;
    }


}
