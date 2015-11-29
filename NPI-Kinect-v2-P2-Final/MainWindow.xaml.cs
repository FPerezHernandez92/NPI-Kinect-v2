using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.ComponentModel;
using Microsoft.Kinect;

namespace NPIKinectv2
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        /// <summary>
        /// Activado el modo noche.
        /// </summary>
        bool modoblanco=false;

        /// <summary>
        /// Porcentaje de error que vamos a admitir.
        /// </summary>
        double porcentaje_alto = 0.10;
        double porcentaje_bajo = 0.10;
        double porcentaje_lados = 0.10;
        double precision = 0.04;
        int error = 50;

        /// <summary>
        /// Flags para bombillas tocadas.
        /// </summary>
        bool[] tocada = { false, false, false, false, false, false, false, false, false};

        /// <summary>
        ///Flag para colocar al usuario.
        /// </summary>
        bool colocado = false;

        /// <summary>
        /// Flags para movernos entre menús.
        /// </summary>
        bool[] botonesmenu = { true, false, false ,false};

        /// <summary>
        /// Iniciamos el tiempo.
        /// </summary>
        int segundos = 0;

        /// <summary>
        /// Inicializamos variable para record.
        /// </summary>
        int mejor_tiempo = 999999;

        /// <summary>
        /// Variable para controlar el tiempo.
        /// </summary>
        DateTime dt;

        /// <summary>
        /// Variables para controlar que mano está abierta en el menú de opciones.
        /// </summary>
        bool activadoder = false, activadoizq = false, activadonoched=true, activadonochei=true;

        /// <summary>
        /// Temporizador.
        /// </summary>
        System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();

        /// <summary>
        /// Variable para mostrar el esqueleto.
        /// </summary>
        bool skeletonFlag = false;

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

        /// <summary>
        /// Añadidos teclas para hotkeys, modificamos el porcentaje de error de los margenes, la dificultad del juego y el flag que muestra el esqueleto.
        /// </summary>
        public static RoutedCommand teclaMasMargenH = new RoutedCommand();
        public static RoutedCommand teclaMenosMargenH = new RoutedCommand();

        public static RoutedCommand teclaMasMargenV = new RoutedCommand();
        public static RoutedCommand teclaMenosMargenV = new RoutedCommand();

        public static RoutedCommand teclaMasMargenIni = new RoutedCommand();
        public static RoutedCommand teclaMenosMargenIni = new RoutedCommand();

        public static RoutedCommand teclaMostrarEsqueleto = new RoutedCommand();

        /// <summary>
        /// Inicialización de una nueva instancia de la clase MainWindow
        /// </summary>
        public MainWindow()
        {
            /// <summary>
            /// Iniciamos el sensor por defecto.
            /// </summary>
            this.kinectSensor = KinectSensor.GetDefault();

            if (this.kinectSensor != null)
            {
                this.coordinateMapper = this.kinectSensor.CoordinateMapper;

                /// <summary>
                /// Abrimos el sensor.
                /// </summary>
                this.kinectSensor.Open();

                /// <summary>
                /// Obtenemos el ancho y el largo del display que vamos a mostrar.
                /// </summary>
                FrameDescription frameDescription = this.kinectSensor.ColorFrameSource.FrameDescription;
                this.displayWidth = frameDescription.Width;
                this.displayHeight = frameDescription.Height;

                /// <summary>
                /// Iniciamos los bodies.
                /// </summary>
                this.bodies = new Body[this.kinectSensor.BodyFrameSource.BodyCount];

                /// <summary>
                /// Abrimos el lector colorFrameReader y bodyFrameReader para el color frame y el cuerpo.
                /// </summary>
                this.colorFrameReader = this.kinectSensor.ColorFrameSource.OpenReader();
                this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();

                /// <summary>
                /// Iniciamos el espacio donde vamos a almacenar los pixeles que se vayan recibiendo
                /// </summary>
                this.pixels = new byte[frameDescription.Width * frameDescription.Height * this.bytesPerPixel];
                this.bodyBytespixels = new byte[frameDescription.Width * frameDescription.Height * this.bytesPerPixel];

                /// <summary>
                /// Creamos el bitmap para la pantalla
                /// </summary>
                this.bitmap = new WriteableBitmap(frameDescription.Width, frameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
                this.drawingGroup = new DrawingGroup();
                _bodySourceRTB = new RenderTargetBitmap(displayWidth, displayHeight, 96.0, 96.0, PixelFormats.Pbgra32);
                rootGrid = new Grid();

                _colorWriteableBitmap = BitmapFactory.New(frameDescription.Width, frameDescription.Height);
                _bodyWriteableBitmap = BitmapFactory.New(frameDescription.Width, frameDescription.Height);
            }

            /// <summary>
            /// Usamos el objeto ventana como el view model.
            /// </summary>
            this.DataContext = this;

            /// <summary>
            /// Inicializamos los componetes (controles) de la ventana.
            /// </summary>
            this.InitializeComponent();

            Image.Source = _colorWriteableBitmap;

            /// <summary>
            /// Asignamos combinación de teclas a comandos.
            /// </summary
            teclaMasMargenH.InputGestures.Add(new KeyGesture(Key.Z, ModifierKeys.Control));
            teclaMenosMargenH.InputGestures.Add(new KeyGesture(Key.X, ModifierKeys.Control));

            teclaMasMargenV.InputGestures.Add(new KeyGesture(Key.C, ModifierKeys.Control));
            teclaMenosMargenV.InputGestures.Add(new KeyGesture(Key.V, ModifierKeys.Control));

            teclaMasMargenIni.InputGestures.Add(new KeyGesture(Key.B, ModifierKeys.Control));
            teclaMenosMargenIni.InputGestures.Add(new KeyGesture(Key.N, ModifierKeys.Control));

            teclaMostrarEsqueleto.InputGestures.Add(new KeyGesture(Key.M, ModifierKeys.Control));
        }

        /// <summary>
        /// Mostramos el esqueleto con las teclas CTRL+M
        /// </summary
        private void mostrarSkeleton(object sender, ExecutedRoutedEventArgs e)
        {
            if( skeletonFlag == false)
            {
                skeletonFlag = true;
            }
            else
            {
                skeletonFlag = false;
            }
        }

        /// <summary>
        /// Función para navegar por los menús.
        /// </summary
        private void Interfaz(HandState handStateleft, HandState handStateright)
        {
            if (botonesmenu[0])
            {
                this.IniciarMenu(handStateleft, handStateright);
            }
            else if (botonesmenu[1])
            {
                this.IniciarJuego(handStateleft, handStateright);
            }
            else if (botonesmenu[2])
            {
                this.IniciarOpciones(handStateleft, handStateright);
            }
        }

        /// <summary>
        /// Función que controla las acciones a realizar al tocar los distintos botones de los menús.
        /// </summary
        void tocarBotonMenu(int posX, int posY, int i, HandState handStateleft, HandState handStateright)
        {
            if ((((int)manoDerecha.X > (posX - (posX * precision))) && ((int)manoDerecha.X < (posX + (posX * precision))))
                ||
                (((int)manoIzquierda.X > (posX - (posX * precision))) && ((int)manoIzquierda.X < (posX + (posX * precision)))))
            {
                if ((((int)manoDerecha.Y > (posY - (posY * precision))) && ((int)manoDerecha.Y < (posY + (posY * precision))))
                    ||
                    (((int)manoIzquierda.Y > (posY - (posY * precision))) && ((int)manoIzquierda.Y < (posY + (posY * precision)))))
                {
                    if (i == 0)
                    {
                        if (modoblanco)
                            refreshwv.Visibility = System.Windows.Visibility.Visible;
                        if (!modoblanco)
                            refreshv.Visibility = System.Windows.Visibility.Visible;
                        switch (handStateleft)
                        {
                            case HandState.Closed:
                                botonesmenu[0] = true;
                                botonesmenu[1] = false;
                                botonesmenu[2] = false;
                                botonesmenu[3] = false;
                                if (segundos < mejor_tiempo && segundos != 0)
                                {
                                    mejor_tiempo = segundos;
                                    mejorpuntuacion.Text = mejor_tiempo.ToString();
                                }
                                segundos = 0;
                                break;
                        }
                        switch (handStateright)
                        {
                            case HandState.Closed:
                                botonesmenu[0] = true;
                                botonesmenu[1] = false;
                                botonesmenu[2] = false;
                                botonesmenu[3] = false;
                                if (segundos < mejor_tiempo && segundos != 0)
                                {
                                    mejor_tiempo = segundos;
                                    mejorpuntuacion.Text = mejor_tiempo.ToString();
                                }
                                segundos = 0;
                                break;
                        }


                    }
                    else if (i == 1)
                    {
                        if (modoblanco)
                            playwv.Visibility = System.Windows.Visibility.Visible;
                        if (!modoblanco)
                            playv.Visibility = System.Windows.Visibility.Visible;
                        switch (handStateleft)
                        {
                            case HandState.Closed:
                                botonesmenu[0] = false;
                                botonesmenu[1] = true;
                                botonesmenu[2] = false;
                                botonesmenu[3] = false;
                                dispatcherTimer.Start();
                                break;
                        }
                        switch (handStateright)
                        {
                            case HandState.Closed:
                                botonesmenu[0] = false;
                                botonesmenu[1] = true;
                                botonesmenu[2] = false;
                                botonesmenu[3] = false;
                                dispatcherTimer.Start();
                                break;
                        }
                    }
                    else if (i == 2)
                    {
                        if (modoblanco)
                            settingswv.Visibility = System.Windows.Visibility.Visible;
                        if (!modoblanco)
                            settingsv.Visibility = System.Windows.Visibility.Visible;
                        switch (handStateleft)
                        {
                            case HandState.Closed:
                                botonesmenu[0] = false;
                                botonesmenu[1] = false;
                                botonesmenu[2] = true;
                                botonesmenu[3] = false;
                                break;
                        }
                        switch (handStateright)
                        {
                            case HandState.Closed:
                                botonesmenu[0] = false;
                                botonesmenu[1] = false;
                                botonesmenu[2] = true;
                                botonesmenu[3] = false;
                                break;
                        }
                    }
                    else if (i == 3)
                    {
                        if (modoblanco)
                            cancelwv.Visibility = System.Windows.Visibility.Visible;
                        if (!modoblanco)
                            cancelv.Visibility = System.Windows.Visibility.Visible;
                        switch (handStateleft)
                        {
                            case HandState.Closed:
                                Environment.Exit(1);
                                break;
                        }
                        switch (handStateright)
                        {
                            case HandState.Closed:
                                Environment.Exit(1);
                                break;
                        }
                    }
                }
            }
            
                
            
        }

        /// <summary>
        /// Inicia el menú inicial mostrando los botones del mismo. También ponemos la precisión del menú e inicializamos las variables del juego.
        /// </summary
        private void IniciarMenu(HandState handStateleft, HandState handStateright)
        {
            Luna.Visibility = System.Windows.Visibility.Hidden;
            Sol.Visibility = System.Windows.Visibility.Hidden;
            if (!modoblanco)
            {
                play.Visibility = System.Windows.Visibility.Visible;
                cancel.Visibility = System.Windows.Visibility.Visible;
                settings.Visibility = System.Windows.Visibility.Visible;
                playw.Visibility = System.Windows.Visibility.Hidden;
                cancelw.Visibility = System.Windows.Visibility.Hidden;
                settingsw.Visibility = System.Windows.Visibility.Hidden;
            }
            else
            {
                playw.Visibility = System.Windows.Visibility.Visible;
                cancelw.Visibility = System.Windows.Visibility.Visible;
                settingsw.Visibility = System.Windows.Visibility.Visible;
                play.Visibility = System.Windows.Visibility.Hidden;
                cancel.Visibility = System.Windows.Visibility.Hidden;
                settings.Visibility = System.Windows.Visibility.Hidden;
            }
            precision = 0.03;
            for (int i=0; i<9; i++)
            {
                tocada[i] = false;
            }

            refresh.Visibility = System.Windows.Visibility.Hidden;
            refreshw.Visibility = System.Windows.Visibility.Hidden;
            slider.Visibility = System.Windows.Visibility.Hidden;
            difitex1.Visibility = System.Windows.Visibility.Hidden;
            difitex2.Visibility = System.Windows.Visibility.Hidden;
            difitex3.Visibility = System.Windows.Visibility.Hidden;
            difitex4.Visibility = System.Windows.Visibility.Hidden;

            refreshv.Visibility = System.Windows.Visibility.Hidden;
            refreshwv.Visibility = System.Windows.Visibility.Hidden;
            settingsv.Visibility = System.Windows.Visibility.Hidden;
            settingswv.Visibility = System.Windows.Visibility.Hidden;
            cancelv.Visibility = System.Windows.Visibility.Hidden;
            cancelwv.Visibility = System.Windows.Visibility.Hidden;
            playv.Visibility = System.Windows.Visibility.Hidden;
            playwv.Visibility = System.Windows.Visibility.Hidden;

            if (!modoblanco)
                Start.Visibility = System.Windows.Visibility.Visible;

            tocarBotonMenu(912 + error, 171 + error, 1, handStateleft, handStateright); //play
            tocarBotonMenu(1251 + error, 312 + error, 3, handStateleft, handStateright); //cancel
            tocarBotonMenu(575 + error, 312 + error, 2, handStateleft, handStateright); //options
        }

        /// <summary>
        /// Controla el slider de dificultad del menú de opciones.
        /// </summary
        private void controlarSlider(int posX, int posY, HandState handStateleft, HandState handStateright)
        {
            MovimientoText.Visibility = System.Windows.Visibility.Hidden;
            if ((((int)manoDerecha.X > (posX - (300))) && ((int)manoDerecha.X < (posX + (300))))
                ||
                (((int)manoIzquierda.X > (posX - (300))) && ((int)manoIzquierda.X < (posX + (300)))))
            {
                if ((((int)manoDerecha.Y > (posY - (posY * precision*2))) && ((int)manoDerecha.Y < (posY + (posY * precision*2))))
                    ||
                    (((int)manoIzquierda.Y > (posY - (posY * precision*2))) && ((int)manoIzquierda.Y < (posY + (posY * precision*2)))))

                {
                    MovimientoText.Visibility = System.Windows.Visibility.Visible;
                    MovimientoText.Text = "Agarra y mueve el slider";
                    switch (handStateleft)
                    {
                        case HandState.Closed:
                            
                            if (slider.Value == 0 && ( ((int)manoIzquierda.X > 647) && ((int)manoIzquierda.X < 847) ) )
                            {
                                activadoizq = true;
                            }
                            if (slider.Value == 1 && (((int)manoIzquierda.X > 847) && ((int)manoIzquierda.X < 1047)))
                            {
                                activadoizq  = true;
                            }
                            if (slider.Value == 2 && (((int)manoIzquierda.X > 1047) && ((int)manoIzquierda.X < 1247)))
                            {
                                activadoizq = true;
                            }
                            if (activadoizq && (((int)manoIzquierda.X < 847)))
                            {
                                slider.Value = 0;
                            }
                            if (activadoizq && (((int)manoIzquierda.X > 847) && ((int)manoIzquierda.X < 1047)))
                            {
                                slider.Value = 1;
                            }
                            if (activadoizq && ((int)manoIzquierda.X > 1047))
                            {
                                slider.Value = 2;
                            }
                            break;

                        case HandState.Open:
                            activadoizq = false;
                            break;
                    }
                    switch (handStateright)
                    {
                        case HandState.Closed:
                            if (slider.Value == 0 && (((int)manoDerecha.X > 647) && ((int)manoDerecha.X < 847)))
                            {
                                activadoder = true;
                            }
                            if (slider.Value == 1 && (((int)manoDerecha.X > 847) && ((int)manoDerecha.X < 1047)))
                            {
                                activadoder = true;
                            }
                            if (slider.Value == 2 && (((int)manoDerecha.X > 1047) && ((int)manoDerecha.X < 1247)))
                            {
                                activadoder = true;
                            }
                            if (activadoder && ( ((int)manoDerecha.X < 847)))
                            {
                                slider.Value = 0;
                            }
                            if (activadoder && (((int)manoDerecha.X > 847) && ((int)manoDerecha.X < 1047)))
                            {
                                slider.Value = 1;
                            }
                            if (activadoder && (((int)manoDerecha.X > 1047) ))
                            {
                                slider.Value = 2;
                            }
                            break;
                        case HandState.Open:
                            activadoder = false;
                            break;
                    }
                }
            }

        }

        /// <summary>
        /// Tocar el check box
        /// </summary
        private void tocarCheckBox(int posX, int posY, HandState handStateleft, HandState handStateright)
        {
            if ((((int)manoDerecha.X > (posX - (posX * precision))) && ((int)manoDerecha.X < (posX + (posX * precision))))
                ||
                (((int)manoIzquierda.X > (posX - (posX * precision))) && ((int)manoIzquierda.X < (posX + (posX * precision)))))
            {
                if ((((int)manoDerecha.Y > (posY - (posY * precision))) && ((int)manoDerecha.Y < (posY + (posY * precision))))
                    ||
                    (((int)manoIzquierda.Y > (posY - (posY * precision))) && ((int)manoIzquierda.Y < (posY + (posY * precision)))))
                {
                    if (modoblanco)
                    {
                        Solv.Visibility = System.Windows.Visibility.Visible;
                    }
                    if (!modoblanco)
                    {
                        Lunav.Visibility = System.Windows.Visibility.Visible;
                    }
                    switch (handStateright)
                    {
                        case HandState.Closed:

                            if (activadonoched)
                            {
                                modoblanco = !modoblanco;
                                activadonoched = false;
                            }
                            break;
                        case HandState.Open:
                            activadonoched = true;
                            break;
                    }
                    switch (handStateleft)
                    {
                        case HandState.Closed:

                            if (activadonochei)
                            {
                                modoblanco = !modoblanco;
                                activadonochei = false;
                            }
                            break;
                        case HandState.Open:
                            activadonochei = true;
                            break;
                    }

                }
            }
        }

        /// <summary>
        /// Inicia el menú de opciones.
        /// </summary
        private void IniciarOpciones(HandState handStateleft, HandState handStateright)
        {
            settingsv.Visibility = System.Windows.Visibility.Hidden;
            settingswv.Visibility = System.Windows.Visibility.Hidden;

            play.Visibility = System.Windows.Visibility.Hidden;
            cancel.Visibility = System.Windows.Visibility.Hidden;
            settings.Visibility = System.Windows.Visibility.Hidden;
            playw.Visibility = System.Windows.Visibility.Hidden;
            cancelw.Visibility = System.Windows.Visibility.Hidden;
            settingsw.Visibility = System.Windows.Visibility.Hidden;

            slider.Visibility = System.Windows.Visibility.Visible;
            difitex1.Visibility = System.Windows.Visibility.Visible;
            difitex2.Visibility = System.Windows.Visibility.Visible;
            difitex3.Visibility = System.Windows.Visibility.Visible;
            difitex4.Visibility = System.Windows.Visibility.Visible;

            Luna.Visibility = System.Windows.Visibility.Visible;
            Lunav.Visibility = System.Windows.Visibility.Hidden;
            Solv.Visibility = System.Windows.Visibility.Hidden;


            tocarCheckBox(660+error,180+error,handStateleft,handStateright);

            if (!modoblanco)
            {
                refresh.Visibility = System.Windows.Visibility.Visible;
                refreshw.Visibility = System.Windows.Visibility.Hidden;
                difitex1.Foreground = Brushes.Black;
                difitex2.Foreground = Brushes.Black;
                difitex3.Foreground = Brushes.Black;
                difitex4.Foreground = Brushes.Black;
                mejorpuntuacion.Foreground = Brushes.Black;
                MovimientoText.Foreground = Brushes.Black;
                CronoText.Background = Brushes.White;
                CronoText.Foreground = Brushes.Black;
                difitex1.Background = difitex5.Background;
                difitex2.Background = difitex5.Background;
                difitex3.Background = difitex5.Background;
                difitex4.Background = difitex5.Background;
                mejorpuntuacion.Background = difitex5.Background;
                MovimientoText.Background = difitex5.Background;
                Sol.Visibility = System.Windows.Visibility.Hidden;
                Luna.Visibility = System.Windows.Visibility.Visible;
                skeletonFlag = false;
                Start.Visibility = System.Windows.Visibility.Visible;
                Startb.Visibility = System.Windows.Visibility.Hidden;
            }
            else
            {
                refresh.Visibility = System.Windows.Visibility.Hidden;
                refreshw.Visibility = System.Windows.Visibility.Visible;
                difitex1.Foreground = Brushes.White;
                difitex2.Foreground = Brushes.White;
                difitex3.Foreground = Brushes.White;
                difitex4.Foreground = Brushes.White;
                mejorpuntuacion.Foreground = Brushes.White;
                MovimientoText.Foreground = Brushes.White;
                CronoText.Background = Brushes.Black;
                CronoText.Foreground = Brushes.White;
                difitex1.Background = Brushes.Transparent;
                difitex2.Background = Brushes.Transparent;
                difitex3.Background = Brushes.Transparent;
                difitex4.Background = Brushes.Transparent;
                mejorpuntuacion.Background = Brushes.Transparent;
                MovimientoText.Background = Brushes.Transparent;
                Sol.Visibility = System.Windows.Visibility.Visible;
                Luna.Visibility = System.Windows.Visibility.Hidden;
                skeletonFlag = true;
                Startb.Visibility = System.Windows.Visibility.Visible;
                Start.Visibility = System.Windows.Visibility.Hidden;
            }
            refreshv.Visibility = System.Windows.Visibility.Hidden;
            refreshwv.Visibility = System.Windows.Visibility.Hidden;
            tocarBotonMenu(1171 + error, 180 + error, 0, handStateleft, handStateright);  //refresh

            controlarSlider(251+713+error,524+error,handStateleft, handStateright);
        }

        /// <summary>
        /// Controla el temporizador y lo muestra en pantalla.
        /// </summary
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            segundos++;
            CronoText.Text = dt.AddSeconds(segundos).ToString("mm:ss");
        }

        /// <summary>
        /// Inicia el menú de juego.
        /// </summary
        private void IniciarJuego(HandState handStateleft, HandState handStateright)
        {
            playv.Visibility = System.Windows.Visibility.Hidden;
            playwv.Visibility = System.Windows.Visibility.Hidden;
            play.Visibility = System.Windows.Visibility.Hidden;
            cancel.Visibility = System.Windows.Visibility.Hidden;
            settings.Visibility = System.Windows.Visibility.Hidden;
            playw.Visibility = System.Windows.Visibility.Hidden;
            cancelw.Visibility = System.Windows.Visibility.Hidden;
            settingsw.Visibility = System.Windows.Visibility.Hidden;

            if (slider.Value == 0) precision = 0.04;
            else if (slider.Value == 2) precision = 0.02;
            else precision = 0.03;


            if (colocado)
            {
                this.JuegoAgilidad(handStateleft,handStateright);
            }
        }


        /// <summary>
        /// Funciones para controlar las acciones de los hotkeys.
        /// </summary
        private void masMargenH(object sender, ExecutedRoutedEventArgs e)
        {
            porcentaje_alto += 0.05;
        }

        private void menosMargenH(object sender, ExecutedRoutedEventArgs e)
        {
            porcentaje_alto -= 0.05;
        }

        private void masMargenV(object sender, ExecutedRoutedEventArgs e)
        {
            porcentaje_bajo += 0.05;
        }

        private void menosMargenV(object sender, ExecutedRoutedEventArgs e)
        {
            porcentaje_bajo -= 0.05;
        }

        private void masMargenIni(object sender, ExecutedRoutedEventArgs e)
        {
            porcentaje_lados += 0.05;
        }

        private void menosMargenIni(object sender, ExecutedRoutedEventArgs e)
        {
            porcentaje_lados -= 0.05;
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

            /// <summary>
            /// Iniciamos todos los elementos gráficos.
            /// </summary
            flecha1.Visibility = System.Windows.Visibility.Hidden;
            flecha2.Visibility = System.Windows.Visibility.Hidden;
            bombilla.Visibility = System.Windows.Visibility.Hidden;
            play.Visibility = System.Windows.Visibility.Hidden;
            cancel.Visibility = System.Windows.Visibility.Hidden;
            settings.Visibility = System.Windows.Visibility.Hidden;
            refresh.Visibility = System.Windows.Visibility.Hidden;

            flecha1w.Visibility = System.Windows.Visibility.Hidden;
            flecha2w.Visibility = System.Windows.Visibility.Hidden;
            playw.Visibility = System.Windows.Visibility.Hidden;
            cancelw.Visibility = System.Windows.Visibility.Hidden;
            settingsw.Visibility = System.Windows.Visibility.Hidden;
            refreshw.Visibility = System.Windows.Visibility.Hidden;

            settingsv.Visibility = System.Windows.Visibility.Hidden;
            settingswv.Visibility = System.Windows.Visibility.Hidden;
            playv.Visibility = System.Windows.Visibility.Hidden;
            playwv.Visibility = System.Windows.Visibility.Hidden;
            cancelv.Visibility = System.Windows.Visibility.Hidden;
            cancelwv.Visibility = System.Windows.Visibility.Hidden;
            refreshv.Visibility = System.Windows.Visibility.Hidden;
            refreshwv.Visibility = System.Windows.Visibility.Hidden;
            Start.Visibility = System.Windows.Visibility.Hidden;
            Startb.Visibility = System.Windows.Visibility.Hidden;

            slider.Visibility = System.Windows.Visibility.Hidden;
            difitex1.Visibility = System.Windows.Visibility.Hidden;
            difitex2.Visibility = System.Windows.Visibility.Hidden;
            difitex3.Visibility = System.Windows.Visibility.Hidden;
            difitex4.Visibility = System.Windows.Visibility.Hidden;
            difitex5.Visibility = System.Windows.Visibility.Hidden;
            Luna.Visibility = System.Windows.Visibility.Hidden;
            Sol.Visibility = System.Windows.Visibility.Hidden;

            Lunav.Visibility = System.Windows.Visibility.Hidden;
            Solv.Visibility = System.Windows.Visibility.Hidden;

            MovimientoText.Visibility = System.Windows.Visibility.Hidden;
            mejorpuntuacion.Visibility = System.Windows.Visibility.Hidden;

            /// <summary>
            /// Creamos el temporizador y le añadimos un tiempo de tick.
            /// </summary
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
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
                    /// <summary>
                    /// Colorframe está disponible.
                    /// </summary
                    using (frame)
                    {
                        FrameDescription frameDescription = frame.FrameDescription;

                        /// <summary>
                        /// Verificamos que los datos y la escritura de los nuevos datos de color frame se pueden hacer en la pantalla del 
                        /// </summary
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
        /// Función para indicar al usuario los movimientos que debe realizar.
        /// </summary>
        private void ControlaPosicion()
        {
            int widthPantallaMin, widthPantallaMax, widthTotal, heightPantallaMin, heightPantallaMax, heightTotal;

            widthPantallaMin = (int) (this.Width * porcentaje_lados); //minimo en el lateral izquierdo (a la vista)
            widthPantallaMax = (int) (this.Width * (1 - porcentaje_lados)); //máximo en le lateral derecho (a la vista)
            heightPantallaMin = (int) (this.Height * porcentaje_alto); //minimo en la cabeza
            heightPantallaMax = (int) (this.Height * (1- porcentaje_bajo)); //porcentaje en los pies
            heightTotal = heightPantallaMax - heightPantallaMin; //el máximo disponible a lo alto
            widthTotal = widthPantallaMax - widthPantallaMin; //el máximo disponible a lo largo

            if ( ((int)cabeza.Y) < heightPantallaMin)
            {
                MovimientoText.Visibility = System.Windows.Visibility.Visible;
                MovimientoText.Text = "Aléjate";
                colocado = false;
                
                flecha1.Visibility = System.Windows.Visibility.Hidden;
                flecha2.Visibility = System.Windows.Visibility.Hidden;
            }
            else if (( ((int)pieDerecho.Y) > heightPantallaMax) || (((int)pieIzquierdo.Y) > heightPantallaMax))
            {
                MovimientoText.Visibility = System.Windows.Visibility.Visible;
                MovimientoText.Text = "Aléjate";
                colocado = false;
                
                flecha1.Visibility = System.Windows.Visibility.Hidden;
                flecha2.Visibility = System.Windows.Visibility.Hidden;
            }
            else
            {
                if (((int)manoIzquierda.X) < widthPantallaMin)
                {
                    MovimientoText.Visibility = System.Windows.Visibility.Visible;
                    MovimientoText.Text = "Muevete al centro";
                    colocado = false;

                    if (!modoblanco)
                    {
                        flecha1.Visibility = System.Windows.Visibility.Visible;
                        flecha2.Visibility = System.Windows.Visibility.Visible;
                        flecha1w.Visibility = System.Windows.Visibility.Hidden;
                        flecha2w.Visibility = System.Windows.Visibility.Hidden;
                    }
                    else
                    {
                        flecha1w.Visibility = System.Windows.Visibility.Visible;
                        flecha2w.Visibility = System.Windows.Visibility.Visible;
                        flecha1.Visibility = System.Windows.Visibility.Hidden;
                        flecha2.Visibility = System.Windows.Visibility.Hidden;
                    }
                }
                else if (((int)manoDerecha.X) > widthPantallaMax)
                {
                    MovimientoText.Visibility = System.Windows.Visibility.Visible;
                    MovimientoText.Text = "Muevete al centro";
                    colocado = false;

                    if (!modoblanco)
                    {
                        flecha1.Visibility = System.Windows.Visibility.Visible;
                        flecha2.Visibility = System.Windows.Visibility.Visible;
                        flecha1w.Visibility = System.Windows.Visibility.Hidden;
                        flecha2w.Visibility = System.Windows.Visibility.Hidden;
                    }
                    else
                    {
                        flecha1w.Visibility = System.Windows.Visibility.Visible;
                        flecha2w.Visibility = System.Windows.Visibility.Visible;
                        flecha1.Visibility = System.Windows.Visibility.Hidden;
                        flecha2.Visibility = System.Windows.Visibility.Hidden;
                    }
                    
                }
                else
                {
                    MovimientoText.Visibility = System.Windows.Visibility.Hidden;
                    MovimientoText.Text = " ";
                    colocado = true;
                }
            }
            
        }

        /// <summary>
        /// Pone la bombilla a tocada durante el juego.
        /// </summary>
        void tocarBombilla(int posXbombilla, int posYbombilla, int i)
        {
            if ((((int)manoDerecha.X > (posXbombilla - (posXbombilla * precision))) && ((int)manoDerecha.X < (posXbombilla + (posXbombilla * precision))))
                ||
                (((int)manoIzquierda.X > (posXbombilla - (posXbombilla * precision))) && ((int)manoIzquierda.X < (posXbombilla + (posXbombilla * precision)))))
            {
                if ((((int)manoDerecha.Y > (posYbombilla - (posYbombilla * precision))) && ((int)manoDerecha.Y < (posYbombilla + (posYbombilla * precision))))
                    ||
                    (((int)manoIzquierda.Y > (posYbombilla - (posYbombilla * precision))) && ((int)manoIzquierda.Y < (posYbombilla + (posYbombilla * precision)))))
                {
                    tocada[i] = true;
                }
            }
        }

        /// <summary>
        /// Inicia el juego.
        /// </summary>
        private void JuegoAgilidad(HandState handStateleft, HandState handStateright)
        {
            int[] bomb1 = { 1346, 590, 1292, 642, 913, 590, 623, 733, 1047 };
            int[] bomb2 = { 396, 518, 256, 180, 661, 396, 737, 340, 128 };
            int[] bomb3 = { 481, 1252, 550, 1200, 929, 1252, 1219, 1109, 795 };
            int[] bomb4 = { 9704, 9522, 9884, 9960, 9479, 9744, 9403, 9800, 10012 };

            bombilla.Visibility = System.Windows.Visibility.Visible;

            if (!tocada[0])
            {
                bombilla.Margin = new Thickness(bomb1[0], bomb2[0], bomb3[0], bomb4[0]);
                tocarBombilla(bomb1[0] + 50, bomb2[0] + 50, 0);
            }
            if (tocada[0])
            {
                if (!tocada[1])
                {
                    bombilla.Margin = new Thickness(bomb1[1], bomb2[1], bomb3[1], bomb4[1]);
                    tocarBombilla(bomb1[1] + 50, bomb2[1] + 50, 1);
                }
                if (tocada[1])
                {
                    if (!tocada[2]){
                        bombilla.Margin = new Thickness(bomb1[2], bomb2[2], bomb3[2], bomb4[2]);
                        tocarBombilla(bomb1[2] + 50, bomb2[2] + 50, 2);
                    }
                    if (tocada[2]) {
                        if (!tocada[3])
                        {
                            bombilla.Margin = new Thickness(bomb1[3], bomb2[3], bomb3[3], bomb4[3]);
                            tocarBombilla(bomb1[3] + 50, bomb2[3] + 50, 3);
                        }
                        if (tocada[3])
                        {
                            if (!tocada[4])
                            {
                                bombilla.Margin = new Thickness(bomb1[4], bomb2[4], bomb3[4], bomb4[4]);
                                tocarBombilla(bomb1[4] + 50, bomb2[4] + 50, 4);
                            }
                            if (tocada[4])
                            {
                                if (!tocada[5])
                                {
                                    bombilla.Margin = new Thickness(bomb1[5], bomb2[5], bomb3[5], bomb4[5]);
                                    tocarBombilla(bomb1[5] + 50, bomb2[5] + 50, 5);
                                }
                                if (tocada[5])
                                {
                                    if (!tocada[6])
                                    {
                                        bombilla.Margin = new Thickness(bomb1[6], bomb2[6], bomb3[6], bomb4[6]);
                                        tocarBombilla(bomb1[6] + 50, bomb2[6] + 50, 6);
                                    }
                                    if (tocada[6])
                                    {
                                        if (!tocada[7])
                                        {
                                            bombilla.Margin = new Thickness(bomb1[7], bomb2[7], bomb3[7], bomb4[7]);
                                            tocarBombilla(bomb1[7] + 50, bomb2[7] + 50, 7);
                                        }
                                        if (tocada[7])
                                        {
                                            if (!tocada[8])
                                            {
                                                bombilla.Margin = new Thickness(bomb1[8], bomb2[8], bomb3[8], bomb4[8]);
                                                tocarBombilla(bomb1[8] + 50, bomb2[8] + 50, 8);
                                            }
                                            if (tocada[8])
                                            {
                                                bombilla.Visibility = System.Windows.Visibility.Hidden;
                                                dispatcherTimer.Stop();
                                                if (!modoblanco)
                                                {
                                                    refresh.Visibility = System.Windows.Visibility.Visible;
                                                    refreshw.Visibility = System.Windows.Visibility.Hidden;
                                                }
                                                else
                                                {
                                                    refreshw.Visibility = System.Windows.Visibility.Visible;
                                                    refresh.Visibility = System.Windows.Visibility.Hidden;
                                                }

                                                mejorpuntuacion.Visibility = System.Windows.Visibility.Visible;
                                                refreshv.Visibility = System.Windows.Visibility.Hidden;
                                                refreshwv.Visibility = System.Windows.Visibility.Hidden;
                                                tocarBotonMenu(1171+50, 180 + 50, 0, handStateleft, handStateright);  //refresh

                                            }
                                        }
                                    }
                                }
                            }
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
                    /// <summary>
                    /// BodyFrame está disponible.
                    /// </summary>
                    using (frame)
                    {
                        using (DrawingContext dc = this.drawingGroup.Open())
                        {

                            /// <summary>
                            /// Dibujamos un fondo transparente para fijar el tamaño del render.
                            /// </summary>
                            dc.DrawRectangle(Brushes.Transparent, null, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));

                            /// <summary>
                            /// La primera vez que se llama, Kinect alojará cada cuerpo en el array.
                            /// Mientras estos objetos del cuerpo no se desechen o no se pongan a null en el array, se pueden volver a suar 
                            /// </summary>
                            frame.GetAndRefreshBodyData(this.bodies);

                            foreach (Body body in this.bodies)
                            {
                                if (body.IsTracked)
                                {
                                    this.DrawClippedEdges(body, dc);
                                    this.ControlaPosicion();
                                    this.Interfaz(body.HandLeftState,body.HandRightState);

                                    IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                                    /// <summary>
                                    /// Convierte los puntos de union de profuncidad (pantalla) del espacio.
                                    /// </summary>
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

                            /// <summary>
                            /// Impide dibujar fuera del area del render.
                            /// </summary>
                            this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
                            bodyImage = new Image { Source = new DrawingImage(drawingGroup), Width = this.displayWidth, Height = this.displayHeight };
                            rootGrid.Children.Clear();

                            /// <summary>
                            /// Añade el cuerpo a la imagen si el flag está activado.
                            /// </summary>
                            if (skeletonFlag)
                            {
                                rootGrid.Children.Add(bodyImage);
                                rootGrid.Measure(new Size(bodyImage.Width, bodyImage.Height));
                                rootGrid.Arrange(new Rect(0, 0, bodyImage.Width, bodyImage.Height));
                            }


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
                /// <summary>
                /// ignoramos si los frame no esta disponible.
                /// </summary>
            }
        }

        private void DrawBody(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, DrawingContext drawingContext)
        {
            /// <summary>
            /// Dibujamos los huesos.
            /// </summary>

            /// <summary>
            /// Torso.
            /// </summary>
            this.DrawBone(joints, jointPoints, JointType.Head, JointType.Neck, drawingContext);
            cabeza = jointPoints[JointType.Head];
            this.DrawBone(joints, jointPoints, JointType.Neck, JointType.SpineShoulder, drawingContext);
            this.DrawBone(joints, jointPoints, JointType.SpineShoulder, JointType.SpineMid, drawingContext);
            this.DrawBone(joints, jointPoints, JointType.SpineMid, JointType.SpineBase, drawingContext);
            this.DrawBone(joints, jointPoints, JointType.SpineShoulder, JointType.ShoulderRight, drawingContext);
            this.DrawBone(joints, jointPoints, JointType.SpineShoulder, JointType.ShoulderLeft, drawingContext);
            this.DrawBone(joints, jointPoints, JointType.SpineBase, JointType.HipRight, drawingContext);
            this.DrawBone(joints, jointPoints, JointType.SpineBase, JointType.HipLeft, drawingContext);

            /// <summary>
            /// Brazo derecho.
            /// </summary>
            this.DrawBone(joints, jointPoints, JointType.ShoulderRight, JointType.ElbowRight, drawingContext);
            hombroDerecho = jointPoints[JointType.ShoulderRight];
            this.DrawBone(joints, jointPoints, JointType.ElbowRight, JointType.WristRight, drawingContext);
            this.DrawBone(joints, jointPoints, JointType.WristRight, JointType.HandRight, drawingContext);
            this.DrawBone(joints, jointPoints, JointType.HandRight, JointType.HandTipRight, drawingContext);
            manoDerecha = jointPoints[JointType.HandRight];
            this.DrawBone(joints, jointPoints, JointType.WristRight, JointType.ThumbRight, drawingContext);

            /// <summary>
            /// Brazo izquierdo.
            /// </summary>
            this.DrawBone(joints, jointPoints, JointType.ShoulderLeft, JointType.ElbowLeft, drawingContext);
            hombroIzquierdo = jointPoints[JointType.ShoulderLeft];
            this.DrawBone(joints, jointPoints, JointType.ElbowLeft, JointType.WristLeft, drawingContext);
            this.DrawBone(joints, jointPoints, JointType.WristLeft, JointType.HandLeft, drawingContext);
            manoIzquierda = jointPoints[JointType.HandLeft];
            this.DrawBone(joints, jointPoints, JointType.HandLeft, JointType.HandTipLeft, drawingContext);
            this.DrawBone(joints, jointPoints, JointType.WristLeft, JointType.ThumbLeft, drawingContext);

            /// <summary>
            /// Pierna derecha.
            /// </summary>
            this.DrawBone(joints, jointPoints, JointType.HipRight, JointType.KneeRight, drawingContext);
            this.DrawBone(joints, jointPoints, JointType.KneeRight, JointType.AnkleRight, drawingContext);
            this.DrawBone(joints, jointPoints, JointType.AnkleRight, JointType.FootRight, drawingContext);
            pieDerecho = jointPoints[JointType.AnkleRight];

            /// <summary>
            /// Pierna izquierda.
            /// </summary>
            this.DrawBone(joints, jointPoints, JointType.HipLeft, JointType.KneeLeft, drawingContext);
            this.DrawBone(joints, jointPoints, JointType.KneeLeft, JointType.AnkleLeft, drawingContext);
            this.DrawBone(joints, jointPoints, JointType.AnkleLeft, JointType.FootLeft, drawingContext);
            pieIzquierdo = jointPoints[JointType.AnkleLeft];

            /// <summary>
            /// Dibujamos las uniones.
            /// </summary>
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

            /// <summary>
            /// Si no podemos encontrar alguno de las uniones, salimos.
            /// </summary>
            if (joint0.TrackingState == TrackingState.NotTracked ||
                joint1.TrackingState == TrackingState.NotTracked)
            {
                return;
            }

            /// <summary>
            /// No dibujamos si cada punto fue inferido.
            /// </summary>
            if (joint0.TrackingState == TrackingState.Inferred &&
                joint1.TrackingState == TrackingState.Inferred)
            {
                return;
            }

            /// <summary>
            /// Asumimos que todos los huesos han sido inferidos a menos que se rastreen ambas articulaciones.
            /// </summary>
            Pen drawPen = this.inferredBonePen;
            if ((joint0.TrackingState == TrackingState.Tracked) && (joint1.TrackingState == TrackingState.Tracked))
            {
                drawPen = this.trackedBonePen;
            }

            drawingContext.DrawLine(drawPen, jointPoints[jointType0], jointPoints[jointType1]);
        }

        /// <summary>
        /// Dibujamos un simbolo de la mano is se produce: círculo rojo = cerrada, verde = abierta, azul = señalando.
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
        /// Dibujar los indicadores de los bordes si el cuerpo los pisa.
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
