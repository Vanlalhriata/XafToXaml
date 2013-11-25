using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using System.Xml.Linq;

namespace XafToXaml
{
    class MainViewModel : INotifyPropertyChanged
    {
        #region Fields

        private double frameRate;
        private double ticksPerFrame;

        private Point3D lastPosition;
        private Vector3D lastLook;

        private static Matrix3D RotateMinus90x = new Matrix3D(1, 0, 0, 0,
                                                              0, 0, -1, 0,
                                                              0, 1, 0, 0,
                                                              0, 0, 0, 1);

        private static Matrix3D Rotate90x = new Matrix3D(1, 0, 0, 0,
                                                         0, 0, 1, 0,
                                                         0, -1, 0, 0,
                                                         0, 0, 0, 1);

        #endregion Fields

        #region Properties

        #region FilePath

        private string _FilePath = String.Empty;

        /// <summary>
        /// Gets or sets the FilePath property. This observable property 
        /// indicates the path to the .xaf file.
        /// </summary>
        public string FilePath
        {
            get { return _FilePath; }
            set
            {
                if (_FilePath != value)
                {
                    _FilePath = value;
                    RaisePropertyChanged();
                }
            }
        }

        #endregion

        #region PositionText

        private string _PositionText = String.Empty;

        /// <summary>
        /// Gets or sets the PositionText property. This observable property 
        /// indicates the Position animation XAML.
        /// </summary>
        public string PositionText
        {
            get { return _PositionText; }
            set
            {
                if (_PositionText != value)
                {
                    _PositionText = value;
                    RaisePropertyChanged();
                }
            }
        }

        #endregion

        #region LookDirectionText

        private string _LookDirectionText = String.Empty;

        /// <summary>
        /// Gets or sets the LookDirectionText property. This observable property 
        /// indicates the LookDirection animation XAML.
        /// </summary>
        public string LookDirectionText
        {
            get { return _LookDirectionText; }
            set
            {
                if (_LookDirectionText != value)
                {
                    _LookDirectionText = value;
                    RaisePropertyChanged();
                }
            }
        }

        #endregion

        #endregion Properties

        #region Commands

        #region Browse
        
        private ICommand _BrowseCommand;
        public ICommand BrowseCommand
        {
            get
            {
                if (_BrowseCommand == null)
                {
                    _BrowseCommand = new RelayCommand<object>(Browse_Executed);
                }
                return _BrowseCommand;
            }
        }

        private void Browse_Executed(object parameter)
        {
            Browse();
        }
        
        #endregion
        
        #region Load
        
        private ICommand _LoadCommand;
        public ICommand LoadCommand
        {
            get
            {
                if (_LoadCommand == null)
                {
                    _LoadCommand = new RelayCommand<object>(Load_Executed);
                }
                return _LoadCommand;
            }
        }

        private void Load_Executed(object parameter)
        {
            Load();
        }
        
        #endregion
        
        #endregion Commands

        #region Methods

        private void Browse()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = ".xaf files|*.xaf|All files|*.*";

            Nullable<bool> result = dialog.ShowDialog();
            if (true == result)
                FilePath = dialog.FileName;
        }

