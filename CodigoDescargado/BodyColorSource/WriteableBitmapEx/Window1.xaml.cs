using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace WriteableBitmapEx
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        private WriteableBitmap writeableBmp;
        private int shapeCount;
        private static Random rand = new Random();
        private int frameCounter = 0;

        public Window1()
        {
            InitializeComponent();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            writeableBmp = BitmapFactory.New((int)ViewPortContainer.Width, (int)ViewPortContainer.Height);
            ImageViewport.Source = writeableBmp;
            DrawStaticShapes();

            // Load an image from the calling Assembly's res
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            
        }

        /// <summary>
        /// Draws the different types of shapes.
        /// </summary>
        private void DrawStaticShapes()
        {
            // Wrap updates in a GetContext call, to prevent invalidation and nested locking/unlocking during this block
            using (writeableBmp.GetBitmapContext())
            {
                // Init some size vars
                int w = this.writeableBmp.PixelWidth - 2;
                int h = this.writeableBmp.PixelHeight - 2;
                int w3rd = w / 3;
                int h3rd = h / 3;
                int w6th = w3rd >> 1;
                int h6th = h3rd >> 1;

                // Clear 
                writeableBmp.Clear();

                // Draw some points
                for (int i = 0; i < 200; i++)
                {
                    writeableBmp.SetPixel(rand.Next(w3rd), rand.Next(h3rd), GetRandomColor());
                }

                // Draw Standard shapes
                writeableBmp.DrawLine(rand.Next(w3rd, w3rd * 2), rand.Next(h3rd), rand.Next(w3rd, w3rd * 2), rand.Next(h3rd),
                                      GetRandomColor());
                writeableBmp.DrawTriangle(rand.Next(w3rd * 2, w - w6th), rand.Next(h6th), rand.Next(w3rd * 2, w),
                                          rand.Next(h6th, h3rd), rand.Next(w - w6th, w), rand.Next(h3rd),
                                          GetRandomColor());

                writeableBmp.DrawQuad(rand.Next(0, w6th), rand.Next(h3rd, h3rd + h6th), rand.Next(w6th, w3rd),
                                      rand.Next(h3rd, h3rd + h6th), rand.Next(w6th, w3rd),
                                      rand.Next(h3rd + h6th, 2 * h3rd), rand.Next(0, w6th), rand.Next(h3rd + h6th, 2 * h3rd),
                                      GetRandomColor());
                writeableBmp.DrawRectangle(rand.Next(w3rd, w3rd + w6th), rand.Next(h3rd, h3rd + h6th),
                                           rand.Next(w3rd + w6th, w3rd * 2), rand.Next(h3rd + h6th, 2 * h3rd),
                                           GetRandomColor());

                // Random polyline
                int[] p = new int[rand.Next(7, 10) * 2];
                for (int j = 0; j < p.Length; j += 2)
                {
                    p[j] = rand.Next(w3rd * 2, w);
                    p[j + 1] = rand.Next(h3rd, 2 * h3rd);
                }
                writeableBmp.DrawPolyline(p, GetRandomColor());

                // Random closed polyline
                p = new int[rand.Next(6, 9) * 2];
                for (int j = 0; j < p.Length - 2; j += 2)
                {
                    p[j] = rand.Next(w3rd);
                    p[j + 1] = rand.Next(2 * h3rd, h);
                }
                p[p.Length - 2] = p[0];
                p[p.Length - 1] = p[1];
                writeableBmp.DrawPolyline(p, GetRandomColor());

                // Ellipses
                writeableBmp.DrawEllipse(rand.Next(w3rd, w3rd + w6th), rand.Next(h3rd * 2, h - h6th),
                                         rand.Next(w3rd + w6th, w3rd * 2), rand.Next(h - h6th, h), GetRandomColor());
                writeableBmp.DrawEllipseCentered(w - w6th, h - h6th, w6th >> 1, h6th >> 1, GetRandomColor());

                // Draw Grid
                writeableBmp.DrawLine(0, h3rd, w, h3rd, Colors.Black);
                writeableBmp.DrawLine(0, 2 * h3rd, w, 2 * h3rd, Colors.Black);
                writeableBmp.DrawLine(w3rd, 0, w3rd, h, Colors.Black);
                writeableBmp.DrawLine(2 * w3rd, 0, 2 * w3rd, h, Colors.Black);

                // Invalidates on exit of using block
            }
        }

        private static int GetRandomColor()
        {
            return (int)(0xFF000000 | (uint)rand.Next(0xFFFFFF));
        }
    }
}
