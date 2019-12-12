using Microsoft.Win32;
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
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace CropImage
{
    public partial class MainWindow : Window
    {
        public string m_img_path;
        public System.Windows.Shapes.Path m_cur_path;
        PolyLineSegment m_cur_bezier;
        public List<System.Windows.Point> m_cur_points = new List<System.Windows.Point>();

        Point m_smudge_start;
        Point m_smudge_end;


        Ellipse m_brush_tip = new Ellipse()
        {
            Stroke = System.Windows.Media.Brushes.Tomato,
            StrokeThickness = 2
        };

        private bool _smudge_mode;
        public bool SmudgeMode
        {
            get
            {
                return _smudge_mode;
            }
            set
            {
                _smudge_mode = value;
            }
        }
        private double _brush_radius;
        public double BrushRadius
        {
            get
            {
                return _brush_radius;
            }

            set
            {
                _brush_radius = value;
                m_brush_tip.Width = value * 2;
                m_brush_tip.Height = value * 2;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ui_canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;

            if( e.ChangedButton == MouseButton.Left)
            {
                System.Windows.Point p = e.GetPosition(ui_input_canvas);

                // create a new path
                if (m_cur_path == null)
                {
                    m_cur_path = new System.Windows.Shapes.Path();
                    m_cur_path.Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0));
                    m_cur_path.StrokeThickness = 2;
                    m_cur_path.StrokeStartLineCap = PenLineCap.Round;
                    m_cur_path.StrokeEndLineCap = PenLineCap.Round;
                    m_cur_path.StrokeDashArray = new DoubleCollection(new double[] { 1, 1 });

                    m_cur_bezier = new PolyLineSegment()
                    {
                        IsStroked = true
                    };
                    m_cur_bezier.Points.Add(p);

                    var figure = new PathFigure()
                    {
                        StartPoint = p
                    };
                    figure.Segments = new PathSegmentCollection(new PathSegment[] { m_cur_bezier });
                    m_cur_path.Data = new PathGeometry(new PathFigure[] { figure });
                    ui_input_canvas.Children.Add(m_cur_path);
                }
                else 
                {
                    // add to the existing path
                    m_cur_bezier.Points.Add(p);
                }

                if(e.ClickCount > 1)
                {
                    // double click closes the polygon
                    m_cur_bezier.Points.Add(m_cur_bezier.Points[0]);
                }
            }
            else
            {
                // right click removes the last vertice
                if (m_cur_bezier.Points.Count > 0)
                {
                    m_cur_bezier.Points.RemoveAt(m_cur_bezier.Points.Count - 1);
                }
                else
                {
                    reset();
                }
            }
        }

        private void btn_load_image_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.Title = "Open Image File";
                dlg.Filter = "Image files|*.jpg;*.png|All files|*.*";
                dlg.InitialDirectory = System.IO.Directory.GetCurrentDirectory();
                if (dlg.ShowDialog().Value)
                {
                    m_img_path = dlg.FileName;
                    ui_input_canvas.Background = new ImageBrush(new BitmapImage(new Uri(m_img_path)));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
            }
        }

        private void btn_reset_Click(object sender, RoutedEventArgs e)
        {
            reset();
        }

        void reset()
        {
            ui_input_canvas.Children.Remove(m_cur_path);
            m_cur_path = null;
        }

        CroppedBitmap get_cropped_image(string org_path, Geometry geo)
        {
            ImageSource bmp = new BitmapImage(new Uri(org_path));
            var size = new System.Windows.Size(bmp.Width, bmp.Height);
            Canvas save_canvas = new Canvas();
            save_canvas.Background = new ImageBrush(bmp);
            var clipGeometry = geo;
            // change the scale 
            clipGeometry.Transform = new ScaleTransform(size.Width / ui_input_canvas.ActualWidth, size.Height / ui_input_canvas.ActualHeight);
            save_canvas.Clip = clipGeometry;

            save_canvas.Measure(size);
            save_canvas.Arrange(new Rect(size));

            RenderTargetBitmap render_bmp = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96, 96, PixelFormats.Pbgra32);
            render_bmp.Render(save_canvas);

            return new CroppedBitmap(render_bmp, new Int32Rect((int)geo.Bounds.Left, (int)geo.Bounds.Top, (int)geo.Bounds.Width, (int)geo.Bounds.Height));
        }
        private void btn_crop_Click(object sender, RoutedEventArgs e)
        {
            if (m_img_path == "" || m_cur_path == null)
                return;

            // we create a temporary canvas and save the cropped image into a bitmap
            var cropped_image = get_cropped_image(m_img_path, m_cur_path.RenderedGeometry);

            // prepare a new image control
            var bound = m_cur_path.RenderedGeometry.Bounds;
            System.Windows.Controls.Image cropped = new System.Windows.Controls.Image() {
                Stretch = Stretch.Fill,
                Width = bound.Width,
                Height = bound.Height,
            };
            Canvas.SetLeft(cropped, bound.Left);
            Canvas.SetTop(cropped, bound.Top);

            cropped.Source = cropped_image;
            cropped.MouseDown += Cropped_MouseDown;
            cropped.MouseMove += Cropped_MouseMove;
            
            // add the control with editor attached
            ui_result.Children.Add(cropped);
        }
       
        private unsafe void Cropped_MouseMove(object sender, MouseEventArgs e)
        {
            if (SmudgeMode && e.LeftButton == MouseButtonState.Pressed)
            {
                System.Windows.Controls.Image image = sender as System.Windows.Controls.Image;
                var location = e.GetPosition(image);
                var croppedControl = sender as Image;
                m_smudge_end = new Point(location.X / croppedControl.Width, location.Y / croppedControl.Height);
                image.Source = Smudge.smudge_image(image.Source, m_smudge_start, m_smudge_end, BrushRadius / croppedControl.Width);
                m_smudge_start = m_smudge_end;
            }
        }

        private void Cropped_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (SmudgeMode == false)
            {
                ui_shape_editor.CaptureElement(sender as FrameworkElement, e);
                e.Handled = true;
            }
            else
            {
                Point location = e.GetPosition((IInputElement)sender);
                var croppedControl = sender as Image;
                m_smudge_start = new Point(location.X / croppedControl.Width, location.Y / croppedControl.Height);
            }
        }

        private void btn_save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // apply the last move/resize
                ui_shape_editor.ReleaseElement();

                SaveFileDialog dlg = new SaveFileDialog();
                dlg.FileName = "output.png";
                dlg.Title = "Save Cropped Image File";
                dlg.Filter = "Image files|*.jpg;*.png|All files|*.*";
                dlg.InitialDirectory = System.IO.Directory.GetCurrentDirectory();
                if (dlg.ShowDialog().Value)
                {
                    var output_file = dlg.FileName;

                    // render the result canvas into a file
                    var visual = ui_result;
                    var encoder = new PngBitmapEncoder();
                    RenderTargetBitmap bitmap = new RenderTargetBitmap((int)ui_result.ActualWidth, (int)ui_result.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                    bitmap.Render(visual);
                    BitmapFrame frame = BitmapFrame.Create(bitmap);
                    encoder.Frames.Add(frame);

                    using (var stream = System.IO.File.Create(output_file))
                    {
                        encoder.Save(stream);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
            }
        }

        private void btn_change_bg_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.Title = "Open Image File";
                dlg.Filter = "Image files|*.jpg;*.png|All files|*.*";
                dlg.InitialDirectory = System.IO.Directory.GetCurrentDirectory();
                if (dlg.ShowDialog().Value)
                {
                    ui_result.Background = new ImageBrush(new BitmapImage(new Uri(dlg.FileName)));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
            }
        }

        private void ui_result_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(SmudgeMode == false)
                ui_shape_editor.ReleaseElement();
        }

        

        private void ui_result_MouseMove(object sender, MouseEventArgs e)
        {
            if(SmudgeMode)
            {
                var pos = e.GetPosition(ui_result);
                if (pos.X - BrushRadius >= 0 && pos.Y - BrushRadius >= 0 && pos.X + BrushRadius < ui_result.ActualWidth && pos.Y + BrushRadius < ui_result.ActualHeight)
                {
                    m_brush_tip.Visibility = Visibility.Visible;
                    Canvas.SetLeft(m_brush_tip, pos.X - BrushRadius);
                    Canvas.SetTop(m_brush_tip, pos.Y - BrushRadius);
                }
                else
                {
                    m_brush_tip.Visibility = Visibility.Hidden;
                }
            }
        }

        private void ui_result_canvas_MouseEnter(object sender, MouseEventArgs e)
        {
            if (SmudgeMode)
            {
                ui_result.Children.Add(m_brush_tip);
            }
        }

        private void ui_result_canvas_MouseLeave(object sender, MouseEventArgs e)
        {
            if (SmudgeMode)
            {
                ui_result.Children.Remove(m_brush_tip);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            BrushRadius = ui_radius.Minimum;
        }

        private void btn_smudge_Checked(object sender, RoutedEventArgs e)
        {
            ui_shape_editor.ReleaseElement();
        }
    }
}
