using System;
using System.Drawing;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;

namespace BlurDetectionROI
{
    public partial class Form1 : Form
    {
        private Image<Bgr, Byte> _img;
        private Image<Bgr, Byte> _roi;
        private (int top, int right, int bottom, int left) _imgBounds;
        private Mat _lap;
        private Point _startPos;
        private Point _endPos;
        private bool _drawing;
        private Rectangle _rectangle;

        public Form1()
        {
            InitializeComponent();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void openToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    _img = new Image<Bgr, Byte>(ofd.FileName);
                    _roi = new Image<Bgr, Byte>(ofd.FileName);
                    pictureBox1.Image = _img.Bitmap;
                    _imgBounds = GetPictureBoxImageBounds(pictureBox1);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (_img == null) return;
            _startPos = e.Location;
            _drawing = true;
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (_img == null) return;
            _endPos = e.Location;
            if (_drawing) pictureBox1.Invalidate();
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (_img == null) return;
            if (_drawing)
            {
                _drawing = false;
                var rect = GetRectangle(_startPos, _endPos);
                if (rect.Width > 0 && rect.Height > 0) _rectangle = rect;
                pictureBox1.Invalidate();

                // Compute sharpness value from selected region and display 
                // recommendations to user
                SetROI();
                var variance = VarianceOfLaplacian();
                DisplayResults(variance);
            }
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            if (_drawing) e.Graphics.DrawRectangle(Pens.Red, GetRectangle(_startPos, _endPos));
        }

        private void SetROI()
        {
            if (ValidateCoordinates())
            {
                // Convert coordinates from pictureBox to the original image
                var start = ConvertCoordinates(pictureBox1, _startPos);
                var end = ConvertCoordinates(pictureBox1, _endPos);

                // Set region of interest and pictureBox to new ROI
                _roi.ROI = GetRectangle(start, end);
                pictureBox1.Image = _roi.Bitmap; 
            }
            else
            {
                MessageBox.Show("Selection is out of bounds. Please make selection within image.");
            }
        }

        /// <summary>
        /// Computes degree of sharpness by using laplacian edge detection.
        /// </summary>
        /// <returns>The variance of the laplacian operator.</returns>
        private double VarianceOfLaplacian()
        {
            // Preprocess image brightness by equalizing histogram
            var equalizedHistogram = EqualizeHistogram(_roi.Mat);

            // Convert image to grayscale
            var gray = new Mat();
            CvInvoke.CvtColor(equalizedHistogram, gray, ColorConversion.Bgr2Gray);

            // Bilateral filter to reduce noise of high ISO images
            var bilat = new Mat();
            CvInvoke.BilateralFilter(gray, bilat, 9, 50, 50);

            // Laplacian filter
            _lap = new Mat();
            CvInvoke.Laplacian(bilat, _lap, DepthType.Cv64F, 3);

            // Compute variance of the laplacian operator
            var mean = new MCvScalar();
            var stdDev = new MCvScalar();
            CvInvoke.MeanStdDev(_lap, ref mean, ref stdDev);
            var variance = stdDev.V0 * stdDev.V0;

            return variance;
        }

        private void DisplayResults(double variance)
        {
            if (variance >= 100) label2.Text = "Not blurry";
            else label2.Text = "Blurry";
            label4.Text = $"{(int)variance}";
        }

        /// <summary>
        /// Pre-processing step to adjust for varying degrees of image intensity
        /// and enhance contrast.
        /// </summary>
        private Mat EqualizeHistogram(Mat src)
        {
            var labChannel = new Mat();
            var hist = new Mat();

            // READ RGB color image and convert it to Lab
            CvInvoke.CvtColor(src, hist, ColorConversion.Bgr2Lab);

            // Extract the L channel
            CvInvoke.ExtractChannel(hist, labChannel, 0);

            // apply the CLAHE algorithm to the L channel
            CvInvoke.CLAHE(labChannel, 2, new Size(16, 16), labChannel);

            // Merge the the color planes back into an Lab image
            CvInvoke.InsertChannel(labChannel, hist, 0);

            // convert back to RGB
            CvInvoke.CvtColor(hist, hist, ColorConversion.Lab2Bgr);

            labChannel.Dispose();

            return hist;
        }

