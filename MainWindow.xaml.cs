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
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 



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
            DataContext = this;
        }
        private async Task LoadEnvironmentDataAsync()
        {
            this.GlobalLoadingIndicator.Visibility = Visibility.Visible;

            await Task.Run(() =>
            {
                try
                {
                    var proc = Utils.RunCondaCmd(VARS.CMD_ENV);
                    proc.Start();
                    var output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit();

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
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        Status.Content = $"Failed To Get Conda: {ex.Message}";
                    });
                }
            });
            this.GlobalLoadingIndicator.Visibility = Visibility.Collapsed;

        }

        private async void OnClickEnv(object sender, SelectionChangedEventArgs e)
        {
            if (envList.SelectedItem != null)
            {
                this.GlobalLoadingIndicator.Visibility = Visibility.Visible;
                var deps = new List<Deps>();
                selectedEnvironment = envList.SelectedItem as string;

                var data = await ReadDependenciesAsync( selectedEnvironment );

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
                this.GlobalLoadingIndicator.Visibility = Visibility.Visible;
                this.Status.Content = VARS.GetCreateCmd(envName, pythonVersion);
                
                await Task.Run(async () =>
                    {
                        await Utils.RunCommandWithVisibleCmd(VARS.GetCreateCmd(envName, pythonVersion));
                        
                    });
                this.GlobalLoadingIndicator.Visibility = Visibility.Collapsed;
            }
            LoadEnvironmentDataAsync();
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
            var (result, resultMsg)  = Shit(filePath, ref envName);
            if(!result)
            {
                MessageBox.Show($"{resultMsg}", "Yaml Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            MessageBox.Show($"{envName}", "Yaml Error", MessageBoxButton.OK, MessageBoxImage.Error);

            this.GlobalLoadingIndicator.Visibility = Visibility.Visible;
            this.Status.Content = VARS.GetImportCmd(envName, yamlFilePath:filePath);

            await Task.Run(async () =>
            {
                await Utils.RunCommandWithVisibleCmd(VARS.GetImportCmd(envName, yamlFilePath: filePath));

            });
            this.GlobalLoadingIndicator.Visibility = Visibility.Collapsed;
            await LoadEnvironmentDataAsync();

        }

        private void OnBtn_RemoveEnv(object sender, RoutedEventArgs e)
        {
            if(string.IsNullOrEmpty(this.selectedEnvironment))
            {
                MessageBox.Show($"Please Select Conda Env under the list", "Environment Removal", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            this.GlobalLoadingIndicator.Visibility = Visibility.Visible;

            var proc = Utils.RunCondaCmd(VARS.CMD_REMOVE_ENV, this.selectedEnvironment);
            try
            {
                proc.Start();
                proc.WaitForExit();
            }
            catch
            {
                MessageBox.Show($"Removal Env is Failed", "Environment Removal", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            this.GlobalLoadingIndicator.Visibility = Visibility.Collapsed;

            LoadEnvironmentDataAsync();
        }

        private async void OnBtn_ExportEnv(object sender, RoutedEventArgs e)
        {
            if(string.IsNullOrEmpty(this.selectedEnvironment))
            {
                MessageBox.Show("Please Select Environment", "Not Selected Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
                
            string envName = this.selectedEnvironment.ToString();
            this.GlobalLoadingIndicator.Visibility = Visibility.Visible;
            await Task.Run(async () =>
            {
                await Utils.RunCommandWithVisibleCmd(VARS.GetExportCmd(envName));
            });
            this.GlobalLoadingIndicator.Visibility = Visibility.Collapsed;

            await LoadEnvironmentDataAsync();
        }

        private void OnMakerClick(object sender, RoutedEventArgs e)
        {
            var Whomade = new Whomade();
            Whomade.ShowDialog();
        }


        //TODO 다른 클래스로 빼기
        private async Task<string> ReadDependenciesAsync(string env)
        {
            var proc = Utils.RunCondaCmd(VARS.CMD_DEP, env);
            try
            {
                IsLoading = true;

                proc.Start();
                string output = await proc.StandardOutput.ReadToEndAsync();
                IsLoading = false;
                return output;
            }
            catch 
            {
                return VARS.RESP_ERROR;
            }
        }


        private void OnBrowseClick(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                CondaPathInput.Text = dialog.FileName; // 테스트용, 폴더 선택이 완료되면 선택된 폴더를 label에 출력
            }
        }

        //TODO 다른 클래스로 빼기
        private (bool isSuccess, OphiuchusYAMLError error) Shit(string path, ref string envName)
        {
            List<string> requiredKeys = new List<string> { "name", "channels", "dependencies" };
            try
            {
                using (var reader = new StreamReader(path))
                {
                    var yaml = new YamlStream();
                    yaml.Load(reader);

                    var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;

                    var missingKeys = new List<string>();
                    foreach (var key in requiredKeys)
                    {
                        if (!mapping.Children.ContainsKey(key))
                        {
                            missingKeys.Add(key);
                        }
                    }
                    if (missingKeys.Count > 0)
                    {
                        return (false, OphiuchusYAMLError.YAMLMissingKeyError);
                    }
                    if (mapping.Children.TryGetValue("name", out var nameNode) && nameNode is YamlScalarNode scalarNode)
                    {
                        envName = scalarNode?.Value ?? string.Empty;
                    }
                    return (true, OphiuchusYAMLError.Success);
                }
            }
            catch (FileNotFoundException)
            {
                return (false, OphiuchusYAMLError.FileNotFoundError);
            }
            catch (YamlDotNet.Core.YamlException e)
            {
                return (false, OphiuchusYAMLError.YAMLParseError);
            }
            catch (Exception e)
            {
                return (false, OphiuchusYAMLError.ExceptionError);
            }
        }
        

    }
}