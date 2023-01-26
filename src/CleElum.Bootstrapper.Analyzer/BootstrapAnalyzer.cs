using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using MonoMod.RuntimeDetour.HookGen;
using MonoMod.Utils;

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
        customTags: new[] {
            Build,
            Compiler,
            CompilationEnd,
        }
    );

    private static readonly DiagnosticDescriptor already_initialized = new(
        id: ALREADY_INITIALIZED_ID,
        title: ALREADY_INITIALIZED_TITLE,
        messageFormat: ALREADY_INITIALIZED_MESSAGE_FORMAT,
        category: CATEGORY_BOOTSTRAP,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: ALREADY_INITIALIZED_DESCRIPTION,
        helpLinkUri: null,
        customTags: CompilationEnd
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(initialization_failed, already_initialized);

    // private static readonly List<Action<bool>> actions = new();
    private static bool initialized;

    // private static bool initializedTwice;
    private static Exception? exception;

    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(ctx => {
            ctx.RegisterSymbolStartAction(_ => { }, default);
        });
        context.RegisterCompilationAction(ctx => {
            /*if (initializedTwice) {
                ctx.ReportDiagnostic(
                    Diagnostic.Create(already_initialized, Location.None)
                );
            }*/

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

        EnsureInitialized();
    }

    public static void EnsureInitialized() {
        if (initialized) {
            // initializedTwice = true;
            return;
        }

        try {
            EnsureAssemblies();
        }
        catch (Exception e) {
            exception = e;
        }
        finally {
            // Don't attempt to initialize again, even if it failed.
            initialized = true;
        }

        Patch();
    }

    private const string ce_ns = "CleElum.Bootstrapper.Analyzer.";
    private const string ext = ".dll";

    private static readonly string[] expected_assemblies = {
        "Mono.Cecil",
        "Mono.Cecil.Rocks",
        "Mono.Cecil.Pdb",
        "Mono.Cecil.Mdb",
        "MonoMod.Common",
        "MonoMod.Utils",
        "MonoMod",
        "MonoMod.RuntimeDetour",
        // "MMHOOK_Microsoft.CodeAnalysis.CSharp",
    };

    private static readonly Dictionary<string, Assembly> assembly_map = new();

    private static void EnsureAssemblies() {
        var asm = typeof(BootstrapAnalyzer).Assembly;

        foreach (var assembly in expected_assemblies) {
            var fileName = ce_ns + assembly + ext;
            var stream = asm.GetManifestResourceStream(fileName);
            if (stream is null)
                throw new InvalidOperationException(
                    $"Could not find embedded resource {fileName}"
                );

            var bytes = new byte[stream.Length];
            var read = stream.Read(bytes, 0, bytes.Length);
            if (read != bytes.Length)
                throw new InvalidOperationException(
                    $"Could not read embedded resource {fileName}"
                );

#pragma warning disable RS1035
            assembly_map[assembly] = Assembly.Load(bytes);
#pragma warning restore RS1035
        }

        AppDomain.CurrentDomain.AssemblyResolve += (_,  args) => {
            var name = new AssemblyName(args.Name);

            return assembly_map.ContainsKey(name.Name)
                ? assembly_map[name.Name]
                : null;
        };
    }

    /*/// <summary>
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
    }*/

    private static void Patch() {
        var asm = typeof(LanguageVersion).Assembly;

        var ac = asm.GetType("Microsoft.CodeAnalysis.CSharp.AccessCheck");
        var isac = ac.GetMethod(
            "IsSymbolAccessibleCore",
            BindingFlags.Static | BindingFlags.NonPublic
        );

        // On_AccessCheck.IsSymbolAccessibleCore += MakeAccessible;
        HookEndpointManager.Add(isac, MakeAccessible);

        /*var type = asm.GetType("Microsoft.CodeAnalysis.CSharp.NameofBinder");
        var meth = type?.GetMethod(
            "LookupSymbolsInSingleBinder",
            BindingFlags.NonPublic | BindingFlags.Instance
        );

        if (type is null || meth is null)
            return;

        HookEndpointManager.Add(
            meth,
            Test
        );*/
    }

    private delegate void OrigIsSymbolAccessibleCore(
        object symbol,
        object within,
        object throughTypeOpt,
        out bool failedThroughTypeCheck,
        CSharpCompilation compilation,
        IntPtr useSiteInfo,
        object basesBeingResolved
    );

    private static bool MakeAccessible(
        OrigIsSymbolAccessibleCore orig,
        object symbol,
        object within,
        object throughTypeOpt,
        out bool failedThroughTypeCheck,
        CSharpCompilation compilation,
        IntPtr useSiteInfo,
        object basesBeingResolved
    ) {
        orig(
            symbol,
            within,
            throughTypeOpt,
            out failedThroughTypeCheck,
            compilation,
            useSiteInfo,
            basesBeingResolved
        );
        return true;
    }
}
