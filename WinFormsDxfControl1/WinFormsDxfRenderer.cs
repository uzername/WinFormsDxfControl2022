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
using IxMilia.Dxf.Entities;

namespace WinFormsDxfControl1
{
    /// <summary>
    /// here is control that draws DXF file, rotated, mirrored and scaled.
    /// this control has only drawing pane.
    /// </summary>
    public partial class WinFormsDxfRenderer : UserControl
    {        
        //====== INTERNAL CLASSES ==========
        class BoundBox
        {
            public double upperLeftX = 0;
            public double upperLeftY = 0;
            public double bottomRightX = 0;
            public double bottomRightY = 0;
            public BoundBox()
            {
                upperLeftX = 0; upperLeftY = 0;
                bottomRightX = 0; bottomRightY = 0;
            }
        }
        
        class RenderStruct
        {
            /// <summary>
            /// list of figures to render. it is flipped. And prepared to be rendered. I mean, rotated and scaled to fit window
            /// </summary>
            public List<RenderFigure> AllFigures = new List<RenderFigure>();
            /// <summary>
            /// the raw bound box of entries in file. Should not change often. Parsed upon filling the AllFigures list            
            /// </summary>
            public BoundBox currentRawBoundBox = new BoundBox();
            /// <summary>
            /// Bound Box with rotation applied
            /// </summary>
            public BoundBox currentRotationBoundBox = new BoundBox();
            /// <summary>
            /// current scale factor to fit into window
            /// </summary>
            public double uniformScaleFactor = 1.0;
            public double offsetX = 0.0;
            public double offsetY = 0.0;
        }
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
        //====== INTERNAL variables ==========
        // names of ignored layers. should be lowercase, lol
        private List<String> ignoredLayers = new List<string> { "dxffix", "ext prof" };
        /// <summary>
        /// current entries of dxf file 
        /// </summary>
        private DxfFile dxfFile;
        /// <summary>
        /// ALL render-related data
        /// </summary>
        RenderStruct renderStruct;
        // margins for rendering dxf
        double offsetLeftRight = 1;
        double offsetTopBottom = 1;

        public WinFormsDxfRenderer()
        {
            InitializeComponent();
        }
        private double ConvertDegreesToRadians(double degrees)
        {
            double radians = (Math.PI / 180) * degrees;
            return (radians);
        }

