#!/usr/bin/env python3
"""Export a simple GLB 2.0 mesh into an OBJ package for free rigging tools.

The generated ZIP is intended for tools such as Mixamo or AccuRIG when the
source asset is a static GLB produced by an AI 3D model generator.
"""

from __future__ import annotations

import argparse
import json
import math
import shutil
import struct
import zipfile
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable


COMPONENT_FORMATS = {
    5120: ("b", 1),
    5121: ("B", 1),
    5122: ("h", 2),
    5123: ("H", 2),
    5125: ("I", 4),
    5126: ("f", 4),
}

TYPE_COMPONENTS = {
    "SCALAR": 1,
    "VEC2": 2,
    "VEC3": 3,
    "VEC4": 4,
    "MAT4": 16,
}

IMAGE_EXTENSIONS = {
    "image/jpeg": ".jpg",
    "image/png": ".png",
    "image/webp": ".webp",
}


@dataclass(frozen=True)
class GlbData:
    json: dict
    binary: bytes


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("input_glb", type=Path, help="Path to selected.glb")
    parser.add_argument("output_dir", type=Path, help="Directory that will receive OBJ, textures and ZIP")
    parser.add_argument("--name", default=None, help="Base file name for exported files")
    return parser.parse_args()


def read_glb(path: Path) -> GlbData:
    with path.open("rb") as file:
        magic = file.read(4)
        version = struct.unpack("<I", file.read(4))[0]
        length = struct.unpack("<I", file.read(4))[0]
        if magic != b"glTF" or version != 2:
            raise ValueError(f"{path} is not a GLB 2.0 file")

        json_chunk = None
        binary_chunk = None
        while file.tell() < length:
            chunk_length = struct.unpack("<I", file.read(4))[0]
            chunk_type = file.read(4)
            data = file.read(chunk_length)
            if chunk_type == b"JSON":
                json_chunk = json.loads(data.decode("utf-8"))
            elif chunk_type == b"BIN\x00":
                binary_chunk = data

    if json_chunk is None or binary_chunk is None:
        raise ValueError("GLB must contain JSON and BIN chunks")

    return GlbData(json_chunk, binary_chunk)


def accessor_values(glb: GlbData, accessor_index: int) -> list[tuple[float, ...]]:
    accessor = glb.json["accessors"][accessor_index]
    view = glb.json["bufferViews"][accessor["bufferView"]]
    fmt, component_size = COMPONENT_FORMATS[accessor["componentType"]]
    components = TYPE_COMPONENTS[accessor["type"]]
    count = accessor["count"]
    view_offset = view.get("byteOffset", 0)
    accessor_offset = accessor.get("byteOffset", 0)
    stride = view.get("byteStride", component_size * components)
    base = view_offset + accessor_offset

    values: list[tuple[float, ...]] = []
    for index in range(count):
        item_offset = base + index * stride
        raw = glb.binary[item_offset:item_offset + component_size * components]
        values.append(struct.unpack("<" + fmt * components, raw))
    return values


def accessor_scalars(glb: GlbData, accessor_index: int) -> list[int]:
    return [int(values[0]) for values in accessor_values(glb, accessor_index)]


def extract_images(glb: GlbData, output_dir: Path) -> dict[int, str]:
    images: dict[int, str] = {}
    for index, image in enumerate(glb.json.get("images", [])):
        view = glb.json["bufferViews"][image["bufferView"]]
        offset = view.get("byteOffset", 0)
        length = view["byteLength"]
        extension = IMAGE_EXTENSIONS.get(image.get("mimeType", ""), ".img")
        safe_name = safe_file_name(image.get("name", f"image_{index}")) + extension
        image_path = output_dir / safe_name
        image_path.write_bytes(glb.binary[offset:offset + length])
        images[index] = safe_name
    return images


def safe_file_name(name: str) -> str:
    allowed = []
    for character in name:
        if character.isalnum() or character in ("-", "_", "."):
            allowed.append(character)
        else:
            allowed.append("_")
    return "".join(allowed).strip("._") or "asset"


def write_mtl(path: Path, material_name: str, diffuse_texture: str | None, normal_texture: str | None) -> None:
    lines = [
        f"newmtl {material_name}",
        "Ka 1.000000 1.000000 1.000000",
        "Kd 1.000000 1.000000 1.000000",
        "Ks 0.050000 0.050000 0.050000",
        "Ns 10.000000",
    ]
    if diffuse_texture:
        lines.append(f"map_Kd {diffuse_texture}")
    if normal_texture:
        lines.append(f"map_Bump {normal_texture}")
    path.write_text("\n".join(lines) + "\n")


