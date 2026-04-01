using MedScan.Shared.Models;
using ZXing.Net.Maui;

namespace MedScan.Pages;

public partial class BarcodeScannerPage : ContentPage
{
    private readonly TaskCompletionSource<BarcodeScanResult> _completionSource = new();
    private readonly CancellationTokenSource _timeoutSource = new();
    private bool _isCompleted;

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
    }

    protected override void OnDisappearing()
    {
        CameraView.BarcodesDetected -= OnBarcodesDetected;
        _timeoutSource.Cancel();
        base.OnDisappearing();
    }

    private async Task StartTimeoutAsync()
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(20), _timeoutSource.Token);
            Complete(new BarcodeScanResult
            {
                Status = BarcodeScanStatus.NotDetected,
                Message = "Kaamera ei tuvastanud ravimit."
            });
            await CloseAsync();
        }
        catch (TaskCanceledException)
        {
        }
    }

    private async void CancelClicked(object? sender, EventArgs e)
    {
        Complete(new BarcodeScanResult
        {
            Status = BarcodeScanStatus.Canceled,
            Message = "Skannimine katkestati."
        });
        await CloseAsync();
    }

    private void ToggleTorchClicked(object? sender, EventArgs e)
    {
        CameraView.IsTorchOn = !CameraView.IsTorchOn;
    }

    private async void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        if (_isCompleted)
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

        Complete(new BarcodeScanResult
        {
            Status = BarcodeScanStatus.Success,
            Barcode = barcodeValue.Trim()
        });

        await CloseAsync();
    }

    private void Complete(BarcodeScanResult result)
    {
        if (_isCompleted)
        {
            return;
        }

        _isCompleted = true;
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