        private void Load()
        {
            XDocument doc = LoadXml();
            if (null == doc)
                return;

            XElement samples = GetSamples(doc);
            if (null == samples)
                return;

            frameRate = GetSceneInfo(doc, "frameRate");
            ticksPerFrame = GetSceneInfo(doc, "ticksPerFrame");
            if (frameRate < 1 || ticksPerFrame < 1)
                return;

            PositionText = "<Point3DAnimationUsingKeyFrames Storyboard.TargetProperty=\"(ProjectionCamera.Position)\" Storyboard.TargetName=\"camera\">\n";
            LookDirectionText = "<Vector3DAnimationUsingKeyFrames Storyboard.TargetProperty=\"(ProjectionCamera.LookDirection)\" Storyboard.TargetName=\"camera\">\n";

            lastPosition = new Point3D();
            lastLook = new Vector3D();

            foreach (XElement s in samples.Descendants())
            {
                TimeSpan time = GetTimeSpan(s);

                double[] v = s.Attribute("v").Value.Split(' ').Select(x => Double.Parse(x)).ToArray();
                Matrix3D vMatrix = GetMatrixFrom(v);

                Point3D position = GetPosition(vMatrix);
                if (position != lastPosition)
                    PositionText += GetXaml(typeof(EasingPoint3DKeyFrame).Name, time, position.ToString());
                else
                    PositionText += "\n";

                Vector3D lookDirection = GetLook(vMatrix);
                if (lookDirection != lastLook)
                    LookDirectionText += GetXaml(typeof(EasingVector3DKeyFrame).Name, time, lookDirection.ToString());
                else
                    LookDirectionText += "\n";

                lastPosition = position;
                lastLook = lookDirection;
            }

            PositionText += "</Point3DAnimationUsingKeyFrames>";
            LookDirectionText += "</Vector3DAnimationUsingKeyFrames>";
        }

        private XDocument LoadXml()
        {
            XDocument doc = null;

            try
            {
                doc = XDocument.Load(FilePath);
            }
            catch (System.IO.IOException)
            {
                MessageBox.Show("Invalid file path");
            }
            catch (ArgumentException)
            {
                MessageBox.Show("Please enter a file path");
            }

            return doc;
        }

        private static XElement GetSamples(XDocument doc)
        {
            XElement samples = null;

            try
            {
                samples = doc.Element("MaxAnimation").Element("Node").Element("Samples");
            }
            catch
            {
                MessageBox.Show("Invalid file");
            }

            return samples;
        }

        private double GetSceneInfo(XDocument doc, string key)
        {
            double result = -1;

            try
            {
                result = Double.Parse(doc.Element("MaxAnimation").Element("SceneInfo").Attribute(key).Value);
            }
            catch
            {
                MessageBox.Show("Invalid SceneInfo attribute");
            }

            return result;
        }

        private TimeSpan GetTimeSpan(XElement s)
        {
            int ticks = Int32.Parse(s.Attribute("t").Value);
            double frame = ticks / ticksPerFrame;
            double seconds = frame / frameRate;

            return TimeSpan.FromSeconds(seconds);
        }

        private static Matrix3D GetMatrixFrom(double[] v)
        {
            return new Matrix3D(v[0], v[1], v[2], 0,
                                v[3], v[4], v[5], 0,
                                v[6], v[7], v[8], 0,
                                v[9], v[10], v[11], 1);
        }

        private static Point3D GetPosition(Matrix3D input)
        {
            Matrix3D rotateMinus90x = new Matrix3D(1, 0, 0, 0, 0, 0, -1, 0, 0, 1, 0, 0, 0, 0, 0, 1);
            Matrix3D translations = Matrix3D.Multiply(input, rotateMinus90x);

            return new Point3D(translations.OffsetX, translations.OffsetY, translations.OffsetZ);
        }

        private static Vector3D GetLook(Matrix3D input)
        {
            Vector3D xamlZeroLook = new Vector3D(0, -1, 0);
            Vector3D maxZeroLook = Rotate90x.Transform(xamlZeroLook);
            Vector3D maxRotated = input.Transform(maxZeroLook);
            return RotateMinus90x.Transform(maxRotated);
        }

        private string GetXaml(string keyFrameType, TimeSpan time, string value)
        {
            string keyTime = time.ToString(@"hh\:mm\:ss\.fff");
            return "    <" + keyFrameType + " KeyTime=\"" + keyTime + "\" Value=\"" + value + "\" />\n";
        }

        #endregion Methods

        #region INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
        {
            if (null != PropertyChanged)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion INotifyPropertyChanged implementation
    }
}
