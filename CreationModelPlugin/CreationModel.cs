using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.ApplicationServices;

namespace CreationModelPlugin
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class CreationModel : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;

            //var res1 = new FilteredElementCollector(doc)
            //    .OfClass(typeof(Wall))
            //    //.Cast<Wall>() может дать исключение, так как типы лишние приходят
            //    .OfType<Wall>()
            //    .ToList();            
            //var res2 = new FilteredElementCollector(doc)
            //    .OfClass(typeof(WallType))                
            //    .OfType<WallType>()
            //    .ToList();
            //var res3 = new FilteredElementCollector(doc)
            //    .OfClass(typeof(FamilyInstance))
            //    .OfCategory(BuiltInCategory.OST_Doors)
            //    .OfType<FamilyInstance>()
            //    .Where(x => x.Name.Equals("1010х2100(h)"))
            //    .ToList();
            //var res4 = new FilteredElementCollector(doc)
            //   .WhereElementIsNotElementType()
            //   .ToList();

            double width = UnitUtils.ConvertToInternalUnits(12000, UnitTypeId.Millimeters);
            double depth = UnitUtils.ConvertToInternalUnits(7000, UnitTypeId.Millimeters);

            Level level_1;
            Level level_2;
            TakeLevels(doc, out level_1, out level_2);
            CreateWalls(doc, level_1, level_2, width, depth);
            return Result.Succeeded;
        }


        private static void CreateWalls(Document doc, Level level_1, Level level_2, double width, double depth)
        {            
            double dx = width / 2;
            double dy = depth / 2;

            List<XYZ> points = new List<XYZ>();
            points.Add(new XYZ(-dx, -dy, 0));
            points.Add(new XYZ(dx, -dy, 0));
            points.Add(new XYZ(dx, dy, 0));
            points.Add(new XYZ(-dx, dy, 0));
            points.Add(new XYZ(-dx, -dy, 0));

            List<Wall> walls = new List<Wall>();

            Transaction transaction = new Transaction(doc, "Построение стен");
            transaction.Start();
            for (int i = 0; i < 4; i++)
            {
                Line line = Line.CreateBound(points[i], points[i + 1]);
                Wall wall = Wall.Create(doc, line, level_1.Id, true);
                walls.Add(wall);
                wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(level_2.Id);
            }
            AddDoor(doc, level_1, walls[0]);
            AddWindow(doc, level_1, walls[1]);
            AddWindow(doc, level_1, walls[2]);
            AddWindow(doc, level_1, walls[3]);
            AddRoof(doc, level_2, walls, width, depth);
            transaction.Commit();
        }

        private static void AddRoof(Document doc, Level level_2, List<Wall> walls, double width, double depth)
        {            
            double dx = width / 2;
            double dy = depth / 2;

            double wallWidth = walls[0].Width;
            double dt = wallWidth / 2;

            double facade = level_2.get_Parameter(BuiltInParameter.LEVEL_ELEV).AsDouble();
            
            
            RoofType roofType = new FilteredElementCollector(doc)
                .OfClass(typeof(RoofType))
                .OfType<RoofType>()
                .Where(x => x.Name.Equals("Типовой - 400мм"))
                .Where(x => x.FamilyName.Equals("Базовая крыша"))
                .FirstOrDefault();

            CurveArray curveArray = new CurveArray();
            curveArray.Append(Line.CreateBound(new XYZ((-dx - dt), (-dy-dt), facade), new XYZ((-dx - dt), 0, 20)));
            curveArray.Append(Line.CreateBound(new XYZ((-dx - dt), 0, 20), new XYZ((-dx - dt), (dy+dt), facade)));

            ReferencePlane plane = doc.Create.NewReferencePlane(new XYZ(0, 0, 0), new XYZ(0, 0, 20), new XYZ(0, 20, 0), doc.ActiveView);
            doc.Create.NewExtrusionRoof(curveArray, plane, level_2, roofType, (-dx - dt), (dx + dt));


        }

        //private static void AddRoof(Document doc, Level level_2, List<Wall> walls)
        //{
        //    RoofType roofType = new FilteredElementCollector(doc)
        //        .OfClass(typeof(RoofType))
        //        .OfType<RoofType>()
        //        .Where(x => x.Name.Equals("Типовой - 400мм"))
        //        .Where(x => x.FamilyName.Equals("Базовая крыша"))
        //        .FirstOrDefault();

        //    double wallWidth = walls[0].Width;
        //    double dt = wallWidth / 2;

        //    List<XYZ> points = new List<XYZ>();
        //    points.Add(new XYZ(-dt, -dt, 0));
        //    points.Add(new XYZ(dt, -dt, 0));
        //    points.Add(new XYZ(dt, dt, 0));
        //    points.Add(new XYZ(-dt, dt, 0));
        //    points.Add(new XYZ(-dt, -dt, 0));

        //    Application application = doc.Application;
        //    CurveArray footprint = application.Create.NewCurveArray();
        //    for (int i = 0; i < 4; i++)
        //    {
        //        LocationCurve locationCurve = walls[i].Location as LocationCurve;
        //        XYZ point_1 = locationCurve.Curve.GetEndPoint(0);
        //        XYZ point_2 = locationCurve.Curve.GetEndPoint(1);
        //        Line line = Line.CreateBound(point_1 + points[i], point_2 + points[i + 1]);
        //        footprint.Append(line);
        //    }

        //    ModelCurveArray footPrintToModelCurveMapping = new ModelCurveArray();
        //    FootPrintRoof footprintRoof = doc.Create.NewFootPrintRoof(footprint, level_2, roofType, out footPrintToModelCurveMapping);
        //    //ModelCurveArrayIterator iterator = footPrintToModelCurveMapping.ForwardIterator();
        //    //iterator.Reset();
        //    //while (iterator.MoveNext())
        //    //{
        //    //    ModelCurve modelCurve = iterator.Current as ModelCurve;
        //    //    footprintRoof.set_DefinesSlope(modelCurve, true);
        //    //    footprintRoof.set_SlopeAngle(modelCurve, 0.5);
        //    //}

        //    foreach (ModelCurve m in footPrintToModelCurveMapping)
        //    {
        //        footprintRoof.set_DefinesSlope(m, true);
        //        footprintRoof.set_SlopeAngle(m, 0.5);
        //    }
        //}

        private static void AddDoor(Document doc, Level level_1, Wall wall)
        {
            FamilySymbol doorType = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Doors)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.Equals("0915 x 2134 мм"))
                .Where(x => x.FamilyName.Equals("Одиночные-Щитовые"))
                .FirstOrDefault();

            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point_1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point_2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point_1 + point_2) / 2;

            if (!doorType.IsActive)
                doorType.Activate();
            doc.Create.NewFamilyInstance(point, doorType, wall, level_1, StructuralType.NonStructural);
        }

        private static void AddWindow(Document doc, Level level_1, Wall wall)
        {
            FamilySymbol windowType = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Windows)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.Equals("0915 x 1830 мм"))
                .Where(x => x.FamilyName.Equals("Фиксированные"))
                .FirstOrDefault();

            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point_1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point_2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point_1 + point_2) / 2;

            if (!windowType.IsActive)
                windowType.Activate();
            var window = doc.Create.NewFamilyInstance(point, windowType, wall, level_1, StructuralType.NonStructural);
            Parameter sillHeight = window.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM);
            double sh = UnitUtils.ConvertToInternalUnits(1000, UnitTypeId.Millimeters);
            sillHeight.Set(sh);
        }

        private static void TakeLevels(Document doc, out Level level_1, out Level level_2)
        {
            List<Level> listLevel = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .OfType<Level>()
                .ToList();

            level_1 = listLevel
                .Where(x => x.Name.Equals("Уровень 1"))
                .FirstOrDefault();

            level_2 = listLevel
                .Where(x => x.Name.Equals("Уровень 2"))
                .FirstOrDefault();
        }




    }
}
