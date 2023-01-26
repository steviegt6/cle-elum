using System;
using System.IO;
using Mono.Cecil;
using MonoMod;
using MonoMod.RuntimeDetour.HookGen;

if (args.Length == 0)
    throw new Exception("Path not specified.");

var filePath = args[0];
var outputPath = "MMHOOK_" + Path.GetFileName(filePath);
if (!File.Exists(filePath))
    throw new Exception("File does not exist: " + filePath);

if (File.Exists(outputPath))
    File.Delete(outputPath);

Environment.SetEnvironmentVariable("MONOMOD_HOOKGEN_PRIVATE", "1");

using var modder = new MonoModder {
    InputPath = filePath,
    OutputPath = outputPath,
    ReadingMode = ReadingMode.Deferred,
    MissingDependencyThrow = false,
};

modder.Read();
modder.MapDependencies();

var hookGen = new HookGenerator(modder, Path.GetFileName(outputPath));
using var mOut = hookGen.OutputModule;
hookGen.Generate();

foreach (var t in mOut.Types) {
    if (string.IsNullOrEmpty(t.Namespace))
        continue;

    t.Name = t.Namespace.Substring(0, 2) + '_' + t.Name;
    t.Namespace = t.Namespace.Substring(Math.Min(3, t.Namespace.Length));
}

mOut.Write(outputPath);