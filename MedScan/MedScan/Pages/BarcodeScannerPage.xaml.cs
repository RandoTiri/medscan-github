using MedScan.Shared.Models;
using MedScan.Services;
using ZXing.Net.Maui;

namespace MedScan.Pages;

public partial class BarcodeScannerPage : ContentPage
{
    private readonly TaskCompletionSource<BarcodeScanResult> _completionSource = new();
    private readonly CancellationTokenSource _timeoutSource = new();
    private TaskCompletionSource<bool?>? _alertCompletionSource;
    private int _isCompleted;

    public BarcodeScannerPage()
    {
        InitializeComponent();

        CameraView.Options = new BarcodeReaderOptions
        {
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

    public Task<BarcodeScanResult> WaitForResultAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.CanBeCanceled)
        {
            cancellationToken.Register(() => Complete(new BarcodeScanResult
            {
                Status = BarcodeScanStatus.Canceled,
                Message = "Skannimine katkestati."
            }));
        }

        return _completionSource.Task;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = StartTimeoutAsync();
        AnimateScanLine();
    }

    private void AnimateScanLine()
    {
        var startY = 0;
        var endY = 198;

        var scanLine = this.FindByName<BoxView>("ScanLine");
        if (scanLine == null) return;

        void callback(double input) => scanLine.TranslationY = input;

        var animation = new Animation {
            { 0, 0.5, new Animation(callback, startY, endY, Easing.Linear) },
            { 0.5, 1, new Animation(callback, endY, startY, Easing.Linear) }
        };

        animation.Commit(this, "ScanLineAnimation", length: 3000, repeat: () => true);
    }

    protected override void OnDisappearing()
    {
        this.AbortAnimation("ScanLineAnimation");
        CameraView.BarcodesDetected -= OnBarcodesDetected;
        _timeoutSource.Cancel();
        _alertCompletionSource?.TrySetResult(null);
        _alertCompletionSource = null;
        base.OnDisappearing();
    }

