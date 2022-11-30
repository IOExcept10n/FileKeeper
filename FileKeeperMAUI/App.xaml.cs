using Microsoft.Maui.Controls;

namespace FileKeeperMAUI
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();
        }
    }
}