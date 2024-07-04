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
            var s = new StringBuilder();

            // Sqlite approach to getting the mount paths.

            s.AppendLine("PATHS FROM SQLITE DATABASE\n");

            var localAppDataPath = UserDataPaths.GetDefault().LocalAppData;

            var syncDbPath = GetDbPath(localAppDataPath);

            SQLitePCL.Batteries_V2.Init();
            using var database = new SqliteConnection($"Data Source='{syncDbPath}'");
            using var cmdRoot = new SqliteCommand("SELECT * FROM roots", database);
            using var cmdMedia = new SqliteCommand(
                "SELECT * FROM media WHERE fs_type=10",
                database
            );

            database.Open();

            TraverseDbResponses(cmdRoot, cmdMedia, s);

            // Registry approach to getting the mount paths.

            s.AppendLine("\nPATHS FROM REGISTRY\n");

            var googleDriveRegValueJson = GetGoogleDriveRegValueJson(out var googleDriveRegValue);

            s.AppendLine(
                string.Join(
                    ", ",
                    googleDriveRegValueJson
                        ?.RootElement
                        .EnumerateObject()
                        .FirstOrDefault()
                        .Value.EnumerateArray()
                        .Select(item => item.GetProperty("value").GetProperty("mount_point_path"))
                        ?? []
                )
            );

            s.AppendLine("\nRAW GOOGLE DRIVE REGISTRY VALUE\n\n" + googleDriveRegValue);

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

        private JsonDocument? GetGoogleDriveRegValueJson(out object? googleDriveRegValue)
        {
            const string googleDriveRegKeyName = @"Software\Google\DriveFS";

            const string googleDriveRegValueName = "PerAccountPreferences";

            using var googleDriveRegKey = Registry.CurrentUser.OpenSubKey(googleDriveRegKeyName);

            googleDriveRegValue = googleDriveRegKey?.GetValue(googleDriveRegValueName);

            JsonDocument? googleDriveRegValueJson = null;
            try
            {
                googleDriveRegValueJson = JsonDocument.Parse(googleDriveRegValue as string ?? "");
            }
            catch (JsonException je) { }

            return googleDriveRegValueJson;
        }

        private void TraverseDbResponses(
            SqliteCommand? cmdRoot,
            SqliteCommand? cmdMedia,
            StringBuilder s
        )
        {
            var i = 0;
            var reader = cmdRoot?.ExecuteReader();
            while (reader?.Read() ?? false)
            {
                var path = reader["last_seen_absolute_path"].ToString();
                if (string.IsNullOrWhiteSpace(path))
                    continue;
                i++;
                s.AppendLine("cmdRoot read: " + path);
            }

            reader = cmdMedia?.ExecuteReader();

            while (reader?.Read() ?? false)
            {
                var path = reader["last_mount_point"].ToString();
                if (string.IsNullOrWhiteSpace(path))
                    continue;
                i++;
                s.AppendLine("cmdMedia read: " + path);
            }

            if (i == 0)
            {
                s.AppendLine("<none>");
            }
        }
    }
}
