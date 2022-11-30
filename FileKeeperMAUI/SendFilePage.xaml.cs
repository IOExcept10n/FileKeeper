using MyCryptLib;
using FileKeeperMAUI.Properties;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Timers;

namespace FileKeeperMAUI;

public partial class SendFilePage : ContentPage
{
	public static int AESKeyByteSize { get; } = 64;
	private DateTime currentTimeSeed;
	private CancellationTokenSource serverTokenSource;
	private double timeShift = 0;

    DateTime RoundUp(DateTime dt, TimeSpan d)
    {
        return new DateTime((dt.Ticks + d.Ticks - 1) / d.Ticks * d.Ticks, dt.Kind);
    }

    public SendFilePage()
	{
		InitializeComponent();
		// Initialize a timer to get new time seed every update. Every reset old qr code will be removed.
		currentTimeSeed = RoundUp(DateTime.UtcNow, TimeSpan.FromMinutes(2));
        System.Timers.Timer timer = new System.Timers.Timer();
		double minutes = DateTime.UtcNow.Minute + (DateTime.UtcNow.Second / 60.0);
        double adjust = 2 - (minutes % 2);
		if (adjust < 1) adjust += 2;
        timeShift = timer.Interval = adjust * 60 * 1000;
		timer.Elapsed += TimerElapsed;
		timer.Start();
    }

	private void TimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
	{
		(sender as System.Timers.Timer).Interval = 2 * 60000;
		currentTimeSeed = RoundUp(DateTime.UtcNow, TimeSpan.FromMinutes(2));
		Dispatcher.Dispatch(() =>
		{
			DisableQRCode();
			FileSelectionBtn.IsEnabled = true;
		});
	}

	private async void FileSelectionBtn_Clicked(object sender, EventArgs e)
	{
		serverTokenSource?.Cancel();
		// User selects a file.
        PickOptions options = new PickOptions() { PickerTitle = Localization.FileSelectionTitle };
        var result = await EncryptionPage.PickFile(options);
        FileSelectionBtn.IsEnabled = false;
        if (result != null)
		{
			// Do it in code block to remove all links after work to clear the memory.
			{
				// Create a random AES encryption key to encrypt the file.
				byte[] aesKey = RandomNumberGenerator.GetBytes(AESKeyByteSize);
				string aesKeyString = Convert.ToBase64String(aesKey);
				// Encrypt the key to send it
				var privateKey = Properties.Resources.Rfile;
                //var encKey = Cryptography.EncryptWithRSA(aesKey, privateKey);
				// Get self IP addresses list to use them for the connection.
				var ips = MainPage.GetIPAddress();
				// Start perparing text for the qr code.
				StringBuilder qrBuilder = new StringBuilder();
                qrBuilder.AppendLine(Convert.ToBase64String(aesKey));
                qrBuilder.AppendLine(Path.GetFileName(result.FullPath));
				//qrBuilder.AppendLine($"{Convert.ToBase64String(privateKey.D)}");
				//qrBuilder.AppendLine($"{Convert.ToBase64String(privateKey.DP)}");
				//qrBuilder.AppendLine($"{Convert.ToBase64String(privateKey.DQ)}");
				//qrBuilder.AppendLine($"{Convert.ToBase64String(privateKey.Exponent)}");
				//qrBuilder.AppendLine($"{Convert.ToBase64String(privateKey.InverseQ)}");
				//qrBuilder.AppendLine($"{Convert.ToBase64String(privateKey.Modulus)}");
				//qrBuilder.AppendLine($"{Convert.ToBase64String(privateKey.P)}");
				//qrBuilder.AppendLine($"{Convert.ToBase64String(privateKey.Q)}");
				int m = 0;
 				foreach (var ip in ips)
				{
					if (m == 3) break;
                    qrBuilder.Append(ip).AppendLine();
					m++;
				}
				// Get date and hour of now to send a file.
				byte[] now = BitConverter.GetBytes(currentTimeSeed.ToBinary());
				// Get and encrypt the string before sending.
				string qr = qrBuilder.ToString()
					.EncryptWithCaesar()
					.CryptWithXor(Convert.ToBase64String(now));
				// Set the text for the QR code generator.
				ResetAndEnableQRCode(qr);
				// Create new TCP manager to send a file with the TCP connection.
				TCPManager mgr = new TCPManager();
				// Create a cancellation token to cancel sending after first connection.
				serverTokenSource = new CancellationTokenSource();
				// Cancels the token to decline all pending connections except first.
				mgr.NewClientConnection = async (s, e) =>
				{
					await Dispatcher.DispatchAsync(async () =>
					{
					    FileSelectionBtn.IsEnabled = true;
						await MainPage.ShowToast("Client has been successfully connected!");
					});
				};
				mgr.FileStatusUpdated = async (_, e) =>
				{
					await Dispatcher.DispatchAsync(async () =>
					{
                        switch (e.Result)
                        {
                            case SendResult.Success:
                                FileSelectionBtn.IsEnabled = true;
                                await MainPage.ShowToast("File was successfully sent!");
                                serverTokenSource.Cancel();
                                DisableQRCode();
                                FileSelectionBtn.IsEnabled = true;
                                break;
                            case SendResult.Failure:
                                await DisplayAlert(Localization.FileErrorTitle, Localization.FileSendingErrorText, "OK");
                                break;
                        }
                    });
				};
				// Creates a server to send a file.
				await Task.Run(() => mgr.RunEncryptedSendingServer(8005, result.FullPath, aesKeyString, serverTokenSource.Token));
				// Don't forget to clear all data to prevent it from stealing.
				Array.Clear(aesKey);
				//Array.Clear(encKey);
			}
			// Start to collecting memory to remove all encryption results.
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
    }

#pragma warning disable CA1416 // Restrictions for qr code.
	private void ResetAndEnableQRCode(string message)
	{
		WarningText.IsVisible = true;
		WarningText.Text = Localization.FileResetWarning
			+ $" {DateTime.Now + TimeSpan.FromMilliseconds(timeShift):HH:mm}"
#if DEBUG
			+ $"\n DEBUG: timeseed = {currentTimeSeed}" + "\n" +
			message
#endif
			;
		BarcodeGenerator.Value = message;
    }

	private void DisableQRCode()
	{
		WarningText.IsVisible = false;
        BarcodeGenerator.Value = "";
    }
}