using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Win32;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WinUiTest
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
        }

        private void myButton_Click(object sender, RoutedEventArgs e)
        {
            var appDataPath = UserDataPaths.GetDefault().LocalAppData;

            const string googleDriveRegKeyName = @"Software\Google\DriveFS";

            const string googleDriveRegValueName = "PerAccountPreferences";

            using var googleDriveRegKey = Registry.CurrentUser.OpenSubKey(googleDriveRegKeyName);

            var googleDriveRegValue = googleDriveRegKey?.GetValue(googleDriveRegValueName);

            var googleDriveRegValueJson = JsonDocument.Parse(googleDriveRegValue as string ?? "");

            var syncDbPath = GetDbPath(appDataPath);

            SQLitePCL.Batteries_V2.Init();
            using var database = new SqliteConnection($"Data Source='{syncDbPath}'");
            using var cmdRoot = new SqliteCommand("SELECT * FROM roots", database);
            using var cmdMedia = new SqliteCommand(
                "SELECT * FROM media WHERE fs_type=10",
                database
            );

            var s = new StringBuilder();

            database.Open();

            var reader = cmdRoot.ExecuteReader();
            while (reader.Read())
            {
                s.AppendLine("hi");
            }

            s.AppendLine("LocalAppData: " + appDataPath);
            s.AppendLine("google drive reg val: " + googleDriveRegValue);
            s.AppendLine(
                "google drive reg val json: "
                    + googleDriveRegValueJson.RootElement.EnumerateObject().Count()
            );
            s.AppendLine("syncDbPath: " + syncDbPath);

            myButton.Content = s;
        }

        private string GetDbPath(string appDataPath)
        {
            return StorageFile
                .GetFileFromPathAsync(
                    Path.Combine(appDataPath, @"Google\DriveFS\root_preference_sqlite.db")
                )
                .AsTask()
                .Result.Path;
        }
    }
}
