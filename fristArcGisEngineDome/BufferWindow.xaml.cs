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
using System.Windows.Shapes;

using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.GlobeCore;
using ESRI.ArcGIS.Output;
using ESRI.ArcGIS.SystemUI;
using ESRI.ArcGIS.Analyst3D;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.DataSourcesRaster;
using ESRI.ArcGIS.Geoprocessor;
using ESRI.ArcGIS.AnalysisTools;
using ESRI.ArcGIS.Geoprocessing;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace fristArcGisEngineDome
{
    /// <summary>
    /// BufferWindow.xaml 的交互逻辑
    /// </summary>
    public partial class BufferWindow : Window
    {
        List<IFeatureLayer> featureLayers = new List<IFeatureLayer>();

        public List<IFeatureLayer> SetFeatureLayers
        {
            set { this.featureLayers = (List<IFeatureLayer>)value; }
            get { return this.featureLayers; }
        }

        public delegate void LoadBufferResult(string filePath, string fileName);
        public event LoadBufferResult loadBufferResultEvemtHandle;

        private string comboBoxSelect;
        private string savaFilePath;

        public BufferWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            System.Collections.ObjectModel.ObservableCollection<FeatureLayerName> FeatureLayerNames = new ObservableCollection<FeatureLayerName>();

            for (int i = 0; i < featureLayers.Count; i++)
            {
                FeatureLayerNames.Add(new FeatureLayerName(i, featureLayers[i].Name.ToString()));
            }

            this.cmb_Layer.ItemsSource = FeatureLayerNames;
            this.cmb_Layer.DisplayMemberPath = "Name";
        }

        private void btn_minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void btn_exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Title_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void cmb_Layer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmb_Layer.SelectedItem == null) return;

            var ComboBox = (ComboBox)sender;
            var item = (FeatureLayerName)ComboBox.SelectedItem;

            comboBoxSelect = item.Name;
        }

        private void btn_GetPath_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();

            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.txb_SavaPath.Text = folderBrowserDialog.SelectedPath + @"\" + comboBoxSelect + "_buffer.shp";
                savaFilePath = folderBrowserDialog.SelectedPath;
            }
        }

        private void btn_Confirm_Click(object sender, RoutedEventArgs e)
        {
            Mouse.SetCursor(Cursors.Wait);

            double bufferDistance;
            double.TryParse(txb_BufferDistance.Text, out bufferDistance);

            if (0.0 == bufferDistance)
            {
                new MessageWindow("缓冲距离不可为0").Show();
                return;
            }
            if (featureLayers == null || featureLayers.Count == 0)
            {
                new MessageWindow("没有图层").Show();
                return;
            }

            IFeatureLayer featureLayer = GetFeatureLayer(comboBoxSelect);

            if (featureLayer == null)
            {
                return;
            }

            Geoprocessor geoprocessor = new Geoprocessor();

            geoprocessor.OverwriteOutput = true;

            ESRI.ArcGIS.AnalysisTools.Buffer buffer = new ESRI.ArcGIS.AnalysisTools.Buffer(featureLayer, this.txb_SavaPath.Text, Convert.ToString(bufferDistance + " Meters"));

            buffer.dissolve_option = "ALL";        //相交部分不会融合
            buffer.line_side = "FULL";             //默认是"FULL"
            buffer.line_end_type = "ROUND";        //默认是"ROUND"
            IGeoProcessorResult geoProcessorResult = null;

            try
            {
                geoProcessorResult = (IGeoProcessorResult)geoprocessor.Execute(buffer, null);

                loadBufferResultEvemtHandle.Invoke(savaFilePath,comboBoxSelect + "_buffer.shp");

                new MessageWindow("缓冲区建立成功").Show();

                Mouse.SetCursor(Cursors.Arrow);
            }
            catch
            {
                new MessageWindow("缓冲区建立失败").Show();

                Mouse.SetCursor(Cursors.Arrow);
            }
        }

        private IFeatureLayer GetFeatureLayer(string selectedItem)
        {
            IFeatureLayer featureLayer = null;

            foreach (IFeatureLayer item in featureLayers)
            {
                if (item.Name == selectedItem)
                {
                    featureLayer = item;
                    break;
                }
            }
            return featureLayer;
        }
    }

    class FeatureLayerName : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string name;

        private int id;

        public FeatureLayerName(int id, string name)
        {
            this.Name = name;

            this.Id = id;
        }

        public string Name
        {
            get { return this.name; }

            set
            {
                this.name = value;

                OnPropertyChanged("Name");
            }
        }

        public int Id
        {
            get { return this.id; }

            set
            {
                this.id = value;

                OnPropertyChanged("Id");
            }
        }

        public void OnPropertyChanged(string propName)
        {
            if (this.PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }
    }
}
