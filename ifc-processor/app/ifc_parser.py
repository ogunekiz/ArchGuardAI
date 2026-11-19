import ifcopenshell
import ifcopenshell.util.element
import ifcopenshell.util.placement


def _get_storey_name(element) -> str:
    """Elemanın bağlı olduğu kat (IfcBuildingStorey) adını bulur."""
    container = ifcopenshell.util.element.get_container(element)
    if container and container.is_a("IfcBuildingStorey"):
        return container.Name or f"Kat-{container.id()}"
    return "Tanımsız Kat"


def _extract_quantities(element) -> dict:
    """Elemanın Pset (Property Set) ve Qto (Quantity Set) içindeki ölçülerini sözlük olarak döner."""
    psets = ifcopenshell.util.element.get_psets(element)
    quantities = {}
    for pset_name, props in psets.items():
        if isinstance(props, dict):
            for key, val in props.items():
                if isinstance(val, (int, float)):
                    quantities[key] = round(float(val), 3)
    return quantities


def parse_ifc_file(file_path: str) -> dict:
    model = ifcopenshell.open(file_path)

    # 1. Proje Genel Bilgisi
    projects = model.by_type("IfcProject")
    project_name = projects[0].Name if projects and projects[0].Name else "Bilinmeyen BIM Projesi"

    # 2. Katlar (IfcBuildingStorey)
    storeys_data = []
    for storey in model.by_type("IfcBuildingStorey"):
        storeys_data.append({
            "global_id": storey.GlobalId,
            "name": storey.Name or "İsimsiz Kat",
            "elevation": round(float(storey.Elevation), 2) if storey.Elevation else 0.0
        })

    # 3. Odalar / Mahaller (IfcSpace)
    spaces_data = []
    for space in model.by_type("IfcSpace"):
        qto = _extract_quantities(space)
        area = qto.get("NetFloorArea") or qto.get("GrossFloorArea") or qto.get("Area") or 0.0
        spaces_data.append({
            "global_id": space.GlobalId,
            "name": space.LongName or space.Name or "İsimsiz Mahal",
            "code": space.Name or "",
            "storey": _get_storey_name(space),
            "area_m2": area
        })

    # 4. Kapılar (IfcDoor) - Yönetmelik denetimi için kritik (genişlik / yükseklik)
    doors_data = []
    for door in model.by_type("IfcDoor"):
        qto = _extract_quantities(door)
        # IFC'de genişlik doğrudan OverallWidth veya Qto içinden gelir (metre veya mm olabilir, normalize ediyoruz)
        raw_width = getattr(door, "OverallWidth", None) or qto.get("Width") or 0.0
        raw_height = getattr(door, "OverallHeight", None) or qto.get("Height") or 0.0

        # Eğer değer 10'dan büyükse mm cinsindendir, metreye çevir
        width_m = round(raw_width / 1000.0, 3) if raw_width > 10 else round(float(raw_width), 3)
        height_m = round(raw_height / 1000.0, 3) if raw_height > 10 else round(float(raw_height), 3)

        psets = ifcopenshell.util.element.get_psets(door)
        door_common = psets.get("Pset_DoorCommon", {})

        doors_data.append({
            "global_id": door.GlobalId,
            "name": door.Name or "İsimsiz Kapı",
            "storey": _get_storey_name(door),
            "width_m": width_m,
            "height_m": height_m,
            "is_fire_exit": bool(door_common.get("FireExit", False)),
            "fire_rating": str(door_common.get("FireRating", "Belirtilmemiş"))
        })

    # 5. Duvarlar (IfcWall) - Metraj ve dış/iç duvar analizi
    walls_data = []
    for wall in model.by_type("IfcWall"):
        qto = _extract_quantities(wall)
        psets = ifcopenshell.util.element.get_psets(wall)
        wall_common = psets.get("Pset_WallCommon", {})

        area = qto.get("NetSideArea") or qto.get("GrossSideArea") or qto.get("Area") or 0.0
        volume = qto.get("NetVolume") or qto.get("GrossVolume") or qto.get("Volume") or 0.0
        length = qto.get("Length") or 0.0

        walls_data.append({
            "global_id": wall.GlobalId,
            "name": wall.Name or "İsimsiz Duvar",
            "storey": _get_storey_name(wall),
            "is_external": bool(wall_common.get("IsExternal", False)),
            "length_m": length,
            "area_m2": area,
            "volume_m3": volume
        })

    return {
        "project_name": project_name,
        "summary": {
            "total_storeys": len(storeys_data),
            "total_spaces": len(spaces_data),
            "total_doors": len(doors_data),
            "total_walls": len(walls_data),
            "total_wall_area_m2": round(sum(w["area_m2"] for w in walls_data), 2),
            "total_wall_volume_m3": round(sum(w["volume_m3"] for w in walls_data), 2)
        },
        "storeys": storeys_data,
        "spaces": spaces_data,
        "doors": doors_data,
        "walls": walls_data
    }