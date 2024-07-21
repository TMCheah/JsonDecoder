using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
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
        private List<KeyValuePair<string, JToken>> _searchResults;
        private ObservableCollection<KeySelectionItem> _keySelectionItems;

        private class KeySelectionItem : INotifyPropertyChanged
        {
            public string Key { get; set; }
            private bool _isSelected;
            public bool IsSelected
            {
                get => _isSelected;
                set
                {
                    if (_isSelected != value)
                    {
                        _isSelected = value;
                        OnPropertyChanged();
                    }
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string jsonContent = await Task.Run(() => File.ReadAllText(openFileDialog.FileName));
                    _rootToken = await Task.Run(() => JToken.Parse(jsonContent));
                    _navigationStack.Clear();
                    DisplayProperties(_rootToken);
                    FileNameTextBlock.Text = $"Current File: {Path.GetFullPath(openFileDialog.FileName)}";
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
                ValueTextBox.Text = $"{nonIndexSelectedProperty}: is null";
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
            if (_rootToken is null)
                return;

            if (_navigationStack.Count > 0)
            {
                _navigationStack.Pop();
                JToken previousToken = _navigationStack.Count > 0 ? _navigationStack.Peek() : _rootToken;
                DisplayProperties(previousToken);
            }
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            if (_rootToken is null)
                return;

            SearchKeyTextBox.Text = "";
            SearchValueTextBox.Text = "";
            _navigationStack.Clear();
            DisplayProperties(_rootToken);
        }
        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            if (_rootToken is null)
                return;

            _rootToken = null;
            SearchKeyTextBox.Text = "";
            SearchValueTextBox.Text = "";
            _navigationStack.Clear();
            PropertyListBox.Items.Clear();
            _navigationStack.Clear();
            ValueTextBox.Text = "";
            FileNameTextBlock.Text = $"Current File: ";
        }

        private async void SearchKey_Click(object sender, RoutedEventArgs e)
        {
            string searchKey = SearchKeyTextBox.Text.Trim();
            if (string.IsNullOrEmpty(searchKey))
            {
                MessageBox.Show("Please enter a key to search.", "Search Key", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_rootToken is null)
            {
                MessageBox.Show("Please open a JSON file before search.", "Search Key", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var results = await Task.Run(() => SearchForKey(_rootToken, searchKey));
            DisplaySearchResults(results);
        }

        private async void SearchValue_Click(object sender, RoutedEventArgs e)
        {
            string searchValue = SearchValueTextBox.Text.Trim();
            if (string.IsNullOrEmpty(searchValue))
            {
                MessageBox.Show("Please enter a value to search.", "Search Value", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_rootToken is null)
            {
                MessageBox.Show("Please open a JSON file before search.", "Search Value", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var results = await Task.Run(() => SearchForValue(_rootToken, searchValue));
            DisplaySearchResults(results);
        }

        private async void CombinedSearch_Click(object sender, RoutedEventArgs e)
        {
            string searchKey = SearchKeyTextBox.Text.Trim();
            string searchValue = SearchValueTextBox.Text.Trim();

            if (string.IsNullOrEmpty(searchKey) || string.IsNullOrEmpty(searchValue))
            {
                MessageBox.Show("Please enter a key and/or value to search.", "Combined Search", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_rootToken is null)
            {
                MessageBox.Show("Please open a JSON file before search.", "Combined Search", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var results = await Task.Run(() => SearchForKeyValue(_rootToken, searchKey, searchValue));
            DisplaySearchResults(results);

            _searchResults = results;
            ExportButton.IsEnabled = _searchResults.Count > 0;
        }

        private List<KeyValuePair<string, JToken>> SearchForKey(JToken token, string key, string path = "")
        {
            var results = new List<KeyValuePair<string, JToken>>();

            if (token is JObject jObject)
            {
                foreach (var property in jObject.Properties())
                {
                    string currentPath = string.IsNullOrEmpty(path) ? property.Name : $"{path}.{property.Name}";
                    if (property.Name.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add(new KeyValuePair<string, JToken>(currentPath, property.Value));
                    }
                    results.AddRange(SearchForKey(property.Value, key, currentPath));
                }
            }
            else if (token is JArray jArray)
            {
                for (int i = 0; i < jArray.Count; i++)
                {
                    string currentPath = $"{path}[{i}]";
                    results.AddRange(SearchForKey(jArray[i], key, currentPath));
                }
            }

            return results;
        }

        private List<KeyValuePair<string, JToken>> SearchForValue(JToken token, string value, string path = "")
        {
            var results = new List<KeyValuePair<string, JToken>>();

            if (token is JObject jObject)
            {
                foreach (var property in jObject.Properties())
                {
                    string currentPath = string.IsNullOrEmpty(path) ? property.Name : $"{path}.{property.Name}";
                    if (property.Value.ToString().Equals(value))
                    {
                        results.Add(new KeyValuePair<string, JToken>(currentPath, property.Value));
                    }
                    if(property.Value.HasValues)
                        results.AddRange(SearchForValue(property.Value, value, currentPath));
                }
            }
            else if (token is JArray jArray)
            {
                for (int i = 0; i < jArray.Count; i++)
                {
                    string currentPath = $"{path}[{i}]";
                    results.AddRange(SearchForValue(jArray[i], value, currentPath));
                }
            }
            else if (token.HasValues == false && token.ToString().Equals(value))
            {
                results.Add(new KeyValuePair<string, JToken>(path, token));
            }

            return results;
        }

        private List<KeyValuePair<string, JToken>> SearchForKeyValue(JToken token, string key, string value, string currentKey = "", string path = "")
        {
            var results = new List<KeyValuePair<string, JToken>>();

            if (token is JObject jObject)
            {
                foreach (var property in jObject.Properties())
                {
                    string currentPath = string.IsNullOrEmpty(path) ? property.Name : $"{path}.{property.Name}";

                    if (property.Name.Equals(key, StringComparison.OrdinalIgnoreCase) && property.Value.ToString().Equals(value))
                    {
                        // Return the entire parent object containing the matching key-value pair
                        results.Add(new KeyValuePair<string, JToken>(path, jObject));
                        return results; // Stop searching further in this branch
                    }
                    results.AddRange(SearchForKeyValue(property.Value, key, value, property.Name, currentPath));
                }
            }
            else if (token is JArray jArray)
            {
                for (int i = 0; i < jArray.Count; i++)
                {
                    string currentPath = $"{path}[{i}]";
                    results.AddRange(SearchForKeyValue(jArray[i], key, value, currentKey, currentPath));
                }
            }
            else if (token.HasValues == false && currentKey.Equals(key) && token.ToString().Equals(value))
            {
                results.Add(new KeyValuePair<string, JToken>(path, token));
            }

            return results;
        }


        private void DisplaySearchResults(List<KeyValuePair<string, JToken>> results)
        {
            if (results.Count == 0)
            {
                ValueTextBox.Text = "Not available";
            }
            else
            {
                ValueTextBox.Clear();
                foreach (var result in results)
                {
                    string value = "";

                    if (result.Value is JObject jObject)
                    {
                        if (jObject.Count == 0)
                        {
                            value = "Empty object {}";
                            //ValueTextBox.AppendText($"Path: {result.Key}\nValue:  Empty object {{}}\n\n");
                        }
                        else
                        {
                            value = result.Value.ToString();
                            //ValueTextBox.AppendText($"Path: {result.Key}\nValue: {result.Value}\n\n");
                        }
                    }
                    else if (result.Value is JArray jArray)
                    {
                        if (jArray.Count == 0)
                        {
                            value = "Empty array []";
                            //ValueTextBox.AppendText($"Path: {result.Key}\nValue:  Empty array []\n\n");
                        }
                        else
                        {
                            value = result.Value.ToString();
                            //ValueTextBox.AppendText($"Path: {result.Key}\nValue: {result.Value}\n\n");
                        }
                    }
                    else if (result.Value.Type is JTokenType.Null)
                    {
                        value = "is nulll";
                    }
                    else
                    {
                        value = result.Value.ToString();
                    }
                    
                    ValueTextBox.AppendText($"Path: {result.Key}\nValue: {value}\n\n");
                    //ValueTextBox.AppendText($"Path: {result.Key}\nValue: {result.Value}\n\n");

                }
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var allKeys = GetAllKeys(_searchResults);
            _keySelectionItems = new ObservableCollection<KeySelectionItem>(
                allKeys.Select(k => new KeySelectionItem { Key = k, IsSelected = false })
            );
            KeyCheckBoxList.ItemsSource = _keySelectionItems;
            KeySelectionPopup.IsOpen = true;
        }

        private async void ConfirmExport_Click(object sender, RoutedEventArgs e)
        {
            var selectedKeys = _keySelectionItems
                .Where(item => item.IsSelected)
                .Select(item => item.Key)
                .ToList();

            if (selectedKeys.Count == 0)
            {
                MessageBox.Show("Please select at least one key to export.", "Export", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                FileName = "JsonDecoderExport.csv"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                await ExportToCsv(saveFileDialog.FileName, selectedKeys);
            }

            KeySelectionPopup.IsOpen = false;
        }

        private HashSet<string> GetAllKeys(List<KeyValuePair<string, JToken>> results)
        {
            var keys = new HashSet<string>();
            foreach (var result in results)
            {
                if (result.Value is JObject jObject)
                {
                    foreach (var property in jObject.Properties())
                    {
                        keys.Add(property.Name);
                    }
                }
            }
            return keys;
        }

        private async Task ExportToCsv(string fileName, List<string> selectedKeys)
        {
            ExportProgressBar.Visibility = Visibility.Visible;
            ExportProgressBar.Value = 0;

            await Task.Run(() =>
            {
                using (var writer = new StreamWriter(fileName))
                {
                    // Write header
                    writer.WriteLine(string.Join(",", selectedKeys));

                    // Write data
                    for (int i = 0; i < _searchResults.Count; i++)
                    {
                        var result = _searchResults[i];
                        var values = new List<string>();

                        foreach (var key in selectedKeys)
                        {
                            string value = "NA";
                            if (result.Value is JObject jObject && jObject.TryGetValue(key, out var jToken))
                            {
                                value = FlattenJToken(jToken);
                            }
                            values.Add($"\"{value}\"");
                        }

                        writer.WriteLine(string.Join(",", values));

                        // Update progress
                        Dispatcher.Invoke(() => ExportProgressBar.Value = (i + 1) * 100 / _searchResults.Count);
                    }
                }
            });

            ExportProgressBar.Visibility = Visibility.Collapsed;
            MessageBox.Show("Export completed successfully!", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private string FlattenJToken(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Null:
                    return "NULL";
                case JTokenType.Array:
                    if (!token.HasValues)
                        return "[]";
                    return $"[{string.Join("|", token.Select(FlattenJToken))}]";
                case JTokenType.Object:
                    if (!token.HasValues)
                        return "{}";
                    return $"{{{string.Join("|", token.Children<JProperty>().Select(p => $"{p.Name}:{FlattenJToken(p.Value)}"))}}}";
                default:
                    return token.ToString().Replace(",", "\\,").Replace("\"", "\"\"").Replace("|", "\\|");
            }
        }
    }
}
