using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NCalc;
using System.Drawing;
using System.Windows;
using System.Drawing.Drawing2D;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Controls;

namespace FloreaGrad
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        public static string expressionsString;
        public string value;

        private int[] unformatted = new int[100];
        private int[] formatted = new int[100];
        private int arrpos = -1;

        public MainWindow()
        {
            InitializeComponent();
            InputTextBox.Text = "";
            FunctionTextBlock = "";
        }
        float offset = 2;

        private void Calculate_Button_Click(object sender, RoutedEventArgs e) //Calculate 
        {
            expressionsString = InputTextBox.Text;
            if (expressionsString.Length == 0)
                return;
            
            float _a = float.Parse(ALimit.Text);
            float _b = float.Parse(BLimit.Text);
            float _epsilon = float.Parse(Epsilon.Text);

            Rezultat resultat = TrapezMethod.CalculeazaAria(_a, _b, _epsilon);
            ResultTextBox.Text = "Valoare = " + resultat.valoare.ToString() + "\nPasi = " + resultat.pasi.ToString();

            MakeGraph(_a-offset, _b+offset, F(_a) - offset, F(_b) + offset);
        }

        private static float F(float x)
        {
            NCalc.Expression ex = new NCalc.Expression(expressionsString);

            ex.Parameters["x"] = x;

            string result = ex.Evaluate().ToString();
            return float.Parse(result);
        }

        // Make the graph.
        private void MakeGraph(float xmin, float xmax, float ymin, float ymax)
        {
            // Make the Bitmap.
            //int wid = (int) GraphImage.Source.Height;
            //int hgt = (int) GraphImage.Source.Height;

            int wid = 200;
            int hgt = 200;

            Bitmap bm = new Bitmap(wid, hgt);
            
            using (Graphics gfx = Graphics.FromImage(bm))
            using (SolidBrush brush = new SolidBrush(Color.White))
            {
                gfx.FillRectangle(brush, 0, 0, wid, hgt);
            }

            using (Graphics gr = Graphics.FromImage(bm))
            {
                gr.SmoothingMode = SmoothingMode.AntiAlias;

                // Transform to map the graph bounds to the Bitmap.
                RectangleF rect = new RectangleF(
                    xmin, ymin, xmax - xmin, ymax - ymin);
                PointF[] pts =
                {
                    new PointF(0, hgt),
                    new PointF(wid, hgt),
                    new PointF(0, 0),
                };

                gr.Transform = new Matrix(rect, pts);

                // Draw the graph.
                using (Pen graph_pen = new Pen(Color.Blue, 0))
                {
                    // Draw the axes.
                    gr.DrawLine(graph_pen, xmin, 0, xmax, 0);
                    gr.DrawLine(graph_pen, 0, ymin, 0, ymax);
                    for (int x = (int)xmin; x <= xmax; x++)
                    {
                        gr.DrawLine(graph_pen, x, -0.1f, x, 0.1f);
                    }
                    for (int y = (int)ymin; y <= ymax; y++)
                    {
                        gr.DrawLine(graph_pen, -0.1f, y, 0.1f, y);
                    }
                    graph_pen.Color = Color.Red;

                    // See how big 1 pixel is horizontally.
                    Matrix inverse = gr.Transform;
                    inverse.Invert();
                    PointF[] pixel_pts =
                    {
                new PointF(0, 0),
                new PointF(1, 0)
            };
                    inverse.TransformPoints(pixel_pts);
                    float dx = pixel_pts[1].X - pixel_pts[0].X;
                    dx /= 2;

                    // Loop over x values to generate points.
                    List<PointF> points = new List<PointF>();
                    for (float x = xmin; x <= xmax; x += dx)
                    {
                        bool valid_point = false;
                        try
                        {
                            // Get the next point.
                            float y = F(x);

                            // If the slope is reasonable,
                            // this is a valid point.
                            if (points.Count == 0) valid_point = true;
                            else
                            {
                                float dy = y - points[points.Count - 1].Y;
                                if (Math.Abs(dy / dx) < 1000)
                                    valid_point = true;
                            }
                            if (valid_point) points.Add(new PointF(x, y));
                        }
                        catch
                        {
                        }
                        

                            // If the new point is invalid, draw
                            // the points in the latest batch.
                            if (!valid_point)
                        {
                            if (points.Count > 1)
                                gr.DrawLines(graph_pen, points.ToArray());
                            points.Clear();
                        }

                    }
                    // Draw the last batch of points.
                    if (points.Count > 1)
                        gr.DrawLines(graph_pen, points.ToArray());
                }
            }

            // Display the result.
            GraphImage.Source = BitmapToImageSource(bm);
        }

        // Transform Bitmap to Image Source
        BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }

        public static class TrapezMethod
        {
            public static float n, a, b, s1, s2, i, dx, epsilon;

            public static Rezultat CalculeazaAria(float _a, float _b, float _epsilon)
            {
                a = _a;
                b = _b;
                epsilon = _epsilon;

                s1 = (F(a) + F(b)) / 2 * (b - a);
                s2 = (F(a) + F((a + b) / 2)) / 2 * ((b - a) / 2) + (F((a + b) / 2) + F(b)) / 2 * ((b - a) / 2);
                if (s1 > s2) dx = s1 - s2;
                else dx = s2 - s1;
                n = 2;
                while (dx > epsilon)
                {
                    s1 = s2;
                    s2 = 0;
                    n = n + 1;
                    i = a + (b - a) / n;
                    while (i <= b)
                    {
                        s2 = s2 + (F(i - (b + a) / n) + F(i)) / 2 * ((b - a) / n);
                        i = i + ((b - a) / n);
                    }
                    if (s1 > s2) dx = s1 - s2;
                    else dx = s2 - s1;
                }

                Rezultat resultat = new Rezultat(s2, n);
                return resultat;
            }
        }

        public class Rezultat
        {
            public float valoare;
            public float pasi;

            public Rezultat(float _valoare, float _pasi)
            {
                valoare = _valoare;
                pasi = _pasi;
            }
        }

        

        //private int parcount = 0;
        private void NumberClick (object sender, RoutedEventArgs e) //KeyPad
        {
            string s = (sender as Button).Content.ToString();
            /*
            if (s == "(")
                parcount++;
            if (s == ")")
                if (parcount > 0)
                    parcount--;
                else
                    return;
            */
            arrpos++;
            unformatted[arrpos] = s.Length;

            InputTextBox.Text = InputTextBox.Text + s ;
        }

        private void DelClick(object sender, RoutedEventArgs e) //Delete
        {
            if(InputTextBox.Text.Length > 0)
            
            InputTextBox.Text = InputTextBox.Text.Remove(InputTextBox.Text.Length-unformatted[arrpos--]);
        }

        private void FuncClick(object sender, RoutedEventArgs e) // Functions
        {
            string s = (sender as Button).Content.ToString();
            arrpos++;
            unformatted[arrpos] = s.Length+1;
            InputTextBox.Text = InputTextBox.Text + s+"(";
        }

        private void Xclick(object sender, RoutedEventArgs e) // X
        {
            string s = (sender as Button).Content.ToString();
            arrpos++;
            unformatted[arrpos] = s.Length+2;
            InputTextBox.Text = InputTextBox.Text + "[x]";
        }
    }

   
}
