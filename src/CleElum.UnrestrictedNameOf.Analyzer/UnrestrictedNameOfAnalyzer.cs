using System;
using System.Collections.Immutable;
using System.Reflection;
using CleElum.Bootstrapper.Analyzer;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using MonoMod.RuntimeDetour.HookGen;

namespace CleElum.UnrestrictedNameOf.Analyzer;

[UsedImplicitly]
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UnrestrictedNameOfAnalyzer : DiagnosticAnalyzer {
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray<DiagnosticDescriptor>.Empty;

    public override void Initialize(AnalysisContext context) {
        //context.RegisterSymbolStartAction(null, );
        BootstrapAnalyzer.EnsureInitialized();
        Patch();
        throw new Exception("guh");
    }

    private static void Patch() {
        var asm = typeof(LanguageVersion).Assembly;

        var ac = asm.GetType("Microsoft.CodeAnalysis.CSharp.AccessCheck");
        var isac = ac.GetMethod(
            "IsSymbolAccessibleCore",
            BindingFlags.Static | BindingFlags.NonPublic
        );

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
        CSharpCompilation compliation,
        ref object useSiteInfo,
        object? basesBeingResolved
    );

    private static bool MakeAccessible(
        OrigIsSymbolAccessibleCore orig,
        object symbol,
        object within,
        object throughTypeOpt,
        out bool failedThroughTypeCheck,
        CSharpCompilation compilation,
        ref object useSiteInfo,
        object? basesBeingResolved
    ) {
        orig(
            symbol,
            within,
            throughTypeOpt,
            out failedThroughTypeCheck,
            compilation,
            ref useSiteInfo,
            basesBeingResolved
        );
        return true;
    }

    /*private delegate void OrigTest(
        object self,
        object result,
        string name,
        int arity,
        object basesBeingResolved,
        object options,
        object originalBinder,
        bool diagnose,
        ref object useSiteInfo
    );

    private static void Test(
        OrigTest orig,
        object self,
        object result,
        string name,
        int arity,
        object basesBeingResolved,
        object options,
        object originalBinder,
        bool diagnose,
        ref object useSiteInfo
    ) {
        orig(
            self,
            result,
            name,
            arity,
            basesBeingResolved,
            options,
            originalBinder,
            diagnose,
            ref useSiteInfo
        );

        var asm = typeof(LanguageVersion).Assembly;
        var lrType = asm.GetType("Microsoft.CodeAnalysis.CSharp.LookupResult");
        var good = lrType.GetMethod(
            "Good",
            BindingFlags.NonPublic | BindingFlags.Static
        );
        var mergeEqual = lrType.GetMethod(
            "MergeEqual",
            BindingFlags.NonPublic | BindingFlags.Static,
            null,
            new[] {
                asm.GetType("Microsoft.CodeAnalysis.CSharp.SingleLookupResult"),
            },
            null
        );

        //if (good is null || mergeEqual is null)
        //    return;

        var slr = good.Invoke(
            null,
            new object?[] {
                null,
            }
        );

        mergeEqual.Invoke(
            result,
            new[] {
                slr
            }
        );
    }*/
}
