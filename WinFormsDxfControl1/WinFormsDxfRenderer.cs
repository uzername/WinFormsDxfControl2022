using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using IxMilia.Dxf;

namespace WinFormsDxfControl1
{
    /// <summary>
    /// here is control that draws DXF file, rotated, mirrored and scaled.
    /// this control has only drawing pane.
    /// </summary>
    public partial class WinFormsDxfRenderer : UserControl
    {
        private DxfFile dxfFile;
        /// <summary>
        /// list of figures to render. The set contains figures already slammed to zero point
        /// also it is already rotated
        /// </summary>
        private List<RenderFigure> AllFigures = new List<RenderFigure>();
        class RenderFigure
        {
            public Color usedColor;
        }
        /// <summary>
        /// position of rendered line
        /// </summary>
        class RenderLine : RenderFigure
        {
            public double StartX;
            public double EndX;
            public double StartY;
            public double EndY;
        }
        /// <summary>
        /// position of rendered arc as required by .NET
        /// </summary>
        class RenderArc : RenderFigure
        {
            /// <summary>
            /// The x-coordinate of the upper-left corner of the rectangle that defines the ellipse.
            /// </summary>
            public double x;
            /// <summary>
            /// The y-coordinate of the upper-left corner of the rectangle that defines the ellipse.
            /// </summary>
            public double y;
            /// <summary>
            /// Width of the rectangle that defines the ellipse.
            /// </summary>
            public double width;
            /// <summary>
            /// Height of the rectangle that defines the ellipse.
            /// </summary>
            public double height;
            /// <summary>
            /// Angle in degrees measured clockwise from the x-axis to the starting point of the arc.
            /// </summary>
            public double startAngle;
            /// <summary>
            /// Angle in degrees measured clockwise from the startAngle parameter to ending point of the arc.
            /// </summary>
            public double sweepAngle;
        }

        public WinFormsDxfRenderer()
        {
            InitializeComponent();
        }
        /// <summary>
        /// entry point to control. Here it begins.
        /// </summary>
        /// <param name="reparseRequired">read dxf file from scratch using ixmilia</param>
        /// <param name="inPathToFile">path to file</param>
        /// <param name="rotationAngleDeg">angle in degrees for rotation</param>
        /// <param name="mirror">do we apply mirror before rotation?</param>
        public void processDXFfileNow(bool reparseRequired, string inPathToFile, double rotationAngleDeg, bool mirror)
        {
            if (reparseRequired)
            {
                dxfFile = DxfFile.Load(inPathToFile);
            }
        }
        private void WinFormsDxfRenderer_Paint(object sender, PaintEventArgs e)
        {
            
            
        }
    }
}