        /// <summary>
        /// Computes bounds of scaled image within PictureBox.
        /// </summary>
        private (int top, int right, int bottom, int left) GetPictureBoxImageBounds(PictureBox pictureBox)
        {
            int top, right, bottom, left;

            float pictureBoxAspectRatio = GetAspectRatio((float)pictureBox.Width, (float)pictureBox.Height);
            float imgAspectRatio = GetAspectRatio((float)pictureBox.Image.Width, (float)pictureBox.Image.Height);

            // PictureBox is wider or shorter than image. 
            // Scale image to height of PictureBox and compute width by scale factor.
            if (pictureBoxAspectRatio > imgAspectRatio)
            {
                float scaleFactor = (float)pictureBox.Height / (float)pictureBox.Image.Height;
                float scaledWidth = (float)pictureBox.Image.Width * scaleFactor;
                float padding = (pictureBox.Width - scaledWidth) / 2;

                top = pictureBox.Top;
                right = pictureBox.Right - (int)padding;
                bottom = pictureBox.Bottom;
                left = (int)padding;
            }
            // PictureBox is narrower or taller than image. 
            // Scale image to width of PictureBox and compute height by scale factor.
            else
            {
                float scaleFactor = (float)pictureBox.Width / (float)pictureBox.Image.Width;
                float scaledHeight = (float)pictureBox.Image.Height * scaleFactor;
                float padding = (pictureBox.Height - scaledHeight) / 2;

                top = (int)padding;
                right = pictureBox.Right;
                bottom = pictureBox.Bottom - (int)padding;
                left = pictureBox.Left;
            }

            return (top, right, bottom, left);
        }

        /// <summary>
        /// Converts coordinates from PictureBox to source image.
        /// </summary>
        private Point ConvertCoordinates(PictureBox pictureBox, Point pictureBoxCoordinates)
        {
            int x, y;

            float pictureBoxAspectRatio = GetAspectRatio((float)pictureBox.Width, (float)pictureBox.Height);
            float imgAspectRatio = GetAspectRatio((float)pictureBox.Image.Width, (float)pictureBox.Image.Height);

            if (pictureBoxAspectRatio > imgAspectRatio)
            {
                float scaleFactor = (float)pictureBox.Height / (float)pictureBox.Image.Height;

                x = (int)((pictureBoxCoordinates.X - _imgBounds.left) / scaleFactor);
                y = (int)(pictureBoxCoordinates.Y / scaleFactor);
            }
            else
            {
                float scaleFactor = (float)pictureBox.Width / (float)pictureBox.Image.Width;

                x = (int)(pictureBoxCoordinates.X / scaleFactor);
                y = (int)((pictureBoxCoordinates.Y - _imgBounds.top) / scaleFactor);
            }

            return new Point(x, y);
        }

        private float GetAspectRatio(float width, float height)
        {
            return width / height;
        }

        private bool ValidateCoordinates()
        {
            if (
                _startPos.Y < _imgBounds.top ||
                _startPos.Y > _imgBounds.bottom ||
                _startPos.X < _imgBounds.left ||
                _startPos.X > _imgBounds.right ||
                _endPos.Y < _imgBounds.top ||
                _endPos.Y > _imgBounds.bottom ||
                _endPos.X < _imgBounds.left ||
                _endPos.X > _imgBounds.right)
                return false;

            else return true;
        }

        private Rectangle GetRectangle(Point start, Point end)
        {
            return new Rectangle(
                Math.Min(start.X, end.X),
                Math.Min(start.Y, end.Y),
                Math.Abs(start.X - end.X),
                Math.Abs(start.Y - end.Y));
        }

        private void ShowLaplaceFilter(Mat input)
        {
            var output = new Mat();

            CvInvoke.ConvertScaleAbs(input, output, 1, 0);

            pictureBox1.Image = output.Bitmap;
        }

        private void showColorButton_Click(object sender, EventArgs e)
        {
            if (_img == null || _roi == null) return;
            pictureBox1.Image = _roi.Bitmap;
        }

        private void resetButton_Click(object sender, EventArgs e)
        {
            if (_img == null) return;
            pictureBox1.Image = _img.Bitmap;
            label2.Text = "Please make selection";
            label4.Text = "Please make selection";
        }

        private void showEdgesButton_Click(object sender, EventArgs e)
        {
            if (_img == null || _lap == null) return;
            ShowLaplaceFilter(_lap);
        }
    }
}
