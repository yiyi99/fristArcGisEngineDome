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
using ESRI.ArcGIS.Analyst3D;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.DataSourcesRaster;

namespace fristArcGisEngineDome
{
    /// <summary>
    /// ArcSdeWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ArcSdeWindow : Window
    {
        public delegate void LoadFeatureEventHandler(IFeatureLayer featureLayer);
        public static event LoadFeatureEventHandler LoadSdeFeature;

        public ArcSdeWindow()
        {
            InitializeComponent();
        }

        private void btn_ConnectTest_Click(object sender, RoutedEventArgs e)
        {
            while (txb_ServerURL.Text == string.Empty
                || txb_ServerName.Text == string.Empty
                || txb_DataBaseName.Text == string.Empty
                || txb_UserName.Text == string.Empty
                || txb_PassWord.Password == string.Empty
                || txb_ServerName.Text == string.Empty
                || txb_ServerName.Text == string.Empty)
            {
                new MessageWindow("请填入不为空的合适的值").ShowDialog();
                return;
            }

            ArcSdeOperate arcSdeOperate = new ArcSdeOperate()
            {
                Server = txb_ServerURL.Text,
                Instance = txb_ServerName.Text,
                Database = txb_DataBaseName.Text,
                User = txb_UserName.Text,
                PassWord = txb_PassWord.Password,
                Version = "SDE.DEFAULT",
                Authentication_Mode = "DBMS"
            };

            string strCheckOut = arcSdeOperate.CheckOutConnection();

            new MessageWindow(strCheckOut).ShowDialog();
        }

        private void btn_Confirm_Click(object sender, RoutedEventArgs e)
        {
            while (txb_ServerURL.Text == string.Empty
                || txb_ServerName.Text == string.Empty
                || txb_DataBaseName.Text == string.Empty
                || txb_UserName.Text == string.Empty
                || txb_PassWord.Password == string.Empty
                || txb_ServerName.Text == string.Empty
                || txb_ServerName.Text == string.Empty)
            {
                new MessageWindow("请填入不为空的合适的值").ShowDialog();
                return;
            }

            ArcSdeOperate arcSdeOperate = new ArcSdeOperate()
            {
                Server = txb_ServerURL.Text,
                Instance = txb_ServerName.Text,
                Database = txb_DataBaseName.Text,
                User = txb_UserName.Text,
                PassWord = txb_PassWord.Password,
                Version = "SDE.DEFAULT",
                Authentication_Mode = "DBMS"
            };

            IFeatureLayer featureLayer = arcSdeOperate.LoadFeature();

            if (LoadSdeFeature != null)
            {
                LoadSdeFeature.Invoke(featureLayer);
            }
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
    }
}
