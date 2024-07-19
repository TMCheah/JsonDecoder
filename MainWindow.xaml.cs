using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace JsonDecoder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private JToken _rootToken;
        private Stack<JToken> _navigationStack = new Stack<JToken>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string jsonContent = File.ReadAllText(openFileDialog.FileName);
                try
                {
                    _rootToken = JToken.Parse(jsonContent);
                    _navigationStack.Clear();
                    DisplayProperties(_rootToken);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error parsing JSON: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DisplayProperties(JToken token)
        {
            PropertyListBox.Items.Clear();
            ValueTextBox.Clear();

            if (token is JObject jObject)
            {
                foreach (var property in jObject.Properties())
                {
                    PropertyListBox.Items.Add(property.Name);
                }
            }
            else if (token is JArray jArray)
            {
                for (int i = 0; i < jArray.Count; i++)
                {
                    PropertyListBox.Items.Add($"[{i}]");
                }
            }
            else
            {
                ValueTextBox.Text = token.ToString();
            }
        }

        private void PropertyListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PropertyListBox.SelectedItem != null)
            {
                string selectedProperty = PropertyListBox.SelectedItem.ToString();
                JToken currentToken = _navigationStack.Count > 0 ? _navigationStack.Peek() : _rootToken;

                JToken nextToken;
                if (currentToken is JObject jObject)
                {
                    nextToken = jObject[selectedProperty];
                }
                else if (currentToken is JArray jArray)
                {
                    int index = int.Parse(selectedProperty.Trim('[', ']'));
                    nextToken = jArray[index];
                }
                else
                {
                    return;
                }

                _navigationStack.Push(nextToken);
                DisplayProperties(nextToken);
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (_navigationStack.Count > 0)
            {
                _navigationStack.Pop();
                JToken previousToken = _navigationStack.Count > 0 ? _navigationStack.Peek() : _rootToken;
                DisplayProperties(previousToken);
            }
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            _navigationStack.Clear();
            DisplayProperties(_rootToken);
        }
    }
}
