using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fristArcGisEngineDome
{
    class WindowsFrame
    {
        /// <summary>
        /// 带有外边框和标题的windows的样式
        /// </summary>
        public const int WS_CAPTION = 0X00C0000;

        /// <summary>
        /// window的基本样式
        /// </summary>
        public const int GWL_STYLE = -16;

        /// <summary>
        /// 设置窗体的样式
        /// </summary>
        /// <param name="handle">操作窗体的句柄</param>
        /// <param name="oldStyle">进行设置窗体的样式类型.</param>
        /// <param name="newStyle">新样式</param>
        [System.Runtime.InteropServices.DllImport("User32.dll")]
        public static extern void SetWindowLong(IntPtr handle, int oldStyle, int newStyle);

        /// <summary>
        /// 获取窗体指定的样式.
        /// </summary>
        /// <param name="handle">操作窗体的句柄</param>
        /// <param name="style">要进行返回的样式</param>
        /// <returns>当前window的样式</returns>
        [System.Runtime.InteropServices.DllImport("User32.dll")]
        public static extern int GetWindowLong(IntPtr handle, int style);
    }
}