        // I want to align dxf profile somehow to axes. I find out bounding box of dxf file
        // I also use here values for rotation
        // inRotationAngleRad is from 0 to 2PI . It does not matter though
        /* https://stackoverflow.com/questions/2259476/rotating-a-point-about-another-point-2d
         * If you rotate point (px, py) around point (ox, oy) by angle theta you'll get:
            p'x = cos(theta) * (px-ox) - sin(theta) * (py-oy) + ox
            p'y = sin(theta) * (px-ox) + cos(theta) * (py-oy) + oy
         */
        private BoundBox getBoundBoxOfDxf(DxfFile inObtainedStructure, bool calculateRotation, double inRotationCenterX, double inRotationCenterY, double inRotationAngleRad)
        {
            bool isFirstEstimation = true;
            bool gotInside = false;
            // bounding box total
            BoundBox retStruct = new BoundBox();
            BoundBox currentStruct = new BoundBox();
            void handleLineDimensions(DxfLine lineEntity)
            {
                double lineEntityP1X = lineEntity.P1.X;
                double lineEntityP2X = lineEntity.P2.X;
                double lineEntityP1Y = lineEntity.P1.Y;
                double lineEntityP2Y = lineEntity.P2.Y;
                if (calculateRotation)
                {
                    double lineEntityP1XNew = Math.Cos(inRotationAngleRad) * (lineEntityP1X - inRotationCenterX) - Math.Sin(inRotationAngleRad) * (lineEntityP1Y - inRotationCenterY) + inRotationCenterX;
                    double lineEntityP1YNew = Math.Sin(inRotationAngleRad) * (lineEntityP1X - inRotationCenterX) + Math.Cos(inRotationAngleRad) * (lineEntityP1Y - inRotationCenterY) + inRotationCenterY;
                    double lineEntityP2XNew = Math.Cos(inRotationAngleRad) * (lineEntityP2X - inRotationCenterX) - Math.Sin(inRotationAngleRad) * (lineEntityP2Y - inRotationCenterY) + inRotationCenterX;
                    double lineEntityP2YNew = Math.Sin(inRotationAngleRad) * (lineEntityP2X - inRotationCenterX) + Math.Cos(inRotationAngleRad) * (lineEntityP2Y - inRotationCenterY) + inRotationCenterY;
                    lineEntityP1X = lineEntityP1XNew;
                    lineEntityP2X = lineEntityP2XNew;
                    lineEntityP1Y = lineEntityP1YNew;
                    lineEntityP2Y = lineEntityP2YNew;
                }
                currentStruct.bottomRightX = Math.Max(lineEntityP1X, lineEntityP2X);
                currentStruct.bottomRightY = Math.Min(lineEntityP1Y, lineEntityP2Y);
                currentStruct.upperLeftX = Math.Min(lineEntityP1X, lineEntityP2X);
                currentStruct.upperLeftY = Math.Max(lineEntityP1Y, lineEntityP2Y);
            }
            void handleArcDimensions(DxfArc arc)
            {
                double arcCenterX = arc.Center.X;
                double arcCenterY = arc.Center.Y;
                //estimation of arc bound box is going to be fun
                double radAngleStart = ConvertDegreesToRadians(arc.StartAngle);
                double radAngleEnd = ConvertDegreesToRadians(arc.EndAngle);
                if (radAngleStart > radAngleEnd)
                {
                    radAngleEnd += Math.PI * 2;
                }
                if (calculateRotation)
                {
                    double arcCenterXNew = Math.Cos(inRotationAngleRad) * (arcCenterX - inRotationCenterX) - Math.Sin(inRotationAngleRad) * (arcCenterY - inRotationCenterY) + inRotationCenterX;
                    double arcCenterYNew = Math.Sin(inRotationAngleRad) * (arcCenterX - inRotationCenterX) + Math.Cos(inRotationAngleRad) * (arcCenterY - inRotationCenterY) + inRotationCenterY;
                    radAngleEnd += inRotationAngleRad;
                    radAngleStart += inRotationAngleRad;
                    arcCenterX = arcCenterXNew;
                    arcCenterY = arcCenterYNew;
                }
                double startPointXcoordinate = arcCenterX + Math.Cos(radAngleStart) * arc.Radius;
                double startPointYcoordinate = arcCenterY + Math.Sin(radAngleStart) * arc.Radius;
                double endPointXcoordinate = arcCenterX + Math.Cos(radAngleEnd) * arc.Radius;
                double endPointYcoordinate = arcCenterY + Math.Sin(radAngleEnd) * arc.Radius;

                currentStruct.bottomRightX = Math.Max(startPointXcoordinate, endPointXcoordinate);
                currentStruct.bottomRightY = Math.Min(startPointYcoordinate, endPointYcoordinate);
                currentStruct.upperLeftX = Math.Min(startPointXcoordinate, endPointXcoordinate);
                currentStruct.upperLeftY = Math.Max(startPointYcoordinate, endPointYcoordinate);
                double angleCounter = 0;
                //iterate in steps of pi/2 from 0 to 4*pi
                while (angleCounter <= Math.PI * 4.0)
                {
                    if ((angleCounter >= radAngleStart) && (angleCounter <= radAngleEnd))
                    {
                        if ((angleCounter == 0) || (angleCounter == (2 * Math.PI)) || (angleCounter == (4 * Math.PI)))
                        {
                            currentStruct.bottomRightX = Math.Max(currentStruct.bottomRightX, arcCenterX + arc.Radius);

                        }
                        else if ((angleCounter == (3 * Math.PI / 2.0)) || (angleCounter == (11 * Math.PI / 2.0)))
                        {
                            currentStruct.bottomRightY = Math.Min(currentStruct.bottomRightY, arcCenterY - arc.Radius);
                        }
                        else if ((angleCounter == Math.PI) || (angleCounter == 3 * Math.PI))
                        {
                            currentStruct.upperLeftX = Math.Min(currentStruct.upperLeftX, arcCenterX - arc.Radius);
                        }
                        else if ((angleCounter == (Math.PI / 2.0)) || (angleCounter == (5 * Math.PI / 2.0)))
                        {
                            currentStruct.upperLeftY = Math.Max(currentStruct.upperLeftY, arcCenterY + arc.Radius);
                        }


                    }
                    if (angleCounter >= radAngleEnd)
                    {
                        break;
                    }
                    angleCounter += Math.PI / 2.0;
                }
            }
            void handleCircleDimensions(DxfCircle circle)
            {
                double circleCenterX = circle.Center.X;
                double circleCenterY = circle.Center.Y;
                double circleRadius = circle.Radius;
                if (calculateRotation)
                {
                    double circleCenterXNew = Math.Cos(inRotationAngleRad) * (circleCenterX - inRotationCenterX) - Math.Sin(inRotationAngleRad) * (circleCenterY - inRotationCenterY) + inRotationCenterX;
                    double circleCenterYNew = Math.Sin(inRotationAngleRad) * (circleCenterX - inRotationCenterX) + Math.Cos(inRotationAngleRad) * (circleCenterY - inRotationCenterY) + inRotationCenterY;
                    circleCenterX = circleCenterXNew;
                    circleCenterY = circleCenterYNew;
                }
                currentStruct.bottomRightX = circleCenterX + circleRadius;
                currentStruct.bottomRightY = circleCenterY - circleRadius;
                currentStruct.upperLeftX = circleCenterX - circleRadius;
                currentStruct.upperLeftY = circleCenterY + circleRadius;
            }
            void handlePolylineDimensions(List<DxfEntity> polylineEntities)
            {
                foreach (DxfEntity entity in polylineEntities)
                {
                    currentStruct = new BoundBox();
                    switch (entity.EntityType)
                    {
                        case DxfEntityType.Line:
                            {
                                if (gotInside == false) { gotInside = true; }
                                DxfLine lineEntity = entity as DxfLine;
                                handleLineDimensions(lineEntity);
                                break;
                            }
                        case DxfEntityType.Arc:
                            {
                                if (gotInside == false) { gotInside = true; }
                                DxfArc arc = (DxfArc)entity;
                                handleArcDimensions(arc);
                                break;
                            }
                        case DxfEntityType.Circle:
                            {
                                if (gotInside == false) { gotInside = true; }
                                DxfCircle circle = (DxfCircle)entity;
                                handleCircleDimensions(circle);
                                break;
                            }
                        case DxfEntityType.Polyline:
                        case DxfEntityType.LwPolyline:
                            {
                                if (gotInside == false)
                                {
                                    gotInside = true;
                                }
                                List<DxfEntity> obtainedEntitiesFromPolyline = new List<DxfEntity>();
                                if (entity is DxfPolyline)
                                {
                                    obtainedEntitiesFromPolyline = (entity as DxfPolyline).AsSimpleEntities().ToList();
                                }
                                else if (entity is DxfLwPolyline)
                                {
                                    obtainedEntitiesFromPolyline = (entity as DxfLwPolyline).AsSimpleEntities().ToList();
                                }

                                break;
                            }
                    }
                    if (gotInside)
                    {
                        if (isFirstEstimation)
                        {
                            retStruct.bottomRightX = currentStruct.bottomRightX;
                            retStruct.bottomRightY = currentStruct.bottomRightY;
                            retStruct.upperLeftX = currentStruct.upperLeftX;
                            retStruct.upperLeftY = currentStruct.upperLeftY;
                            isFirstEstimation = false;
                        }
                        else
                        {
                            retStruct.bottomRightX = Math.Max(currentStruct.bottomRightX, retStruct.bottomRightX);
                            retStruct.bottomRightY = Math.Min(currentStruct.bottomRightY, retStruct.bottomRightY);
                            retStruct.upperLeftX = Math.Min(currentStruct.upperLeftX, retStruct.upperLeftX);
                            retStruct.upperLeftY = Math.Max(currentStruct.upperLeftY, retStruct.upperLeftY);
                        }
                        gotInside = false;
                    }
                }
            }
            foreach (DxfEntity entity in inObtainedStructure.Entities)
            {
                if (ignoredLayers.Contains(entity.Layer.ToLower()) == false)
                {
                    currentStruct = new BoundBox();
                    switch (entity.EntityType)
                    {
                        case DxfEntityType.Line:
                            {
                                if (gotInside == false)
                                {
                                    gotInside = true;
                                }
                                DxfLine lineEntity = entity as DxfLine;
                                handleLineDimensions(lineEntity);
                                break;
                            }
                        case DxfEntityType.Arc:
                            {
                                if (gotInside == false)
                                {
                                    gotInside = true;
                                }
                                DxfArc arc = (DxfArc)entity;

                                handleArcDimensions(arc);

                                break;
                            }
                        case DxfEntityType.Circle:
                            {
                                if (gotInside == false)
                                {
                                    gotInside = true;
                                }
                                DxfCircle circle = (DxfCircle)entity;
                                handleCircleDimensions(circle);
                                break;
                            }
                        case DxfEntityType.Polyline:
                        case DxfEntityType.LwPolyline:
                            {
                                if (gotInside == false)
                                {
                                    gotInside = true;
                                }
                                List<DxfEntity> obtainedEntitiesFromPolyline = new List<DxfEntity>();
                                if (entity is DxfPolyline)
                                {
                                    obtainedEntitiesFromPolyline = (entity as DxfPolyline).AsSimpleEntities().ToList();
                                }
                                else if (entity is DxfLwPolyline)
                                {
                                    obtainedEntitiesFromPolyline = (entity as DxfLwPolyline).AsSimpleEntities().ToList();
                                }
                                handlePolylineDimensions(obtainedEntitiesFromPolyline);
                                break;
                            }
                    }
                    if (gotInside)
                    {
                        if (isFirstEstimation)
                        {
                            retStruct.bottomRightX = currentStruct.bottomRightX;
                            retStruct.bottomRightY = currentStruct.bottomRightY;
                            retStruct.upperLeftX = currentStruct.upperLeftX;
                            retStruct.upperLeftY = currentStruct.upperLeftY;
                            isFirstEstimation = false;
                        }
                        else
                        {
                            retStruct.bottomRightX = Math.Max(currentStruct.bottomRightX, retStruct.bottomRightX);
                            retStruct.bottomRightY = Math.Min(currentStruct.bottomRightY, retStruct.bottomRightY);
                            retStruct.upperLeftX = Math.Min(currentStruct.upperLeftX, retStruct.upperLeftX);
                            retStruct.upperLeftY = Math.Max(currentStruct.upperLeftY, retStruct.upperLeftY);
                        }
                        gotInside = false;
                    }
                }
            }
            return retStruct;
        }

