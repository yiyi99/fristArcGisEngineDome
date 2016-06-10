using System;
using System.Collections.Generic;
using System.Data;
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

namespace fristArcGisEngineDome
{
    /// <summary>
    /// AttributeWindow.xaml 的交互逻辑
    /// </summary>
    public partial class AttributeWindow : Window
    {
        private ILayer layer;
        private String queryFilter;

        public AttributeWindow(ILayer layer, string queryFilter)
        {
            InitializeComponent();

            txb_Prompt.Visibility = Visibility.Collapsed;
            datagrid_Attribute.Visibility = Visibility.Visible;

            this.layer = layer;
            this.queryFilter = queryFilter;

            txb_Title.Text = "  图层：" + layer.Name + "     查询字段：" + queryFilter;
        }

        private void AttributeQueryResult_Loaded(object sender, RoutedEventArgs e)
        {
            LoadQueryResult();
        }

        private void Rectangle_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void btn_exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void LoadQueryResult()
        {
            IFeatureLayer featureLayer = this.layer as IFeatureLayer;

            if (featureLayer == null)
            {
                txb_Prompt.Visibility = Visibility.Visible;
                datagrid_Attribute.Visibility = Visibility.Collapsed;
                return;
            }

            IFeatureClass featureClass = featureLayer.FeatureClass;
            ILayerFields layerFields = featureLayer as ILayerFields;

            DataColumn dataColumn = null;
            DataTable dataTable = new DataTable(featureLayer.Name);
            DataTable dataTableResult = new DataTable(featureLayer.Name);

            string classFilter = string.Empty;

            for (int i = 0; i < layerFields.FieldCount; i++)
            {
                dataColumn = new DataColumn(layerFields.get_Field(i).Name);
                dataTable.Columns.Add(dataColumn);
                dataColumn = null;
            }

            IFeature feature = null;
            IQueryFilter queryFilter = new QueryFilterClass();
            IFeatureCursor featureCursor;

            featureCursor = featureClass.Search(null, true);
            feature = featureCursor.NextFeature();

            while (feature != null)
            {
                DataRow dataRow = dataTable.NewRow();

                for (int i = 0; i < layerFields.FieldCount; i++)
                {
                    if (layerFields.FindField(featureClass.ShapeFieldName) == i)
                        dataRow[i] = featureClass.ShapeType.ToString();
                    else
                        dataRow[i] = feature.get_Value(i);
                }
                dataTable.Rows.Add(dataRow);
                feature = featureCursor.NextFeature();
            }

            classFilter = GetColumnCaption(dataTable);

            if (classFilter != string.Empty)
            {
                DataRow[] dataRows = dataTable.Select(classFilter + "=" + "'" + this.queryFilter + "'");

                dataTableResult = dataTable.Clone();

                foreach (var dataRow in dataRows)
                {
                    dataTableResult.ImportRow(dataRow);
                }

                dataTableResult.Columns[classFilter].SetOrdinal(0);

                if (dataTable.Rows.Count > 0)
                {
                    datagrid_Attribute.DataContext = dataTableResult;
                }
                else
                {
                    txb_Prompt.Visibility = Visibility.Visible;
                    datagrid_Attribute.Visibility = Visibility.Collapsed;
                    return;
                }
            }
            else
            {
                if (dataTable.Rows.Count > 0)
                {
                    datagrid_Attribute.DataContext = dataTable;
                }
                else
                {
                    txb_Prompt.Visibility = Visibility.Visible;
                    datagrid_Attribute.Visibility = Visibility.Collapsed;
                    return;
                }
            }
        }

        private string GetColumnCaption(DataTable dataTable)
        {
            string columnCaption = string.Empty;

            foreach (DataRow dataRowTemp in dataTable.Rows)
            {
                foreach (DataColumn dataColumnTemp in dataTable.Columns)
                {
                    if (dataRowTemp[dataColumnTemp.Caption].ToString() == queryFilter)
                    {
                        columnCaption = dataColumnTemp.Caption;
                        return columnCaption;
                    }
                }
            }
            return columnCaption;
        }
    }
}
