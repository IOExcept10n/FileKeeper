using MyCryptLib;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace FileKeeperMAUI;

public partial class RecieveFilePage : ContentPage
{
    private DateTime currentTimeSeed;
    //private double timeShift;

    static DateTime RoundUp(DateTime dt, TimeSpan d)
    {
        return new DateTime((dt.Ticks + d.Ticks - 1) / d.Ticks * d.Ticks, dt.Kind);
    }

    public RecieveFilePage()
    {
        InitializeComponent();
        OnAppearing();
        // Initialize a timer to get new time seed every update. Every reset old qr code will be removed.
        currentTimeSeed = RecieveFilePage.RoundUp(DateTime.UtcNow, TimeSpan.FromMinutes(2));
        System.Timers.Timer timer = new System.Timers.Timer();
        double minutes = DateTime.UtcNow.Minute + (DateTime.UtcNow.Second / 60.0);
        double adjust = 2 - (minutes % 2);
        if (adjust < 1) adjust += 2;
        timer.Interval = adjust * 60 * 1000;
        timer.Elapsed += TimerElapsed;
        timer.Start();
#if DEBUG
        QRHint.Text = $"Debug: timeseed = {currentTimeSeed:G}";
#endif
    }

    private void TimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
    {
        (sender as System.Timers.Timer)!.Interval = 2 * 1000;
        currentTimeSeed = RecieveFilePage.RoundUp(DateTime.UtcNow, TimeSpan.FromMinutes(2));
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
#pragma warning disable CA1416
        MainReader.Options = new ZXing.Net.Maui.BarcodeReaderOptions()
        {
            Formats = ZXing.Net.Maui.BarcodeFormats.All,
            AutoRotate = false,
            Multiple = false,
            TryHarder = false,
        };
        MainReader.IsTorchOn = true;
    }

    private async void MainReader_BarcodesDetected(object sender, ZXing.Net.Maui.BarcodeDetectionEventArgs e)
    {
        await Dispatcher.DispatchAsync(async () =>
        {
            await MainPage.ShowToast("QR code has been founded!");
            ContentStack.Remove(MainReader);
            MainReader.BarcodesDetected -= MainReader_BarcodesDetected;
        });
        // Get a time key to first stage of a QR decryption.
        byte[] now = BitConverter.GetBytes(currentTimeSeed.ToBinary());
        // Decrypts QR text with a XOR algorithm.
        string decVig = e.Results[0].Value.CryptWithXor(Convert.ToBase64String(now));
        // Decrypts QR text with a Caesar algorithm.
        string decCaes = decVig.DecryptWithCaesar();
        // Next action we should parse a string to get new information.
        // We know that it can be random QR code which cannot be read.
        try
        {
            // Stage 1: split all text by lines.
            var lines = decCaes.Split('\n').Where(x => !string.IsNullOrEmpty(x)).Select(x => x.Replace("\r", "")).ToList();
            // Stage 2: Get values from the string.
            int pos = 0;
            string ak64 = lines[pos++];
            string fileName = lines[pos++];
            // Stage 3: Get ip addresses from the string.
            IPAddress[] addresses = new IPAddress[lines.Count - pos];
            for (int i = pos; i < lines.Count; i++)
            {
                addresses[i - pos] = IPAddress.Parse(lines[i]);
            }
            StringBuilder builder = new StringBuilder()
                .AppendLine("Start file getting...")
                //.Append($"ak = {ak64}")
                .Append("filename = ").AppendLine(fileName)
                .AppendJoin<IPAddress>("\n", addresses);

            await Dispatcher.DispatchAsync(() =>
            {
#if DEBUG
                QRHint.Text = builder.ToString();
#endif
                FileDescriptionFrame.IsVisible = true;
            });

            bool endOfReading = false;
            // Stage 3: Start TCP connection to get a file.
            TCPManager mgr = new TCPManager
            {
                // Prepare to notify user about updates.
                FileStatusUpdated = async (_, e) =>
                {
                    switch (e.Result)
                    {
                        case SendResult.Failure:
                            {
                                await DisplayAlert(Localization.FileErrorTitle, Localization.FileReceivingError, "OK");
                                break;
                            }
                        case SendResult.Progress:
                            {
                                await Dispatcher.DispatchAsync(() =>
                                {
                                    FileSendingProgress.Progress = e.Progress;
                                    QRHint.Text =
#if DEBUG
                                builder.ToString() + 
#endif
                                    $"{Localization.FileProgress} {e.FileSize * e.Progress / 1024:0.##} / {e.FileSize / 1024.0:0.##} Kb";
                                });
                                break;
                            }
                        case SendResult.Success:
                            {
                                await Dispatcher.DispatchAsync(async () =>
                                {
                                    QRHint.Text = $"{Localization.FileSaved}";
                                    FileDescriptionFrame.IsVisible = false;
                                    await MainPage.ShowToast(Localization.FileSaved);
                                });
                                endOfReading = true;
                                break;
                            }
                    }
                }
            };
            var privateKey = Properties.Resources.Rfile;
            byte[] ak = Convert.FromBase64String(ak64);
            //byte[] akDec = Cryptography.DecryptWithRSA(ak, privateKey);
            string aes = Convert.ToBase64String(ak);
            List<long> pings = new List<long>();
#if DEBUG
            int j = 0;
#endif
            foreach (var ip in addresses)
            {
                try
                {
                    long ping = await TCPManager.Ping(ip.ToString(), 8005, 100);
                    pings.Add(ping);
#if DEBUG
                    builder.AppendLine($"ping {j++}: {ping}");
                    await Dispatcher.DispatchAsync(() =>
                    {
                        QRHint.Text = builder.ToString();
                        FileDescriptionFrame.IsVisible = true;
                    });
#endif
                    if (ping != -1) await mgr.RunEncryptedReceivingClient(ip, 8005, MainPage.DefaultSavePath + "TransferedFiles/" + fileName, "");
                    if (endOfReading) break;
                }
                catch
                {
                }
            }
            if (endOfReading)
            {
                await Dispatcher.DispatchAsync(() =>
                {
                    QRHint.Text = new StringBuilder()
                    .Append("Файл ").Append(fileName).Append(" Успешно передан!")
                    .ToString();
                    FileDescriptionFrame.IsVisible = false;
                });
            }
            else
            {
                await Dispatcher.DispatchAsync(() =>
                {
                    QRHint.Text = "Не удалось получить файл, пожалуйста, попробуйте ещё раз.";
                    FileDescriptionFrame.IsVisible = false;
                });
            }
            await Task.Delay(1000);
            await Dispatcher.DispatchAsync(() =>
                {
                    ContentStack.Add(MainReader);
                    MainReader.BarcodesDetected += MainReader_BarcodesDetected;
                });
        }
        catch
#if DEBUG
        (Exception ex)
#endif
        {
#if DEBUG
            Dispatcher.Dispatch(() => QRHint.Text = ex.ToString());
#endif
            return;
        }
    }
}