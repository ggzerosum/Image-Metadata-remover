using System.Drawing.Imaging;

namespace Image_Metadata_Remover;

static class Program
{
    public class MetadataRemoverForm : Form
    {
        private TextBox txtInputFolder;
        private TextBox txtOutputFolder;
        private Button btnBrowseInput;
        private Button btnBrowseOutput;
        private Button btnRemoveMetadata;
        private TrackBar qualityTrackBar;
        private Label lblQuality;

        public MetadataRemoverForm()
        {
            this.Text = "Image Metadata Remover";
            this.Size = new Size(600, 300);
            this.MinimumSize = new Size(400, 250);

            this.FormClosing += (s, e) => Application.Exit();
            
            Label lblInput = new Label() { Text = "Input Folder:", Location = new Point(10, 20), AutoSize = true };
            txtInputFolder = new TextBox()
            {
                Location = new Point(100, 20), Width = 400,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            btnBrowseInput = new Button()
            {
                Text = "Browse", Location = new Point(510, 18), Width = 80,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnBrowseInput.Click += (s, e) => BrowseFolder(txtInputFolder);

            Label lblOutput = new Label() { Text = "Output Folder:", Location = new Point(10, 60), AutoSize = true };
            txtOutputFolder = new TextBox()
            {
                Location = new Point(100, 60), Width = 400,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            btnBrowseOutput = new Button()
            {
                Text = "Browse", Location = new Point(510, 58), Width = 80,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnBrowseOutput.Click += (s, e) => BrowseFolder(txtOutputFolder);

            lblQuality = new Label() { Text = "JPEG Quality: 100", Location = new Point(10, 100), AutoSize = true };
            qualityTrackBar = new TrackBar()
            {
                Location = new Point(100, 95), Minimum = 0, Maximum = 100, Value = 100, Width = 400,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            qualityTrackBar.Scroll += (s, e) => lblQuality.Text = $"JPEG Quality: {qualityTrackBar.Value}";

            btnRemoveMetadata = new Button()
            {
                Text = "Remove Metadata", Location = new Point(10, 150), Width = 580,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            btnRemoveMetadata.Click += (s, e) =>
                ProcessFolder(txtInputFolder.Text, txtOutputFolder.Text, qualityTrackBar.Value);

            this.Controls.Add(lblInput);
            this.Controls.Add(txtInputFolder);
            this.Controls.Add(btnBrowseInput);
            this.Controls.Add(lblOutput);
            this.Controls.Add(txtOutputFolder);
            this.Controls.Add(btnBrowseOutput);
            this.Controls.Add(lblQuality);
            this.Controls.Add(qualityTrackBar);
            this.Controls.Add(btnRemoveMetadata);
        }

        private void BrowseFolder(TextBox targetBox)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    targetBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void ProcessFolder(string inputFolder, string outputFolder, int quality)
        {
            if (string.IsNullOrWhiteSpace(inputFolder) || string.IsNullOrWhiteSpace(outputFolder))
            {
                MessageBox.Show("Please select input and output folders!", "Warning", MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (!Directory.Exists(inputFolder))
            {
                MessageBox.Show("Input folder does not exist!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Directory.CreateDirectory(outputFolder);

            string[] files = Directory.GetFiles(inputFolder, "*.*")
                .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (files.Length == 0)
            {
                MessageBox.Show("No image files found in the folder!", "Information", MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            int successCount = 0;

            foreach (string inputPath in files)
            {
                string outputPath = Path.Combine(outputFolder, Path.GetFileName(inputPath));

                try
                {
                    RemoveMetadata(inputPath, outputPath, quality);
                    successCount++;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to process file: {inputPath}\n{ex.Message}", "Error", MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }

            MessageBox.Show($"Processed {successCount}/{files.Length} files successfully!", "Completed",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void RemoveMetadata(string inputPath, string outputPath, int quality)
        {
            using (Image image = Image.FromFile(inputPath))
            {
                foreach (var propertyItem in image.PropertyItems.ToList())
                {
                    image.RemovePropertyItem(propertyItem.Id);
                }

                using (Bitmap newImage = new Bitmap(image.Width, image.Height))
                {
                    using (Graphics g = Graphics.FromImage(newImage))
                    {
                        g.DrawImage(image, 0, 0, image.Width, image.Height);
                    }

                    string extension = Path.GetExtension(outputPath).ToLower();
                    ImageFormat format = extension switch
                    {
                        ".png" => ImageFormat.Png,
                        ".jpg" or ".jpeg" => ImageFormat.Jpeg,
                        _ => throw new NotSupportedException("Unsupported file format.")
                    };

                    if (format == ImageFormat.Jpeg)
                    {
                        ImageCodecInfo jpgEncoder = ImageCodecInfo.GetImageDecoders()
                            .FirstOrDefault(codec => codec.FormatID == ImageFormat.Jpeg.Guid);

                        EncoderParameters encoderParameters = new EncoderParameters(1);
                        encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, (long)quality);

                        newImage.Save(outputPath, jpgEncoder, encoderParameters);
                    }
                    else
                    {
                        newImage.Save(outputPath, format);
                    }
                }
            }
        }
    }
    
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        Application.Run(new MetadataRemoverForm());
    }
}
