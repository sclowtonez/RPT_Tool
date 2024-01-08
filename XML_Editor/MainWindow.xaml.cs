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


namespace XML_Editor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {

            InitializeComponent();
        }
        private void OpenXmlFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "XML Files (*.xml)|*.xml|All Files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                string selectedFilePath = openFileDialog.FileName;

                try
                {
                    // Read the content of the XML file
                    string xmlContent = File.ReadAllText(selectedFilePath);

                    // Display the content in the RichTextBox with syntax highlighting
                    xmlContentRichTextBox.Document = HighlightXmlSyntax(xmlContent);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error reading the file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExtractAndSaveTypeContent_Click(object sender, RoutedEventArgs e)
        {
            string typeContent = ExtractTypeContent(xmlContentRichTextBox.Document);

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

        private string ExtractTypeContent(FlowDocument document)
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
    }
}
