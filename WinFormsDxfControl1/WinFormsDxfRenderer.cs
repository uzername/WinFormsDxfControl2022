using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection.Metadata;
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
            /// list of figures to render. And prepared to be rendered. I mean, rotated and mirrored
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

            public BoundBox rotBoxScaled = new BoundBox();
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
        private double ConvertRadiansToDegrees(double radians)
        {
            double degrees = ( radians * 180.0 )/ Math.PI;
            return(degrees);
        }

        // I want to align dxf profile somehow to axes. I find out bounding box of dxf file
        // I also use here values for rotation
        // inRotationAngleRad is from 0 to 2PI . It does not matter though
        // bool? mirroring :: null - do not care about mirroring, true - mirror it, false - no mirror it. Mirroring affects bound box, but only of rotated figurine
        /* https://stackoverflow.com/questions/2259476/rotating-a-point-about-another-point-2d
         * If you rotate point (px, py) around point (ox, oy) by angle theta you'll get:
            p'x = cos(theta) * (px-ox) - sin(theta) * (py-oy) + ox
            p'y = sin(theta) * (px-ox) + cos(theta) * (py-oy) + oy
         */
        private BoundBox getBoundBoxOfDxf(DxfFile inObtainedStructure, bool calculateRotation, double inRotationCenterX, double inRotationCenterY, double inRotationAngleRad, bool? mirroring)
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
                if (mirroring.GetValueOrDefault())
                {
                    // mirror of line
                    double midPointHorizontal = inRotationCenterX;
                    lineEntityP1X = mirrorPointByGuide(lineEntityP1X, midPointHorizontal);
                    lineEntityP2X = mirrorPointByGuide(lineEntityP2X, midPointHorizontal);
                }
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
                if (mirroring.GetValueOrDefault())
                {
                    // mirror of arc
                    double midPointHorizontal = inRotationCenterX;
                    radAngleStart = mirrorAngleByGuide(radAngleStart);
                    radAngleEnd = mirrorAngleByGuide(radAngleEnd);
                    // swap?

                    double tempDecimal = radAngleStart;
                    radAngleStart = radAngleEnd;
                    radAngleEnd = tempDecimal;

                    if (radAngleStart > radAngleEnd)
                    {
                        radAngleEnd += Math.PI * 2;
                    }
                    arcCenterX = mirrorPointByGuide(arcCenterX, midPointHorizontal);
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

        ///<summary>
        /// perform mirroring of point with X=in_lineEntityP1X, while mirroring plane is at in_midPointHorizontal
        /// </summary>
        double mirrorPointByGuide(double in_lineEntityP1X, double in_midPointHorizontal)
        {
            double out_lineEntityP1X = in_lineEntityP1X;
            if (in_lineEntityP1X < in_midPointHorizontal)
            {
                out_lineEntityP1X = in_lineEntityP1X + 2 * Math.Abs(in_lineEntityP1X - in_midPointHorizontal);
            }
            else if (in_lineEntityP1X > in_midPointHorizontal)
            {
                out_lineEntityP1X = in_lineEntityP1X - 2 * Math.Abs(in_lineEntityP1X - in_midPointHorizontal);
            }
            return out_lineEntityP1X;
        }
        // https://stackoverflow.com/a/60580020/5128696
        double mirrorAngleByGuide(double angleRad)
        {
            double mirrored_Angle = Math.PI - angleRad;
            if (mirrored_Angle < 0)
                mirrored_Angle = 2 * Math.PI + mirrored_Angle;
            return mirrored_Angle;
        }
        // https://www.geeksforgeeks.org/2d-transformation-in-computer-graphics-set-1-scaling-of-objects/
        void scalePointAroundAnotherPoint(double P1X, double P1Y, double OX, double OY, double k, out double P2X, out double P2Y )
        {
            P2X = k * (P1X - OX) + OX;
            P2Y = k * (P1Y - OY)+   OY;
        }

        /// <summary>
        /// fill in renderStruct.AllFigures, for rendering. apply mirroring and then rotation
        /// </summary>
        /// <param name="inObtainedStructure">raw structure of dxf entities, provided by ixMilia</param>
        /// <param name="inRotationAngleRad">rotation angle</param>        
        /// <param name="mirror">mirroring, before rotating</param>
        private void initRenderFigures(DxfFile inObtainedStructure, double inRotationAngleRad, bool mirror)
        {
            double midPointHorizontal = (renderStruct.currentRawBoundBox.upperLeftX + renderStruct.currentRawBoundBox.bottomRightX) / 2.0;
            double midPointVertical = (renderStruct.currentRawBoundBox.upperLeftY + renderStruct.currentRawBoundBox.bottomRightY) / 2.0;
            RenderLine handleLineGeometry(DxfLine lineEntity)
            {
                RenderLine rLine = new RenderLine();
                double lineEntityP1X = lineEntity.P1.X;
                double lineEntityP2X = lineEntity.P2.X;
                double lineEntityP1Y = lineEntity.P1.Y;
                double lineEntityP2Y = lineEntity.P2.Y;
                
                double inRotationCenterX = midPointHorizontal;
                double inRotationCenterY = midPointVertical;
                // crude mirroring
                if (mirror)
                {
                    lineEntityP1X = mirrorPointByGuide(lineEntityP1X, midPointHorizontal);
                    lineEntityP2X = mirrorPointByGuide(lineEntityP2X, midPointHorizontal);
                }
                if (inRotationAngleRad != 0)
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
                rLine.StartX = lineEntityP1X; rLine.StartY = lineEntityP1Y;
                rLine.EndX = lineEntityP2X; rLine.EndY = lineEntityP2Y;
                return rLine;
            }
            
            RenderArc handleArcGeometry(DxfArc arc)
            {
                RenderArc rArc = new RenderArc();
                double arcCenterX = arc.Center.X;
                double arcCenterY = arc.Center.Y;
                double radAngleStart = ConvertDegreesToRadians(arc.StartAngle);
                double radAngleEnd = ConvertDegreesToRadians(arc.EndAngle);                
                if (radAngleStart > radAngleEnd)
                {
                    radAngleEnd += Math.PI * 2;
                }
                // uncertain crude experimental mirror of arc
                // experiments are showing that start angle becomes end angle,
                // and end angle becomes start - after mirroring
                if (mirror)
                {
                    radAngleStart = mirrorAngleByGuide(radAngleStart);
                    radAngleEnd = mirrorAngleByGuide(radAngleEnd);
                    // swap?

                    double tempDecimal = radAngleStart;
                    radAngleStart = radAngleEnd;
                    radAngleEnd = tempDecimal;

                    if (radAngleStart > radAngleEnd)
                    {
                        radAngleEnd += Math.PI * 2;
                    }
                    arcCenterX = mirrorPointByGuide(arcCenterX, midPointHorizontal);
                }
                if (inRotationAngleRad != 0)
                {
                    double arcCenterXNew = Math.Cos(inRotationAngleRad) * (arcCenterX - midPointHorizontal) - Math.Sin(inRotationAngleRad) * (arcCenterY - midPointVertical) + midPointHorizontal;
                    double arcCenterYNew = Math.Sin(inRotationAngleRad) * (arcCenterX - midPointHorizontal) + Math.Cos(inRotationAngleRad) * (arcCenterY - midPointVertical) + midPointVertical;
                    radAngleEnd += inRotationAngleRad;
                    radAngleStart += inRotationAngleRad;
                    arcCenterX = arcCenterXNew;
                    arcCenterY = arcCenterYNew;
                }                
                if (radAngleStart < radAngleEnd)
                {
                    rArc.sweepAngle = ConvertRadiansToDegrees(radAngleEnd - radAngleStart);
                }
                else
                {
                    // should it be here
                    rArc.sweepAngle = ConvertRadiansToDegrees(radAngleEnd + Math.PI*2 - radAngleStart);
                }

                rArc.startAngle = ConvertRadiansToDegrees( 2*Math.PI- radAngleEnd );
                rArc.height = arc.Radius * 2;
                rArc.width = arc.Radius * 2;
                rArc.y = arcCenterY + arc.Radius;
                rArc.x = arcCenterX - arc.Radius;
                return rArc;
            }
            /// this one is recursive
            void handlePolyline(List<DxfEntity> theInternalsOfPolyLine)
            {
                foreach (DxfEntity entity in theInternalsOfPolyLine)
                {
                    if (entity is DxfLwPolyline)
                    {
                        List<DxfEntity> theInternalsOfPolyLine2 = new List<DxfEntity>((entity as DxfLwPolyline).AsSimpleEntities());
                        handlePolyline(theInternalsOfPolyLine2);
                    }
                    else if (entity is DxfPolyline)
                    {
                        List<DxfEntity> theInternalsOfPolyLine2 = new List<DxfEntity>(((entity as DxfPolyline).AsSimpleEntities()));
                        handlePolyline(theInternalsOfPolyLine2);
                    } else
                   
                        switch (entity.EntityType)
                        {
                            case (DxfEntityType.Line):
                                {
                                    RenderLine line1 = handleLineGeometry(entity as DxfLine);
                                    renderStruct.AllFigures.Add(line1);
                                    break;
                                }
                            case (DxfEntityType.Arc):
                                {
                                    RenderArc arc1 = handleArcGeometry(entity as DxfArc);
                                    renderStruct.AllFigures.Add(arc1);
                                    break;
                                }
                            default:
                                break;
                        }
                    
                }
            }

            renderStruct.AllFigures = new List<RenderFigure>();
            foreach (DxfEntity entity in dxfFile.Entities)
            {
                if (ignoredLayers.Contains(entity.Layer.ToLower()) == false)
                {
                    switch (entity.EntityType)
                    {

                        case DxfEntityType.Line:
                            {
                                DxfLine line = (DxfLine)entity;
                                RenderLine line1 = handleLineGeometry(line);
                                renderStruct.AllFigures.Add(line1);
                                break;
                            }
                        case DxfEntityType.Arc:
                            {
                                DxfArc arc = (DxfArc)entity;
                                RenderArc arc1 = handleArcGeometry(arc);
                                renderStruct.AllFigures.Add(arc1);
                                break;
                            }
                        case DxfEntityType.Circle:
                            {
                                
                                DxfCircle circle = (DxfCircle)entity;
                                double circleCenterX = circle.Center.X;
                                double circleCenterY = circle.Center.Y;
                                double circleRadius = circle.Radius;
                                           
                                /// TODO handle circle
                                break;
                            }
                        case DxfEntityType.LwPolyline:
                        case DxfEntityType.Polyline:
                            {
                                List<DxfEntity> theInternalsOfPolyLine = new List<DxfEntity>();
                                handlePolyline(theInternalsOfPolyLine);
                                break;
                            }
                    }
                }
            }

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
                // we do not care about mirroring here, since boundbox of figure is not changed by mirroring
                renderStruct.currentRawBoundBox = getBoundBoxOfDxf(dxfFile, false, 0, 0, 0, null);  
            }
            double rawCenterX = (renderStruct.currentRawBoundBox.bottomRightX + renderStruct.currentRawBoundBox.upperLeftX) / 2;
            double rawCenterY = (renderStruct.currentRawBoundBox.bottomRightY + renderStruct.currentRawBoundBox.upperLeftY) / 2;
            double rotationAngleRad = ConvertDegreesToRadians(rotationAngleDeg);
            // next... calculate rotation bound box 
            renderStruct.currentRotationBoundBox = getBoundBoxOfDxf(dxfFile, true, rawCenterX, rawCenterY, rotationAngleRad, mirror);
            // scale factor
            recalculateScaleFactor();
            //and init rendering figures
            initRenderFigures(dxfFile, rotationAngleRad, mirror);
            // finally draw
            this.Refresh();
        }
        /// <summary>
        /// assigns scale factor to renderStruct.uniformScaleFactor
        /// </summary>
        private void recalculateScaleFactor()
        {
            if (renderStruct != null)
            {
                //calculate appropriate scale. largest dimension of rotated boundbox must fit inside smallest dimension of control
                double rotBoxWidth = Math.Abs(renderStruct.currentRotationBoundBox.bottomRightX - renderStruct.currentRotationBoundBox.upperLeftX);
                double rotBoxHeight = Math.Abs(renderStruct.currentRotationBoundBox.bottomRightY - renderStruct.currentRotationBoundBox.upperLeftY);
                double horizontalScale = (this.Width - (2 * offsetLeftRight)) / rotBoxWidth;
                double verticalScale = (this.Height - (2 * offsetTopBottom)) / rotBoxHeight;
                bool widthIsBiggerDxf = (rotBoxWidth > rotBoxHeight);
                bool widthIsBiggerControl = this.Width > this.Height;
                // https://stackoverflow.com/a/1373879/5128696 scale one rectangle into another
                renderStruct.uniformScaleFactor = Math.Min(horizontalScale, verticalScale);

                // we want to scale output figures to fit the window.
                double rotBoxScalingCenterX = (renderStruct.currentRotationBoundBox.bottomRightX + renderStruct.currentRotationBoundBox.upperLeftX) / 2;
                double rotBoxScalingCenterY = (renderStruct.currentRotationBoundBox.bottomRightY + renderStruct.currentRotationBoundBox.upperLeftY) / 2;
                renderStruct.rotBoxScaled = new BoundBox();
                scalePointAroundAnotherPoint(
                    renderStruct.currentRotationBoundBox.bottomRightX,
                    renderStruct.currentRotationBoundBox.bottomRightY,
                    rotBoxScalingCenterX, rotBoxScalingCenterY,
                    renderStruct.uniformScaleFactor,
                    out renderStruct.rotBoxScaled.bottomRightX, out renderStruct.rotBoxScaled.bottomRightY);
                scalePointAroundAnotherPoint(
                    renderStruct.currentRotationBoundBox.upperLeftX,
                    renderStruct.currentRotationBoundBox.upperLeftY,
                    rotBoxScalingCenterX, rotBoxScalingCenterY,
                    renderStruct.uniformScaleFactor,
                    out renderStruct.rotBoxScaled.upperLeftX, out renderStruct.rotBoxScaled.upperLeftY);
            }
        }
        private void WinFormsDxfRenderer_Paint(object sender, PaintEventArgs e)
        {
            // https://stackoverflow.com/questions/1485745/flip-coordinates-when-drawing-to-control
            // but I shall just do Height-Y, so there won't be problems if I ever decide to draw text
            drawBoundBox(e.Graphics);
            drawAllFigures(e.Graphics);
            
        }
        private void drawBoundBox(Graphics in_graphics)
        {
            if (renderStruct != null)
            {
                // apparently offset is not needed here
                double rotBoxWidth = Math.Abs(renderStruct.currentRotationBoundBox.bottomRightX - renderStruct.currentRotationBoundBox.upperLeftX);
                double rotBoxHeight = Math.Abs(renderStruct.currentRotationBoundBox.bottomRightY - renderStruct.currentRotationBoundBox.upperLeftY);
                Pen limePen = Pens.LimeGreen;
                in_graphics.DrawLine(limePen, (float)offsetLeftRight, Height - (float)offsetTopBottom, (float)offsetLeftRight, Height - (float)(rotBoxHeight * renderStruct.uniformScaleFactor));
                in_graphics.DrawLine(limePen, (float)offsetLeftRight, Height - (float)offsetTopBottom, (float)(rotBoxWidth * renderStruct.uniformScaleFactor), Height - (float)offsetTopBottom);
                in_graphics.DrawLine(limePen, (float)offsetLeftRight, Height - (float)(rotBoxHeight * renderStruct.uniformScaleFactor), (float)(rotBoxWidth * renderStruct.uniformScaleFactor), Height - (float)(rotBoxHeight * renderStruct.uniformScaleFactor));
                in_graphics.DrawLine(limePen, (float)(rotBoxWidth * renderStruct.uniformScaleFactor), Height - (float)offsetTopBottom, (float)(rotBoxWidth * renderStruct.uniformScaleFactor), Height - (float)(rotBoxHeight * renderStruct.uniformScaleFactor));
            }
        }
        private void drawAllFigures(Graphics in_graphics)
        {
            /// TODO handle color
            Pen currentPen = Pens.Blue;
            if (renderStruct!=null)
            {
                // assuming that rotboxscaled was calculated, in recalculateScaleFactor
                double offsetRotatedScaledX = -renderStruct.rotBoxScaled.upperLeftX;
                double offsetRotatedScaledY = -renderStruct.rotBoxScaled.bottomRightY;
                double cntrX = (renderStruct.currentRotationBoundBox.bottomRightX + renderStruct.currentRotationBoundBox.upperLeftX) / 2.0;
                double cntrY = (renderStruct.currentRotationBoundBox.bottomRightY + renderStruct.currentRotationBoundBox.upperLeftY) / 2.0;
                foreach (RenderFigure currentFigure in renderStruct.AllFigures)
                {
                    if (currentFigure is RenderLine)
                    {
                        RenderLine rLine = (RenderLine)currentFigure;
                        double X1 = rLine.StartX; double X2 = rLine.EndX;
                        double Y1 = rLine.StartY; double Y2 = rLine.EndY;
                        scalePointAroundAnotherPoint(rLine.StartX, rLine.StartY, cntrX, cntrY, renderStruct.uniformScaleFactor, out X1, out Y1);
                        scalePointAroundAnotherPoint(rLine.EndX, rLine.EndY, cntrX, cntrY, renderStruct.uniformScaleFactor, out X2, out Y2);
                        X1=X1+offsetRotatedScaledX; X2=X2+offsetRotatedScaledX;
                        // origin point to bottom
                        Y1=Height-(Y1+offsetRotatedScaledY); Y2=Height-(Y2+offsetRotatedScaledY);

                        in_graphics.DrawLine(currentPen, (float)X1, (float)Y1, (float)X2, (float)Y2);
                    }else if (currentFigure is RenderArc)
                    {
                        RenderArc rArc = (RenderArc)currentFigure;
                        double X = rArc.x; double Y = rArc.y;
                        scalePointAroundAnotherPoint(rArc.x, rArc.y, cntrX, cntrY, renderStruct.uniformScaleFactor, out X, out Y);
                        X = X + offsetRotatedScaledX;
                        Y = Height-( Y + offsetRotatedScaledY );
                        double width = rArc.width * renderStruct.uniformScaleFactor;
                        double height = rArc.height * renderStruct.uniformScaleFactor;
                        // do not harass render engine with tiny arcs
                        if ((width >= 2)&&(height>= 2))
                        {
                            in_graphics.DrawArc(currentPen, (float)X, (float)Y, (float)width, (float)height, (float)rArc.startAngle, (float)rArc.sweepAngle);
                        } else
                        {

                        }
                    }
                }
            }
        }

        private void WinFormsDxfRenderer_Resize(object sender, EventArgs e)
        {
            recalculateScaleFactor();
            this.Refresh();
        }
    }
}
