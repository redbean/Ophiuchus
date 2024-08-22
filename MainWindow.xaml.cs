using System.CodeDom;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;

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
            LoadEnvironmentData();
            DataContext = this;
            IsLoading = true;
        }

        private void LoadEnvironmentData()
        {
            Task.Run(() =>
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
        }

        private async void OnClickEnv(object sender, SelectionChangedEventArgs e)
        {
            if (envList.SelectedItem != null)
            {
                this.LoadingIndicator.Visibility = Visibility.Visible;
                var deps = new List<Deps>();
                selectedEnvironment = envList.SelectedItem as string;
                
                var data = await ReadDependenciesAsync( selectedEnvironment );
                var lines = data.Split(new[] {'\n', '\r'}, StringSplitOptions.RemoveEmptyEntries);

                int startIndex = 0;
                // 첫 번째 줄이 헤더인지 확인
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
                this.LoadingIndicator.Visibility = Visibility.Collapsed;
                this.Status.Content = $"Selected Env : {selectedEnvironment}";
            }
        }

        private void OnBtn_CreateEnv(object sender, RoutedEventArgs e)
        {
            var createEnvWindow = new CreateEnvWindow();
            if (createEnvWindow.ShowDialog() == true)
            {
                string envName = createEnvWindow.EnvironmentName;
                string pythonVersion = createEnvWindow.PythonVersion;

                var proc = Utils.RunCondaCmd(VARS.GetCreateCmd(envName, pythonVersion));
                this.Status.Content = VARS.GetCreateCmd(envName, pythonVersion);
                try
                {
                    proc.Start();
                    proc.WaitForExit();
                }
                catch (Exception)
                {
                    MessageBox.Show($"Creating Conda is failed", "Environment Creation", MessageBoxButton.OK, MessageBoxImage.Error);
                    throw;
                }
            }
        }

        private void OnBtn_RemoveEnv(object sender, RoutedEventArgs e)
        {
            if(string.IsNullOrEmpty(this.selectedEnvironment))
            {
                MessageBox.Show($"Please Select Conda Env under the list", "Environment Removal", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
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
            LoadEnvironmentData();
        }

        private void OnBtn_CreateEnvSaveFile(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Not Implemented Yet.", "NotImplementedException", MessageBoxButton.OK, MessageBoxImage.Error);
        }



        private async Task<string> ReadDependenciesAsync(string env)
        {
            var proc = Utils.RunCondaCmd(VARS.CMD_DEP, env);
            try
            {
                proc.Start();
                string output = await proc.StandardOutput.ReadToEndAsync();
                return output;
            }
            catch 
            {
                return VARS.RESP_ERROR;
            }
        }


        private void OnBrowseClick(object sender, RoutedEventArgs e)
        {
            //var dialog = new System.Windows.Forms.FolderBrowserDialog();
            //System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            //if (result == System.Windows.Forms.DialogResult.OK)
            //{
            //    CondaPathInput.Text = dialog.SelectedPath;
            //}
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                CondaPathInput.Text = dialog.FileName; // 테스트용, 폴더 선택이 완료되면 선택된 폴더를 label에 출력
            }
        }

    }
}