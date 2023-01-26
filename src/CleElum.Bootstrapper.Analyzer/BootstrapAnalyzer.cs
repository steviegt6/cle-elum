using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CleElum.Bootstrapper.Analyzer;

/// <summary>
///     Core <see cref="DiagnosticAnalyzer"/> used to bootstrap MonoMod and
///     handle the requests of assemblies looking to execute actions after we
///     have initialized.
/// </summary>
[UsedImplicitly]
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BootstrapAnalyzer : DiagnosticAnalyzer {
    private static readonly DiagnosticDescriptor initialization_failed = new(
        id: INITIALIZATION_FAILED_ID,
        title: INITIALIZATION_FAILED_TITLE,
        messageFormat: INITIALIZATION_FAILED_MESSAGE_FORMAT,
        category: CATEGORY_BOOTSTRAP,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: INITIALIZED_FAILED_DESCRIPTION,
        helpLinkUri: null,
        customTags: "CompilationEnd"
    );

    private static readonly DiagnosticDescriptor already_initialized = new(
        id: ALREADY_INITIALIZED_ID,
        title: ALREADY_INITIALIZED_TITLE,
        messageFormat: ALREADY_INITIALIZED_MESSAGE_FORMAT,
        category: CATEGORY_BOOTSTRAP,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: ALREADY_INITIALIZED_DESCRIPTION,
        helpLinkUri: "CompilationEnd"
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(initialization_failed, already_initialized);

    private static readonly List<Action<bool>> actions = new();
    private static bool initialized;
    private static bool initializedTwice;
    private static Exception? exception;

    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationAction(ctx => {
            if (initializedTwice) {
                ctx.ReportDiagnostic(
                    Diagnostic.Create(already_initialized, Location.None)
                );
            }

            if (exception is null)
                return;

            ctx.ReportDiagnostic(
                Diagnostic.Create(
                    initialization_failed,
                    Location.None,
                    exception
                )
            );
        });

        if (initialized) {
            initializedTwice = true;
            return;
        }

        try {
            EnsureAssemblies();
            InitializeMonoMod();
        }
        catch (Exception e) {
            exception = e;
        }
        finally {
            // Don't attempt to initialize again, even if it failed.
            initialized = true;

            // Run post-initialization actions.
            foreach (var action in actions)
                action(exception is not null);
            actions.Clear();
        }
    }

    private static readonly string[] expected_assemblies = {
        "CleElum.Bootstrapper.Analyzer.MonoMod.Common.dll",
        "CleElum.Bootstrapper.Analyzer.MonoMod.Utils.dll",
        "CleElum.Bootstrapper.Analyzer.MonoMod.dll",
        "CleElum.Bootstrapper.Analyzer.MonoMod.RuntimeDetour.dll",
    };

    private static void EnsureAssemblies() {
        var asm = typeof(BootstrapAnalyzer).Assembly;

        foreach (var assembly in expected_assemblies) {
            var stream = asm.GetManifestResourceStream(assembly);
            if (stream is null)
                throw new InvalidOperationException(
                    $"Could not find embedded resource {assembly}"
                );

            var bytes = new byte[stream.Length];
            var read = stream.Read(bytes, 0, bytes.Length);
            if (read != bytes.Length)
                throw new InvalidOperationException(
                    $"Could not read embedded resource {assembly}"
                );

#pragma warning disable RS1035
            Assembly.Load(bytes);
#pragma warning restore RS1035
        }
    }

    private static void InitializeMonoMod() {
        // TODO: Anything important to do here?
    }

    /// <summary>
    ///     Executes the given <paramref name="action"/> post-initialization.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <exception cref="ArgumentNullException">action is null</exception>
    public static void ExecuteAfterInitialization(Action<bool> action) {
        if (action is null)
            throw new ArgumentNullException(nameof(action));

        if (initialized)
            action(exception is not null);
        else
            actions.Add(action);
    }
}