def write_obj(
    path: Path,
    base_name: str,
    material_name: str,
    positions: list[tuple[float, ...]],
    normals: list[tuple[float, ...]],
    uvs: list[tuple[float, ...]],
    indices: list[int],
) -> None:
    lines: list[str] = [
        f"mtllib {base_name}.mtl",
        f"o {base_name}",
        f"usemtl {material_name}",
    ]

    for x, y, z in positions:
        lines.append(f"v {x:.8f} {y:.8f} {z:.8f}")

    for uv in uvs:
        u = uv[0]
        v = uv[1] if len(uv) > 1 else 0.0
        lines.append(f"vt {u:.8f} {1.0 - v:.8f}")

    for normal in normals:
        x, y, z = normalize3(normal)
        lines.append(f"vn {x:.8f} {y:.8f} {z:.8f}")

    for triangle in batched(indices, 3):
        if len(triangle) != 3:
            continue
        face = []
        for vertex_index in triangle:
            obj_index = vertex_index + 1
            face.append(f"{obj_index}/{obj_index}/{obj_index}")
        lines.append("f " + " ".join(face))

    path.write_text("\n".join(lines) + "\n")


def normalize3(values: tuple[float, ...]) -> tuple[float, float, float]:
    x, y, z = values[:3]
    length = math.sqrt(x * x + y * y + z * z)
    if length <= 0.000001:
        return (0.0, 1.0, 0.0)
    return (x / length, y / length, z / length)


def batched(values: list[int], size: int) -> Iterable[list[int]]:
    for index in range(0, len(values), size):
        yield values[index:index + size]


def texture_file_for_material(glb: GlbData, images: dict[int, str], texture_kind: str) -> str | None:
    material = glb.json.get("materials", [{}])[0]
    texture_index = None
    if texture_kind == "baseColor":
        texture_info = material.get("pbrMetallicRoughness", {}).get("baseColorTexture")
        texture_index = texture_info.get("index") if texture_info else None
    elif texture_kind == "normal":
        texture_info = material.get("normalTexture")
        texture_index = texture_info.get("index") if texture_info else None

    if texture_index is None:
        return None

    image_index = glb.json.get("textures", [])[texture_index].get("source")
    return images.get(image_index)


def create_zip(output_dir: Path, base_name: str, files: list[Path]) -> Path:
    zip_path = output_dir / f"{base_name}_mixamo_input.zip"
    with zipfile.ZipFile(zip_path, "w", compression=zipfile.ZIP_DEFLATED) as archive:
        for file_path in files:
            archive.write(file_path, arcname=file_path.name)
    return zip_path


def write_manifest(path: Path, glb: GlbData, zip_path: Path, files: list[Path]) -> None:
    mesh = glb.json["meshes"][0]
    primitive = mesh["primitives"][0]
    manifest = {
        "source": str(path.name),
        "zip": zip_path.name,
        "nodes": len(glb.json.get("nodes", [])),
        "meshes": len(glb.json.get("meshes", [])),
        "materials": len(glb.json.get("materials", [])),
        "skins": len(glb.json.get("skins", [])),
        "animations": len(glb.json.get("animations", [])),
        "vertices": glb.json["accessors"][primitive["attributes"]["POSITION"]]["count"],
        "indices": glb.json["accessors"][primitive["indices"]]["count"],
        "files": [file.name for file in files],
    }
    (zip_path.parent / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n")


def export_mixamo_package(input_glb: Path, output_dir: Path, base_name: str) -> Path:
    glb = read_glb(input_glb)
    if len(glb.json.get("meshes", [])) != 1:
        raise ValueError("This exporter expects exactly one mesh")

    primitive = glb.json["meshes"][0]["primitives"][0]
    attributes = primitive["attributes"]
    positions = accessor_values(glb, attributes["POSITION"])
    normals = accessor_values(glb, attributes["NORMAL"])
    uvs = accessor_values(glb, attributes["TEXCOORD_0"])
    indices = accessor_scalars(glb, primitive["indices"])

    output_dir.mkdir(parents=True, exist_ok=True)
    for item in output_dir.iterdir():
        if item.is_file():
            item.unlink()
        elif item.is_dir():
            shutil.rmtree(item)

    images = extract_images(glb, output_dir)
    diffuse_texture = texture_file_for_material(glb, images, "baseColor")
    normal_texture = texture_file_for_material(glb, images, "normal")
    material_name = f"{base_name}_material"

    obj_path = output_dir / f"{base_name}.obj"
    mtl_path = output_dir / f"{base_name}.mtl"
    write_mtl(mtl_path, material_name, diffuse_texture, normal_texture)
    write_obj(obj_path, base_name, material_name, positions, normals, uvs, indices)

    files = [obj_path, mtl_path] + [output_dir / file_name for file_name in images.values()]
    zip_path = create_zip(output_dir, base_name, files)
    write_manifest(input_glb, glb, zip_path, files)
    return zip_path


def main() -> None:
    args = parse_args()
    base_name = safe_file_name(args.name or args.input_glb.stem)
    zip_path = export_mixamo_package(args.input_glb, args.output_dir, base_name)
    print(zip_path)


if __name__ == "__main__":
    main()
