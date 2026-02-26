namespace MedScan {
    public partial class MainPage : ContentPage {
        public MainPage() {
            InitializeComponent();
            barcodeReader.Options = new ZXing.Net.Maui.BarcodeReaderOptions {
                Formats = ZXing.Net.Maui.BarcodeFormats.All,
                AutoRotate = true,
                Multiple = true    
            };
        }

        private void barcodeReader_BarcodesDetected(object sender, ZXing.Net.Maui.BarcodeDetectionEventArgs e) {
            var first = e.Results?.FirstOrDefault();

            if (first is null) return;

            Dispatcher.DispatchAsync(async ()=> {
                await DisplayAlertAsync("Barcode Detected", first.Value, "OK");
            });
        }
    }
}
