using MedScan.Shared.Models;
using ZXing.Net.Maui;

namespace MedScan.Pages;

public partial class BarcodeScannerPage : ContentPage
{
    private readonly TaskCompletionSource<BarcodeScanResult> _completionSource = new();
    private readonly CancellationTokenSource _timeoutSource = new();
    private int _isCompleted;

    public BarcodeScannerPage()
    {
        InitializeComponent();

        CameraView.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormat.Ean13 | BarcodeFormat.Ean8 | BarcodeFormat.Code128 | BarcodeFormat.Code39 | BarcodeFormat.UpcA | BarcodeFormat.UpcE,
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

        Action<double> callback = input => scanLine.TranslationY = input;

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
        base.OnDisappearing();
    }

    private async Task StartTimeoutAsync()
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(20), _timeoutSource.Token);
            bool retry = await MainThread.InvokeOnMainThreadAsync(async () =>
                await DisplayAlertAsync("Viga", "Kaamera ei tuvastanud ravimit.","Skaneeri uuesti", "Käsitsi otsimine"));

            if (retry)
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

        var barcodeValue = e.Results
            .Select(result => result.Value)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        if (string.IsNullOrWhiteSpace(barcodeValue))
        {
            return;
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
            var medication = await scannerFlowService.FindByBarcodeAsync(barcodeValue.Trim());
            if (medication == null)
            {
                bool retry = await MainThread.InvokeOnMainThreadAsync(async () => 
                    await DisplayAlertAsync("Tundmatu triipkood", "Tuvastatud triipkoodi andmeid ei leitud andmebaasist.", "Skaneeri uuesti", "Käsitsi otsimine"));

                if (retry)
                {
                    CameraView.IsDetecting = true;
                    return;
                }
                else
                {
                    Complete(new BarcodeScanResult
                    {
                        Status = BarcodeScanStatus.ManualSearch
                    });
                    await CloseAsync();
                    return;
                }
            }
        }

        Complete(new BarcodeScanResult
        {
            Status = BarcodeScanStatus.Success,
            Barcode = barcodeValue.Trim()
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
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (Navigation.ModalStack.Contains(this))
            {
                await Navigation.PopModalAsync();
            }
        });
    }
}