    private async Task StartTimeoutAsync()
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(20), _timeoutSource.Token);
            if (Volatile.Read(ref _isCompleted) == 1 || !IsModalOpen())
            {
                return;
            }

            var retry = await TryDisplayAlertAsync("Viga", "Kaamera ei tuvastanud ravimit.", "Skaneeri uuesti", "Käsitsi otsimine");
            if (!retry.HasValue)
            {
                return;
            }

            if (retry.Value)
            {
                _ = StartTimeoutAsync();
            }
            else
            {
                Complete(new BarcodeScanResult
                {
                    Status = BarcodeScanStatus.ManualSearch
                });
                await CloseAsync();
            }
        }
        catch (TaskCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private async void CancelClicked(object? sender, EventArgs e)
    {
        if (sender is View view)
        {
            await view.ScaleToAsync(0.9, 100);
            await view.ScaleToAsync(1.0, 100);
        }

        Complete(new BarcodeScanResult
        {
            Status = BarcodeScanStatus.Canceled,
            Message = "Skannimine katkestati."
        });
        await CloseAsync();
    }

    private async void ManualSearchClicked(object? sender, EventArgs e)
    {
        if (sender is View view)
        {
            await view.ScaleToAsync(0.9, 100);
            await view.ScaleToAsync(1.0, 100);
        }

        Complete(new BarcodeScanResult
        {
            Status = BarcodeScanStatus.ManualSearch
        });
        await CloseAsync();
    }

    private async void ToggleTorchClicked(object? sender, EventArgs e)
    {
        if (sender is View view)
        {
            await view.ScaleToAsync(0.9, 100);
            await view.ScaleToAsync(1.0, 100);
        }
        CameraView.IsTorchOn = !CameraView.IsTorchOn;
    }

    private async void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        if (Volatile.Read(ref _isCompleted) == 1)
        {
            return;
        }

        var firstDetected = e.Results
            .FirstOrDefault(result => !string.IsNullOrWhiteSpace(result.Value));

        if (firstDetected is null)
        {
            return;
        }

        var barcodeValue = firstDetected.Value.Trim();
        if (string.IsNullOrWhiteSpace(barcodeValue))
        {
            return;
        }

        var lookupBarcode = barcodeValue;
        DateOnly? expirationDate = null;
        string? batchNumber = null;
        if (firstDetected.Format == BarcodeFormat.DataMatrix &&
            Gs1DataMatrixParser.TryExtract(
                barcodeValue,
                out var parsedBarcode,
                out var parsedExpirationDate,
                out var parsedBatchNumber,
                out _) &&
            !string.IsNullOrWhiteSpace(parsedBarcode))
        {
            lookupBarcode = parsedBarcode;
            expirationDate = parsedExpirationDate;
            batchNumber = parsedBatchNumber;
        }

        CameraView.IsDetecting = false;

#if WINDOWS || ANDROID || IOS || MACCATALYST
        var services = IPlatformApplication.Current?.Services;
#else
        var services = this.Handler?.MauiContext?.Services;
#endif
        var scannerFlowService = services?.GetService<MedScan.Shared.Services.IScannerFlowService>();
        if (scannerFlowService != null)
        {
            MedicationLookupResult? medication;
            try
            {
                medication = await scannerFlowService.FindByBarcodeAsync(lookupBarcode);
            }
            catch
            {
                var retryFromError = await TryDisplayAlertAsync("Viga", "Skannimisel tekkis tõrge.", "Skaneeri uuesti", "Käsitsi otsimine");
                if (retryFromError.GetValueOrDefault())
                {
                    CameraView.IsDetecting = true;
                    return;
                }

                Complete(new BarcodeScanResult
                {
                    Status = BarcodeScanStatus.ManualSearch
                });
                await CloseAsync();
                return;
            }

            if (medication == null)
            {
                var retry = await TryDisplayAlertAsync(
                    "Tundmatu triipkood",
                    "Tuvastatud triipkoodi andmeid ei leitud andmebaasist.",
                    "Skaneeri uuesti",
                    "Käsitsi otsimine");

                if (!retry.HasValue)
                {
                    return;
                }

                if (retry.Value)
                {
                    CameraView.IsDetecting = true;
                    return;
                }

                Complete(new BarcodeScanResult
                {
                    Status = BarcodeScanStatus.ManualSearch
                });
                await CloseAsync();
                return;
            }
        }

        Complete(new BarcodeScanResult
        {
            Status = BarcodeScanStatus.Success,
            Barcode = lookupBarcode,
            ExpirationDate = expirationDate,
            BatchNumber = batchNumber
        });

        await CloseAsync();
    }

    private void Complete(BarcodeScanResult result)
    {
        if (Interlocked.Exchange(ref _isCompleted, 1) == 1)
        {
            return;
        }

        _timeoutSource.Cancel();
        _completionSource.TrySetResult(result);
    }

    private async Task CloseAsync()
    {
        if (!IsModalOpen())
        {
            return;
        }

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (Navigation.ModalStack.Contains(this))
            {
                await Navigation.PopModalAsync();
            }
        });
    }

    private bool IsModalOpen()
    {
        return Application.Current?.Windows.FirstOrDefault()?.Page?.Navigation?.ModalStack.Contains(this) == true;
    }

    private async Task<bool?> TryDisplayAlertAsync(string title, string message, string accept, string cancel)
    {
        if (Volatile.Read(ref _isCompleted) == 1 || !IsModalOpen())
        {
            return null;
        }

        try
        {
            return await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (_alertCompletionSource is not null)
                {
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
        }
        catch
        {
            return null;
        }
    }

    private void AlertPrimaryClicked(object? sender, EventArgs e)
    {
        CloseAlert(true);
    }

    private void AlertSecondaryClicked(object? sender, EventArgs e)
    {
        CloseAlert(false);
    }

    private void CloseAlert(bool result)
    {
        AlertOverlay.IsVisible = false;
        _alertCompletionSource?.TrySetResult(result);
        _alertCompletionSource = null;
    }
}

