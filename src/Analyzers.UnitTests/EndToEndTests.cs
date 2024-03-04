﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.UnitTests;
using Microsoft.Build.UnitTests.Shared;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Build.Analyzers.UnitTests
{
    public class EndToEndTests : IDisposable
    {
        private readonly TestEnvironment _env;
        public EndToEndTests(ITestOutputHelper output)
        {
            _env = TestEnvironment.Create(output);

            // this is needed to ensure the binary logger does not pollute the environment
            _env.WithEnvironmentInvariant();
        }

        public void Dispose() => _env.Dispose();

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void SampleAnalyzerIntegrationTest(bool buildInOutOfProcessNode)
        {
            string contents = $"""
                <Project Sdk="Microsoft.NET.Sdk" DefaultTargets="Hello">
                    
                    <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>net8.0</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                    </PropertyGroup>
                      
                    <PropertyGroup Condition="$(Test) == true">
                    <TestProperty>Test</TestProperty>
                    </PropertyGroup>
                     
                    <ItemGroup>
                    <ProjectReference Include=".\FooBar-Copy.csproj" />
                    </ItemGroup>
                      
                    <Target Name="Hello">
                    <Message Importance="High" Condition="$(Test2) == true" Text="XYZABC" />
                    </Target>
                    
                </Project>
                """;

            string contents2 = $"""
                <Project Sdk="Microsoft.NET.Sdk">
                                   
                    <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>net8.0</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                    </PropertyGroup>
                                     
                    <PropertyGroup Condition="$(Test) == true">
                    <TestProperty>Test</TestProperty>
                    </PropertyGroup>
                                    
                    <ItemGroup>
                    <Reference Include="bin/foo.dll" />
                    </ItemGroup>
                                    
                    <Target Name="Hello">
                    <Message Importance="High" Condition="$(Test2) == true" Text="XYZABC" />
                    </Target>
                </Project>
                """;
            TransientTestFolder workFolder = _env.CreateFolder(createFolder: true);
            TransientTestFile projectFile = _env.CreateFile(workFolder, "FooBar.csproj", contents);
            TransientTestFile projectFile2 = _env.CreateFile(workFolder, "FooBar-Copy.csproj", contents2);
            TransientTestFile config = _env.CreateFile(workFolder, ".editorconfig",
            """
            root=true

            [*.csproj]
            msbuild_analyzer.BC0101.IsEnabled=true
            msbuild_analyzer.BC0101.Severity=warning

            msbuild_analyzer.COND0543.IsEnabled=false
            msbuild_analyzer.COND0543.Severity=Error
            msbuild_analyzer.COND0543.EvaluationAnalysisScope=AnalyzedProjectOnly
            msbuild_analyzer.COND0543.CustomSwitch=QWERTY

            msbuild_analyzer.BLA.IsEnabled=false
            """);

            // OSX links /var into /private, which makes Path.GetTempPath() return "/var..." but Directory.GetCurrentDirectory return "/private/var...".
            // This discrepancy breaks path equality checks in analyzers if we pass to MSBuild full path to the initial project.
            // TODO: See if there is a way of fixing it in the engine.
            _env.SetCurrentDirectory(Path.GetDirectoryName(projectFile.Path));

            _env.SetEnvironmentVariable("MSBUILDNOINPROCNODE", buildInOutOfProcessNode ? "1" : "0");
            _env.SetEnvironmentVariable("MSBUILDLOGPROPERTIESANDITEMSAFTEREVALUATION", "1");
            // string output = RunnerUtilities.ExecMSBuild($"{projectFile.Path} /m:1 -nr:False", out bool success);
            string output = BootstrapRunner.ExecBootstrapedMSBuild($"{Path.GetFileName(projectFile.Path)} /m:1 -nr:False -restore -analyze", out bool success);
            _env.Output.WriteLine(output);
            success.ShouldBeTrue();
            // The conflicting outputs warning appears
            output.ShouldContain("warning : BC0101");
        }
    }
}
