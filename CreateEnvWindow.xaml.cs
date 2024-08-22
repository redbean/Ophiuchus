using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Ophiuchus
{
    public partial class CreateEnvWindow : Window
    {
        public string EnvironmentName { get; private set; }
        public string PythonVersion { get; private set; }

        public CreateEnvWindow()
        {
            InitializeComponent();
        }

        private void OnCreateClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CondaNameTextBox.Text))
            {
                MessageBox.Show("Please enter a name for the Conda environment.", "Input Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            EnvironmentName = CondaNameTextBox.Text;
            PythonVersion = (PythonVersionComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

            DialogResult = true;
            Close();
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}