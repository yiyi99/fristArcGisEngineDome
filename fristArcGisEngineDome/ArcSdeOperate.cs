using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    class ArcSdeOperate
    {
        public string Server { get; set; }
        public string Instance { get; set; }
        public string Database { get; set; }
        public string User { get; set; }
        public string PassWord { get; set; }
        public string Version { get; set; }
        public string Authentication_Mode { get; set; }

        public IFeatureLayer LoadFeature()
        {
            IWorkspace workspace = CreateWorkspace();

            if (workspace == null)
            {
                new MessageWindow("连接数据库失败").ShowDialog();
                return new FeatureLayerClass() as IFeatureLayer;
            }

            IEnumDataset enumDataSet = workspace.get_Datasets(esriDatasetType.esriDTAny);
            enumDataSet.Reset();

            IDataset dataSet = enumDataSet.Next();

            while (dataSet != null)
            {
                //判断数据集中的数据是什么类型
                if (dataSet is IFeatureDataset)
                {
                    //如果是FeatureDataSet做以下处理
                    IFeatureWorkspace featureWorkspace = workspace as IFeatureWorkspace;
                    IFeatureDataset featureDataset = featureWorkspace.OpenFeatureDataset(dataSet.Name);
                    IEnumDataset EnumDataset = featureDataset.Subsets;
                    EnumDataset.Reset();
                    IDataset dataset = EnumDataset.Next();

                    if (dataset is IFeatureClass)
                    {
                        IFeatureLayer featureLayer = new FeatureLayerClass();
                        featureLayer.FeatureClass = featureWorkspace.OpenFeatureClass(dataset.Name);
                        featureLayer.Name = featureLayer.FeatureClass.AliasName;
                        return featureLayer;
                    }
                }
                else if (dataSet is IFeatureClass)
                {
                    //如果是FeatureClass做以下处理
                    IFeatureWorkspace featureWorkspace = workspace as IFeatureWorkspace;
                    IFeatureClass feature = featureWorkspace.OpenFeatureClass(dataSet.Name);
                    IFeatureLayer layer = new FeatureLayerClass();
                    layer.FeatureClass = feature;
                    layer.Name = feature.AliasName;
                    return layer;
                }
                dataSet = enumDataSet.Next();
            }

            return new FeatureLayerClass() as IFeatureLayer;
        }

        public string CheckOutConnection()
        {
            IWorkspace workspace = CreateWorkspace();

            return workspace != null ? "连接SDE数据库成功" : "连接SDE数据库失败";
        }

        private IWorkspace CreateWorkspace()
        {
            try
            {
                IWorkspaceFactory workspaceFactory = new SdeWorkspaceFactoryClass();

                IPropertySet propertySet = new PropertySetClass();

                propertySet.SetProperty("SERVER", this.Server);
                propertySet.SetProperty("INSTANCE", this.Instance);
                propertySet.SetProperty("DATABASE", this.Database);
                propertySet.SetProperty("USER", this.User);
                propertySet.SetProperty("PASSWORD", this.PassWord);
                propertySet.SetProperty("VERSION", this.Version);
                propertySet.SetProperty("AUTHENTICATION_MODE", this.Authentication_Mode);

                IWorkspace workspace = workspaceFactory.Open(propertySet, 0);

                return workspace;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.ToString());
            }
            return null;
        }
    }
}