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
//Using añadidos
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Kinect;
using System.Collections.Generic;

namespace NPIKinectv2
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        /// <summary>
        /// Porcenaje de error que vamos a admitir.
        /// </summary>
        double porcH = 0.15;
        double porcV = 0.15;
        double porcIni = 0.15;

        /// <summary>
        /// Tamaño de los pixel RGB en el bitmap
        /// </summary>
        private readonly int bytesPerPixel = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;

        /// <summary>
        /// Activar el sensor de kinect
        /// </summary>
        private KinectSensor kinectSensor = null;

        /// <summary>
        /// Lector de color frames
        /// </summary>
        private ColorFrameReader colorFrameReader = null;

        /// <summary>
        /// Bitmap en pantalla
        /// </summary>
        private WriteableBitmap bitmap = null;

        /// <summary>
        /// Color del círculo de la mano cuando está cerrada
        /// </summary>
        private readonly Brush handClosedBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));

        /// <summary>
        /// Color del círculo de la mano cuando está abierta
        /// </summary>
        private readonly Brush handOpenBrush = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));

        /// <summary>
        /// Color del círculo de la mano cuando está apuntando
        /// </summary>
        private readonly Brush handLassoBrush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255));

        /// <summary>
        /// Color de las articulaciones no inferidas
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Color de las articulaciones inferidas
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Color de los huesos
        /// </summary>
        private readonly Pen trackedBonePen = new Pen(Brushes.Green, 6);

        /// <summary>
        /// Color de los huesos inferidos
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary>
        /// Grosor de los puntos de union
        /// </summary>  
        private const double JointThickness = 3;

        /// <summary>
        /// Tamaño de las manos
        /// </summary>  
        private const double HandSize = 30;

        /// <summary>
        /// Almacenamiento intermedio para recibir datos frame del sensor en color
        /// </summary>
        private byte[] pixels = null;

        /// <summary>
        /// Almacenamiento intermedio para recibir datos del sensor del cuerpo
        /// </summary>
        private byte[] bodyBytespixels = null;

        /// <summary>
        /// Mapeador de la cordinacion
        /// </summary>
        private CoordinateMapper coordinateMapper = null;

        /// <summary>
        /// Cronometro para calcular los FPS
        /// </summary>
        private int displayWidth;

        /// <summary>
        /// Cronometro para calcular los FPS
        /// </summary>
        private int displayHeight;

        /// <summary>
        /// Cuadrilla para represenatar la imagen
        /// </summary>
        Grid rootGrid;

        /// <summary>
        /// Imagen del cuerpo 
        /// </summary>
        Image bodyImage;

        /// <summary>
        /// Mapa de bit para guardar el stream de color
        /// </summary>
        private readonly WriteableBitmap _colorWriteableBitmap;

        /// <summary>
        /// Mapa de bit para guardar el stream del cuerpo
        /// </summary>
        private readonly WriteableBitmap _bodyWriteableBitmap;

        /// <summary>
        /// Grosor del borde del rectángulo
        /// </summary>
        private const double ClipBoundsThickness = 10;

        /// <summary>
        ///  Points donde voy a almacenar los de la cabeza, hombros y pies
        /// </summary>
        Point cabeza;
        Point hombroDerecho;
        Point hombroIzquierdo;
        Point pieDerecho;
        Point pieIzquierdo;
        Point manoDerecha;
        Point manoIzquierda;

        // Añadimos comandos para hotkeys
        public static RoutedCommand teclaMasMargenH = new RoutedCommand();
        public static RoutedCommand teclaMenosMargenH = new RoutedCommand();

        public static RoutedCommand teclaMasMargenV = new RoutedCommand();
        public static RoutedCommand teclaMenosMargenV = new RoutedCommand();

        public static RoutedCommand teclaMasMargenIni = new RoutedCommand();
        public static RoutedCommand teclaMenosMargenIni = new RoutedCommand();

        /// <summary>
        /// Inicialización de una nueva instancia de la clase MainWindow
        /// </summary>
        public MainWindow()
        {
            // Ponemos el sensor por defecto para abrirlo
            this.kinectSensor = KinectSensor.GetDefault();

            if (this.kinectSensor != null)
            {
                this.coordinateMapper = this.kinectSensor.CoordinateMapper;

                // Abrimos el sensor
                this.kinectSensor.Open();

                FrameDescription frameDescription = this.kinectSensor.ColorFrameSource.FrameDescription;
                this.displayWidth = frameDescription.Width;
                this.displayHeight = frameDescription.Height;
                this.bodies = new Body[this.kinectSensor.BodyFrameSource.BodyCount];

                // Abrimos el lector colorFrameReader y bodyFrameReader para el color frame y el cuerpo
                this.colorFrameReader = this.kinectSensor.ColorFrameSource.OpenReader();
                this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();

                // Iniciamos el espacio donde vamos a almacenar los pixeles que se vayan recibiendo
                this.pixels = new byte[frameDescription.Width * frameDescription.Height * this.bytesPerPixel];
                this.bodyBytespixels = new byte[frameDescription.Width * frameDescription.Height * this.bytesPerPixel];

                // Creamos el bitmap para la pantalla
                this.bitmap = new WriteableBitmap(frameDescription.Width, frameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
                this.drawingGroup = new DrawingGroup();
                _bodySourceRTB = new RenderTargetBitmap(displayWidth, displayHeight, 96.0, 96.0, PixelFormats.Pbgra32);
                rootGrid = new Grid();

                _colorWriteableBitmap = BitmapFactory.New(frameDescription.Width, frameDescription.Height);
                _bodyWriteableBitmap = BitmapFactory.New(frameDescription.Width, frameDescription.Height);
            }


            // Usamos el objeto ventana como el view model
            this.DataContext = this;

            // Inicializamos los componetes (controles) de la ventana
            this.InitializeComponent();

            Image.Source = _colorWriteableBitmap;

            // Asignamos combinación de teclas a comandos.
            teclaMasMargenH.InputGestures.Add(new KeyGesture(Key.Z, ModifierKeys.Control));
            teclaMenosMargenH.InputGestures.Add(new KeyGesture(Key.X, ModifierKeys.Control));

            teclaMasMargenV.InputGestures.Add(new KeyGesture(Key.C, ModifierKeys.Control));
            teclaMenosMargenV.InputGestures.Add(new KeyGesture(Key.V, ModifierKeys.Control));

            teclaMasMargenIni.InputGestures.Add(new KeyGesture(Key.B, ModifierKeys.Control));
            teclaMenosMargenIni.InputGestures.Add(new KeyGesture(Key.N, ModifierKeys.Control));

        }

        // Añadimos funciones a realizar con hotkeys.
        private void masMargenH(object sender, ExecutedRoutedEventArgs e)
        {
            porcH += 0.05;
            porcHText.Text = porcH.ToString();
        }

        private void menosMargenH(object sender, ExecutedRoutedEventArgs e)
        {
            porcH -= 0.05;
            porcHText.Text = porcH.ToString();
        }

        private void masMargenV(object sender, ExecutedRoutedEventArgs e)
        {
            porcV += 0.05;
            porcVText.Text = porcV.ToString();
        }

        private void menosMargenV(object sender, ExecutedRoutedEventArgs e)
        {
            porcV -= 0.05;
            porcVText.Text = porcV.ToString();
        }

        private void masMargenIni(object sender, ExecutedRoutedEventArgs e)
        {
            porcIni += 0.05;
            porcIniText.Text = porcIni.ToString();
        }

        private void menosMargenIni(object sender, ExecutedRoutedEventArgs e)
        {
            porcIni -= 0.05;
            porcIniText.Text = porcIni.ToString();
        }

        /// <summary>
        /// Notificador del manejador de eventos para indicar los cambios en los datos 
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Obtenemos el bitmap para la pantalla 
        /// </summary>
        public ImageSource ImageSource
        {
            get
            {
                return this.bitmap;
            }
        }

        /// <summary>
        /// Variables para el lector del cuerpo, donde los almacenaremos, y donde almacenamos el body
        /// </summary>
        private BodyFrameReader bodyFrameReader = null;
        private Body[] bodies = null;
        private RenderTargetBitmap _bodySourceRTB;

        /// <summary>
        /// Donde vamos a dibujar la union de bones 
        /// </summary>
        private DrawingGroup drawingGroup;


        /// <summary>
        /// Comenzamos la ejecucion de tareas
        /// </summary>
        /// <param name="sender">objeto que envia los enventos</param>
        /// <param name="e">argumentos del evento</param>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.colorFrameReader != null)
            {
                this.colorFrameReader.FrameArrived += this.ColorFrameReaderFrameArrived;
            }

            if (this.bodyFrameReader != null)
            {
                this.bodyFrameReader.FrameArrived += this.BodyFrameReaderFrameArrived;
            }

            PosicionInicio.Visibility = System.Windows.Visibility.Visible;
            circuloRojo.Visibility = System.Windows.Visibility.Hidden;
            circuloVerde.Visibility = System.Windows.Visibility.Hidden;
            flecha1.Visibility = System.Windows.Visibility.Hidden;
            flecha2.Visibility = System.Windows.Visibility.Hidden;
            InstruccionesText.Text = "Imítame";

            porcHText.Text = porcH.ToString();
            porcVText.Text = porcV.ToString();
            porcIniText.Text = porcIni.ToString();


        }

        /// <summary>
        /// Ejecutamos la suspensión de las tareas
        /// </summary>
        /// <param name="sender">objeto que envia los eventos</param>
        /// <param name="e">argumentos del evento</param>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (this.colorFrameReader != null)
            {
                // ColorFrameReder está disponible
                this.colorFrameReader.Dispose();
                this.colorFrameReader = null;
            }

            if (this.bodyFrameReader != null)
            {
                // BodyFrameReder está disponible
                this.bodyFrameReader.Dispose();
                this.bodyFrameReader = null;
            }
            
            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }

        /// <summary>
        /// Manejador para los datos de llegada del sensor del color frame
        /// </summary>
        /// <param name="sender">objetos enviados del evento</param>
        /// <param name="e">argumentos del evento</param>
        private void ColorFrameReaderFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            ColorFrameReference frameReference = e.FrameReference;

            try
            {
                ColorFrame frame = frameReference.AcquireFrame();

                if (frame != null)
                {
                    // ColorFrame está disponible
                    using (frame)
                    {
                        FrameDescription frameDescription = frame.FrameDescription;

                        // Verificamos que los datos y la escritura de los nuevos datos de color frame se pueden hacer en la pantalla del bitmap
                        if ((frameDescription.Width == this.bitmap.PixelWidth) && (frameDescription.Height == this.bitmap.PixelHeight))
                        {
                            if (frame.RawColorImageFormat == ColorImageFormat.Bgra)
                            {
                                frame.CopyRawFrameDataToArray(this.pixels);
                            }
                            else
                            {
                                frame.CopyConvertedFrameDataToArray(this.pixels, ColorImageFormat.Bgra);
                            }

                            _colorWriteableBitmap.FromByteArray(this.pixels);
                            var rec = new Rect(0, 0, frameDescription.Width, frameDescription.Height);
                            using (_colorWriteableBitmap.GetBitmapContext())
                            {
                                using (_bodyWriteableBitmap.GetBitmapContext())
                                {
                                    _colorWriteableBitmap.Blit(rec, _bodyWriteableBitmap, rec, WriteableBitmapExtensions.BlendMode.Additive);
                                }
                            }

                        }
                    }
                }
            }
            catch (Exception)
            {
                // ignoramos si los frame no esta disponible
            }
        }

    
        /// <summary>
        /// Función para indicar al usuario los movimientos que debe realizar
        /// </summary>
        private void ControlaPosicion()
        {
            int widthPantallaMin, widthPantallaMax, widthTotal, heightPantallaMin, heightPantallaMax, heightTotal;
            int manoEjeX = 1236, manoEjeY = 184;

            //limalt.Text = (this.Width * (porcentaje)).ToString();
            //limder.Text = (this.Width * (1-porcentaje)).ToString();
            //widthPantallaMin = (int) (this.Width * porcentaje);
            //widthPantallaMax = (int) (this.Width * (1 - porcentaje));
            //HE TENIDO QUE MODIFICAR ESTOS VALORES YA QUE NO ME VALIAN LOS DEL XAML
            widthPantallaMin = 250;
            widthPantallaMax = 1550;
            heightPantallaMin = 0;
            heightPantallaMax = 958;
            heightTotal = heightPantallaMax - heightPantallaMin;
            widthTotal = widthPantallaMax - widthPantallaMin;
            if ((int)cabeza.X < ((widthTotal * porcH) + widthPantallaMin))
            {
                MovimientoText.Text = "Muevete al centro";
                circuloVerde.Visibility = System.Windows.Visibility.Hidden;
                circuloRojo.Visibility = System.Windows.Visibility.Hidden;

                flecha1.Visibility = System.Windows.Visibility.Visible;
                flecha2.Visibility = System.Windows.Visibility.Visible;
            }
            else if ((int)cabeza.X > ((widthTotal * (1 - porcH)) + widthPantallaMin))
            {
                MovimientoText.Text = "Muevete al centro";
                circuloVerde.Visibility = System.Windows.Visibility.Hidden;
                circuloRojo.Visibility = System.Windows.Visibility.Hidden;

                flecha1.Visibility = System.Windows.Visibility.Visible;
                flecha2.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                if ((int)pieDerecho.Y > ((heightTotal * (1 - porcV)) + heightPantallaMin))
                {
                    MovimientoText.Text = "Alejate";
                    circuloVerde.Visibility = System.Windows.Visibility.Hidden;
                    circuloRojo.Visibility = System.Windows.Visibility.Hidden;

                    flecha1.Visibility = System.Windows.Visibility.Hidden;
                    flecha2.Visibility = System.Windows.Visibility.Hidden;
                }
                else if ((int)cabeza.Y < ((heightTotal * (porcV * 2)) + heightPantallaMin))
                {
                    MovimientoText.Text = "Alejate";
                    circuloVerde.Visibility = System.Windows.Visibility.Hidden;
                    circuloRojo.Visibility = System.Windows.Visibility.Hidden;

                    flecha1.Visibility = System.Windows.Visibility.Hidden;
                    flecha2.Visibility = System.Windows.Visibility.Hidden;
                }
                else
                {
                    if ((int)manoDerecha.Y > ((int)cabeza.Y))
                    {
                        MovimientoText.Text = "Levanta la mano";
                        circuloRojo.Visibility = System.Windows.Visibility.Visible;
                        circuloVerde.Visibility = System.Windows.Visibility.Hidden;

                        flecha1.Visibility = System.Windows.Visibility.Hidden;
                        flecha2.Visibility = System.Windows.Visibility.Hidden;
                    }
                    else
                    {
                        if (!(
                            ((int)manoDerecha.X > (manoEjeX - (manoEjeX * porcIni))) && ((int)manoDerecha.X < (manoEjeX + (manoEjeX * porcIni)))
                            &&
                            ((int)manoDerecha.Y > (manoEjeY - (manoEjeY * porcIni))) && ((int)manoDerecha.Y < (manoEjeY + (manoEjeY * porcIni)))
                            ))
                        {
                            MovimientoText.Text = "Coloca la mano en el círculo";
                            circuloVerde.Visibility = System.Windows.Visibility.Visible;
                            circuloRojo.Visibility = System.Windows.Visibility.Hidden;
                            flecha1.Visibility = System.Windows.Visibility.Hidden;
                            flecha2.Visibility = System.Windows.Visibility.Hidden;
                        }
                        else
                        {
                            MovimientoText.Text = "COMIENZA EL JUEGO";
                            circuloVerde.Visibility = System.Windows.Visibility.Hidden;
                            circuloRojo.Visibility = System.Windows.Visibility.Hidden;
                            flecha1.Visibility = System.Windows.Visibility.Hidden;
                            flecha2.Visibility = System.Windows.Visibility.Hidden;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Manejador para los datos de llegada del sensor del body
        /// </summary>
        /// <param name="sender">objetos enviados del evento</param>
        /// <param name="e">argumentos del evento</param>
        private void BodyFrameReaderFrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            BodyFrameReference frameReference = e.FrameReference;

            try
            {
                BodyFrame frame = frameReference.AcquireFrame();

                if (frame != null)
                {
                    // BodyFrame está disponible
                    using (frame)
                    {
                        using (DrawingContext dc = this.drawingGroup.Open())
                        {
                            // Dibujamos un fondo transparente para fijar el tamaño del render

                            dc.DrawRectangle(Brushes.Transparent, null, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));

                            // La primera vez que se llama, Kinect alojará cada cuerpo en el array.
                            // Mientras estos objetos del cuerpo no se desechen o no se pongan a null en el array, se pueden volver a suar .
                            frame.GetAndRefreshBodyData(this.bodies);

                            foreach (Body body in this.bodies)
                            {
                                if (body.IsTracked)
                                {
                                    this.DrawClippedEdges(body, dc);
                                    this.ControlaPosicion();

                                    IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                                    // convierte los puntos de union de profuncidad (pantalla) del espacio
                                    var jointPoints = new Dictionary<JointType, Point>();
                                    foreach (JointType jointType in joints.Keys)
                                    {
                                        ColorSpacePoint colorSpacePoint = this.coordinateMapper.MapCameraPointToColorSpace(joints[jointType].Position);
                                        jointPoints[jointType] = new Point(colorSpacePoint.X, colorSpacePoint.Y);
                                    }

                                    this.DrawBody(joints, jointPoints, dc);

                                    this.DrawHand(body.HandLeftState, jointPoints[JointType.HandLeft], dc);
                                    this.DrawHand(body.HandRightState, jointPoints[JointType.HandRight], dc);
                                }
                            }

                            // impide dibujar fuera del area del render
                            this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
                            bodyImage = new Image { Source = new DrawingImage(drawingGroup), Width = this.displayWidth, Height = this.displayHeight };
                            rootGrid.Children.Clear();
                            rootGrid.Children.Add(bodyImage);
                            rootGrid.Measure(new Size(bodyImage.Width, bodyImage.Height));
                            rootGrid.Arrange(new Rect(0, 0, bodyImage.Width, bodyImage.Height));
                            _bodySourceRTB.Clear();
                            _bodySourceRTB.Render(rootGrid);
                            _bodySourceRTB.CopyPixels(this.bodyBytespixels, displayWidth * this.bytesPerPixel,
                                0);
                            _bodyWriteableBitmap.FromByteArray(this.bodyBytespixels);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // ignoramos si los frame no esta disponible
            }
        }

        private void DrawBody(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, DrawingContext drawingContext)
        {
            // Dibujamos los huesos

            // Torso
            this.DrawBone(joints, jointPoints, JointType.Head, JointType.Neck, drawingContext);
            cabeza = jointPoints[JointType.Head];
            this.DrawBone(joints, jointPoints, JointType.Neck, JointType.SpineShoulder, drawingContext);
            this.DrawBone(joints, jointPoints, JointType.SpineShoulder, JointType.SpineMid, drawingContext);
            this.DrawBone(joints, jointPoints, JointType.SpineMid, JointType.SpineBase, drawingContext);
            this.DrawBone(joints, jointPoints, JointType.SpineShoulder, JointType.ShoulderRight, drawingContext);
            this.DrawBone(joints, jointPoints, JointType.SpineShoulder, JointType.ShoulderLeft, drawingContext);
            this.DrawBone(joints, jointPoints, JointType.SpineBase, JointType.HipRight, drawingContext);
            this.DrawBone(joints, jointPoints, JointType.SpineBase, JointType.HipLeft, drawingContext);

            // Brazo derecho   
            this.DrawBone(joints, jointPoints, JointType.ShoulderRight, JointType.ElbowRight, drawingContext);
            hombroDerecho = jointPoints[JointType.ShoulderRight];
            this.DrawBone(joints, jointPoints, JointType.ElbowRight, JointType.WristRight, drawingContext);
            this.DrawBone(joints, jointPoints, JointType.WristRight, JointType.HandRight, drawingContext);
            this.DrawBone(joints, jointPoints, JointType.HandRight, JointType.HandTipRight, drawingContext);
            manoDerecha = jointPoints[JointType.HandRight];
            this.DrawBone(joints, jointPoints, JointType.WristRight, JointType.ThumbRight, drawingContext);

            // Brazo izquierdo
            this.DrawBone(joints, jointPoints, JointType.ShoulderLeft, JointType.ElbowLeft, drawingContext);
            hombroIzquierdo = jointPoints[JointType.ShoulderLeft];
            this.DrawBone(joints, jointPoints, JointType.ElbowLeft, JointType.WristLeft, drawingContext);
            this.DrawBone(joints, jointPoints, JointType.WristLeft, JointType.HandLeft, drawingContext);
            manoIzquierda = jointPoints[JointType.HandLeft];
            this.DrawBone(joints, jointPoints, JointType.HandLeft, JointType.HandTipLeft, drawingContext);
            this.DrawBone(joints, jointPoints, JointType.WristLeft, JointType.ThumbLeft, drawingContext);

            // Pierna derecha
            this.DrawBone(joints, jointPoints, JointType.HipRight, JointType.KneeRight, drawingContext);
            this.DrawBone(joints, jointPoints, JointType.KneeRight, JointType.AnkleRight, drawingContext);
            this.DrawBone(joints, jointPoints, JointType.AnkleRight, JointType.FootRight, drawingContext);
            pieDerecho = jointPoints[JointType.AnkleRight];

            // Pierna izquierda
            this.DrawBone(joints, jointPoints, JointType.HipLeft, JointType.KneeLeft, drawingContext);
            this.DrawBone(joints, jointPoints, JointType.KneeLeft, JointType.AnkleLeft, drawingContext);
            this.DrawBone(joints, jointPoints, JointType.AnkleLeft, JointType.FootLeft, drawingContext);
            pieIzquierdo = jointPoints[JointType.AnkleLeft];

            // Dibujamos las uniones
            foreach (JointType jointType in joints.Keys)
            {
                Brush drawBrush = null;

                TrackingState trackingState = joints[jointType].TrackingState;

                if (trackingState == TrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;
                }
                else if (trackingState == TrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType], JointThickness, JointThickness);
                }
            }
        }

        /// <summary>
        /// Dibujar un hueso del cuerpo (articulación a articulación)
        /// </summary>
        /// <param name="joints">Uniones para dibujar</param>
        /// <param name="jointPoints">Translación de posición de las uniones dibujadas</param>
        /// <param name="jointType0">Prirema unión del hueso dibujado</param>
        /// <param name="jointType1">Segunda unión del hueso dibujado</param>
        /// <param name="drawingContext">Dibujo del contexto para dibujar</param>
        private void DrawBone(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, JointType jointType0, JointType jointType1, DrawingContext drawingContext)
        {
            Joint joint0 = joints[jointType0];
            Joint joint1 = joints[jointType1];

            // Si no podemos encontrar alguno de las uniones, salimos
            if (joint0.TrackingState == TrackingState.NotTracked ||
                joint1.TrackingState == TrackingState.NotTracked)
            {
                return;
            }

            // No dibujamos si cada punto fue inferido
            if (joint0.TrackingState == TrackingState.Inferred &&
                joint1.TrackingState == TrackingState.Inferred)
            {
                return;
            }

            // Asumimos que todos los huesos han sido inferidos a menos que se rastreen ambas articulaciones 
            Pen drawPen = this.inferredBonePen;
            if ((joint0.TrackingState == TrackingState.Tracked) && (joint1.TrackingState == TrackingState.Tracked))
            {
                drawPen = this.trackedBonePen;
            }

            drawingContext.DrawLine(drawPen, jointPoints[jointType0], jointPoints[jointType1]);
        }

        /// <summary>
        /// Dibujamos un simbolo de la mano is se produce: círculo rojo = cerrada, verde = abierta, azul = señalando
        /// </summary>
        /// <param name="handState">estado de la mano</param>
        /// <param name="handPosition">posición de la mano</param>
        /// <param name="drawingContext">Dibujamos el contesto para dibujar</param>
        private void DrawHand(HandState handState, Point handPosition, DrawingContext drawingContext)
        {
            switch (handState)
            {
                case HandState.Closed:
                    drawingContext.DrawEllipse(this.handClosedBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Open:
                    drawingContext.DrawEllipse(this.handOpenBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Lasso:
                    drawingContext.DrawEllipse(this.handLassoBrush, null, handPosition, HandSize, HandSize);
                    break;
            }
        }

        /// <summary>
        /// Dibujar los indicadores de los bordes si el cuerpo los pisa
        /// </summary>
        /// <param name="body">cuerpo para dibujar la información</param>
        /// <param name="drawingContext">Dibujamos el contesto para dibujar</param>
        private void DrawClippedEdges(Body body, DrawingContext drawingContext)
        {
            FrameEdges clippedEdges = body.ClippedEdges;

            if (clippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, this.displayHeight - ClipBoundsThickness, this.displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, this.displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, this.displayHeight));
            }

            if (clippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(this.displayWidth - ClipBoundsThickness, 0, ClipBoundsThickness, this.displayHeight));
            }
        }
    }
}
