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
        private string nonIndexSelectedProperty = "";

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
                    FileNameTextBlock.Text = $"Current File: {Path.GetFileName(openFileDialog.FileName)}";
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
                if (jObject.Count == 0)
                {
                    ValueTextBox.Text = $"{nonIndexSelectedProperty}: Empty object {{}}";
                }
                else
                {
                    foreach (var property in jObject.Properties())
                    {
                        PropertyListBox.Items.Add(property.Name);
                    }
                }
            }
            else if (token is JArray jArray)
            {
                if (jArray.Count == 0)
                {
                    ValueTextBox.Text = $"{nonIndexSelectedProperty}: Empty array []";
                }
                else
                {
                    for (int i = 0; i < jArray.Count; i++)
                    {
                        PropertyListBox.Items.Add($"{nonIndexSelectedProperty} [{i}]");
                    }
                }
            }
            else if (token.Type == JTokenType.Null)
            {
                ValueTextBox.Text = $"{nonIndexSelectedProperty}: null";
            }
            else
            {
                ValueTextBox.Text = $"{nonIndexSelectedProperty}: {token.ToString()}";
            }

            if (ValueTextBox.Text == string.Empty)
            {
                ValueTextBox.Text = token.ToString();
            }
        }

        private void PropertyListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PropertyListBox.SelectedItem != null)
            {
                string selectedProperty = PropertyListBox.SelectedItem.ToString();
                selectedProperty = selectedProperty.Replace($"{nonIndexSelectedProperty} ", "");

                JToken currentToken = _navigationStack.Count > 0 ? _navigationStack.Peek() : _rootToken;

                JToken nextToken;
                if (currentToken is JObject jObject)
                {
                    nonIndexSelectedProperty = selectedProperty;
                    nextToken = jObject[selectedProperty];
                }
                else if (currentToken is JArray jArray)
                {
                    int index = int.Parse(selectedProperty.Trim('[', ']'));
                    nextToken = jArray[index];
                }
                else
                {
                    nonIndexSelectedProperty = selectedProperty;
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
