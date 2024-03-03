using System;
using System.Collections.Generic;
using System.IO;
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

namespace XML_Editor
{
    /// <summary>
    /// Interaction logic for ProgressWindow.xaml
    /// </summary>
    public partial class ProgressWindow : Window
    {

        private bool loadingComplete;


        public ProgressWindow()
        {
            InitializeComponent();
            CenterWindowOnScreen();
            loadingComplete = false;
        }

        public void UpdateProgress(int percent, string status = null)
        {
            progressBar.Value = percent;

            if (status != null)
            {
                statusText.Text = status;
            }

        }
        public async void LoadFile(string filePath)
        {
            try
            {
                List<string> lines = await Task.Run(() => ReadFileLines(filePath));

                if (lines == null)
                {
                    // Handle the case where reading the file failed
                    return;
                }

                int totalLines = lines.Count;
                int processedLines = 0;

                foreach (string line in lines)
                {
                    // Process each line here

                    // Update progress every line or every few lines
                    processedLines++;
                    int percentComplete = (int)((double)processedLines / totalLines * 100);
                    UpdateProgress(percentComplete, $"Processing line {processedLines} of {totalLines}");

                    // Introduce a small delay to make the progress more visible
                    System.Threading.Thread.Sleep(50); // Adjust the sleep duration as needed
                }

                // Loading complete
                LoadingComplete();
                UpdateStatus("Loading complete!");
            }
            catch (Exception ex)
            {
                // Handle other exceptions
                UpdateStatus($"Error: {ex.Message}");
            }
        }

        private List<string> ReadFileLines(string filePath)
        {
            List<string> lines = new List<string>();

            // Read lines from the file into the 'lines' list
            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        lines.Add(line);
                    }
                }

                return lines;
            }
            catch (Exception ex)
            {
                // Handle file reading errors
                UpdateStatus($"Error reading file: {ex.Message}");
                return null;
            }
        }



        public void LoadingComplete()
        {
            loadingComplete = true;
        }

        public void UpdateStatus(string status)
        {
            statusText.Text = status;
        }

        private void CenterWindowOnScreen()
        {
            Left = (SystemParameters.PrimaryScreenWidth - Width) / 2;
            Top = (SystemParameters.PrimaryScreenHeight - Height) / 2;
        }
    }
}