        /// <summary>
        /// entry point to control. Here it begins. Process DXF file. Or re-calculate parameters of figures to render
        /// </summary>
        /// <param name="reparseRequired">read dxf file from scratch using ixmilia. RotateRequired and RescaleRequired parameters are ignored if this one is true</param>
        /// <param name="inPathToFile">path to file</param>
        /// <param name="rotationAngleDeg">angle in degrees for rotation</param>
        /// <param name="mirror">do we apply mirror before rotation?</param>
        public void processDXFfileNow(bool reparseRequired, string inPathToFile, double rotationAngleDeg, bool mirror)
        {
            if (reparseRequired)
            {
                dxfFile = DxfFile.Load(inPathToFile);
                // create renderstruct
                renderStruct = new RenderStruct();
                renderStruct.currentRawBoundBox = getBoundBoxOfDxf(dxfFile, false, 0, 0, 0);
            }
            double rawCenterX = (renderStruct.currentRawBoundBox.bottomRightX + renderStruct.currentRawBoundBox.upperLeftX) / 2;
            double rawCenterY = (renderStruct.currentRawBoundBox.bottomRightY + renderStruct.currentRawBoundBox.upperLeftY) / 2;
            double rotationAngleRad = ConvertDegreesToRadians(rotationAngleDeg);
            // next... calculate rotation bound box 
            renderStruct.currentRotationBoundBox = getBoundBoxOfDxf(dxfFile, true, rawCenterX, rawCenterY, rotationAngleRad);
            //calculate appropriate scale. largest dimension of rotated boundbox must fit inside smallest dimension of control
            double rotBoxWidth = Math.Abs(renderStruct.currentRotationBoundBox.bottomRightX - renderStruct.currentRotationBoundBox.upperLeftX);
            double rotBoxHeight = Math.Abs(renderStruct.currentRotationBoundBox.bottomRightY - renderStruct.currentRotationBoundBox.upperLeftY);
            double horizontalScale = (this.Width - (2 * offsetLeftRight)) / rotBoxWidth;
            double verticalScale = (this.Height - (2 * offsetTopBottom)) / rotBoxHeight;
            bool widthIsBiggerDxf = (rotBoxWidth> rotBoxHeight);
            bool widthIsBiggerControl = this.Width > this.Height;
            // dxf file has bigger width than height. Control has bigger width than height. We are going to scale using height
            if (widthIsBiggerDxf && widthIsBiggerControl) { renderStruct.uniformScaleFactor = verticalScale; }
            // dxf file has bigger height than width . Control has bigger height than width. We are going to scale using width
            if (!widthIsBiggerControl && !widthIsBiggerDxf) { renderStruct.uniformScaleFactor = horizontalScale; }
            // dxf file has bigger width than height. Control has bigger height than width. We are going to use ... width scale
            if (widthIsBiggerDxf && !widthIsBiggerControl) { renderStruct.uniformScaleFactor = horizontalScale; }
            // dxf file has bigger height than width. Control has bigger width than height. We are going to use ... height scale
            if (!widthIsBiggerDxf && widthIsBiggerControl) { renderStruct.uniformScaleFactor = verticalScale; }
            //and init rendering figures

        }
        private void WinFormsDxfRenderer_Paint(object sender, PaintEventArgs e)
        {
            drawBoundBox(e.Graphics);
            
        }
        private void drawBoundBox(Graphics in_graphics)
        {
            if (renderStruct != null)
            {
                double bboxOffsetX = -renderStruct.currentRotationBoundBox.bottomRightX;
                double bboxOffsetY = -renderStruct.currentRotationBoundBox.bottomRightY;
                double rotBoxWidth = Math.Abs(renderStruct.currentRotationBoundBox.bottomRightX - renderStruct.currentRotationBoundBox.upperLeftX);
                double rotBoxHeight = Math.Abs(renderStruct.currentRotationBoundBox.bottomRightY - renderStruct.currentRotationBoundBox.upperLeftY);
                Pen limePen = Pens.LimeGreen;
                in_graphics.DrawLine(limePen, (float)offsetLeftRight, (float)offsetTopBottom, (float)offsetLeftRight, (float)(rotBoxHeight * renderStruct.uniformScaleFactor));
                in_graphics.DrawLine(limePen, (float)offsetLeftRight, (float)offsetTopBottom, (float)(rotBoxWidth * renderStruct.uniformScaleFactor), (float)offsetTopBottom);
            }
        }
    }
}
