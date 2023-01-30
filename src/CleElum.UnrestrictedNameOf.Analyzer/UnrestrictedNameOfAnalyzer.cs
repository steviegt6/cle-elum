using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using CleElum.Bootstrapper.Analyzer;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using MonoMod.Cil;
using MonoMod.RuntimeDetour.HookGen;

namespace CleElum.UnrestrictedNameOf.Analyzer;

[UsedImplicitly]
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UnrestrictedNameOfAnalyzer : DiagnosticAnalyzer {
    private static readonly DiagnosticDescriptor patch = new(
        id: PATCH_ID,
        title: PATCH_TITLE,
        messageFormat: PATCH_FORMAT,
        category: CATEGORY_PATCH,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: PATCH_DESCRIPTION,
        helpLinkUri: null,
        customTags: new[] {
            Build,
            Compiler,
            CompilationEnd,
        }
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(patch);

    public override void Initialize(AnalysisContext context) {
        //context.RegisterSymbolStartAction(null, );
        BootstrapAnalyzer.EnsureInitialized();
        Patch();
    }

    private static void Patch() {
        var asm = typeof(LanguageVersion).Assembly;

        var ac = asm.GetType("Microsoft.CodeAnalysis.CSharp.AccessCheck");
        var isac = ac.GetMethod(
            "IsSymbolAccessibleCore",
            BindingFlags.Static | BindingFlags.NonPublic
        );

        // On_AccessCheck.IsSymbolAccessibleCore += MakeAccessible;
        var a = typeof(HookEndpointManager);
        HookEndpointManager.Modify(
            isac,
            (ILContext il) => {
                var c = new ILCursor(il) {
                    Next = null,
                };

                while (c.TryGotoPrev(MoveType.Before, x => x.MatchRet())) {
                    c.Emit(Pop);
                    c.Emit(Ldc_I4_1);
                }
            }
        );
    }
}
