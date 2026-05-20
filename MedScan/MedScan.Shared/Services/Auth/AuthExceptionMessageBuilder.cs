using MedScan.Shared.Services.Common;

namespace MedScan.Shared.Services.Auth;

internal static class AuthExceptionMessageBuilder {
    public static string Build(string operation,Exception exception) {
        if (exception is HttpRequestException) {
            return "Serveriga ei saadud ühendust. Kontrolli, et API töötab. USB Android testis tee ka adb reverse tcp:5183 tcp:5183.";
        }

        if (exception is TaskCanceledException) {
            return "Päring aegus. Proovi uuesti.";
        }

        return $"{operation} ebaõnnestus. Proovi uuesti.";
    }

    public static void Log(string operation,string baseAddress,Exception exception) =>
        SharedDiagnostics.Log(operation,$"BaseAddress={baseAddress}",exception);
}
