
namespace FileKeeperMAUI;

public partial class ScanQRPage : ContentPage, IQueryAttributable
{
    private bool initialized;
    private Page previousPage;
    private bool sendMode;

	public ScanQRPage()
	{
		InitializeComponent();
	}

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (initialized)
        {

            return;
        }
        previousPage = query["page"] as Page;
        sendMode = (bool)query["send"];

    }

    private void MainReader_BarcodesDetected(object sender, ZXing.Net.Maui.BarcodeDetectionEventArgs e)
    {

    }
}