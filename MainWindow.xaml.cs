using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Windows;
using System.Windows.Controls;

namespace JsonDecoder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MemoryMappedFile _memoryMappedFile;
        private long _fileSize;
        private Stack<LazyJToken> _navigationStack = new Stack<LazyJToken>();
        private ConcurrentDictionary<string, long> _keyIndex = new ConcurrentDictionary<string, long>();
        private CancellationTokenSource _indexingCancellationTokenSource;
        private CancellationTokenSource _searchCancellationTokenSource;
        private string nonIndexSelectedProperty = "";

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
                    await LoadJsonFile(openFileDialog.FileName);
                    FileNameTextBlock.Text = $"Current File: {Path.GetFullPath(openFileDialog.FileName)}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error parsing JSON: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task LoadJsonFile(string filePath)
        {
            nonIndexSelectedProperty = "";
            _fileSize = new FileInfo(filePath).Length;
            _memoryMappedFile = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open);

            _navigationStack.Clear();
            _keyIndex.Clear();

            using (var stream = _memoryMappedFile.CreateViewStream())
            using (var reader = new StreamReader(stream))
            using (var jsonReader = new JsonTextReader(reader))
            {
                var rootToken = await Task.Run(() => JToken.ReadFrom(jsonReader));
                var lazyRootToken = new LazyJToken(rootToken, 0, _fileSize);
                _navigationStack.Push(lazyRootToken);
            }

            DisplayProperties(_navigationStack.Peek());

            // Start background indexing
            StartBackgroundIndexing();
        }

        private void StartBackgroundIndexing()
        {
            _indexingCancellationTokenSource?.Cancel();
            _indexingCancellationTokenSource = new CancellationTokenSource();

            Task.Run(() => BuildKeyIndex(_indexingCancellationTokenSource.Token));
        }

        private async Task BuildKeyIndex(CancellationToken cancellationToken)
        {
            Dispatcher.Invoke(() => {
                IndexingProgressBar.Visibility = Visibility.Visible;
                IndexingProgressBar.Value = 0;
            });

            using (var stream = _memoryMappedFile.CreateViewStream())
            using (var reader = new StreamReader(stream))
            using (var jsonReader = new JsonTextReader(reader))
            {
                long totalBytes = stream.Length;
                long processedBytes = 0;

                while (await jsonReader.ReadAsync())
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    if (jsonReader.TokenType == JsonToken.PropertyName)
                    {
                        string key = jsonReader.Value.ToString();
                        long position = jsonReader.LinePosition;
                        _keyIndex.TryAdd(key, position);
                    }

                    processedBytes += jsonReader.LinePosition;
                    Dispatcher.Invoke(() => {
                        IndexingProgressBar.Value = (double)processedBytes / totalBytes * 100;
                    });
                }
            }

            Dispatcher.Invoke(() => {
                IndexingProgressBar.Visibility = Visibility.Collapsed;
            });
        }

        private void DisplayProperties(LazyJToken lazyToken)
        {
            PropertyListBox.Items.Clear();
            ValueTextBox.Clear();

            if (lazyToken.Token is JObject jObject)
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
            else if (lazyToken.Token is JArray jArray)
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
            else if (lazyToken.Token.Type == JTokenType.Null)
            {
                ValueTextBox.Text = $"{nonIndexSelectedProperty}: is null";
            }
            else
            {
                ValueTextBox.Text = $"{nonIndexSelectedProperty}: {lazyToken.Token.ToString()}";
            }

            if (ValueTextBox.Text == string.Empty)
            {
                ValueTextBox.Text = lazyToken.Token.ToString();
            }

            //ValueTextBox.Text = lazyToken.Token.ToString();
        }

        private void PropertyListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PropertyListBox.SelectedItem != null)
            {
                string selectedProperty = PropertyListBox.SelectedItem.ToString();
                selectedProperty = selectedProperty.Replace($"{nonIndexSelectedProperty} ", "");

                LazyJToken currentToken = _navigationStack.Peek();

                LazyJToken nextToken;
                JToken valueToken = null;
                if (currentToken.Token is JObject jObject)
                {
                    nonIndexSelectedProperty = selectedProperty;

                    var property = jObject.Property(selectedProperty);
                    nextToken = new LazyJToken(property.Value, property.Value.Path);
                    valueToken = property.Value;
                }
                else if (currentToken.Token is JArray jArray)
                {
                    int index = int.Parse(selectedProperty.Trim('[', ']'));
                    nextToken = new LazyJToken(jArray[index], jArray[index].Path);
                    valueToken = jArray[index];
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
            if (_navigationStack.Count > 1)
            {
                _navigationStack.Pop();
                DisplayProperties(_navigationStack.Peek());
            }
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            if (_navigationStack.Count > 0)
            {
                while (_navigationStack.Count > 1)
                {
                    _navigationStack.Pop();
                }
                DisplayProperties(_navigationStack.Peek());
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            _memoryMappedFile?.Dispose();
            _memoryMappedFile = null;
            _navigationStack.Clear();
            _keyIndex.Clear();
            PropertyListBox.Items.Clear();
            ValueTextBox.Clear();
            FileNameTextBlock.Text = "Current File: ";
            _indexingCancellationTokenSource?.Cancel();
            nonIndexSelectedProperty = "";
        }

        private async void SearchKey_Click(object sender, RoutedEventArgs e)
        {
            if (_searchCancellationTokenSource != null)
            {
                _searchCancellationTokenSource.Cancel();
                _searchCancellationTokenSource = null;
            }

            string searchKey = SearchKeyTextBox.Text.Trim();
            if (string.IsNullOrEmpty(searchKey) || _memoryMappedFile == null)
            {
                MessageBox.Show("Please enter a key to search and ensure a JSON file is loaded.", "Search Key", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _searchCancellationTokenSource = new CancellationTokenSource();
            CancelSearchButton.Visibility = Visibility.Visible;

            var results = await Task.Run(() => SearchForKey(searchKey, _searchCancellationTokenSource.Token));
            DisplaySearchResults(results);

            CancelSearchButton.Visibility = Visibility.Collapsed;
            _searchCancellationTokenSource = null;
        }

        private async void SearchValue_Click(object sender, RoutedEventArgs e)
        {
            if (_searchCancellationTokenSource != null)
            {
                _searchCancellationTokenSource.Cancel();
                _searchCancellationTokenSource = null;
            }

            string searchValue = SearchValueTextBox.Text.Trim();
            if (string.IsNullOrEmpty(searchValue) || _memoryMappedFile == null)
            {
                MessageBox.Show("Please enter a value to search and ensure a JSON file is loaded.", "Search Value", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _searchCancellationTokenSource = new CancellationTokenSource();
            CancelSearchButton.Visibility = Visibility.Visible;

            var results = await Task.Run(() => SearchForValue(searchValue, _searchCancellationTokenSource.Token));
            DisplaySearchResults(results);

            CancelSearchButton.Visibility = Visibility.Collapsed;
            _searchCancellationTokenSource = null;
        }


        private async void CombinedSearch_Click(object sender, RoutedEventArgs e)
        {
            if (_searchCancellationTokenSource != null)
            {
                _searchCancellationTokenSource.Cancel();
                _searchCancellationTokenSource = null;
            }

            string searchKey = SearchKeyTextBox.Text.Trim();
            string searchValue = SearchValueTextBox.Text.Trim();

            if (string.IsNullOrEmpty(searchKey) || string.IsNullOrEmpty(searchValue) || _memoryMappedFile == null)
            {
                MessageBox.Show("Please enter a key and value to search, and ensure a JSON file is loaded.", "Combined Search", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _searchCancellationTokenSource = new CancellationTokenSource();
            CancelSearchButton.Visibility = Visibility.Visible;

            var results = await Task.Run(() => SearchForKeyValue(searchKey, searchValue, _searchCancellationTokenSource.Token));
            DisplaySearchResults(results);

            CancelSearchButton.Visibility = Visibility.Collapsed;
            _searchCancellationTokenSource = null;
        }

        private void CancelSearch_Click(object sender, RoutedEventArgs e)
        {
            _searchCancellationTokenSource?.Cancel();
            CancelSearchButton.Visibility = Visibility.Collapsed;
        }

        private IEnumerable<KeyValuePair<string, JToken>> SearchForKey(string key, CancellationToken cancellationToken)
        {
            if (_keyIndex.TryGetValue(key, out long position))
            {
                using (var stream = _memoryMappedFile.CreateViewStream(position, _fileSize - position))
                using (var reader = new StreamReader(stream))
                using (var jsonReader = new JsonTextReader(reader))
                {
                    var token = JToken.ReadFrom(jsonReader);
                    if (cancellationToken.IsCancellationRequested)
                        yield break;

                    yield return new KeyValuePair<string, JToken>(token.Path, token);
                }
            }
        }

        private IEnumerable<KeyValuePair<string, JToken>> SearchForValue(string value, CancellationToken cancellationToken)
        {
            using (var stream = _memoryMappedFile.CreateViewStream())
            using (var reader = new StreamReader(stream))
            using (var jsonReader = new JsonTextReader(reader))
            {
                while (jsonReader.Read())
                {
                    if (cancellationToken.IsCancellationRequested)
                        yield break;

                    if (jsonReader.TokenType == JsonToken.String ||
                        jsonReader.TokenType == JsonToken.Integer ||
                        jsonReader.TokenType == JsonToken.Float)
                    {
                        if (jsonReader.Value.ToString() == value)
                        {
                            var token = JToken.ReadFrom(jsonReader);
                            yield return new KeyValuePair<string, JToken>(token.Path, token);
                        }
                    }
                }
            }
        }

        private IEnumerable<KeyValuePair<string, JToken>> SearchForKeyValue(string key, string value, CancellationToken cancellationToken)
        {
            if (_keyIndex.TryGetValue(key, out long position))
            {
                using (var stream = _memoryMappedFile.CreateViewStream(position, _fileSize - position))
                using (var reader = new StreamReader(stream))
                using (var jsonReader = new JsonTextReader(reader))
                {
                    var token = JToken.ReadFrom(jsonReader);
                    if (token.ToString() == value)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            yield break;

                        yield return new KeyValuePair<string, JToken>(token.Path, token);
                    }
                }
            }
        }

        private void DisplaySearchResults(IEnumerable<KeyValuePair<string, JToken>> results)
        {
            ValueTextBox.Clear();
            foreach (var result in results)
            {
                ValueTextBox.AppendText($"Path: {result.Key}\nValue: {result.Value}\n\n");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _memoryMappedFile?.Dispose();
            _indexingCancellationTokenSource?.Cancel();
        }

        //private void ExportButton_Click(object sender, RoutedEventArgs e)
        //{
        //    var allKeys = GetAllKeys(_searchResults);
        //    _keySelectionItems = new ObservableCollection<KeySelectionItem>(
        //        allKeys.Select(k => new KeySelectionItem { Key = k, IsSelected = false })
        //    );
        //    KeyCheckBoxList.ItemsSource = _keySelectionItems;
        //    KeySelectionPopup.IsOpen = true;
        //}
        //
        //private async void ConfirmExport_Click(object sender, RoutedEventArgs e)
        //{
        //    var selectedKeys = _keySelectionItems
        //        .Where(item => item.IsSelected)
        //        .Select(item => item.Key)
        //        .ToList();
        //
        //    if (selectedKeys.Count == 0)
        //    {
        //        MessageBox.Show("Please select at least one key to export.", "Export", MessageBoxButton.OK, MessageBoxImage.Warning);
        //        return;
        //    }
        //
        //    var saveFileDialog = new SaveFileDialog
        //    {
        //        Filter = "CSV files (*.csv)|*.csv",
        //        FileName = "JsonDecoderExport.csv"
        //    };
        //
        //    if (saveFileDialog.ShowDialog() == true)
        //    {
        //        await ExportToCsv(saveFileDialog.FileName, selectedKeys);
        //    }
        //
        //    KeySelectionPopup.IsOpen = false;
        //}
        //
        //private HashSet<string> GetAllKeys(List<KeyValuePair<string, JToken>> results)
        //{
        //    var keys = new HashSet<string>();
        //    foreach (var result in results)
        //    {
        //        if (result.Value is JObject jObject)
        //        {
        //            foreach (var property in jObject.Properties())
        //            {
        //                keys.Add(property.Name);
        //            }
        //        }
        //    }
        //    return keys;
        //}
        //
        //private async Task ExportToCsv(string fileName, List<string> selectedKeys)
        //{
        //    ExportProgressBar.Visibility = Visibility.Visible;
        //    ExportProgressBar.Value = 0;
        //
        //    await Task.Run(() =>
        //    {
        //        using (var writer = new StreamWriter(fileName))
        //        {
        //            // Write header
        //            writer.WriteLine(string.Join(",", selectedKeys));
        //
        //            // Write data
        //            for (int i = 0; i < _searchResults.Count; i++)
        //            {
        //                var result = _searchResults[i];
        //                var values = new List<string>();
        //
        //                foreach (var key in selectedKeys)
        //                {
        //                    string value = "NA";
        //                    if (result.Value is JObject jObject && jObject.TryGetValue(key, out var jToken))
        //                    {
        //                        value = FlattenJToken(jToken);
        //                    }
        //                    values.Add($"\"{value}\"");
        //                }
        //
        //                writer.WriteLine(string.Join(",", values));
        //
        //                // Update progress
        //                Dispatcher.Invoke(() => ExportProgressBar.Value = (i + 1) * 100 / _searchResults.Count);
        //            }
        //        }
        //    });
        //
        //    ExportProgressBar.Visibility = Visibility.Collapsed;
        //    MessageBox.Show("Export completed successfully!", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
        //}
        //
        //private string FlattenJToken(JToken token)
        //{
        //    switch (token.Type)
        //    {
        //        case JTokenType.Null:
        //            return "NULL";
        //        case JTokenType.Array:
        //            if (!token.HasValues)
        //                return "[]";
        //            return $"[{string.Join("|", token.Select(FlattenJToken))}]";
        //        case JTokenType.Object:
        //            if (!token.HasValues)
        //                return "{}";
        //            return $"{{{string.Join("|", token.Children<JProperty>().Select(p => $"{p.Name}:{FlattenJToken(p.Value)}"))}}}";
        //        default:
        //            return token.ToString().Replace(",", "\\,").Replace("\"", "\"\"").Replace("|", "\\|");
        //    }
        //}

    }
}
