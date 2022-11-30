using MyCryptLib;
using System.Collections.ObjectModel;
using System.Net.Sockets;
using System.Net;
using System.Security.Cryptography;

namespace FileKeeperMAUI;

public partial class EncryptionPage : ContentPage
{
    // TODO: make keys system (private is universal, public is encrypt-only)
    private string publicKey;
    private string privateKey;

    public ObservableCollection<string> EncryptionModes { get; } = new ObservableCollection<string>()
	{
		Localization.AESModeName,
        //Don't use RSA because of its size limitations.
		//Localization.RSAModeName,
		// Don't use Caesar and Vigenere bacause of their text-only limitations.
		//Localization.CaesarModeName,
		//Localization.VigenereModeName
	};

	public EncryptionPage()
	{
		InitializeComponent();
		EncryptionModePicker.ItemsSource = EncryptionModes;
        EncryptionModePicker.SelectedIndex = 0;
        UpdateTextOnButtons(true);
	}

	private void EncryptionModePicker_SelectedIndexChanged(object sender, EventArgs e)
	{
        UpdateTextOnButtons(true);
    }

	private void CryptoMode_Toggled(object sender, ToggledEventArgs e)
	{
        UpdateTextOnButtons(false);
    }

	private void UpdateTextOnButtons(bool shouldReloadKeys)
	{
        switch (EncryptionModePicker.SelectedIndex)
        {
            // AES
            case 0:
                CipherDescription.Text = Localization.AESDescription;
                CryptoKey.IsVisible = true;
                RSAButtons.IsVisible = false;
                break;
            //// RSA
            //case 1:
            //    CipherDescription.Text = Localization.RSADescription;
            //    if (shouldReloadKeys) _ = Cryptography.CreateKeysForRSA(false, out publicKey, out privateKey);
            //    CryptoKey.IsVisible = false;
            //    RSAButtons.IsVisible = true;
            //    break;
            //case 2:
            //    CipherDescription.Text = Localization.CaesarDescription;
            //    CryptoKey.IsVisible = true;
            //    RSAButtons.IsVisible = false;
            //    break;
            //case 3:
            //    CipherDescription.Text = Localization.VigenereDescription;
            //    CryptoKey.IsVisible = true;
            //    RSAButtons.IsVisible = false;
            //    break;
        }
        // If decrypt mode
        if (CryptoMode.IsToggled)
		{
            DoCipherBtn.Text = Localization.DecryptFileText;
            LoadOpenKey.IsVisible = false;
            GenerateOpenKey.IsVisible = false;
            GeneratePrivateKey.IsVisible = false;
		}
        else
        {
            DoCipherBtn.Text = Localization.EncryptFileText;
            LoadOpenKey.IsVisible = true;
            GenerateOpenKey.IsVisible = true;
            GeneratePrivateKey.IsVisible = true;
        }
	}

    private async void DoCipherBtn_Clicked(object sender, EventArgs e)
    {
        PickOptions options = new PickOptions() { PickerTitle = Localization.FileSelectionTitle };
        var result = await PickFile(options);
        string prePath = MainPage.DefaultSavePath;
        if (result != null)
        {
            switch (EncryptionModePicker.SelectedIndex)
            {
                // AES
                case 0:
                    {
                        if (string.IsNullOrEmpty(CryptoKey.Text)) return;
                        if (CryptoMode.IsToggled)
                        {
                            string nameWithoutExtension = Path.GetFileNameWithoutExtension(result.FullPath);
                            if (nameWithoutExtension != null)
                                await Cryptography.DecryptFileAsync(result.FullPath, $"{prePath}Decrypted/{nameWithoutExtension}", CryptoKey.Text);
                            else return;
                        }
                        else
                        {
                            string nameWithoutExtension = Path.GetFileNameWithoutExtension(result.FullPath);
                            if (nameWithoutExtension != null)
                                await Cryptography.EncryptFileAsync(result.FullPath, $"{prePath}Encrypted/{nameWithoutExtension}.enc", CryptoKey.Text);
                            else return;
                        }
                        break;
                    }
                // RSA
                case 1:
                    {
                        if (CryptoMode.IsToggled)
                        {
                            if (privateKey == null) return;
                            string nameWithoutExtension = Path.GetFileNameWithoutExtension(result.FullPath);
                            if (nameWithoutExtension != null)
                            {
                                byte[] file = File.ReadAllBytes(result.FullPath);
                                file = Cryptography.DecryptWithRSA(file, privateKey);
                                string path = $"{prePath}Decrypted/{nameWithoutExtension}";
                                if (File.Exists(path))
                                {
                                    int i = 0;
                                    while (File.Exists(path + i + ".enc")) i++;
                                    path += i + ".enc";
                                }
                                File.WriteAllBytes(path, file);
                            }
                            else return;
                        }
                        else
                        {
                            string key = publicKey ?? privateKey;
                            if (key == null) return;
                            string nameWithoutExtension = Path.GetFileNameWithoutExtension(result.FullPath);
                            if (nameWithoutExtension != null)
                            {
                                byte[] file = File.ReadAllBytes(result.FullPath);
                                file = Cryptography.EncryptWithRSA(file, key);
                                string path = $"{prePath}Decrypted/{nameWithoutExtension}";
                                if (File.Exists(path))
                                {
                                    int i = 0;
                                    while (File.Exists(path + i + ".enc")) i++;
                                    path += i + ".enc";
                                }
                                File.WriteAllBytes(path, file);
                            }
                            else return;
                        }
                        break;
                    }
            }
            await MainPage.ShowToast(Localization.SuccessResult);
        }
    }

    internal static async Task<FileResult> PickFile(PickOptions options)
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(options);
            return result;
        }
        catch (Exception ex)
        {
            // TODO: make logging
            Console.WriteLine(ex);
        }
        return null;
    }

    private async void LoadOpenKey_Clicked(object sender, EventArgs e)
    {
        PickOptions options = new PickOptions() { PickerTitle = Localization.FileSelectionTitle };
        var result = await PickFile(options);
        if (result != null)
        {
            using StreamReader sr = new StreamReader(result.FullPath);
            publicKey = await sr.ReadToEndAsync();
            privateKey = null;
            await MainPage.ShowToast(Localization.FileLoaded);
        }
    }

    private async void GenerateOpenKey_Clicked(object sender, EventArgs e)
    {
        string prePath = MainPage.DefaultSavePath ?? Environment.ProcessPath ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        using StreamWriter sw = new StreamWriter($"{prePath}OpenKeys/okey_{DateTime.Now:yyyyMMddHHmmss}.okx");
        await sw.WriteAsync(publicKey);
        await MainPage.ShowToast(Localization.FileSaved);
    }

    private async void LoadPrivateKey_Clicked(object sender, EventArgs e)
    {
        PickOptions options = new PickOptions() { PickerTitle = Localization.FileSelectionTitle };
        var result = await PickFile(options);
        if (result != null)
        {
            using StreamReader sr = new StreamReader(result.FullPath);
            privateKey = await sr.ReadToEndAsync();
            publicKey = null;
            await MainPage.ShowToast(Localization.FileLoaded);
        }
    }

    private async void GeneratePrivateKey_Clicked(object sender, EventArgs e)
    {
        string prePath = MainPage.DefaultSavePath ?? Environment.ProcessPath ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        using StreamWriter sw = new StreamWriter($"{prePath}PrivateKeys/pkey_{DateTime.Now:yyyyMMddHHmmss}.pkx");
        await sw.WriteAsync(publicKey);
        await MainPage.ShowToast(Localization.FileSaved);
    }
}