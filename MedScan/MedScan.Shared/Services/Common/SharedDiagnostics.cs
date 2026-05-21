using System.Diagnostics;

namespace MedScan.Shared.Services.Common;

public static class SharedDiagnostics {
    public static void Log(string tag,Exception exception) {
        Debug.WriteLine($"{tag} ERROR | {exception}");
    }

    public static void Log(string tag,string context,Exception exception) {
        Debug.WriteLine($"{tag} ERROR | {context} | {exception}");
    }
}