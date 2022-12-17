#if ANDROID
using Android;
using Android.Accessibilityservice.AccessibilityService;
using Android.Content.PM;
using Android.OS;
using CommunityToolkit.Maui.Alerts;
#else
#pragma warning disable CS1998
#endif
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net;
using MyCryptLib;


namespace FileKeeperMAUI
{
    public partial class MainPage : ContentPage
    {
        private static List<IPAddress> iPAddresses = new List<IPAddress>();

        public static string DefaultSavePath { get; set; }

        //int count = 0;
        public static async Task ShowToast(string text)
        {
#if ANDROID
            CancellationTokenSource source = new CancellationTokenSource();
            var toast = Toast.Make(text);
            await toast.Show(source.Token);
#endif
        }
        public MainPage()
        {
            InitializeComponent();
            InitDirectories();
        }

        public async void InitDirectories()
        {
#if ANDROID
            if (await CheckAppPermissions() == PermissionStatus.Denied) return;
#endif
            await Task.Run(async () =>
            {
#if ANDROID
                DefaultSavePath = "/storage/emulated/0/FileKeeper/";
#elif WINDOWS
                DefaultSavePath = (Path.GetDirectoryName(Environment.ProcessPath) + "/FileKeeper/").Replace("\\", "/");
#endif
                if (DefaultSavePath != null)
                {
                    try
                    {
                        Directory.CreateDirectory(Path.Combine(DefaultSavePath, "Encrypted/"));
                        Directory.CreateDirectory(Path.Combine(DefaultSavePath, "Decrypted/"));
                        Directory.CreateDirectory(Path.Combine(DefaultSavePath, "OpenKeys/"));
                        Directory.CreateDirectory(Path.Combine(DefaultSavePath, "PrivateKeys/"));
                        Directory.CreateDirectory(Path.Combine(DefaultSavePath, "TransferedFiles/"));
                    }
                    catch
                    {
#if ANDROID
                        await ShowToast("Please, give permissions to use application.");
#endif
                    }
                }
            });

        }

        //private void OnCounterClicked(object sender, EventArgs e)
        //{
        //    count++;

        //    if (count % 10 == 1)
        //        CounterBtn.Text = $"Clicked {count} time";
        //    else
        //        CounterBtn.Text = $"Clicked {count} times";


        //}
#if ANDROID
        private async Task<PermissionStatus> CheckAppPermissions()
        {
            if ((int)Build.VERSION.SdkInt < 23)
            {
                return PermissionStatus.Unknown;
            }
            else
            {
                PermissionStatus status = await Permissions.CheckStatusAsync<Permissions.StorageWrite>();
                if (status != PermissionStatus.Granted)
                {
                    if (Permissions.ShouldShowRationale<Permissions.StorageWrite>())
                    {
                        var result = await DisplayPromptAsync(Localization.StorageWritePromptTitle, Localization.StorageWritePromptText);
                    }
                    status = await Permissions.RequestAsync<Permissions.StorageWrite>();
                }
                return status;
            }
        }
#endif

        internal static List<IPAddress> GetIPAddress()
        {
            var result = new List<IPAddress>();
            try
            {
                var upAndNotLoopbackNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces().Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback
                                                                                                              && n.OperationalStatus == OperationalStatus.Up);
                foreach (var networkInterface in upAndNotLoopbackNetworkInterfaces)
                {
                    var iPInterfaceProperties = networkInterface.GetIPProperties();

                    var unicastIpAddressInformation = iPInterfaceProperties.UnicastAddresses.FirstOrDefault(u => u.Address.AddressFamily == AddressFamily.InterNetwork);
                    if (unicastIpAddressInformation == null) continue;

                    result.Add(unicastIpAddressInformation.Address);
                }
                iPAddresses = result;
            }
            catch
            {
            }
            finally
            {
            }
            return iPAddresses;
        }
    }
}