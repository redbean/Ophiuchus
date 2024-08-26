using System.CodeDom;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection.Emit;
using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;
using YamlDotNet.RepresentationModel;

namespace Ophiuchus
{
    // main window - Only UI work in this, Every Conda cmds are in other thread.
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public ObservableCollection<string> EnvironmentList { get; } = new ObservableCollection<string>();

        public event PropertyChangedEventHandler PropertyChanged;

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged(nameof(IsLoading));
                }
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private string selectedEnvironment;

        public MainWindow()
        {
            InitializeComponent();
            Start();
        }

        private void Start()
        {
            _ = LoadEnvironmentDataAsync();
            SetCondaPath();
            DataContext = this;
        }


        private void SetCondaPath()
        {
            CondaPathInput.Text = string.Empty;
            if (CondaServiceHelpers.IsAnacondaInstalled())
            {
                string anacondaPath = VARS.PATH_CONDA_DEFAULTPATH;
                CondaPathInput.Text = anacondaPath;
            }
            else
            {
                MessageBox.Show("Conda 패스를 설정해 주세요", "Conda Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        // To Avoid async contamination, made it.
        // read all conda envs for every updating envs
        private async Task LoadEnvironmentDataAsync()
        {
            string output = string.Empty;
            try
            {
                output = await CmdWithOutput(VARS.CMD_ENV);
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    Status.Content = $"Failed To Get Conda: {ex.Message}";
                });
            }

            var lines = output.Split('\n');
            var newEnvironments = new List<string>();
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith("#"))
                {
                    string envName = line.Split(' ')[0].Trim();
                    if (!string.IsNullOrWhiteSpace(envName))
                    {
                        newEnvironments.Add(envName);
                    }
                }
            }

            Dispatcher.Invoke(() =>
            {
                EnvironmentList.Clear();
                foreach (var env in newEnvironments)
                {
                    EnvironmentList.Add(env);
                }
                Status.Content = EnvironmentList.Count > 0 ? "Conda Lists are Loaded" : "No Conda environments found";
            });
        }

        //on list view's click, list all dependencies
        private async void OnClickEnv(object sender, SelectionChangedEventArgs e)
        {
            if (envList.SelectedItem != null)
            {
                this.GlobalLoadingIndicator.Visibility = Visibility.Visible;
                var deps = new List<Deps>();
                selectedEnvironment = envList.SelectedItem as string;
                var data = await CondaServiceHelpers.ReadDependenciesAsync( selectedEnvironment );

                var lines = data.Split(new[] {'\n', '\r'}, StringSplitOptions.RemoveEmptyEntries);
                int startIndex = 0;

                foreach ( string line in lines )
                {
                    bool hasHeader = line.StartsWith("#");
                    startIndex = hasHeader ? startIndex + 1 : startIndex;
                }
                foreach( string line in lines.Skip(startIndex)) 
                { 
                    var parts = line.Split(new[] {' '}  , StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 3)
                    {
                        deps.Add(new Deps(parts[0], parts[1], parts[^1]));
                    }
                }
                this.dependency.ItemsSource = null; 
                this.dependency.ItemsSource = deps;
                this.GlobalLoadingIndicator.Visibility = Visibility.Collapsed;
                this.Status.Content = $"Selected Env : {selectedEnvironment}";
            }
        }

        private async void OnBtn_CreateEnv(object sender, RoutedEventArgs e)
        {
            var createEnvWindow = new CreateEnvWindow();
            if (createEnvWindow.ShowDialog() == true)
            {
                string envName = createEnvWindow.EnvironmentName;
                string pythonVersion = createEnvWindow.PythonVersion;
                this.Status.Content = VARS.GetCreateCmd(envName, pythonVersion);
                await CmdRun(VARS.GetCreateCmd(envName, pythonVersion));
            }
            await LoadEnvironmentDataAsync();
        }

        private async void OnBtn_ImportEnv(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.Filters.Add(new CommonFileDialogFilter("YAML Files", "*.yaml,*.yml"));
            dialog.Filters.Add(new CommonFileDialogFilter("All Files", "*.*"));
            dialog.DefaultExtension = ".yaml";
            string filePath = string.Empty;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                filePath = dialog.FileName;
            }
            string envName = string.Empty ;
            var (result, resultMsg)  = CondaServiceHelpers.ReadYamlFile(filePath, ref envName);
            if(!result)
            {
                MessageBox.Show($"{resultMsg}", "Yaml Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            await CmdRun(VARS.GetImportCmd(envName, yamlFilePath: filePath));
            await LoadEnvironmentDataAsync();

        }

        private async void OnBtn_RemoveEnv(object sender, RoutedEventArgs e)
        {
            if(string.IsNullOrEmpty(this.selectedEnvironment))
            {
                MessageBox.Show($"Please Select Conda Env under the list", "Environment Removal", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            await CmdRun(VARS.GetRemoveCmd(this.selectedEnvironment));
            await LoadEnvironmentDataAsync();
        }

        private async void OnBtn_ExportEnv(object sender, RoutedEventArgs e)
        {
            if(string.IsNullOrEmpty(this.selectedEnvironment))
            {
                MessageBox.Show("Please Select Environment", "Not Selected Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if(string.IsNullOrEmpty(CondaExportPath.Text))
            {
                MessageBox.Show("Please Select Export Path", "Not Selected Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if(!Directory.Exists(CondaExportPath.Text))
            {
                MessageBox.Show("Selected Path is not validate.", "Path Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }


            string envName = this.selectedEnvironment.ToString();
            await CmdRun(VARS.GetExportCmd(CondaExportPath.Text, envName));
            await LoadEnvironmentDataAsync();
        }

        private void OnMakerClick(object sender, RoutedEventArgs e)
        {
            var Whomade = new Whomade();
            Whomade.ShowDialog();
        }

        private void OnBrowseClick(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                CondaPathInput.Text = dialog.FileName; 
            }
        }

        private void OnExportSelectBrowseClick(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                CondaExportPath.Text = dialog.FileName;
            }
        }


        private void OnDoubleClickEnv(object sender, RoutedEventArgs e)
        {
            var path = CondaPathInput.Text;
            var result = CondaServiceHelpers.OpenNewCmdWithActivation(path, selectedEnvironment);
            if (result != OphiuchusError.Success)
            {
                MessageBox.Show("Something's Wrong", $"{result.ToString()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private async Task CmdRun(string cmd)
        {
            this.GlobalLoadingIndicator.Visibility = Visibility.Visible;
            await Task.Run(async () =>
            {
                await CondaServiceHelpers.RunCommandWithVisibleCmd(cmd);
            });
            this.GlobalLoadingIndicator.Visibility = Visibility.Collapsed;
        }

        private async Task<string> CmdWithOutput(string cmd)
        {
            string result = string.Empty;
            this.GlobalLoadingIndicator.Visibility = Visibility.Visible;
            await Task.Run(async () =>
            {
                result = await CondaServiceHelpers.RunCommandOutput(cmd);
            });
            this.GlobalLoadingIndicator.Visibility = Visibility.Collapsed;
            return result;
        }


    }
}