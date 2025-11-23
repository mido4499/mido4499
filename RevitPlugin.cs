using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using DLL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Plumbing;

namespace ELE_SpotElevation
{
    public class MainMethodsHandler : MainWindowVM
    {
        private static Document document = OpenMainWindowCommand.document;
        private static UIDocument uidoc = OpenMainWindowCommand.uiDocument;
        private static Selection selection;
        public static double TotalLength = 0;
        public static Element SelectedFamilyForElectricalFixture;
        public static System.Collections.Generic.Queue<Reference> ListOfSelectedElements = new System.Collections.Generic.Queue<Reference>();
        //public static List<Element> ListOfSelectedElements;
        public static Document LinkedDoc;
        private static RevitLinkInstance revitLinkInstance;
        private static Transform linkTransform;
        private static Element FamilyEle;
        public static List<SpotInfo> spotCoordinatesList = new List<SpotInfo>();
        public static List<SpotInfo> spotElevationsList = new List<SpotInfo>();

        public enum MethodsName
        {
            PickElements,
            SelectAll,
            AddSpotElevations,
            oldAddSpotElevations
        }
        public static void MethodsHandler(object Item = null)
        {
            switch (Events_Handler.EventHandler.mainMethodsName)
            {
                case MethodsName.PickElements:
                    TotalLength = 0;
                    OpenMainWindowCommand.mainWindow.WindowState = WindowState.Minimized;
                    try
                    {
                        DataGridVM.DataGridElementsList.Clear();
                        spotCoordinatesList.Clear();
                        spotElevationsList.Clear();
                        dataGridVM.IsAllSelected = false;
                        dataGridVM.ElementsCount = DataGridVM.DataGridElementsList.Where(e => e.IsSelected).ToList().Count;
                        if (OpenMainWindowCommand.mainWindow.AllDocuments.SelectedIndex == 0)
                        {
                            ListOfSelectedElements = RevitSelection.PickElements(ObjectType.Element);
                            //ListOfSelectedElements = uidoc.Selection.PickObjects(ObjectType.Element, new MEPSelectionFilter(), "Select MEP elements");
                        }
                        else
                        {
                            //ListOfSelectedElements = RevitSelection.PickElements(ObjectType.LinkedElement);
                            ListOfSelectedElements.Clear();

                            Selection sel = uidoc.Selection;

                            var s = new MEPSelectionFilter();
                            Reference pickedRef = sel.PickObject(ObjectType.LinkedElement, s, "Select one linked MEP element");
                            if (pickedRef != null)
                            {
                                Element selectedElement = GetLinkedElements.GetLinkedElement(pickedRef.LinkedElementId.ToString()).element;
                                RevitLinkInstance selectedLinkInstance = document.GetElement(pickedRef.ElementId) as RevitLinkInstance;
                                if (selectedElement != null && selectedLinkInstance != null)
                                {
                                    ProjectMethods.CollectSimilarLinkedElementsInView(selectedElement, selectedLinkInstance, ListOfSelectedElements);
                                }
                            }


                            //while (true)
                            //{
                            //    try
                            //    {
                            //        //Reference pickedRef = sel.PickObject(ObjectType.LinkedElement, "Select linked element or press ESC to finish");

                            //        var s = new MEPSelectionFilter();
                            //        Reference pickedRef = sel.PickObject(ObjectType.LinkedElement, s, "Select linked MEP element or press ESC to finish");
                            //        if (pickedRef != null)
                            //        {
                            //            ListOfSelectedElements.Enqueue(pickedRef);
                            //        }

                            //    }
                            //    catch
                            //    {
                            //        // Escape was pressed to end selection
                            //        break;
                            //    }
                            //}

                        }
                        if (ListOfSelectedElements.Count > 0)
                        {
                            var n = 0;
                            foreach (var ele in ListOfSelectedElements)
                            {
                                if (null != ele.ElementId)
                                {
                                    if (OpenMainWindowCommand.mainWindow.AllDocuments.SelectedIndex == 0)
                                    {
                                        FamilyEle = document.GetElement(ele.ElementId);
                                    }
                                    else
                                    {
                                        FamilyEle = GetLinkedElements.GetLinkedElement(ele.LinkedElementId.ToString()).element;
                                        LinkedDoc = GetLinkedElements.GetLinkedElement(ele.LinkedElementId.ToString()).doc;
                                        revitLinkInstance = document.GetElement(ele.ElementId) as RevitLinkInstance;
                                        linkTransform = revitLinkInstance.GetTransform();
                                    }
                                    if (null != FamilyEle)
                                    {
                                        if ((FamilyEle.Category.Id == Category.GetCategory(document, BuiltInCategory.OST_ElectricalEquipment).Id)
                                            || (FamilyEle.Category.Id == Category.GetCategory(document, BuiltInCategory.OST_CommunicationDevices).Id)
                                            || (FamilyEle.Category.Id == Category.GetCategory(document, BuiltInCategory.OST_PlumbingFixtures).Id))
                                        {
                                            var familyinstance = FamilyEle as FamilyInstance;
                                            //if (!DataGridVM.DataGridElementsList.Any(x => x.Id == FamilyEle.Id))
                                            //{
                                            DataGridVM.DataGridElementsList.Add(new DataGridElements()
                                            {
                                                Id = FamilyEle.Id,
                                                Name = FamilyEle.Name,
                                                ElementPicked = FamilyEle,
                                                familyInstance = familyinstance,
                                                LinkedDocument = LinkedDoc,
                                                LinkInstance = revitLinkInstance,
                                                LinkTransform = linkTransform,
                                                UniqueId = FamilyEle.UniqueId
                                            });
                                            //}
                                        }
                                        ;
                                        n++;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        TaskDialog.Show("Error", ex.Message);
                        throw;
                    }
                    OpenMainWindowCommand.mainWindow.WindowState = WindowState.Normal;
                    break;
                case MethodsName.SelectAll:
                    try
                    {
                        foreach (var element in DataGridVM.DataGridElementsList)
                        {
                            element.IsSelected = dataGridVM.IsAllSelected;
                        }
                        dataGridVM.ElementsCount = DataGridVM.DataGridElementsList.Where(e => e.IsSelected).ToList().Count;
                    }
                    catch (Exception exc) { }
                    break;
                case MethodsName.AddSpotElevations:
                    try
                    {
                        if (DataGridVM.DataGridElementsList.Count > 0)
                        {
                            foreach (var element in DataGridVM.DataGridElementsList)
                            {
                                if (element.IsSelected)
                                {

                                    //FamilyInstance instance = element.familyInstance;
                                    Options options = new Options { ComputeReferences = true, View = uidoc.ActiveView };
                                    GeometryElement geometryElement = element.familyInstance.get_Geometry(options);
                                    //GeometryElement geometryElement = familyInstance.get_Geometry(options);
                                    if (geometryElement != null)
                                    {
                                        Edge edge = null;
                                        Reference LinkedEdge = null;
                                        XYZ origin = null;
                                        XYZ bend = null;
                                        XYZ end = null;
                                        XYZ transformedOrigin = null;
                                        XYZ transformedBend = null;
                                        XYZ transformedEnd = null;
                                        foreach (GeometryObject geomObj in geometryElement)
                                        {
                                            if (geomObj is GeometryInstance instance)
                                            {
                                                GeometryElement instanceGeometry = instance.GetSymbolGeometry();
                                                foreach (GeometryObject nestedGeomObj in instanceGeometry)
                                                {
                                                    if (nestedGeomObj is Solid solid && solid.Volume > 0)
                                                    {
                                                        foreach (Face face in solid.Faces)
                                                        {
                                                            PlanarFace planarFace = face as PlanarFace;
                                                            if (planarFace != null)
                                                            {
                                                                if (planarFace.FaceNormal.IsAlmostEqualTo(XYZ.BasisY))
                                                                {
                                                                    edge = face.EdgeLoops.get_Item(0).get_Item(0);
                                                                    break;
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                                GeometryElement instanceGeometry2 = instance.GetInstanceGeometry();
                                                foreach (GeometryObject nestedGeomObj in instanceGeometry2)
                                                {
                                                    if (nestedGeomObj is Solid solid && solid.Volume > 0)
                                                    {
                                                        var originq = solid.Edges.get_Item(0).AsCurve().GetEndPoint(0);
                                                        var origin2 = element.familyInstance.GetTransform().Origin;
                                                        //origin = ProjectMethods.GetFamilyInstanceCenter(element.familyInstance);
                                                        origin = new XYZ(origin2.X, origin2.Y, originq.Z);

                                                        if (linkTransform != null)
                                                        {
                                                            transformedOrigin = element.LinkTransform.OfPoint(origin);
                                                            transformedBend = transformedOrigin + new XYZ(2, 0, 0);
                                                            transformedEnd = transformedBend + new XYZ(0, 2, 0);
                                                        }
                                                        else
                                                        {
                                                            bend = origin + new XYZ(2, 0, 0);
                                                            end = bend + new XYZ(0, 2, 0);
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                        if (Leader)
                                        {
                                            OpenMainWindowCommand.mainWindow.LeaderProperties.IsEnabled = true;
                                            if (textBoxElements.LeaderLength != 0 || textBoxElements.LeaderAngle != 0)
                                            {
                                                var convertLeaderLength = UnitUtils.ConvertToInternalUnits(textBoxElements.LeaderLength, UnitTypeId.Millimeters);
                                                double XBend = convertLeaderLength * MathEquations.CosAngle(textBoxElements.LeaderAngle);
                                                double YBend = convertLeaderLength * MathEquations.SinAngle(textBoxElements.LeaderAngle);
                                                var xbend = UnitUtils.ConvertToInternalUnits(700, UnitTypeId.Millimeters);
                                                double coordinateTailLength = 0;

                                                if ((textBoxElements.LeaderAngle >= -90 && textBoxElements.LeaderAngle <= 90) || (textBoxElements.LeaderAngle >= 270 && textBoxElements.LeaderAngle <= 360))
                                                {
                                                    textBoxElements.RightSide = true;

                                                    if (element.LinkTransform != null)
                                                    {
                                                        transformedBend = transformedOrigin + new XYZ(XBend, YBend, 0);
                                                        transformedEnd = transformedBend + new XYZ(-xbend, 0, 0);
                                                    }
                                                    else
                                                    {
                                                        bend = origin + new XYZ(-XBend, YBend, 0);
                                                        end = bend + new XYZ(xbend, 0, 0);
                                                    }

                                                    if (dataGridVM.IsCoordinatesEnabled)
                                                    {
                                                        //Removing the negative sign + adding length to fit the coordinates
                                                        coordinateTailLength = 11.5 * xbend;
                                                        if (element.LinkTransform != null)
                                                        {
                                                            transformedEnd = transformedBend + new XYZ(coordinateTailLength, 0, 0);
                                                        }
                                                        else
                                                        {
                                                            bend = origin + new XYZ(coordinateTailLength, YBend, 0);
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    textBoxElements.RightSide = false;
                                                    if (element.LinkTransform != null)
                                                    {
                                                        transformedBend = transformedOrigin + new XYZ(XBend, YBend, 0);
                                                        transformedEnd = transformedBend + new XYZ(xbend, 0, 0);
                                                    }
                                                    else
                                                    {
                                                        bend = origin + new XYZ(XBend, YBend, 0);
                                                        end = bend + new XYZ(xbend, 0, 0);
                                                    }

                                                    if (dataGridVM.IsCoordinatesEnabled)
                                                    {
                                                        //Adding the negative sign + adding length to fit the coordinates
                                                        coordinateTailLength = -11.5 * xbend;
                                                        if (element.LinkTransform != null)
                                                        {
                                                            transformedEnd = transformedBend + new XYZ(coordinateTailLength, 0, 0);
                                                        }
                                                        else
                                                        {
                                                            bend = origin + new XYZ(coordinateTailLength, YBend, 0);
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                        //if (dataGridVM.IsCoordinatesEnabled /*&& edge?.Reference != null*/)
                                        //{
                                        //    Reference topFaceRef = ProjectMethods.GetTopFaceReference(element.familyInstance);

                                        //    Reference elevationRef = null;

                                        //    if (topFaceRef != null)
                                        //    {
                                        //        elevationRef = (element.LinkInstance != null) ? topFaceRef.CreateLinkReference(element.LinkInstance) : topFaceRef;
                                        //    }
                                        //    else
                                        //    {
                                        //        // لو مفيش وجه علوي ناخد الـ reference الافتراضي (مثل edge.Reference)
                                        //        elevationRef = (element.LinkInstance != null) ? edge.Reference.CreateLinkReference(element.LinkInstance) : edge.Reference;
                                        //    }
                                        //    //SpotInfo spot = new SpotInfo
                                        //    //{
                                        //    //    Reference = (element.LinkInstance != null) ? edge.Reference.CreateLinkReference(element.LinkInstance) : edge.Reference,
                                        //    //    //Origin = (LinkedDoc != null) ? transformedOrigin : origin,
                                        //    //    Origin = (document != null) ? transformedOrigin : origin,                                              
                                        //    //    Bend = (LinkedDoc != null) ? transformedBend : bend,
                                        //    //    End = (LinkedDoc != null) ? transformedEnd : end,
                                        //    //    IsLinked = (LinkedDoc != null)
                                        //    //};
                                        //    //spotCoordinatesList.Add(spot);
                                        //    // نحاول نجيب الوجه العلوي كـ Reference للسنتر
                                        //    Reference coordinateRef = ProjectMethods.GetTopFaceReference(element.familyInstance);

                                        //    XYZ usedOrigin = (document != null) ? transformedOrigin : origin;

                                        //    // تأكد إن النقطة فعلاً على الفيس
                                        //    if (coordinateRef != null)
                                        //    {
                                        //        Face topFace = ProjectMethods.GetFaceFromReference(coordinateRef, document);
                                        //        if (!ProjectMethods.IsPointOnFace(topFace, usedOrigin))
                                        //        {
                                        //            coordinateRef = null; // نلغيها عشان نروح للفول باك
                                        //        }
                                        //    }

                                        //    // fallback 1: edge
                                        //    // لو مفيش وجه أفقي، نرجع لـ edge كـ fallback (اختياري)
                                        //    if (coordinateRef == null && edge != null)
                                        //        coordinateRef = edge.Reference;

                                        //    // fallback 2: أي وجه أفقي تاني (مش شرط top)
                                        //    if (coordinateRef == null)
                                        //    {
                                        //        coordinateRef = ProjectMethods.GetAnyPlanarFaceReference(element.familyInstance);
                                        //    }


                                        //    // لو لقينا reference نكمل
                                        //    if (coordinateRef != null)
                                        //    {
                                        //        SpotInfo spot = new SpotInfo
                                        //        {
                                        //            //Reference = (element.LinkInstance != null)
                                        //            //    ? coordinateRef.CreateLinkReference(element.LinkInstance)
                                        //            //    : coordinateRef,
                                        //            Reference = elevationRef,
                                        //            Origin = (document != null) ? transformedOrigin : origin,
                                        //            Bend = (LinkedDoc != null) ? transformedBend : bend,
                                        //            End = (LinkedDoc != null) ? transformedEnd : end,
                                        //            IsLinked = (LinkedDoc != null)
                                        //        };
                                        //        spotCoordinatesList.Add(spot);
                                        //    }
                                        //}

                                        //if (dataGridVM.IsSpotEleEnabled)
                                        //{
                                        //    // نحصل على Reference الوجه الأفقي من العنصر (FamilyInstance)
                                        //    Reference topFaceRef = ProjectMethods.GetTopFaceReference(element.familyInstance);

                                        //    Reference elevationRef = null;

                                        //    if (topFaceRef != null)
                                        //    {
                                        //        elevationRef = (element.LinkInstance != null) ? topFaceRef.CreateLinkReference(element.LinkInstance) : topFaceRef;
                                        //    }
                                        //    else
                                        //    {
                                        //        // لو مفيش وجه علوي ناخد الـ reference الافتراضي (مثل edge.Reference)
                                        //        elevationRef = (element.LinkInstance != null) ? edge.Reference.CreateLinkReference(element.LinkInstance) : edge.Reference;
                                        //    }

                                        //    // نضيف SpotInfo مع الإبقاء على نفس الإحداثيات (Origin, Bend, End)
                                        //    SpotInfo spot = new SpotInfo
                                        //    {
                                        //        Reference = elevationRef,
                                        //        Origin = (document != null) ? transformedOrigin : origin,
                                        //        Bend = (LinkedDoc != null) ? transformedBend : bend,
                                        //        End = (LinkedDoc != null) ? transformedEnd : end,
                                        //        IsLinked = (LinkedDoc != null)
                                        //    };
                                        //    spotElevationsList.Add(spot);
                                        //}

                                        //نحصل على Reference الوجه الأفقي من العنصر(FamilyInstance)
                                        Reference topFaceRef = ProjectMethods.GetTopFaceReference(element.familyInstance);

                                        Reference elevationRef = null;

                                        if (topFaceRef != null)
                                        {
                                            elevationRef = (element.LinkInstance != null) ? topFaceRef.CreateLinkReference(element.LinkInstance) : topFaceRef;
                                        }
                                        else
                                        {
                                            // لو مفيش وجه علوي ناخد الـ reference الافتراضي (مثل edge.Reference)
                                            elevationRef = (element.LinkInstance != null) ? edge.Reference.CreateLinkReference(element.LinkInstance) : edge.Reference;
                                        }

                                        // ------ إنشاء SpotInfo مرة واحدة ونستخدمها لكلا النوعين ------
                                        SpotInfo unifiedSpot = new SpotInfo
                                        {
                                            Reference = elevationRef,
                                            Origin = (document != null) ? transformedOrigin : origin,
                                            Bend = (document != null) ? transformedBend : bend,
                                            End = (document != null) ? transformedEnd : end,
                                            IsLinked = (document != null)
                                        };

                                        // إسناد للـ Spot Coordinate
                                        if (dataGridVM.IsCoordinatesEnabled)
                                        {
                                            spotCoordinatesList.Add(unifiedSpot);
                                        }

                                        // إسناد للـ Spot Elevation
                                        if (dataGridVM.IsSpotEleEnabled)
                                        {
                                            spotElevationsList.Add(unifiedSpot);
                                        }
                                    }
                                }
                            }
                            // Draw Spot Coordinates
                            foreach (var spot in spotCoordinatesList)
                            {
                                if (dataGridVM.IsCoordinatesEnabled)
                                {
                                    document.Create.NewSpotCoordinate(document.ActiveView, spot.Reference, spot.Origin, spot.Bend, spot.End, spot.Origin, Leader);
                                }
                            }

                            // Draw Spot Elevations
                            foreach (var spot in spotElevationsList)
                            {
                                if (dataGridVM.IsSpotEleEnabled)
                                {
                                    try
                                    {
                                        document.Create.NewSpotElevation(document.ActiveView, spot.Reference, spot.Origin, spot.Bend, spot.End, spot.Origin, Leader);
                                    }
                                    catch (Exception /*ex*/)
                                    {
                                        /*TaskDialog.Show("Elevation Error", ex.Message);*/
                                    }
                                }
                            }

                        }
                        //spotCoordinatesList.Clear();
                        //spotElevationsList.Clear();
                    }
                    catch (Exception) { throw; }
                    break;
                case MethodsName.oldAddSpotElevations:
                    try
                    {
                        if (DataGridVM.DataGridElementsList.Count > 0)
                        {
                            foreach (var element in DataGridVM.DataGridElementsList)
                            {
                                if (element.IsSelected)
                                {

                                    //FamilyInstance instance = element.familyInstance;
                                    Options options = new Options { ComputeReferences = true, View = uidoc.ActiveView };
                                    GeometryElement geometryElement = element.familyInstance.get_Geometry(options);
                                    //GeometryElement geometryElement = familyInstance.get_Geometry(options);
                                    if (geometryElement != null)
                                    {
                                        Edge edge = null;
                                        Reference LinkedEdge = null;
                                        XYZ origin = null;
                                        XYZ bend = null;
                                        XYZ end = null;
                                        XYZ transformedOrigin = null;
                                        XYZ transformedBend = null;
                                        XYZ transformedEnd = null;
                                        foreach (GeometryObject geomObj in geometryElement)
                                        {
                                            if (geomObj is GeometryInstance instance)
                                            {
                                                GeometryElement instanceGeometry = instance.GetSymbolGeometry();
                                                foreach (GeometryObject nestedGeomObj in instanceGeometry)
                                                {
                                                    if (nestedGeomObj is Solid solid && solid.Volume > 0)
                                                    {
                                                        foreach (Face face in solid.Faces)
                                                        {
                                                            PlanarFace planarFace = face as PlanarFace;
                                                            if (planarFace != null)
                                                            {
                                                                if (planarFace.FaceNormal.IsAlmostEqualTo(XYZ.BasisY))
                                                                {
                                                                    edge = face.EdgeLoops.get_Item(0).get_Item(0);
                                                                    break;
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                                GeometryElement instanceGeometry2 = instance.GetInstanceGeometry();
                                                foreach (GeometryObject nestedGeomObj in instanceGeometry2)
                                                {
                                                    if (nestedGeomObj is Solid solid && solid.Volume > 0)
                                                    {
                                                        origin = solid.Edges.get_Item(0).AsCurve().GetEndPoint(0);
                                                        if (linkTransform != null)
                                                        {
                                                            transformedOrigin = element.LinkTransform.OfPoint(origin);
                                                            transformedBend = transformedOrigin + new XYZ(2, 0, 0);
                                                            transformedEnd = transformedBend + new XYZ(0, 2, 0);
                                                        }
                                                        else
                                                        {
                                                            bend = origin + new XYZ(2, 0, 0);
                                                            end = bend + new XYZ(0, 2, 0);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        if (Leader)
                                        {
                                            OpenMainWindowCommand.mainWindow.LeaderProperties.IsEnabled = true;
                                            if (textBoxElements.LeaderLength != 0 || textBoxElements.LeaderAngle != 0)
                                            {
                                                var convertLeaderLength = UnitUtils.ConvertToInternalUnits(textBoxElements.LeaderLength, UnitTypeId.Millimeters);
                                                double XBend = convertLeaderLength * MathEquations.CosAngle(textBoxElements.LeaderAngle);
                                                double YBend = convertLeaderLength * MathEquations.SinAngle(textBoxElements.LeaderAngle);
                                                var xbend = UnitUtils.ConvertToInternalUnits(700, UnitTypeId.Millimeters);
                                                if (element.LinkTransform != null)
                                                {
                                                    transformedBend = transformedOrigin + new XYZ(XBend, YBend, 0);
                                                    transformedEnd = transformedBend + new XYZ(xbend, 0, 0);
                                                }
                                                else
                                                {
                                                    bend = origin + new XYZ(XBend, YBend, 0);
                                                    end = bend + new XYZ(xbend, 0, 0);
                                                }
                                            }
                                        }

                                        if (dataGridVM.IsCoordinatesEnabled && edge?.Reference != null)
                                        {
                                            SpotInfo spot = new SpotInfo
                                            {
                                                Reference = (element.LinkInstance != null) ? edge.Reference.CreateLinkReference(element.LinkInstance) : edge.Reference,
                                                //Origin = (LinkedDoc != null) ? transformedOrigin : origin,
                                                Origin = (document != null) ? transformedOrigin : origin,
                                                Bend = (LinkedDoc != null) ? transformedBend : bend,
                                                End = (LinkedDoc != null) ? transformedEnd : end,
                                                IsLinked = (LinkedDoc != null)
                                            };
                                            spotCoordinatesList.Add(spot);
                                        }

                                        //if (dataGridVM.IsSpotEleEnabled && edge?.Reference != null)
                                        //{
                                        //    SpotInfo spot = new SpotInfo
                                        //    {
                                        //        Reference = (LinkedDoc != null) ? edge.Reference.CreateLinkReference(revitLinkInstance) : edge.Reference,
                                        //        Origin = (document != null) ? transformedOrigin : origin,
                                        //        Bend = (LinkedDoc != null) ? transformedBend : bend,
                                        //        End = (LinkedDoc != null) ? transformedEnd : end,
                                        //        IsLinked = (LinkedDoc != null)
                                        //    };
                                        //    spotElevationsList.Add(spot);
                                        //}

                                        if (dataGridVM.IsSpotEleEnabled)
                                        {
                                            // نحصل على Reference الوجه الأفقي من العنصر (FamilyInstance)
                                            Reference topFaceRef = ProjectMethods.GetTopFaceReference(element.familyInstance);

                                            Reference elevationRef = null;

                                            if (topFaceRef != null)
                                            {
                                                elevationRef = (element.LinkInstance != null) ? topFaceRef.CreateLinkReference(element.LinkInstance) : topFaceRef;
                                            }
                                            else
                                            {
                                                // لو مفيش وجه علوي ناخد الـ reference الافتراضي (مثل edge.Reference)
                                                elevationRef = (element.LinkInstance != null) ? edge.Reference.CreateLinkReference(element.LinkInstance) : edge.Reference;
                                            }

                                            // نضيف SpotInfo مع الإبقاء على نفس الإحداثيات (Origin, Bend, End)
                                            SpotInfo spot = new SpotInfo
                                            {
                                                Reference = elevationRef,
                                                Origin = (document != null) ? transformedOrigin : origin,
                                                Bend = (LinkedDoc != null) ? transformedBend : bend,
                                                End = (LinkedDoc != null) ? transformedEnd : end,
                                                IsLinked = (LinkedDoc != null)
                                            };
                                            spotElevationsList.Add(spot);
                                        }


                                    }
                                }
                            }
                            // Draw Spot Coordinates
                            foreach (var spot in spotCoordinatesList)
                            {
                                if (dataGridVM.IsCoordinatesEnabled)
                                {
                                    try
                                    {
                                        document.Create.NewSpotCoordinate(document.ActiveView, spot.Reference, spot.Origin, spot.Bend, spot.End, spot.Origin, Leader);
                                        spotCoordinatesList.Clear();
                                        spotElevationsList.Clear();
                                    }
                                    catch (Exception ex) { TaskDialog.Show("Coordinate Error", ex.Message); }
                                }
                            }

                            // Draw Spot Elevations
                            foreach (var spot in spotElevationsList)
                            {
                                if (dataGridVM.IsSpotEleEnabled)
                                {
                                    try
                                    {
                                        document.Create.NewSpotElevation(document.ActiveView, spot.Reference, spot.Origin, spot.Bend, spot.End, spot.Origin, Leader);
                                        spotCoordinatesList.Clear();
                                        spotElevationsList.Clear();
                                    }
                                    //catch (Exception ex) { TaskDialog.Show("Elevation Error", ex.Message); }
                                    catch (Exception /*ex*/) { /*TaskDialog.Show("Elevation Error", ex.Message);*/ }
                                }
                            }
                        }
                    }
                    catch (Exception) { throw; }
                    break;
            }
        }
    }
}
