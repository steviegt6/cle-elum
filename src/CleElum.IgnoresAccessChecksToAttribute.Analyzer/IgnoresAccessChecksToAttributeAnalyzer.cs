using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using CleElum.Bootstrapper.Analyzer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using MonoMod.Cil;
using MonoMod.RuntimeDetour.HookGen;

namespace CleElum.IgnoresAccessChecksToAttribute.Analyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class IgnoresAccessChecksToAttributeAnalyzer :
    DiagnosticAnalyzer {
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray<DiagnosticDescriptor>.Empty;

    public IgnoresAccessChecksToAttributeAnalyzer() {
        BootstrapAnalyzer.EnsureInitialized();
        Patch();
    }

    public override void Initialize(AnalysisContext context) { }

    private const BindingFlags flags = BindingFlags.Public
                                     | BindingFlags.NonPublic
                                     | BindingFlags.Static
                                     | BindingFlags.Instance;

    private static void Patch() {
        Patch_IAssemblySymbol_GivesAccessTo();
    }

    private static void Patch_IAssemblySymbol_GivesAccessTo() {
        Debugger.Launch();
        var asm = typeof(CSharpExtensions).Assembly;
        var type = asm.GetType(
            "Microsoft.CodeAnalysis.CSharp.Symbols.PublicModel.AssemblySymbol"
        );
        var inter = typeof(IAssemblySymbol);
        var map = type.GetInterfaceMap(inter);
        var method = map.TargetMethods.First(
            x => x.Name
              == "Microsoft.CodeAnalysis.IAssemblySymbol.GivesAccessTo"
        );

        HookEndpointManager.Modify(
            method,
            (ILContext il) => {
                var c = new ILCursor(il);

                if (!c.TryGotoNext(x => x.MatchRet())) {
                    throw new Exception("ref");
                }

                if (!c.TryGotoNext(MoveType.After, x => x.MatchLdarg(0))) {
                    throw new Exception("ldarg.0");
                }

                c.Emit(Pop);
                c.Emit(Ldarg_1);
                c.EmitDelegate((IAssemblySymbol asmSymbol) => {
                    // ok so like right now this is me being really lazy and I
                    // am too lazy to do what I actually want to do here but
                    // this is really significant progress anyway. so right now
                    // you can just legit access whatever members you want (a la
                    // ignoresaccesschecksto) but like without checking if you
                    // actually have the interface. i will do that later kthxbai
                    var a = asmSymbol;
                    var t = asmSymbol.GetType();
                    return true;
                });
                c.Emit(Ret);
            }
        );
    }
}
