using MedScan.MAUI.Services.Scanning;
using MedScan.Shared.Models;
using ZXing.Net.Maui;

namespace MedScan.MAUI.Pages;

public partial class BarcodeScannerPage : ContentPage {
    private readonly BarcodeScanFlowHandler _scanFlowHandler;
    private readonly TaskCompletionSource<BarcodeScanResult> _completionSource = new();
    private readonly CancellationTokenSource _timeoutSource = new();
    private TaskCompletionSource<bool?>? _alertCompletionSource;
    private int _isCompleted;

    public BarcodeScannerPage(BarcodeScanFlowHandler scanFlowHandler) {
        _scanFlowHandler = scanFlowHandler;
        InitializeComponent();

        CameraView.Options = new BarcodeReaderOptions {
            Formats = BarcodeFormat.Ean13
                | BarcodeFormat.Ean8
                | BarcodeFormat.Code128
                | BarcodeFormat.Code39
                | BarcodeFormat.UpcA
                | BarcodeFormat.UpcE
                | BarcodeFormat.DataMatrix,
            AutoRotate = true,
            Multiple = false
        };

        CameraView.BarcodesDetected += OnBarcodesDetected;
    }

    public Task<BarcodeScanResult> WaitForResultAsync(CancellationToken cancellationToken = default) {
        if (cancellationToken.CanBeCanceled) {
            cancellationToken.Register(() => Complete(new BarcodeScanResult {
                Status = BarcodeScanStatus.Canceled,
                Message = "Skaneerimine katkestati."
            }));
        }

        return _completionSource.Task;
    }

    protected override void OnAppearing() {
        base.OnAppearing();
        _ = StartTimeoutAsync();
        AnimateScanLine();
    }

    private void AnimateScanLine() {
        var startY = 0;
        var endY = 198;

        var scanLine = this.FindByName<BoxView>("ScanLine");
        if (scanLine == null) return;

        void callback(double input) => scanLine.TranslationY = input;

        var animation = new Animation {
            { 0, 0.5, new Animation(callback, startY, endY, Easing.Linear) },
            { 0.5, 1, new Animation(callback, endY, startY, Easing.Linear) }
        };

        animation.Commit(this,"ScanLineAnimation",length: 3000,repeat: () => true);
    }

    protected override void OnDisappearing() {
        this.AbortAnimation("ScanLineAnimation");
        CameraView.BarcodesDetected -= OnBarcodesDetected;
        _timeoutSource.Cancel();
        _alertCompletionSource?.TrySetResult(null);
        _alertCompletionSource = null;
        base.OnDisappearing();
    }

    private async Task StartTimeoutAsync() {
        try {
            await Task.Delay(TimeSpan.FromSeconds(20),_timeoutSource.Token);
            if (Volatile.Read(ref _isCompleted) == 1 || !IsModalOpen()) {
                return;
            }

            var retry = await TryDisplayAlertAsync(
                "Viga",
                "Kaamera ei tuvastanud ravimit.",
                "Skanneeri uuesti",
                "Otsi käsitsi");
            if (!retry.HasValue) {
                return;
            }

            if (retry.Value) {
                _ = StartTimeoutAsync();
            } else {
                Complete(new BarcodeScanResult {
                    Status = BarcodeScanStatus.ManualSearch
                });
                await CloseAsync();
            }
        } catch (TaskCanceledException) {
        } catch (ObjectDisposedException) {
        }
    }

    private async void CancelClicked(object? sender,EventArgs e) {
        if (sender is View view) {
            await view.ScaleToAsync(0.9,100);
            await view.ScaleToAsync(1.0,100);
        }

        Complete(new BarcodeScanResult {
            Status = BarcodeScanStatus.Canceled,
            Message = "Skaneerimine katkestati."
        });
        await CloseAsync();
    }

