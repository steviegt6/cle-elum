> **Warning** | This is not a practical project. If you find yourself ever needing this, consider rethinking everything.

# cle-elum

> compiler hacking!!!!!1!! but for c#

---

Experimental, questionable Roslyn activities.

## Things

### Roslyn Modding

Modify Roslyn in-memory with [MonoMod](https://github.com/MonoMod/MonoMod) (RuntimeDetour) and a somewhat convenient little bit of API boilerplate:

```cs
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MyAnalyzer : DiagnosticAnalyzer {
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray<DiagnosticDescriptor>.Empty;

    public MyAnalyzer() {
        // Your entrypoint lies within this class' constructor.
        BootstrapAnalyzer.EnsureInitialized();
        Patch();
    }

    public override void Initialize(AnalysisContext context) { }

    private static void Patch() {
        // use monomod here
    }
}
```

General changes that make normally-invalid stuff valid will be reflected properly in Visual Studio, but not anything that doesn't use Roslyn (such as Rider).
