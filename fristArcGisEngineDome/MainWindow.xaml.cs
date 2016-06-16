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
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Drawing;
using System.Data;

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



namespace fristArcGisEngineDome
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private IMapDocument mapDocument;

        private AxTOCControl tocControl;
        private AxMapControl mapControl;
        private SplitContainer splitControl;
        private SplitContainer splitControlEagleEyed;

        private AxMapControl mapControlEagleEyed;

        Dictionary<string, ESRI.ArcGIS.SystemUI.ICommand> toolBarCommand;

        private System.Windows.Forms.ContextMenuStrip tocContextMenuStrip;

        private string commandSelection = string.Empty;
        private string drawSelection = string.Empty;
        private string calculateSelection = string.Empty;

        private ILayer passLayer;
        private String queryFilter;

        private IFeatureCursor featureCursorBuffer;

        public MainWindow()
        {
            //ESRI.ArcGIS.RuntimeManager.Bind(ESRI.ArcGIS.ProductCode.EngineOrDesktop);
            //IAoInitialize aoInitialize = new AoInitialize();
            //esriLicenseStatus licenseStatus = esriLicenseStatus.esriLicenseUnavailable;
            //licenseStatus = aoInitialize.Initialize(esriLicenseProductCode.esriLicenseProductCodeAdvanced);

            InitializeComponent();

            //GainAuthorize();
        }

        //private void GainAuthorize()
        //{
        //    if (!ESRI.ArcGIS.RuntimeManager.Bind(ESRI.ArcGIS.ProductCode.Engine))
        //    {
        //        if (!ESRI.ArcGIS.RuntimeManager.Bind(ESRI.ArcGIS.ProductCode.Desktop))
        //        {
        //            new MessageWindow("没有找到ArcGIS运行环境，程序将关闭").ShowDialog();
        //            return;
        //        }
        //    }

        //    IAoInitialize pAOinit = new AoInitializeClass();
        //    esriLicenseStatus licenseStatus = pAOinit.IsProductCodeAvailable(esriLicenseProductCode.esriLicenseProductCodeEngine);
        //}

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 获取窗体句柄
            IntPtr windowsHandle = new System.Windows.Interop.WindowInteropHelper(this).Handle;

            // 获得窗体的样式
            int oldStyle = WindowsFrame.GetWindowLong(windowsHandle, WindowsFrame.GWL_STYLE);

            // 更改窗体的样式为无边框窗体
            WindowsFrame.SetWindowLong(windowsHandle, WindowsFrame.GWL_STYLE, oldStyle & ~WindowsFrame.WS_CAPTION);

            PrepareEnvironment();
        }

        private void removeLayerItem_Click(object sender, EventArgs e)
        {
            new MessageWindow("移除所选被按下").ShowDialog();
        }

        private void openAttributeItem_Click(object sender, EventArgs e)
        {
            if (passLayer == null || queryFilter == null) return;

            AttributeWindow attributeWindow = new AttributeWindow(passLayer, queryFilter);
            attributeWindow.Show();
        }

        private void tocControl_OnMouseUp(object sender, ITOCControlEvents_OnMouseUpEvent e)
        {
            if (mapControl.LayerCount == 0) return;

            ILayer layer = new FeatureLayerClass();
            IBasicMap basicMap = new MapClass();
            object legend = new object();
            object index = new object();
            esriTOCControlItem tocItem = new esriTOCControlItem();

            tocControl.HitTest(e.x, e.y, ref tocItem, ref basicMap, ref layer, ref legend, ref index);
            if (e.button == 2)
            {
                if (tocItem == esriTOCControlItem.esriTOCControlItemLegendClass)
                {
                    ILegendGroup legendGroup = legend as ILegendGroup;
                    ILegendClass legendClass = legendGroup.get_Class(Convert.ToInt32(index.ToString()));

                    passLayer = layer;
                    queryFilter = legendClass.Label.ToString();

                    CreatetocContextMenuStrip();
                    tocContextMenuStrip.Show(tocControl, new System.Drawing.Point(e.x, e.y));
                }

                //else if (tocItem == esriTOCControlItem.esriTOCControlItemLayer)
                //{
                //    passLayer = layer;

                //    CreatetocContextMenuStrip();
                //    tocContextMenuStrip.Show(tocControl, new System.Drawing.Point(e.x, e.y));
                //}
            }
        }

        private void mapControlEagleEyed_OnMouseMove(object sender, IMapControlEvents2_OnMouseMoveEvent e)
        {
            if (e.button != 1)
            {
                return;
            }
            IPoint point = new PointClass();
            point.PutCoords(e.mapX, e.mapY);
            mapControl.CenterAt(point);
            mapControl.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGeography, null, null);
        }

        private void mapControlEagleEyed_OnMouseDown(object sender, IMapControlEvents2_OnMouseDownEvent e)
        {
            if (e.button == 1)
            {
                IPoint point = new PointClass();
                point.PutCoords(e.mapX, e.mapY);
                IEnvelope envelope = mapControl.Extent;
                envelope.CenterAt(point);
                mapControl.Extent = envelope;
                mapControl.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGeography, null, null);
            }

            else if (e.button == 2)
            {
                IEnvelope envelope = mapControlEagleEyed.TrackRectangle();
                mapControl.Extent = envelope;
                mapControl.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGeography, null, null);
            }
        }

        private void mapControl_OnExtentUpdated(object sender, IMapControlEvents2_OnExtentUpdatedEvent e)
        {
            if (mapControlEagleEyed == null) return;

            IEnvelope envelope = (IEnvelope)e.newEnvelope;
            IGraphicsContainer graphicsContainer = mapControlEagleEyed.Map as IGraphicsContainer;
            IActiveView activeView = graphicsContainer as IActiveView;

            graphicsContainer.DeleteAllElements();
            IRectangleElement rectangleElement = new RectangleElementClass();
            IElement element = rectangleElement as IElement;
            element.Geometry = envelope;

            IRgbColor rgbColor = new RgbColorClass();
            rgbColor.Red = 223;
            rgbColor.Green = 125;
            rgbColor.Blue = 127;
            rgbColor.Transparency = 255;

            ILineSymbol lineSymbol = new SimpleLineSymbolClass();
            lineSymbol.Width = 2;
            lineSymbol.Color = rgbColor;

            rgbColor = new RgbColorClass();
            rgbColor.Red = 223;
            rgbColor.Green = 125;
            rgbColor.Blue = 127;
            rgbColor.Transparency = 0;

            IFillSymbol fillSymbol = new SimpleFillSymbolClass();
            fillSymbol.Color = rgbColor;
            fillSymbol.Outline = lineSymbol;

            IFillShapeElement fillShapeElement = element as IFillShapeElement;
            fillShapeElement.Symbol = fillSymbol;

            graphicsContainer.AddElement((IElement)fillShapeElement, 0);

            activeView.PartialRefresh(esriViewDrawPhase.esriViewGeography, null, null);
        }

        private void mapControl_OnMapReplaced(object sender, IMapControlEvents2_OnMapReplacedEvent e)
        {
            if (mapControlEagleEyed == null) return;

            mapControlEagleEyed.Map = new MapClass();
            IEnumLayer enumLayer = mapControl.Map.Layers;
            enumLayer.Reset();
            ILayer layer = enumLayer.Next();

            while (layer != null)
            {
                mapControlEagleEyed.Map.AddLayer(layer);
                layer = enumLayer.Next();
            }

            mapControlEagleEyed.Extent = mapControl.FullExtent;
            mapControlEagleEyed.Refresh();
        }

        private void mapControl_OnMouseMove(object sender, IMapControlEvents2_OnMouseMoveEvent e)
        {
            txb_StatusBar.Text = " X,Y 坐标 : " + string.Format("{0}, {1}  {2}", e.mapX.ToString("#######.##"), e.mapY.ToString("#######.##"), mapControl.MapUnits.ToString().Substring(4));
        }

        private void mapControl_OnMouseDown(object sender, IMapControlEvents2_OnMouseDownEvent e)
        {
            if (mapControl.LayerCount == 0) return;

            if (e.button == 1)
            {
                if (commandSelection != string.Empty)
                {
                    IEnvelope envelope = null;

                    switch (commandSelection)
                    {
                        case "放大":
                            {
                                envelope = mapControl.TrackRectangle();
                                mapControl.Extent = envelope;
                                break;
                            }
                        case "缩小":
                            {
                                envelope = mapControl.Extent;
                                envelope.Expand(2, 2, true);
                                mapControl.Extent = envelope;
                                break;
                            }
                        case "漫游":
                            {
                                envelope = mapControl.Extent;
                                mapControl.Pan();
                                break;
                            }
                        default:
                            break;
                    }
                }

                if (drawSelection != string.Empty)
                {
                    try
                    {
                        switch (drawSelection)
                        {
                            case "点绘制":
                                {
                                    IRgbColor rgbColor = new RgbColorClass();
                                    rgbColor.Red = 223;
                                    rgbColor.Green = 125;
                                    rgbColor.Blue = 127;
                                    rgbColor.Transparency = 255;

                                    IPoint point = mapControl.ToMapPoint(e.x, e.y);
                                    IMarkerElement markerElement = new MarkerElementClass();
                                    IMarkerSymbol markerSymbol = new SimpleMarkerSymbolClass();

                                    markerSymbol.Color = rgbColor as IColor;
                                    markerElement.Symbol = markerSymbol;

                                    IElement element = markerElement as IElement;
                                    element.Geometry = point;

                                    IGraphicsContainer graphicsContainer = mapControl.Map as IGraphicsContainer;
                                    graphicsContainer.AddElement((IElement)markerElement, 0);
                                    IActiveView activeView = mapControl.ActiveView;
                                    activeView.Refresh();

                                    break;
                                }
                            case "线绘制":
                                {
                                    IRgbColor rgbColor = new RgbColorClass();
                                    rgbColor.Red = 223;
                                    rgbColor.Green = 125;
                                    rgbColor.Blue = 127;
                                    rgbColor.Transparency = 255;

                                    IGeometry geometry = mapControl.TrackLine();

                                    ILineElement lineElement = new LineElementClass();
                                    ILineSymbol lineSymbol = new SimpleLineSymbolClass();

                                    lineSymbol.Color = rgbColor as IColor;

                                    lineSymbol.Color = rgbColor as IColor;
                                    lineElement.Symbol = lineSymbol;

                                    IElement element = lineElement as IElement;
                                    element.Geometry = geometry;

                                    IGraphicsContainer graphicsContainer = mapControl.Map as IGraphicsContainer;
                                    graphicsContainer.AddElement((IElement)lineElement, 0);
                                    IActiveView activeView = mapControl.ActiveView;
                                    activeView.Refresh();

                                    break;
                                }
                            case "圆绘制":
                                {
                                    IRgbColor rgbColor = new RgbColorClass();
                                    rgbColor.Red = 223;
                                    rgbColor.Green = 125;
                                    rgbColor.Blue = 127;
                                    rgbColor.Transparency = 255;

                                    IGeometry geometry = mapControl.TrackCircle();

                                    ICircleElement circleElement = new CircleElementClass();

                                    IElement element = circleElement as IElement;
                                    element.Geometry = geometry;

                                    IFillShapeElement fillShapeElement = (IFillShapeElement)element;

                                    ILineSymbol lineSymbol = (ILineSymbol)CreateSimpleLineSymbol(rgbColor, 2, esriSimpleLineStyle.esriSLSSolid);

                                    rgbColor.Transparency = 100;

                                    fillShapeElement.Symbol = CreateSimpleFillSymbol(rgbColor, esriSimpleFillStyle.esriSFSSolid, lineSymbol);

                                    IGraphicsContainer graphicsContainer = mapControl.Map as IGraphicsContainer;
                                    graphicsContainer.AddElement((IElement)circleElement, 0);
                                    IActiveView activeView = mapControl.ActiveView;
                                    activeView.Refresh();

                                    break;
                                }
                            case "矩形绘制":
                                {
                                    IRgbColor rgbColor = new RgbColorClass();
                                    rgbColor.Red = 223;
                                    rgbColor.Green = 125;
                                    rgbColor.Blue = 127;
                                    rgbColor.Transparency = 255;

                                    IGeometry geometry = mapControl.TrackRectangle();

                                    IRectangleElement rectangElement = new RectangleElementClass();

                                    IElement element = rectangElement as IElement;
                                    element.Geometry = geometry;

                                    IFillShapeElement fillShapeElement = (IFillShapeElement)element;

                                    ILineSymbol lineSymbol = (ILineSymbol)CreateSimpleLineSymbol(rgbColor, 2, esriSimpleLineStyle.esriSLSSolid);

                                    rgbColor.Transparency = 100;

                                    fillShapeElement.Symbol = CreateSimpleFillSymbol(rgbColor, esriSimpleFillStyle.esriSFSSolid, lineSymbol);

                                    IGraphicsContainer graphicsContainer = mapControl.Map as IGraphicsContainer;
                                    graphicsContainer.AddElement((IElement)rectangElement, 0);
                                    IActiveView activeView = mapControl.ActiveView;
                                    activeView.Refresh();

                                    break;
                                }
                            case "多边形绘制":
                                {
                                    IRgbColor rgbColor = new RgbColorClass();
                                    rgbColor.Red = 223;
                                    rgbColor.Green = 125;
                                    rgbColor.Blue = 127;
                                    rgbColor.Transparency = 255;

                                    IGeometry geometry = mapControl.TrackPolygon();

                                    IPolygonElement polygonElement = new PolygonElementClass();

                                    IElement element = polygonElement as IElement;
                                    element.Geometry = geometry;

                                    IFillShapeElement fillShapeElement = (IFillShapeElement)element;

                                    ILineSymbol lineSymbol = (ILineSymbol)CreateSimpleLineSymbol(rgbColor, 2, esriSimpleLineStyle.esriSLSSolid);

                                    rgbColor.Transparency = 100;

                                    fillShapeElement.Symbol = CreateSimpleFillSymbol(rgbColor, esriSimpleFillStyle.esriSFSSolid, lineSymbol);

                                    IGraphicsContainer graphicsContainer = mapControl.Map as IGraphicsContainer;
                                    graphicsContainer.AddElement((IElement)polygonElement, 0);
                                    IActiveView activeView = mapControl.ActiveView;
                                    activeView.Refresh();

                                    break;
                                }
                            default:
                                break;
                        }
                    }
                    catch
                    {
                        return;
                    }
                }

                if (calculateSelection != string.Empty)
                {
                    try
                    {
                        switch (calculateSelection)
                        {
                            case "距离量算":
                                {
                                    IGeometry geometry = mapControl.TrackLine();
                                    double areaNumer = GetMeasureDistance(geometry);
                                    new MessageWindow("距离量算结果：" + areaNumer.ToString("#.######")).Show();

                                    break;
                                }
                            case "面积量算":
                                {
                                    IGeometry geometry = mapControl.TrackPolygon();
                                    double areaNumer = GetMeasureArea(geometry);
                                    new MessageWindow("面积量算结果：" + areaNumer.ToString("#.######")).Show();

                                    break;
                                }
                            default:
                                break;
                        }
                    }
                    catch
                    {
                        return;
                    }
                }
            }

            else if (e.button == 2)
            {
                commandSelection = string.Empty;
                drawSelection = string.Empty;
                calculateSelection = string.Empty;

                mapControl.CurrentTool = null;

                mapControl.MousePointer = esriControlsMousePointer.esriPointerDefault;
            }
        }

        private void mapControl_OnMouseUp(object sender, IMapControlEvents2_OnMouseUpEvent e)
        {
            if (e.button == 1)
            {
                ITopologicalOperator topologicalOperator;
                IGeometry geometry;
                IFeature feature;
                IFeatureLayer featureLayer;
                ISpatialFilter spatialFilter;
                IPoint pPoint;

                for (int i = 0; i < mapControl.Map.LayerCount; i++)
                {
                    pPoint = new PointClass();
                    pPoint.PutCoords(e.mapX, e.mapY);
                    topologicalOperator = pPoint as ITopologicalOperator;
                    double radius = 2;
                    geometry = topologicalOperator.Buffer(radius);
                    if (geometry == null)
                        continue;
                    mapControl.Map.SelectByShape(geometry, null, true);//第三个参数为是否只选中一个
                    mapControl.Refresh(esriViewDrawPhase.esriViewGeoSelection, null, null); //选中要素高亮显示
                    spatialFilter = new SpatialFilterClass();
                    spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                    spatialFilter.Geometry = geometry;
                    featureLayer = mapControl.Map.get_Layer(i) as IFeatureLayer;
                    featureCursorBuffer = featureLayer.Search(spatialFilter, false);
                    feature = featureCursorBuffer.NextFeature();
                }
            }
            else if (e.button == 2)
            {

            }
        }

        private void Title_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void Title_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ChangeWindowSize();
            }
        }


        private void btn_minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void btn_exit_Click(object sender, RoutedEventArgs e)
        {
            //Environment.exit(0);
            System.Windows.Application.Current.Shutdown();
        }

        private void btn_maximize_Click(object sender, RoutedEventArgs e)
        {
            ChangeWindowSize();
        }

        private void btn_OpenMxd_Click(object sender, RoutedEventArgs e)
        {
            toolBarCommand["OpenDocCommand"].OnClick();
            if (mapControl.LayerCount != 0)
            {
                splitHost.Visibility = Visibility.Visible;
                txb_StatusBar.Visibility = Visibility.Visible;
            }
        }

        private void btn_AddData_Click(object sender, RoutedEventArgs e)
        {
            toolBarCommand["AddDataCommand"].OnClick();
            if (mapControl.LayerCount != 0)
            {
                splitHost.Visibility = Visibility.Visible;
                txb_StatusBar.Visibility = Visibility.Visible;
            }
        }

        private void btn_SavaAs_Click(object sender, RoutedEventArgs e)
        {
            if (mapControl.LayerCount == 0) return;
            toolBarCommand["SaveAsDocCommand"].OnClick();
        }

        private void btn_ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            if (mapControl.LayerCount == 0) return;
            mapControl.CurrentTool = toolBarCommand["MapZoomInTool"] as ESRI.ArcGIS.SystemUI.ITool;
        }

        private void btn_ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            if (mapControl.LayerCount == 0) return;
            mapControl.CurrentTool = toolBarCommand["MapZoomOutTool"] as ESRI.ArcGIS.SystemUI.ITool;
        }

        private void btn_FixedZoomIn_Click(object sender, RoutedEventArgs e)
        {
            if (mapControl.LayerCount == 0) return;
            toolBarCommand["MapZoomInFixedCommand"].OnClick();
        }

        private void btn_FixedZoomOut_Click(object sender, RoutedEventArgs e)
        {
            if (mapControl.LayerCount == 0) return;
            toolBarCommand["MapZoomOutFixedCommand"].OnClick();
        }

        private void btn_Pan_Click(object sender, RoutedEventArgs e)
        {
            if (mapControl.LayerCount == 0) return;
            mapControl.CurrentTool = toolBarCommand["MapPanTool"] as ESRI.ArcGIS.SystemUI.ITool;
        }

        private void btn_FullExtent_Click(object sender, RoutedEventArgs e)
        {
            if (mapControl.LayerCount == 0) return;
            toolBarCommand["MapFullExtentCommand"].OnClick();
        }

        private void btn_Indentify_Click(object sender, RoutedEventArgs e)
        {
            if (mapControl.LayerCount == 0) return;
            mapControl.CurrentTool = toolBarCommand["MapIdentifyTool"] as ESRI.ArcGIS.SystemUI.ITool;
        }


        private void menu_EagleEyed_Click(object sender, RoutedEventArgs e)
        {
            if (mapControl.LayerCount == 0)
            {
                new MessageWindow("请先导入地图").ShowDialog();
                return;
            }

            if (menu_EagleEyed.Header.ToString() == "关闭鹰眼")
            {
                splitControlEagleEyed.Panel1.Controls.Remove(tocControl);
                splitControlEagleEyed.Panel2.Controls.Remove(mapControlEagleEyed);

                splitControl.Panel1.Controls.Remove(splitControlEagleEyed);
                splitControl.Panel1.Controls.Add(tocControl);

                menu_EagleEyed.Header = "鹰眼";
            }

            else
            {
                mapControlEagleEyed = new AxMapControl();
                mapControlEagleEyed.Dock = DockStyle.Fill;

                splitControl.Panel1.Controls.Remove(tocControl);
                splitControl.Panel1.Controls.Add(splitControlEagleEyed);

                splitControlEagleEyed.Panel1.Controls.Add(tocControl);
                splitControlEagleEyed.Panel2.Controls.Add(mapControlEagleEyed);

                if (mapControl.LayerCount != 0)
                {
                    try
                    {
                        for (int i = 1; i <= mapControl.LayerCount; i++)
                        {
                            mapControlEagleEyed.Map.AddLayer(mapControl.get_Layer(mapControl.LayerCount - i));
                        }

                        mapControlEagleEyed.Extent = mapControl.FullExtent;
                        mapControlEagleEyed.Refresh();
                    }
                    catch
                    {
                        new MessageWindow("未知错误，鹰眼图加载失败").ShowDialog();
                    }
                }

                mapControlEagleEyed.OnMouseDown += mapControlEagleEyed_OnMouseDown;
                mapControlEagleEyed.OnMouseMove += mapControlEagleEyed_OnMouseMove;

                menu_EagleEyed.Header = "关闭鹰眼";
            }

        }

        private void menu_OpenMxd_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "打开Mxd文档";
            openFileDialog.Multiselect = false;
            openFileDialog.Filter = "*.mxd|*.mxd";
            openFileDialog.RestoreDirectory = true;
            openFileDialog.CheckFileExists = true;

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    mapDocument = new MapDocumentClass();

                    bool isPassWordProtected = mapDocument.get_IsPasswordProtected(System.IO.Path.GetFullPath(openFileDialog.FileName));

                    if (!isPassWordProtected)
                    {
                        mapDocument.Open(System.IO.Path.GetFullPath(openFileDialog.FileName), string.Empty);
                        mapControl.ClearLayers();

                        for (int i = 0; i < mapDocument.MapCount; i++)
                        {
                            mapControl.Map = mapDocument.get_Map(i);
                        }
                    }

                    else
                    {
                        new MessageWindow("该文档设有密码，请联系文档作者").ShowDialog();
                        return;
                    }

                    mapControl.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGraphics, null, null);
                    mapControl.Extent = mapControl.FullExtent;

                    splitHost.Visibility = Visibility.Visible;
                    txb_StatusBar.Visibility = Visibility.Visible;
                }
                catch
                {
                    new MessageWindow("打开" + System.IO.Path.GetFileName(openFileDialog.FileName) + "失败").ShowDialog();
                    return;
                }
            }
        }

        private void menu_SavaMxd_Click(object sender, RoutedEventArgs e)
        {
            if (mapControl.LayerCount == 0) return;

            if (mapDocument.get_IsReadOnly(mapDocument.DocumentFilename))
            {
                new MessageWindow("地图文档只读，不能保存").ShowDialog();
                return;
            }
            else
            {
                try
                {
                    mapDocument.Save(mapDocument.UsesRelativePaths, true);
                    new MessageWindow("地图文档保存成功").ShowDialog();
                }
                catch
                {
                    new MessageWindow("出现未知错误，文档保存失败").ShowDialog();
                    return;
                }
            }
        }

        private void menu_SavaAs_Click(object sender, RoutedEventArgs e)
        {
            if (mapControl.LayerCount == 0) return;

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "另存为";
            saveFileDialog.Filter = "地图文档(*.mxd)|*.mxd";
            saveFileDialog.ShowDialog();

            if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    mapDocument.SaveAs(System.IO.Path.GetFullPath(saveFileDialog.FileName), true, true);
                    new MessageWindow("地图文档保存成功").ShowDialog();
                }
                catch
                {
                    new MessageWindow("出现未知错误，文档保存失败").ShowDialog();
                    return;
                }
            }
        }

        private void menu_AddRaster_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "添加栅格数据";
            openFileDialog.Multiselect = false;
            openFileDialog.Filter = "栅格数据(*.jpg,*.bmp,*.tif,*.img,*png)|*.jpg;*.bmp;*.tif;*.img;*.png";
            openFileDialog.RestoreDirectory = true;
            openFileDialog.CheckFileExists = true;

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    //IRasterLayer rasterLayer = new RasterLayerClass();
                    //rasterLayer.CreateFromFilePath(openFileDialog.FileName);
                    //mapControl.AddLayer(rasterLayer);

                    IWorkspaceFactory rasterWorkspaceFactory = new RasterWorkspaceFactoryClass();
                    IRasterWorkspace rasterWorkspace = rasterWorkspaceFactory.OpenFromFile(System.IO.Path.GetDirectoryName(openFileDialog.FileName), 0) as IRasterWorkspace;
                    IRasterDataset rasterDataset = rasterWorkspace.OpenRasterDataset(System.IO.Path.GetFileName(openFileDialog.FileName));
                    IRasterLayer rasterLayer = new RasterLayerClass();
                    rasterLayer.CreateFromRaster(rasterDataset.CreateDefaultRaster());
                    mapControl.AddLayer(rasterLayer);
                    mapControl.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGraphics, null, null);
                    mapControl.Extent = mapControl.FullExtent;

                    splitHost.Visibility = Visibility.Visible;
                    txb_StatusBar.Visibility = Visibility.Visible;
                }
                catch
                {
                    new MessageWindow("打开" + System.IO.Path.GetFileName(openFileDialog.FileName) + "失败").ShowDialog();
                }
            }
        }

        private void menu_AddShp_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "添加Shape数据";
            openFileDialog.Multiselect = false;
            openFileDialog.Filter = "Shape|*.shp";
            openFileDialog.RestoreDirectory = true;
            openFileDialog.CheckFileExists = true;

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                for (int i = 0; i < openFileDialog.FileNames.Length; i++)
                {
                    try
                    {
                        //string path = System.IO.Path.GetDirectoryName(openFileDialog.FileNames[i]);
                        //string name = System.IO.Path.GetFileName(openFileDialog.FileNames[i]);
                        //mapControl.AddShapeFile(path, name);

                        IWorkspaceFactory shapeWorkspaceFactory = new ShapefileWorkspaceFactoryClass();
                        IFeatureWorkspace shapeFeatureWorkspace = shapeWorkspaceFactory.OpenFromFile(System.IO.Path.GetDirectoryName(openFileDialog.FileNames[i]), 0) as IFeatureWorkspace;
                        IFeatureClass featureClass = shapeFeatureWorkspace.OpenFeatureClass(System.IO.Path.GetFileName(openFileDialog.FileNames[i]));
                        IFeatureLayer featureLayer = new FeatureLayerClass();
                        featureLayer.Name = featureClass.AliasName;
                        featureLayer.FeatureClass = featureClass;

                        mapControl.Map.AddLayer(featureLayer);
                        mapControl.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGraphics, null, null);
                        mapControl.Extent = mapControl.FullExtent;

                        splitHost.Visibility = Visibility.Visible;
                        txb_StatusBar.Visibility = Visibility.Visible;
                    }
                    catch
                    {
                        new MessageWindow("打开" + System.IO.Path.GetFileName(openFileDialog.FileName) + "失败").ShowDialog();
                    }
                }
            }
        }

        private void menu_AddMdb_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "添加个人数据库文件";
            openFileDialog.Multiselect = false;
            openFileDialog.Filter = "个人数据库文件(*.mdb)|*.mdb";
            openFileDialog.RestoreDirectory = true;
            openFileDialog.CheckFileExists = true;

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    //string name = openFileDialog.FileName;
                    //IWorkspaceFactory workspaceFactory = new AccessWorkspaceFactoryClass();
                    //IWorkspace workspace = workspaceFactory.OpenFromFile(name, 0);

                    IWorkspaceFactory accessWorkspaceFactory;
                    IFeatureWorkspace featureWorkspace;
                    IFeatureLayer featureLayer;
                    IFeatureDataset featureDataset;
                    accessWorkspaceFactory = new AccessWorkspaceFactoryClass();

                    IWorkspace workspace = accessWorkspaceFactory.OpenFromFile(System.IO.Path.GetFullPath(openFileDialog.FileName), 0) as IWorkspace;
                    IEnumDataset enumDataset = workspace.get_Datasets(ESRI.ArcGIS.Geodatabase.esriDatasetType.esriDTAny);
                    enumDataset.Reset();
                    IDataset dataset = enumDataset.Next();

                    if (dataset is IFeatureDataset)
                    {
                        featureWorkspace = accessWorkspaceFactory.OpenFromFile(System.IO.Path.GetDirectoryName(openFileDialog.FileName), 0) as IFeatureWorkspace;
                        featureDataset = featureWorkspace.OpenFeatureDataset(dataset.Name);
                        IEnumDataset enumDatasetItem = featureDataset.Subsets;
                        enumDatasetItem.Reset();
                        IDataset datasetItem = enumDatasetItem.Next();

                        if (datasetItem is IFeatureClass)
                        {
                            featureLayer = new FeatureLayerClass();
                            featureLayer.FeatureClass = featureWorkspace.OpenFeatureClass(datasetItem.Name);
                            featureLayer.Name = featureLayer.FeatureClass.AliasName;
                            mapControl.Map.AddLayer(featureLayer);
                            mapControl.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGraphics, null, null);
                            mapControl.Extent = mapControl.FullExtent;
                        }
                    }
                    else
                    {
                        featureWorkspace = (IFeatureWorkspace)workspace;
                        featureLayer = new FeatureLayerClass();
                        featureLayer.FeatureClass = featureWorkspace.OpenFeatureClass(dataset.Name);
                        featureLayer.Name = featureLayer.FeatureClass.AliasName;
                        mapControl.Map.AddLayer(featureLayer);
                        mapControl.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGraphics, null, null);
                        mapControl.Extent = mapControl.FullExtent;
                    }
                    splitHost.Visibility = Visibility.Visible;
                    txb_StatusBar.Visibility = Visibility.Visible;
                }
                catch (Exception ex)
                {
                    new MessageWindow("打开" + System.IO.Path.GetFileName(openFileDialog.FileName) + "失败").ShowDialog();
                    System.Windows.MessageBox.Show(ex.ToString());
                }
            }
        }

        private void menu_ConnectArcSde_Click(object sender, RoutedEventArgs e)
        {
            ArcSdeWindow.LoadSdeFeature += this.LoadSdeFeature;

            ArcSdeWindow arcSdeWindow = new ArcSdeWindow();
            arcSdeWindow.Show();
        }

        private void menu_CloseWorkspace_Click(object sender, RoutedEventArgs e)
        {
            if (mapControl.LayerCount == 0) return;

            PrepareEnvironment();

            if (menu_EagleEyed.Header.ToString() == "关闭鹰眼")
            {
                //mapControlEagleEyed.ClearLayers();

                //splitControlEagleEyed.Panel1.Controls.Remove(tocControl);
                //splitControlEagleEyed.Panel2.Controls.Remove(mapControlEagleEyed);

                //splitControl.Panel1.Controls.Remove(splitControlEagleEyed);
                //splitControl.Panel1.Controls.Add(tocControl);

                menu_EagleEyed.Header = "鹰眼";
            }
        }

        private void menu_Command_Click(object sender, RoutedEventArgs e)
        {
            if (mapControl.LayerCount == 0) return;

            commandSelection = string.Empty;
            drawSelection = string.Empty;
            calculateSelection = string.Empty;
            mapControl.CurrentTool = null;
            mapControl.MousePointer = esriControlsMousePointer.esriPointerDefault;

            System.Windows.Controls.MenuItem menuItem = sender as System.Windows.Controls.MenuItem;

            switch (menuItem.Header.ToString())
            {
                case "放大":
                    {
                        mapControl.MousePointer = esriControlsMousePointer.esriPointerPageZoomIn;
                        commandSelection = "放大";
                        break;
                    }
                case "缩小":
                    {
                        mapControl.MousePointer = esriControlsMousePointer.esriPointerPageZoomOut;
                        commandSelection = "缩小";
                        break;
                    }
                case "漫游":
                    {
                        mapControl.MousePointer = esriControlsMousePointer.esriPointerPan;
                        commandSelection = "漫游";
                        break;
                    }
                default:
                    break;
            }
        }

        private void menu_Extent_Click(object sender, RoutedEventArgs e)
        {
            if (mapControl.LayerCount == 0) return;
            mapControl.Extent = mapControl.FullExtent;
        }

        private void menu_DrawFeature_Click(object sender, RoutedEventArgs e)
        {
            if (mapControl.LayerCount == 0) return;

            System.Windows.Controls.MenuItem menuItem = sender as System.Windows.Controls.MenuItem;

            mapControl.MousePointer = esriControlsMousePointer.esriPointerCrosshair;

            switch (menuItem.Header.ToString())
            {
                case "点绘制":
                    {
                        drawSelection = "点绘制";
                        break;
                    }
                case "线绘制":
                    {
                        drawSelection = "线绘制";
                        break;
                    }
                case "圆绘制":
                    {
                        drawSelection = "圆绘制";
                        break;
                    }
                case "矩形绘制":
                    {
                        drawSelection = "矩形绘制";
                        break;
                    }
                case "多边形绘制":
                    {
                        drawSelection = "多边形绘制";
                        break;
                    }
                default:
                    break;
            }
        }

        private void menu_Calculate_Click(object sender, RoutedEventArgs e)
        {
            if (mapControl.LayerCount == 0) return;

            System.Windows.Controls.MenuItem menuItem = sender as System.Windows.Controls.MenuItem;

            mapControl.MousePointer = esriControlsMousePointer.esriPointerCrosshair;

            switch (menuItem.Header.ToString())
            {
                case "距离量算":
                    {
                        calculateSelection = "距离量算";
                        break;
                    }
                case "面积量算":
                    {
                        calculateSelection = "面积量算";
                        break;
                    }
                default:
                    break;
            }
        }

        private void menu_BufferAnalysis_Click(object sender, RoutedEventArgs e)
        {
            BufferWindow bufferWindow = new BufferWindow();
            bufferWindow.SetFeatureLayers = GetFeatureLayers();
            bufferWindow.loadBufferResultEvemtHandle += LoadBufferResult;
            bufferWindow.ShowDialog();
        }

        
        private void PrepareEnvironment()
        {
            splitHost.Visibility = Visibility.Collapsed;
            txb_StatusBar.Visibility = Visibility.Collapsed;

            CreateEngineControls();

            SetControlsProperty();

            CreateCommand();
        }

        private void ChangeWindowSize()
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }

        private void CreateCommand()
        {
            toolBarCommand = new Dictionary<string, ESRI.ArcGIS.SystemUI.ICommand>();

            ESRI.ArcGIS.SystemUI.ICommand command;

            command = new ESRI.ArcGIS.Controls.ControlsOpenDocCommand();
            command.OnCreate(mapControl.Object);
            toolBarCommand.Add("OpenDocCommand", command);

            command = new ESRI.ArcGIS.Controls.ControlsAddDataCommand();
            command.OnCreate(mapControl.Object);
            toolBarCommand.Add("AddDataCommand", command);

            command = new ESRI.ArcGIS.Controls.ControlsEditingSaveCommand();
            command.OnCreate(mapControl.Object);
            toolBarCommand.Add("EditingSaveCommand", command);

            command = new ESRI.ArcGIS.Controls.ControlsSaveAsDocCommand();
            command.OnCreate(mapControl.Object);
            toolBarCommand.Add("SaveAsDocCommand", command);

            command = new ESRI.ArcGIS.Controls.ControlsMapZoomInFixedCommand();
            command.OnCreate(mapControl.Object);
            toolBarCommand.Add("MapZoomInFixedCommand", command);

            command = new ESRI.ArcGIS.Controls.ControlsMapZoomOutFixedCommand();
            command.OnCreate(mapControl.Object);
            toolBarCommand.Add("MapZoomOutFixedCommand", command);

            command = new ESRI.ArcGIS.Controls.ControlsMapZoomInTool();
            command.OnCreate(mapControl.Object);
            toolBarCommand.Add("MapZoomInTool", command);

            command = new ESRI.ArcGIS.Controls.ControlsMapZoomOutTool();
            command.OnCreate(mapControl.Object);
            toolBarCommand.Add("MapZoomOutTool", command);

            command = new ESRI.ArcGIS.Controls.ControlsMapPanTool();
            command.OnCreate(mapControl.Object);
            toolBarCommand.Add("MapPanTool", command);

            command = new ESRI.ArcGIS.Controls.ControlsMapFullExtentCommand();
            command.OnCreate(mapControl.Object);
            toolBarCommand.Add("MapFullExtentCommand", command);

            command = new ESRI.ArcGIS.Controls.ControlsMapIdentifyTool();
            command.OnCreate(mapControl.Object);
            toolBarCommand.Add("MapIdentifyTool", command);
        }

        private void CreatetocContextMenuStrip()
        {
            tocContextMenuStrip = new System.Windows.Forms.ContextMenuStrip();

            System.Windows.Forms.ToolStripItem openAttributeItem = new System.Windows.Forms.ToolStripMenuItem();
            openAttributeItem.Text = "属性查询";
            openAttributeItem.Click += openAttributeItem_Click;

            System.Windows.Forms.ToolStripItem removeLayerItem = new System.Windows.Forms.ToolStripMenuItem();
            removeLayerItem.Text = "移除所选";
            removeLayerItem.Click += removeLayerItem_Click;

            tocContextMenuStrip.Items.Add(openAttributeItem);
            tocContextMenuStrip.Items.Add(removeLayerItem);
        }

        private void CreateEngineControls()
        {
            //toolbarControl = new AxToolbarControl();
            //toolbarControl.Dock = DockStyle.Fill;
            //toolBarHost.Child = toolbarControl;

            tocControl = new AxTOCControl();
            tocControl.Dock = DockStyle.Fill;

            mapControl = new AxMapControl();
            mapControl.Dock = DockStyle.Fill;

            splitControl = new SplitContainer();
            splitControl.Dock = DockStyle.Fill;

            splitControl.Panel1.Controls.Add(tocControl);
            splitControl.Panel2.Controls.Add(mapControl);

            splitHost.Child = splitControl;

            splitControlEagleEyed = new SplitContainer();
            splitControlEagleEyed.Orientation = System.Windows.Forms.Orientation.Horizontal;
            splitControlEagleEyed.Dock = DockStyle.Fill;
        }

        private void SetControlsProperty()
        {
            //toolbarControl.SetBuddyControl(mapControl);
            tocControl.SetBuddyControl(mapControl);
            tocControl.OnMouseUp += tocControl_OnMouseUp;

            //toolbarControl.AddItem("esriControls.ControlsOpenDocCommand");
            //toolbarControl.AddItem("esriControls.ControlsAddDataCommand");
            //toolbarControl.AddItem("esriControls.ControlsSaveAsDocCommand");
            //toolbarControl.AddItem("esriControls.ControlsMapNavigationToolbar");
            //toolbarControl.AddItem("esriControls.ControlsMapIdentifyTool");

            //toolbarControl.BackColor = System.Drawing.Color.FromArgb(255, 245, 245, 245);

            mapControl.OnMapReplaced += mapControl_OnMapReplaced;
            mapControl.OnMouseMove += mapControl_OnMouseMove;
            mapControl.OnExtentUpdated += mapControl_OnExtentUpdated;
            mapControl.OnMouseDown += mapControl_OnMouseDown;
            mapControl.OnMouseUp += mapControl_OnMouseUp;
        }

        private void LoadSdeFeature(IFeatureLayer featureLayer)
        {
            //new MessageWindow("featureLayer已收到").ShowDialog();

            mapControl.Map.AddLayer(featureLayer);
            mapControl.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGraphics, null, null);
            mapControl.Extent = mapControl.FullExtent;

            splitHost.Visibility = Visibility.Visible;
            txb_StatusBar.Visibility = Visibility.Visible;
        }

        private void LoadBufferResult(string filePath, string fileName)
        {
            try
            {
                mapControl.AddShapeFile(filePath, fileName);
                mapControl.MoveLayerTo(mapControl.LayerCount - 1, 0);
            }
            catch
            {
            }
        }

        private double GetMeasureDistance(IGeometry geometry)
        {
            ICurve curve = (ICurve)geometry;
            return Math.Abs(curve.Length);
        }

        private double GetMeasureArea(IGeometry geometry)
        {
            IArea area = (IArea)geometry;
            return Math.Abs(area.Area);
        }

        private List<IFeatureLayer> GetFeatureLayers()
        {
            List<IFeatureLayer> featureLayers = new List<IFeatureLayer>();
            List<ILayer> layers = new List<ILayer>();
            for (int i = 0; i < this.mapControl.ActiveView.FocusMap.LayerCount; i++)
			{
                layers.Add(this.mapControl.ActiveView.FocusMap.get_Layer(i));
			}
            foreach (ILayer item in layers)
            {
                IFeatureLayer featureLayer = item as IFeatureLayer;
                featureLayers.Add(featureLayer);
            }
            return featureLayers;
        }

        private ISimpleFillSymbol CreateSimpleFillSymbol(IRgbColor rgbColor, esriSimpleFillStyle simpleFillStyle, ILineSymbol lineSymbol)
        {
            ISimpleFillSymbol simpleFillSymbol = new SimpleFillSymbolClass();
            simpleFillSymbol.Style = simpleFillStyle;
            simpleFillSymbol.Color = rgbColor as IColor;
            simpleFillSymbol.Outline = lineSymbol;
            return simpleFillSymbol;
        }

        private ISimpleLineSymbol CreateSimpleLineSymbol(IRgbColor rgbColor, int width, esriSimpleLineStyle simpleLineStyle)
        {
            ISimpleLineSymbol simpleLineSymbol = new SimpleLineSymbolClass();
            simpleLineSymbol.Style = simpleLineStyle;
            simpleLineSymbol.Color = rgbColor as IColor;
            simpleLineSymbol.Width = width;
            return simpleLineSymbol;
        }
    }
}
