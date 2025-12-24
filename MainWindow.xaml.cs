using ConnectToOneDriveAzurePortal.AzurePortalConfigurations;
using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace ConnectToOneDriveAzurePortal
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private GraphService _graphService;

        public MainWindow()
        {
            InitializeComponent();
            _graphService = new GraphService(new GraphClientFactory());
        }

        private void AzurePortalWindow_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private async void button_Click(object sender, RoutedEventArgs e)
        {
            this.Logo.Source = null;

            // Step 1: Get the graph service client for the current user in OneDrive
            var graphClient = await _graphService.Client;

            // Step 2: Access OneDrive and locate the folder "LogoClients"
            var drive = await graphClient.Me.Drive.GetAsync();

            // Step 3: To get the root drive item, use the drive.Id and access it via graphClient.Drives[drive.Id]

            var currentDrive = graphClient.Drives[drive.Id];

            // Step 4: Find the folder "LogoClients"
            var folderResult = await currentDrive.Items["root"].Children.GetAsync(requestConfig =>
            {
                requestConfig.QueryParameters.Filter = "name eq 'LogoClients'";
            });

            var folder = folderResult?.Value.ToList().FirstOrDefault();

            // Step 5: Get the files in the folder
            if (folder == null)
            {
                MessageBox.Show("Folder 'LogoClients' not found in OneDrive.");
                return;
            }

            // Step 6: Get the files in the "LogoClients" folder
            var files = await currentDrive.Items[folder.Id].Children.GetAsync();

            var systemDriveRoot = Path.GetPathRoot(Environment.SystemDirectory); // Typically "C:\"

            string downloadFolderPath = Path.Combine(
                systemDriveRoot,
                "Logos");


            if (!Directory.Exists(downloadFolderPath))
            {
                Directory.CreateDirectory(downloadFolderPath);
            }

            // Step 7: Download the images from OneDrive to the local folder "Logos"
            var downloadPath = string.Empty;

            foreach (var file in files.Value)
            {
                // MessageBox.Show($"File: {file.Name} - ID: {file.Id}");

                downloadPath = Path.Combine(downloadFolderPath, file.Name);

                if (!File.Exists(downloadPath))
                {
                    var stream = await currentDrive.Items[file.Id].Content.GetAsync();

                    using (var fileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                    {
                        await stream.CopyToAsync(fileStream);
                    }
                }

            }

            this.Logo.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(downloadPath));
        }
    }
}

