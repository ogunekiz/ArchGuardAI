using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Net.Http;
using System.Net.Http.Json;

namespace ArchGuard.RevitPlugin;

/// <summary>
/// Autodesk Revit Ribbon menüsünden tetiklenen ExternalCommand.
/// Aktif açık olan Revit projesindeki Kat, Mahal, Kapı ve Duvar elemanlarını
/// dışarıya .ifc aktarmaya gerek kalmadan doğrudan ArchGuard .NET 9 API'sine gönderir.
/// </summary>
[Transaction(TransactionMode.ReadOnly)]
public class SendToArchGuardCommand : IExternalCommand
{
	private const string ApiIngestUrl = "https://localhost:7002/api/Projects/ingest-from-revit";

	public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
	{
		UIDocument uiDoc = commandData.Application.ActiveUIDocument;
		if (uiDoc is null)
		{
			TaskDialog.Show("ArchGuard AI", "Lütfen önce bir Revit projesi açın.");
			return Result.Cancelled;
		}

		Document doc = uiDoc.Document;

		try
		{
			// 1. Katları (Levels) Oku
			var levels = new FilteredElementCollector(doc)
					.OfClass(typeof(Level))
					.Cast<Level>()
					.Select(l => new
					{
						global_id = l.UniqueId,
						name = l.Name,
						elevation = Math.Round(UnitUtils.ConvertFromInternalUnits(l.Elevation, UnitTypeId.Meters), 2)
					}).ToList();

			// 2. Kapıları (OST_Doors) ve Genişlik/Yükseklik Parametrelerini Oku
			var doors = new FilteredElementCollector(doc)
					.OfCategory(BuiltInCategory.OST_Doors)
					.WhereElementIsNotElementType()
					.Cast<FamilyInstance>()
					.Select(d =>
					{
						ElementType? doorType = doc.GetElement(d.GetTypeId()) as ElementType;
						double rawWidth = doorType?.get_Parameter(BuiltInParameter.DOOR_WIDTH)?.AsDouble() ?? 0.0;
						double rawHeight = doorType?.get_Parameter(BuiltInParameter.DOOR_HEIGHT)?.AsDouble() ?? 0.0;
						string fireRating = doorType?.get_Parameter(BuiltInParameter.FIRE_RATING)?.AsString() ?? "Belirtilmemiş";
						string levelName = (doc.GetElement(d.LevelId) as Level)?.Name ?? "Tanımsız Kat";

						// .NET Framework 4.8 uyumlu büyük/küçük harf duyarsız arama
						bool isFireExit = (d.Name != null && (d.Name.IndexOf("Yangın", StringComparison.OrdinalIgnoreCase) >= 0 || d.Name.IndexOf("Exit", StringComparison.OrdinalIgnoreCase) >= 0));

						return new
						{
							global_id = d.UniqueId,
							name = d.Name,
							storey = levelName,
							width_m = Math.Round(UnitUtils.ConvertFromInternalUnits(rawWidth, UnitTypeId.Meters), 3),
							height_m = Math.Round(UnitUtils.ConvertFromInternalUnits(rawHeight, UnitTypeId.Meters), 3),
							is_fire_exit = isFireExit,
							fire_rating = string.IsNullOrWhiteSpace(fireRating) ? "Belirtilmemiş" : fireRating
						};
					}).ToList();

			// 3. Duvarları (OST_Walls) ve Alan/Hacim Metrajlarını Oku
			var walls = new FilteredElementCollector(doc)
					.OfCategory(BuiltInCategory.OST_Walls)
					.WhereElementIsNotElementType()
					.Cast<Wall>()
					.Select(w =>
					{
						double areaInternal = w.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED)?.AsDouble() ?? 0.0;
						double volumeInternal = w.get_Parameter(BuiltInParameter.HOST_VOLUME_COMPUTED)?.AsDouble() ?? 0.0;
						double lengthInternal = w.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH)?.AsDouble() ?? 0.0;
						string levelName = (doc.GetElement(w.LevelId) as Level)?.Name ?? "Tanımsız Kat";

						return new
						{
							global_id = w.UniqueId,
							name = w.Name,
							storey = levelName,
							is_external = w.WallType.Function == WallFunction.Exterior,
							length_m = Math.Round(UnitUtils.ConvertFromInternalUnits(lengthInternal, UnitTypeId.Meters), 2),
							area_m2 = Math.Round(UnitUtils.ConvertFromInternalUnits(areaInternal, UnitTypeId.SquareMeters), 2),
							volume_m3 = Math.Round(UnitUtils.ConvertFromInternalUnits(volumeInternal, UnitTypeId.CubicMeters), 2)
						};
					}).ToList();

			// 4. Payload'ı Oluştur ve .NET 9 API'ye Gönder
			var payload = new
			{
				project_name = doc.Title,
				source_file = $"{doc.Title}.rvt",
				storeys = levels,
				spaces = Array.Empty<object>(),
				doors,
				walls
			};

			using var httpClient = new HttpClient();
			var response = httpClient.PostAsJsonAsync(ApiIngestUrl, payload).GetAwaiter().GetResult();

			if (!response.IsSuccessStatusCode)
			{
				TaskDialog.Show("ArchGuard AI - Hata", $"Sunucu hatası: {response.StatusCode}");
				return Result.Failed;
			}

			TaskDialog.Show("ArchGuard AI", $"Model başarıyla senkronize edildi!\nGönderilen Kapı: {doors.Count}, Duvar: {walls.Count}");
			return Result.Succeeded;
		}
		catch (Exception ex)
		{
			message = ex.Message;
			return Result.Failed;
		}
	}
}