    private async void ManualSearchClicked(object? sender,EventArgs e) {
        if (sender is View view) {
            await view.ScaleToAsync(0.9,100);
            await view.ScaleToAsync(1.0,100);
        }

        Complete(new BarcodeScanResult {
            Status = BarcodeScanStatus.ManualSearch
        });
        await CloseAsync();
    }

    private async void ToggleTorchClicked(object? sender,EventArgs e) {
        if (sender is View view) {
            await view.ScaleToAsync(0.9,100);
            await view.ScaleToAsync(1.0,100);
        }
        CameraView.IsTorchOn = !CameraView.IsTorchOn;
    }

    private async void OnBarcodesDetected(object? sender,BarcodeDetectionEventArgs e) {
        if (Volatile.Read(ref _isCompleted) == 1) {
            return;
        }

        var firstDetected = e.Results
            .FirstOrDefault(result => !string.IsNullOrWhiteSpace(result.Value));

        if (firstDetected is null) {
            return;
        }

        CameraView.IsDetecting = false;

        var scanResult = await _scanFlowHandler.HandleDetectedAsync(firstDetected.Value,firstDetected.Format);
        if (scanResult.Kind == BarcodeScanFlowResultKind.Ignore) {
            CameraView.IsDetecting = true;
            return;
        }

        if (scanResult.Kind == BarcodeScanFlowResultKind.NeedsPrompt && scanResult.Prompt is not null) {
            await HandleScanPromptAsync(scanResult.Prompt);
            return;
        }

        if (scanResult.Result is not null) {
            Complete(scanResult.Result);
        }

        await CloseAsync();
    }

    private async Task HandleScanPromptAsync(BarcodeScanPrompt prompt) {
        var retry = await TryDisplayAlertAsync(
            prompt.Title,
            prompt.Message,
            prompt.Accept,
            prompt.Cancel);

        if (!retry.HasValue) {
            return;
        }

        if (retry.Value) {
            CameraView.IsDetecting = true;
            return;
        }

        Complete(new BarcodeScanResult {
            Status = BarcodeScanStatus.ManualSearch
        });
        await CloseAsync();
    }

    private void Complete(BarcodeScanResult result) {
        if (Interlocked.Exchange(ref _isCompleted,1) == 1) {
            return;
        }

        _timeoutSource.Cancel();
        _completionSource.TrySetResult(result);
    }

    private async Task CloseAsync() {
        if (!IsModalOpen()) {
            return;
        }

        await MainThread.InvokeOnMainThreadAsync(async () => {
            if (Navigation.ModalStack.Contains(this)) {
                await Navigation.PopModalAsync();
            }
        });
    }

    private bool IsModalOpen() {
        return Application.Current?.Windows.FirstOrDefault()?.Page?.Navigation?.ModalStack.Contains(this) == true;
    }

    private async Task<bool?> TryDisplayAlertAsync(string title,string message,string accept,string cancel) {
        if (Volatile.Read(ref _isCompleted) == 1 || !IsModalOpen()) {
            return null;
        }

        try {
            return await MainThread.InvokeOnMainThreadAsync(() => {
                if (_alertCompletionSource is not null) {
                    return Task.FromResult<bool?>(null);
                }

                _alertCompletionSource = new TaskCompletionSource<bool?>();
                AlertTitleLabel.Text = title;
                AlertMessageLabel.Text = message;
                AlertPrimaryButton.Text = accept;
                AlertSecondaryButton.Text = cancel;
                AlertOverlay.IsVisible = true;
                return _alertCompletionSource.Task;
            });
        } catch {
            return null;
        }
    }

    private void AlertPrimaryClicked(object? sender,EventArgs e) {
        CloseAlert(true);
    }

    private void AlertSecondaryClicked(object? sender,EventArgs e) {
        CloseAlert(false);
    }

    private void CloseAlert(bool result) {
        AlertOverlay.IsVisible = false;
        _alertCompletionSource?.TrySetResult(result);
        _alertCompletionSource = null;
    }
}
