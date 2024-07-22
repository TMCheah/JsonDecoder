# JSON Decoder

JSON Decoder is an interactive WPF application that allows users to navigate and explore JSON structures visually. It provides an intuitive interface for opening JSON files and traversing through their hierarchical structure, making it easy to inspect complex JSON data.

If you find this project useful, please consider giving it a star ⭐️ on GitHub. It helps others discover the project and motivates further development!

## Features

- **File Selection**: Open any JSON file from your local system.
- **Interactive Navigation**: Click through JSON properties and array elements to explore nested structures.
- **Breadcrumb Navigation**: Easily go back to previous levels in the JSON hierarchy.
- **Value Display**: View the contents of JSON properties, including special handling for empty objects, empty arrays, and null values.
- **Current File Display**: Always know which file you're currently exploring.
- **Search Functionality**:
	- **Search by Key**: Find all occurrences of a specific key in the JSON structure.
	- **Search by Value**: Locate all instances of a particular value within the JSON.
	- **Combined Search**: Search for specific key-value pairs in the JSON.
- **CSV Export**:
	- Export search results to a CSV file.
	- Select specific keys to include in the export.
	- Flatten nested structures and arrays for easy viewing in spreadsheet applications.
	- Special handling for null values, empty objects, and empty arrays.
	- Visual progress indicator during export.

## Getting Started

### Prerequisites

- Windows operating system
- .NET Framework 4.7.2 or later
- Visual Studio 2019 or later (for development)

### Installation

1. Clone the repository:
   ```
   git clone https://github.com/tmcheah/json-decoder.git
   ```
2. Open the solution file `JsonDecoder.sln` in Visual Studio.
3. Restore NuGet packages:
   - Right-click on the solution in Solution Explorer
   - Select "Restore NuGet Packages"
4. Build the solution:
   - Click on "Build" in the top menu
   - Select "Build Solution" or press F6

### Usage

1. Run the application from Visual Studio or navigate to the build output directory and run `JsonDecoder.exe`.
2. Click the "Open JSON File" button to select a JSON file from your system.
3. The left panel will display the top-level properties or array indices of your JSON.
4. Click on any property or index to navigate deeper into the JSON structure.
5. The right panel will display values for terminal nodes (strings, numbers, booleans, or null).
6. Use the "Back" button to return to the previous level in the JSON hierarchy.
7. Click "Reset" to return to the top level of the JSON structure.
8. To search within the JSON:
   - Enter a key in the "Search Key" text box and click "Search Key" to find all occurrences of that key.
   - Enter a value in the "Search Value" text box and click "Search Value" to find all instances of that value.
   - Use both text boxes and click "Combined Search" to find specific key-value pairs.
   - Search results will be displayed in the right panel, showing the path and value of each match.
9. To export search results to CSV:
   - After performing a search, click the "Export to CSV" button.
   - In the popup, select the keys you want to include in the export.
   - Choose a filename and location for the CSV file.
   - The export process will begin, with a progress bar indicating the status.
   - Once complete, you can open the CSV file in your preferred spreadsheet application.
   - Note: The CSV export handles special cases as follows:
      - Null values are represented as "NULL"
      - Empty objects are represented as "{}"
      - Empty arrays are represented as "[]"
      - Nested structures are flattened and separated by pipe (|) characters  

## Technical Details

- **Language**: C#
- **Framework**: WPF (.NET Framework)
- **JSON Parsing**: Newtonsoft.Json

## Project Structure

- `MainWindow.xaml`: The main interface of the application.
- `MainWindow.xaml.cs`: The code-behind file containing the application logic.

## Dependencies

- Newtonsoft.Json: A popular high-performance JSON framework for .NET.

## Contributing

Contributions to improve JSON Decoder are welcome. Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.

## Acknowledgments

- Thanks to Newtonsoft for their excellent Json.NET library.
- Inspired by the need for a simple, interactive JSON exploration tool.

