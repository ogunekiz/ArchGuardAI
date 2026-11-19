using System.Text.Json.Serialization;

namespace ArchGuard.Application.DTOs;

public class IfcParseResponseDto
{
	[JsonPropertyName("project_name")]
	public string ProjectName { get; set; } = string.Empty;

	[JsonPropertyName("source_file")]
	public string SourceFile { get; set; } = string.Empty;

	[JsonPropertyName("summary")]
	public IfcSummaryDto Summary { get; set; } = new();

	[JsonPropertyName("storeys")]
	public List<IfcStoreyDto> Storeys { get; set; } = new();

	[JsonPropertyName("spaces")]
	public List<IfcSpaceDto> Spaces { get; set; } = new();

	[JsonPropertyName("doors")]
	public List<IfcDoorDto> Doors { get; set; } = new();

	[JsonPropertyName("walls")]
	public List<IfcWallDto> Walls { get; set; } = new();
}

public class IfcSummaryDto
{
	[JsonPropertyName("total_storeys")]
	public int TotalStoreys { get; set; }

	[JsonPropertyName("total_spaces")]
	public int TotalSpaces { get; set; }

	[JsonPropertyName("total_doors")]
	public int TotalDoors { get; set; }

	[JsonPropertyName("total_walls")]
	public int TotalWalls { get; set; }

	[JsonPropertyName("total_wall_area_m2")]
	public double TotalWallAreaM2 { get; set; }

	[JsonPropertyName("total_wall_volume_m3")]
	public double TotalWallVolumeM3 { get; set; }
}

public class IfcStoreyDto
{
	[JsonPropertyName("global_id")]
	public string GlobalId { get; set; } = string.Empty;

	[JsonPropertyName("name")]
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("elevation")]
	public double Elevation { get; set; }
}

public class IfcSpaceDto
{
	[JsonPropertyName("global_id")]
	public string GlobalId { get; set; } = string.Empty;

	[JsonPropertyName("name")]
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("code")]
	public string Code { get; set; } = string.Empty;

	[JsonPropertyName("storey")]
	public string Storey { get; set; } = string.Empty;

	[JsonPropertyName("area_m2")]
	public double AreaM2 { get; set; }
}

public class IfcDoorDto
{
	[JsonPropertyName("global_id")]
	public string GlobalId { get; set; } = string.Empty;

	[JsonPropertyName("name")]
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("storey")]
	public string Storey { get; set; } = string.Empty;

	[JsonPropertyName("width_m")]
	public double WidthM { get; set; }

	[JsonPropertyName("height_m")]
	public double HeightM { get; set; }

	[JsonPropertyName("is_fire_exit")]
	public bool IsFireExit { get; set; }

	[JsonPropertyName("fire_rating")]
	public string FireRating { get; set; } = string.Empty;
}

public class IfcWallDto
{
	[JsonPropertyName("global_id")]
	public string GlobalId { get; set; } = string.Empty;

	[JsonPropertyName("name")]
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("storey")]
	public string Storey { get; set; } = string.Empty;

	[JsonPropertyName("is_external")]
	public bool IsExternal { get; set; }

	[JsonPropertyName("length_m")]
	public double LengthM { get; set; }

	[JsonPropertyName("area_m2")]
	public double AreaM2 { get; set; }

	[JsonPropertyName("volume_m3")]
	public double VolumeM3 { get; set; }
}