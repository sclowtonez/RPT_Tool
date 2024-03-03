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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using Microsoft.Win32;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Windows.Controls.Primitives;



namespace XML_Editor
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

        }
        private int removedClassCount = 0;



        private void CheckIgnoreLinesWarning()
        {
            if (CheckBox_IgnoreLines.IsChecked == true)
            {
                MessageBox.Show("Ignoring the Line Count can cause the application to freeze temporarily while the XML loads. The 'Balance Nominal to Min Ratio' button may also temporarily freeze the application while it processes and Syntax Highlighting will be disabled for performance since line count is 25,000+.\n\nClean[CE][offlineDB] will still function.", "Caution: Ignoring Line Count!", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }


        private async void OpenXmlFile_Click(object sender, RoutedEventArgs e)
        {
            await OpenXmlFileAsync();
        }
        private async Task OpenXmlFileAsync()
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "XML Files (*.xml)|*.xml|All Files (*.*)|*.*",
                Multiselect = false // Allow selecting only one file
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string selectedFilePath = openFileDialog.FileName;

                    // Read the content of the selected XML file asynchronously
                    string xmlContent = await ReadFileAsync(selectedFilePath);

                    // Display the content in the RichTextBox with syntax highlighting
                    xmlContentRichTextBox.Document = HighlightXmlSyntax_Types(xmlContent);

                    // Get only the file name from the full path
                    string fileName = System.IO.Path.GetFileName(selectedFilePath);

                    // Update the status bar with the file name
                    UpdateStatusBar($"{fileName} : loaded successfully");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error reading the file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }



        private async void OpenXml_TypesFolder_Click(object sender, RoutedEventArgs e)
        {
            CheckIgnoreLinesWarning();
            await OpenXmlTypesFoldersAsync("dayzOffline");
        }
        private async Task OpenXmlTypesFoldersAsync(string folderName)
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string specificPath = System.IO.Path.Combine(desktopPath, folderName);

            // Check if the folder exists
            if (!Directory.Exists(specificPath))
            {
                MessageBox.Show($"The folder '{folderName}' does not exist on the desktop.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "XML Files (*.xml)|*.xml|All Files (*.*)|*.*",
                Multiselect = false, // Allow selecting only one file
                InitialDirectory = specificPath // Set the initial directory to the specific path
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Create a progress window
                    var progressWindow = new ProgressWindow();
                    progressWindow.Show();

                    // Initialize percent variable
                    int percent = 0;

                    StringBuilder allXmlContent = new StringBuilder(); // Declare and initialize here

                    // Report progress using Progress<T>
                    var progress = new Progress<int>(p =>
                    {
                        progressWindow.UpdateProgress(p);
                    });

                    // Report status using Progress<T>
                    var statusProgress = new Progress<string>(status =>
                    {
                        progressWindow.UpdateStatus(status);
                    });

                    foreach (string xmlFilePath in openFileDialog.FileNames)
                    {
                        // Check the state of the "Ignore 25,000 Line Max" checkbox
                        if (!CheckBox_IgnoreLines.IsChecked == true)
                        {
                            // Check line count before opening the file
                            int lineCount = await GetFileLineCountAsync(xmlFilePath);
                            if (lineCount > 25000)
                            {
                                MessageBox.Show($"The XML file '{System.IO.Path.GetFileName(xmlFilePath)}' has 25,000+ lines. Consider breaking the XML into two files and try again.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                                continue; // Skip to the next file if line count exceeds 25,000
                            }
                        }

                        // Read file content and report progress
                        string xmlFileContent = await ReadFileAsync(xmlFilePath, progress, statusProgress);

                        // Add the opened XML file and its content to the dictionary
                        AddOpenedXmlFile(System.IO.Path.GetFileName(xmlFilePath), xmlFileContent);

                        // Append content to allXmlContent
                        allXmlContent.AppendLine(xmlFileContent);
                        ExtractTypeName(xmlFileContent);
                    }

                    // Indicate loading is complete
                    progressWindow.Close();

                    // Concatenate and display the content in the RichTextBox with syntax highlighting
                    xmlContentRichTextBox2.Document = HighlightXmlSyntax_Types(allXmlContent.ToString());
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error reading the file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        private async Task<int> GetFileLineCountAsync(string filePath)
        {
            // Asynchronously read the file and count the lines
            using (StreamReader reader = new StreamReader(filePath))
            {
                int lineCount = 0;

                while (await reader.ReadLineAsync() != null)
                {
                    lineCount++;
                }

                return lineCount;
            }
        }

        private string ExtractTypeName(string xmlContent)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlContent);

            XmlNode typeNode = xmlDoc.SelectSingleNode("//type");
            if (typeNode != null && typeNode.Attributes["name"] != null)
            {
                return typeNode.Attributes["name"].Value;
            }

            return "Unknown";
        }

        /*
        private async Task<string> ReadAndHighlightFileAsync(string filePath, IProgress<int> progress)
        {
            try
            {
                using (var reader = new StreamReader(filePath))
                {
                    // Get the total file length
                    long totalLength = reader.BaseStream.Length;
                    int bytesRead = 0;
                    int bufferSize = 8192; // You can adjust the buffer size as needed

                    // Read the file in chunks
                    char[] buffer = new char[bufferSize];
                    int read;
                    StringBuilder result = new StringBuilder();

                    while ((read = await reader.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        result.Append(buffer, 0, read);
                        bytesRead += read;

                        // Report progress for reading the file
                        int readProgress = (int)((double)bytesRead / totalLength * 100);
                        progress.Report(readProgress);
                    }

                    // Highlight the XML syntax
                    string highlightedContent = HighlightXmlSyntax_Types(result.ToString()).ToString();

                    // Report progress for highlighting
                    progress.Report(100);

                    return highlightedContent;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading file '{filePath}': {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return string.Empty; // or handle the error as needed
            }
        }
        */
        private async Task<string> ReadFileAsync(string filePath, IProgress<int> progress, IProgress<string> status)
        {
            try
            {
                using (var reader = new StreamReader(filePath))
                {
                    long totalLength = reader.BaseStream.Length;
                    int bytesRead = 0;
                    int bufferSize = 8192;
                    char[] buffer = new char[bufferSize];
                    int read;
                    StringBuilder result = new StringBuilder();

                    while ((read = await reader.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        result.Append(buffer, 0, read);
                        bytesRead += read;

                        // Report progress
                        int percent = (int)((double)bytesRead / totalLength * 100);
                        progress.Report(percent);

                        // Report status
                        status.Report($"Loading: {System.IO.Path.GetFileName(filePath)}");
                    }

                    return result.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading file '{filePath}': {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return string.Empty;
            }
        }
        private async Task<string> ReadFileAsync(string filePath)
        {
            try
            {
                using (var reader = new StreamReader(filePath))
                {
                    return await reader.ReadToEndAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading file '{filePath}': {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return string.Empty; // or handle the error as needed
            }
        }









        private void OpenRPT_File_Click(object sender, RoutedEventArgs e)
        {
            OpenRPTFile();
        }
        private void OpenRPTFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "RPT Files (*.rpt)|*.rpt|All Files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                string selectedFilePath = openFileDialog.FileName;

                try
                {
                    // Read the content of the RPT file
                    string rptContent = File.ReadAllText(selectedFilePath);

                    // Convert string content to FlowDocument
                    FlowDocument flowDocument = new FlowDocument(new Paragraph(new Run(rptContent)));

                    // Display the content in the RichTextBox
                    xmlContentRichTextBox3.Document = flowDocument;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error reading the file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }





        private void ExtractAndSaveTypeContent_Click(object sender, RoutedEventArgs e)
        {
            string typeContent = ExtractEventContent(xmlContentRichTextBox.Document);

            if (!string.IsNullOrEmpty(typeContent))
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string outputPath = System.IO.Path.Combine(desktopPath, "TypeContent.txt");

                try
                {
                    // Save the type content to a text file on the desktop
                    File.WriteAllText(outputPath, typeContent);
                    MessageBox.Show($"Type content saved to {outputPath}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving the file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("No type content found.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private void ExtractAndSaveTypes_Classname_Click(object sender, RoutedEventArgs e)
        {
            string typeContent = ExtractTypeContent(xmlContentRichTextBox2.Document);

            if (!string.IsNullOrEmpty(typeContent))
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string outputPath = System.IO.Path.Combine(desktopPath, "TypeContent.txt");

                try
                {
                    // Save the type content to a text file on the desktop
                    File.WriteAllText(outputPath, typeContent);
                    MessageBox.Show($"Type content saved to {outputPath}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving the file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("No type content found.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private string ExtractEventContent(FlowDocument document)
        {
            if (document == null)
                return string.Empty;

            TextRange textRange = new TextRange(document.ContentStart, document.ContentEnd);
            string xmlContent = textRange.Text;

            // Use regex to find content between quotes after type=
            MatchCollection matches = Regex.Matches(xmlContent, @"type=""([^""]*)""");

            // Join matched values into a single string
            string typeContent = string.Join(Environment.NewLine, matches.Cast<Match>().Select(match => match.Groups[1].Value));

            return typeContent;
        }
        private string ExtractTypeContent(FlowDocument document)
        {
            if (document == null)
                return string.Empty;

            TextRange textRange = new TextRange(document.ContentStart, document.ContentEnd);
            string xmlContent = textRange.Text;

            // Use regex to find content between quotes after type=
            MatchCollection matches = Regex.Matches(xmlContent, @"type name=""([^""]*)""");

            // Join matched values into a single string
            string typeContent = string.Join(Environment.NewLine, matches.Cast<Match>().Select(match => match.Groups[1].Value));

            return typeContent;
        }



        /* //OLD SyntaxHighlighting

        private FlowDocument HighlightXmlSyntax(string xml)
        {
            FlowDocument flowDocument = new FlowDocument();
            Paragraph paragraph = new Paragraph();

            // Basic regex patterns for XML syntax highlighting
            string tagPattern = @"<[^>]+>";
            string attributePattern = @"(\w+)=[""'][^""']*[>""']";

            // Create a brush for tag highlighting (you can customize colors as needed)
            SolidColorBrush tagBrush = new SolidColorBrush(Colors.Blue);

            // Create a brush for attribute highlighting
            SolidColorBrush attributeBrush = new SolidColorBrush(Colors.Green);

            // Apply syntax highlighting
            int currentPosition = 0;

            MatchCollection matches = Regex.Matches(xml, $"{tagPattern}|{attributePattern}");

            foreach (Match match in matches)
            {
                int matchIndex = match.Index;
                int matchLength = match.Length;

                // Add the plain text before the current match
                if (currentPosition < matchIndex)
                {
                    string plainText = xml.Substring(currentPosition, matchIndex - currentPosition);
                    paragraph.Inlines.Add(new Run(plainText));
                }

                // Create a Run for the matched content
                var run = new Run(xml.Substring(matchIndex, matchLength));

                // Set the foreground color based on the type of match
                if (match.Value.StartsWith("<") && match.Value.EndsWith(">"))
                {
                    // Tag highlighting
                    run.Foreground = tagBrush;
                }
                else
                {
                    // Attribute highlighting
                    run.Foreground = attributeBrush;
                }

                // Add the Run to the Paragraph
                paragraph.Inlines.Add(run);

                // Update the current position
                currentPosition = matchIndex + matchLength;
            }

            // Add any remaining text after the last match
            if (currentPosition < xml.Length)
            {
                string remainingText = xml.Substring(currentPosition);
                paragraph.Inlines.Add(new Run(remainingText));
            }

            // Add the Paragraph to the FlowDocument
            flowDocument.Blocks.Add(paragraph);

            return flowDocument;
        }
        */


        private FlowDocument HighlightXmlSyntax_Types(string xml)
        {
            FlowDocument flowDocument = new FlowDocument();
            Paragraph paragraph = new Paragraph();

            // Define regex patterns for different XML constructs
            string tagPattern = @"<type(?:\s+\w+(?:\s*=\s*""[^""]*""|\s*=\s*'\S*'))*\s*>([\s\S]*?)<\/type>";
            string attributePattern = @"types";
            string attribute2Pattern = @"type";
            string tierPattern = @"""([^""]*)""";
            string contentPattern = @">([^<]+)<";

            // Combine all patterns
            string combinedPattern = $"{tagPattern}|{attributePattern}|{attribute2Pattern}|{tierPattern}|{contentPattern}";

            // Create brushes for syntax highlighting
            SolidColorBrush purpleBrush = new SolidColorBrush(Colors.Purple);
            SolidColorBrush greenBrush = new SolidColorBrush(Colors.Green);
            SolidColorBrush blueBrush = new SolidColorBrush(Colors.Blue);
            SolidColorBrush pinkBrush = new SolidColorBrush(Colors.Pink);
            SolidColorBrush redBrush = new SolidColorBrush(Colors.Red);

            // Apply syntax highlighting
            int currentPosition = 0;

            MatchCollection matches = Regex.Matches(xml, combinedPattern);

            foreach (Match match in matches)
            {
                int matchIndex = match.Index;
                int matchLength = match.Length;

                // Add the plain text before the current match
                string plainText = xml.Substring(currentPosition, matchIndex - currentPosition);
                paragraph.Inlines.Add(new Run(plainText));

                // Create a Run for the matched content
                var run = new Run(xml.Substring(matchIndex, matchLength));


                if (match.Value.Contains("<") || match.Value.Contains(">") || match.Value.Contains("</"))
                {
                    run.Foreground = redBrush;
                }

                if (match.Value.Contains("type"))
                {
                    run.Foreground = pinkBrush;
                }

                if (match.Value.StartsWith(""))
                {
                    var tierMatch = Regex.Match(match.Value, tierPattern);

                    if (tierMatch.Success && tierMatch.Groups.Count > 1)
                    {
                        // Use the captured group for the name
                        string nameValue = tierMatch.Groups[1].Value;

                        // Change the Text inside the quotes for the name
                        run.Foreground = greenBrush;
                    }
                }



                // Add the Run to the Paragraph
                paragraph.Inlines.Add(run);

                // Update the current position
                currentPosition = matchIndex + matchLength;
            }

            // Add any remaining text after the last match
            string remainingText = xml.Substring(currentPosition);
            paragraph.Inlines.Add(new Run(remainingText));

            // Add the Paragraph to the FlowDocument
            flowDocument.Blocks.Add(paragraph);

            return flowDocument;
        }





        private void ClearEvents_Click(object sender, RoutedEventArgs e)
        {
            // Clear the content of xmlContentRichTextBox2
            xmlContentRichTextBox.Document.Blocks.Clear();
            ClearStatusBar();
        }
        private void ClearTypes_Click(object sender, RoutedEventArgs e)
        {
            // Clear the content of xmlContentRichTextBox2
            xmlContentRichTextBox2.Document.Blocks.Clear();
            ClearStatusBar();
        }
        private void ClearRPT_Click(object sender, RoutedEventArgs e)
        {
            // Clear the content of xmlContentRichTextBox2
            xmlContentRichTextBox3.Document.Blocks.Clear();
            ClearStatusBar();
        }
        private void ClearStatusBar()
        {
            statusBarLabel.Items.Clear();
        }





        private void BalanceNominalMin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get the XML content from xmlContentRichTextBox2
                string xmlContent = new TextRange(xmlContentRichTextBox2.Document.ContentStart, xmlContentRichTextBox2.Document.ContentEnd).Text;

                // Ensure xmlContent is not null or empty
                if (!string.IsNullOrEmpty(xmlContent))
                {
                    // Check if CheckIgnoreLinesWarning is checked
                    bool ignoreLinesWarning = CheckBox_IgnoreLines.IsChecked ?? false;

                    // Check line count only if CheckIgnoreLinesWarning is not checked
                    if (!ignoreLinesWarning)
                    {
                        int lineCount = xmlContent.Split('\n').Length;

                        if (lineCount > 25000)
                        {
                            MessageBox.Show("XML content exceeds 25,000 lines. Please reduce the size.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return; // Do not execute further if line count exceeds 25,000
                        }
                    }

                    // Load XML document
                    XDocument xmlDoc = XDocument.Parse(xmlContent);

                    // Iterate through each <type> element
                    foreach (XElement typeElement in xmlDoc.Descendants("type"))
                    {
                        XElement nominalElement = typeElement.Element("nominal");
                        XElement minElement = typeElement.Element("min");

                        if (nominalElement != null && minElement != null)
                        {
                            // Parse values
                            if (int.TryParse(nominalElement.Value, out int nominal) &&
                                int.TryParse(minElement.Value, out int currentMin))
                            {
                                // Check if Nominal is higher or equal to Min
                                if (nominal >= currentMin)
                                {
                                    // Do nothing, leave it alone
                                }
                                else
                                {
                                    // Set min to be 50% of nominal only if nominal is less than currentMin
                                    // If nominal is zero, set min to zero as well
                                    int newMinValue = (nominal == 0) ? 0 : (int)(nominal * 0.5);
                                    minElement.SetValue(newMinValue.ToString());

                                    // Additional correction: If Nominal is 0 and Min is greater than 0, set both to 0
                                    if (nominal == 0 && currentMin > 0)
                                    {
                                        nominalElement.SetValue("0");
                                        minElement.SetValue("0");
                                    }
                                }
                            }
                        }
                    }

                    // Update the RichTextBox with the corrected XML content and preserve XML declaration
                    var xmlSettings = new XmlWriterSettings
                    {
                        Indent = true,
                        IndentChars = "\t", // Use tabs for indentation
                        NewLineOnAttributes = false,
                        OmitXmlDeclaration = false // Preserve XML declaration
                    };

                    using (var memoryStream = new MemoryStream())
                    {
                        using (var writer = XmlWriter.Create(memoryStream, xmlSettings))
                        {
                            xmlDoc.Save(writer);
                        }

                        memoryStream.Position = 0;

                        // Load the corrected XML content into the xmlContentRichTextBox2
                        TextRange textRange = new TextRange(xmlContentRichTextBox2.Document.ContentStart, xmlContentRichTextBox2.Document.ContentEnd);
                        textRange.Load(memoryStream, DataFormats.Text); // Change to DataFormats.Text

                        // Reapply syntax highlighting only if CheckIgnoreLinesWarning is not checked
                        if (!ignoreLinesWarning)
                        {
                            string updatedXmlContent = Regex.Replace(textRange.Text, @"\n+", "");

                            xmlContentRichTextBox2.Document = HighlightXmlSyntax_Types(updatedXmlContent);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("XML content is empty or null. Load an XML file first.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error balancing nominal and min: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }






        private void RPT_CE_offlineDB_Click(object sender, EventArgs e)
        {
            // Check if xmlContentRichTextBox3 is not null and has content
            if (xmlContentRichTextBox3.Document == null || string.IsNullOrWhiteSpace(new TextRange(xmlContentRichTextBox3.Document.ContentStart, xmlContentRichTextBox3.Document.ContentEnd).Text))
            {
                MessageBox.Show("Please load an RPT before executing this operation.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return; // Exit the method
            }
            // Ensure that the documents are not null
            if (xmlContentRichTextBox3.Document != null && xmlContentRichTextBox2.Document != null)
            {
                // Create a HashSet to store unique extracted classnames
                HashSet<string> uniqueClassnames = new HashSet<string>();

                // Define the target pattern to match class names inside single quotes
                string targetPattern = @"will be ignored. (Type does not exist. (Typo?)";
                string target2Pattern = @"(Not spawnable. (Scope is not public?))";



                // Iterate through each paragraph in xmlContentRichTextBox3's FlowDocument
                foreach (Paragraph paragraph in xmlContentRichTextBox3.Document.Blocks.OfType<Paragraph>())
                {
                    // Get the text content of the paragraph
                    string paragraphText = new TextRange(paragraph.ContentStart, paragraph.ContentEnd).Text;

                    // Split the paragraph into lines
                    string[] lines = paragraphText.Split('\n');

                    // Iterate through each line
                    foreach (string line in lines)
                    {
                        // Check if the line contains the target pattern
                        if (line.Contains(targetPattern) || line.Contains(target2Pattern))
                        {
                            // Extract text inside quotes (assuming it follows a specific structure)
                            int startIndex = line.IndexOf("'");
                            int endIndex = line.LastIndexOf("'");
                            if (startIndex != -1 && endIndex != -1 && endIndex > startIndex)
                            {
                                string textInsideQuotes = line.Substring(startIndex + 1, endIndex - startIndex - 1);

                                // Add the classname to the list
                                uniqueClassnames.Add(textInsideQuotes);
                            }
                        }
                    }
                }

                // Check if there are any matches in xmlContentRichTextBox2
                List<string> matchedClassNames = new List<string>();
                foreach (Paragraph paragraph in xmlContentRichTextBox2.Document.Blocks.OfType<Paragraph>())
                {
                    string paragraphText = new TextRange(paragraph.ContentStart, paragraph.ContentEnd).Text;

                    // Check if any classname in uniqueClassnames is found in xmlContentRichTextBox2
                    IEnumerable<string> matchingClassNames = uniqueClassnames.Where(className => paragraphText.Contains(className));

                    // Add all matched classnames to the list
                    matchedClassNames.AddRange(matchingClassNames);
                }


                // Remove the XML entries based on the matched classnames
                RemoveXmlEntries(matchedClassNames);

                if (matchedClassNames.Count > 0)
                {
                    // Display a message box indicating removal and color classnames red
                    string message = $"Removed Classnames from XML That RPT calls useless [REMOVED]: {string.Join(", ", matchedClassNames)}";
                    MessageBox.Show(message, "Information", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Highlight only the removed classnames in red in xmlContentRichTextBox2
                    HighlightRemovedClassNames(uniqueClassnames, matchedClassNames);
                }
                else
                {
                    MessageBox.Show("No matching classnames found in XML. Cleaned XML [X]", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                if (matchedClassNames.Count > 0)
                {
                    // Display a message box indicating removal
                    string removedMessage = $"Removed Classnames from XML That RPT calls useless [REMOVED]: {string.Join(", ", matchedClassNames)}";

                    // Find the remaining classnames (not removed)
                    HashSet<string> remainingClassnames = new HashSet<string>(uniqueClassnames.Except(matchedClassNames));

                    // For demonstration purposes, display the remaining classnames in a message box
                    string remainingClassnamesMessage = string.Join(", ", remainingClassnames);
                    MessageBox.Show($"{removedMessage}\n\nFound remaining classnames: {remainingClassnamesMessage}", "Information", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Update RPT file with lines containing removed class names in red
                    UpdateRPTFile(xmlContentRichTextBox3, matchedClassNames);
                }
                else
                {
                    // Display a message box indicating no matching classnames found in XML
                    MessageBox.Show("No matching classnames found in RPT.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void UpdateRPTFile(RichTextBox richTextBox, List<string> removedClassNames)
        {
            // Create a list to store the paragraphs to be modified
            List<Paragraph> paragraphsToModify = new List<Paragraph>();

            // Extract paragraphs from the document for modification
            foreach (Paragraph paragraph in richTextBox.Document.Blocks.OfType<Paragraph>())
            {
                paragraphsToModify.Add(paragraph);
            }

            // Iterate over the list of paragraphs for modification
            foreach (Paragraph paragraph in paragraphsToModify)
            {
                string paragraphText = new TextRange(paragraph.ContentStart, paragraph.ContentEnd).Text;

                // Check if any classname in removedClassNames is found in the paragraph
                foreach (string className in removedClassNames)
                {
                    int startIndex = paragraphText.IndexOf(className);
                    while (startIndex != -1)
                    {
                        int endIndex = startIndex + className.Length;
                        TextPointer startPointer = paragraph.ContentStart.GetPositionAtOffset(startIndex);
                        TextPointer endPointer = paragraph.ContentStart.GetPositionAtOffset(endIndex);

                        // Apply red color to the text
                        if (startPointer != null && endPointer != null)
                        {
                            var range = new TextRange(startPointer, endPointer);
                            range.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Red);
                        }

                        startIndex = paragraphText.IndexOf(className, startIndex + 1);
                    }
                }
            }
        }

        private void HighlightRemovedClassNames(HashSet<string> allClassNames, List<string> removedClassNames)
        {
            // Create a list to store the TextRange instances for highlighting
            List<TextRange> rangesToHighlight = new List<TextRange>();

            // Iterate through paragraphs in xmlContentRichTextBox2's FlowDocument
            foreach (Paragraph paragraph in xmlContentRichTextBox2.Document.Blocks.OfType<Paragraph>())
            {
                string paragraphText = new TextRange(paragraph.ContentStart, paragraph.ContentEnd).Text;

                // Check if any classname in removedClassNames is found in the paragraph
                foreach (string className in removedClassNames)
                {
                    if (allClassNames.Contains(className)) // Check if it was in the original set
                    {
                        int startIndex = paragraphText.IndexOf(className);
                        if (startIndex != -1)
                        {
                            int lineStartIndex = paragraphText.LastIndexOf('\n', startIndex) + 1; // Find the start of the line
                            int lineEndIndex = paragraphText.IndexOf('\n', startIndex); // Find the end of the line
                            if (lineEndIndex == -1)
                            {
                                lineEndIndex = paragraphText.Length; // If no newline character, highlight till the end
                            }

                            TextPointer startPointer = paragraph.ContentStart.GetPositionAtOffset(lineStartIndex);
                            TextPointer endPointer = paragraph.ContentStart.GetPositionAtOffset(lineEndIndex);

                            // Create a TextRange and add it to the list
                            if (startPointer != null && endPointer != null)
                            {
                                rangesToHighlight.Add(new TextRange(startPointer, endPointer));

                                // Increment the counter for each removed class name
                            }
                        }
                        removedClassCount++;
                    }
                }
            }

            // Apply the highlighting after the iteration to avoid modifying the collection during enumeration
            foreach (var range in rangesToHighlight)
            {
                range.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Red);
            }

            // Update the UI label with the total removed class names count
            removedClassCountLabel.Text = $"Total Removed Class Names:  {removedClassCount}";
        }

        private void RemoveXmlEntries(IEnumerable<string> uniqueClassNames)
        {
            // Check if the document is not null
            if (xmlContentRichTextBox2.Document != null)
            {
                // Get the XML content from xmlContentRichTextBox2
                string xmlContent = new TextRange(xmlContentRichTextBox2.Document.ContentStart, xmlContentRichTextBox2.Document.ContentEnd).Text;

                foreach (string classNameToRemove in uniqueClassNames)
                {
                    // Create the XML entry start tag
                    string startTag = $"<type name=\"{classNameToRemove}\">";
                    // Create the XML entry end tag
                    string endTag = $"</type>";

                    int startIndex = xmlContent.IndexOf(startTag);

                    // Loop to remove all occurrences of the class name
                    while (startIndex != -1)
                    {
                        int endIndex = xmlContent.IndexOf(endTag, startIndex);

                        // Check if both start and end index are found
                        if (endIndex != -1)
                        {
                            // Remove the XML entry from the content
                            xmlContent = xmlContent.Remove(startIndex, endIndex - startIndex + endTag.Length);
                        }

                        // Search for the next occurrence
                        startIndex = xmlContent.IndexOf(startTag, startIndex + 1);
                    }
                }

                // Reapply syntax highlighting by passing the text content
                FlowDocument highlightedFlowDocument = HighlightXmlSyntax_Types(xmlContent);

                // Clear existing content in xmlContentRichTextBox2
                xmlContentRichTextBox2.Document.Blocks.Clear();

                // Set the modified and highlighted FlowDocument to xmlContentRichTextBox2
                xmlContentRichTextBox2.Document = highlightedFlowDocument;
            }
        }







        private void UpdateStatusBar(string status)
        {
            // Assuming you have a StatusBar with a single StatusBarItem named statusBarLabel
            statusBarLabel.Items.Clear(); // Clear existing items
            statusBarLabel.Items.Add(new StatusBarItem { Content = status });
        }


        private Dictionary<string, string> openedXmlFiles = new Dictionary<string, string>();

        // Method to add opened XML file and its content to the dictionary
        private void AddOpenedXmlFile(string fileName, string content)
        {
            if (!openedXmlFiles.ContainsKey(fileName))
            {
                // Update status before adding the file
                UpdateStatusBar("Adding file: " + fileName);

                // Invoke the addition on the UI thread
                this.Dispatcher.Invoke(() =>
                {
                    openedXmlFiles.Add(fileName, content);
                });

                // Update status after adding the file
                UpdateStatusBar(fileName + ": added successfully" );
            }
        }


        // Method to save and extract changes to XML files
        private void SaveAndExtractChanges(object sender, EventArgs e)
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            foreach (var entry in openedXmlFiles)
            {
                string fileName = entry.Key;
                string originalContent = entry.Value;

                try
                {
                    string modifiedContent = new TextRange(xmlContentRichTextBox2.Document.ContentStart, xmlContentRichTextBox2.Document.ContentEnd).Text;

                    // Debugging: Log or display original and modified content
                    LogContent("Original Content:", originalContent);
                    LogContent("Modified Content:", modifiedContent);

                    string changes = ExtractChanges(originalContent, modifiedContent);

                    // Debugging: Log or display the extracted changes
                    LogContent("Extracted Changes:", changes);

                    changes = AddOrUpdateXmlDeclaration(changes);

                    string outputPath = System.IO.Path.Combine(desktopPath, $"{fileName}");

                    File.WriteAllText(outputPath, changes);

                    // Provide feedback about successful save for each file
                    Console.WriteLine($"Changes saved and extracted for file: {fileName}");
                }
                catch (Exception ex)
                {
                    // Handle exceptions (log or display an error message)
                    Console.WriteLine($"Error processing file {fileName}: {ex.Message}");
                }
            }

            // Clear the dictionary for the next set of opened XML files
            openedXmlFiles.Clear();

            MessageBox.Show("Changes saved and extracted to desktop.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LogContent(string header, string content)
        {
            Console.WriteLine(header);
            Console.WriteLine(content);
        }





        private string AddOrUpdateXmlDeclaration(string content)
        {
            // Check if the XML declaration already exists
            if (!content.StartsWith("<?xml"))
            {
                // Add the XML declaration at the beginning
                content = $"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>{Environment.NewLine}{content}";
            }
            else
            {
                // Update the existing XML declaration
                int endIndex = content.IndexOf("?>") + 2;
                string existingDeclaration = content.Substring(0, endIndex);
                content = $"{existingDeclaration}{Environment.NewLine}{content.Substring(endIndex)}";
            }

            return content;
        }

        // Method to extract changes from original and modified XML content
        private string ExtractChanges(string originalContent, string modifiedContent)
        {
            // Assuming the XML structure remains consistent
            string xmlHeader = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>";
            string typesStartTag = "<types>";
            string typesEndTag = "</types>";

            int headerIndex = modifiedContent.IndexOf(xmlHeader);
            int typesStartIndex = modifiedContent.IndexOf(typesStartTag);
            int typesEndIndex = modifiedContent.IndexOf(typesEndTag, typesStartIndex);

            // Check if typesStartIndex and typesEndIndex are valid
            if (typesStartIndex < 0 || typesEndIndex < 0)
            {
                throw new InvalidOperationException("Invalid XML structure. Unable to extract changes.");
            }

            // Extract changes between typesStartIndex and typesEndIndex
            string changes = modifiedContent.Substring(typesStartIndex, typesEndIndex - typesStartIndex + typesEndTag.Length);

            // Check if there are changes to export
            if (string.IsNullOrWhiteSpace(changes))
            {
                throw new InvalidOperationException("No changes to export.");
            }

            return changes;
        }

    }
}