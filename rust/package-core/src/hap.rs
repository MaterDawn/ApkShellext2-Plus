use std::{collections::HashMap, path::Path};

use serde_json::Value;

use crate::{safe_zip, IconData, Limits, PackageError, PackageFormat, PackageInfo, Result};

struct HapMeta {
    root: Value,
    strings: HashMap<String, String>,
    media_by_id: HashMap<u32, String>,
}

pub(crate) fn inspect(path: &Path, limits: Limits) -> Result<PackageInfo> {
    let mut archive = safe_zip::open(path, limits)?;
    let meta = read_meta(&mut archive, limits)?;
    let app = meta.root.get("app").and_then(Value::as_object);
    let ability = meta
        .root
        .pointer("/module/abilities/0")
        .and_then(Value::as_object);
    let label = object_string(app, "label").or_else(|| object_string(ability, "label"));
    let app_name = resolve_string(label.unwrap_or_default(), &meta.strings);
    let icon_ref = object_string(app, "icon").or_else(|| object_string(ability, "icon"));
    let icon_source = icon_ref
        .and_then(|v| resolve_icon_name(&mut archive, &meta, v).ok().flatten())
        .or_else(|| fallback_icon(&mut archive).ok().flatten())
        .map(|(_, name)| name);
    Ok(PackageInfo {
        format: PackageFormat::Hap,
        package_id: object_string(app, "bundleName")
            .unwrap_or_default()
            .to_owned(),
        app_name,
        version_name: object_string(app, "versionName")
            .unwrap_or_default()
            .to_owned(),
        version_code: object_value_string(app, "versionCode"),
        icon_source,
    })
}

pub(crate) fn extract_icon(path: &Path, limits: Limits) -> Result<IconData> {
    let mut archive = safe_zip::open(path, limits)?;
    let meta = read_meta(&mut archive, limits)?;
    let app = meta.root.get("app").and_then(Value::as_object);
    let ability = meta
        .root
        .pointer("/module/abilities/0")
        .and_then(Value::as_object);
    let icon_ref = object_string(app, "icon").or_else(|| object_string(ability, "icon"));
    let resolved = match icon_ref {
        Some(v) => resolve_icon_name(&mut archive, &meta, v)?,
        None => None,
    }
    .or_else(|| fallback_icon(&mut archive).ok().flatten())
    .ok_or(PackageError::IconNotFound)?;
    let bytes = safe_zip::read_index(&mut archive, resolved.0, limits)?;
    Ok(IconData {
        bytes,
        media_type: safe_zip::media_type(&resolved.1),
        source: resolved.1,
    })
}

fn read_meta<R: std::io::Read + std::io::Seek>(
    archive: &mut zip::ZipArchive<R>,
    limits: Limits,
) -> Result<HapMeta> {
    let module_index = safe_zip::find_ordinal_ignore_case(archive, "module.json")
        .ok_or_else(|| PackageError::EntryNotFound("module.json".into()))?;
    let bytes = safe_zip::read_index(archive, module_index, limits)?;
    let root: Value = serde_json::from_slice(&bytes)?;
    if !root.get("app").map(Value::is_object).unwrap_or(false) {
        return Err(PackageError::InvalidData(
            "module.json has no app object".into(),
        ));
    }
    let mut strings = HashMap::new();
    let mut media_by_id = HashMap::new();
    if let Some(index) = safe_zip::find_ordinal_ignore_case(archive, "resources.index") {
        let bytes = safe_zip::read_index(archive, index, limits)?;
        parse_resource_index(&bytes, &mut strings, &mut media_by_id);
    }
    Ok(HapMeta {
        root,
        strings,
        media_by_id,
    })
}

fn parse_resource_index(
    bytes: &[u8],
    strings: &mut HashMap<String, String>,
    media_by_id: &mut HashMap<u32, String>,
) {
    if bytes.len() < 136 {
        return;
    }
    let Some(group_count) = u32_at(bytes, 132) else {
        return;
    };
    let mut offset = 136usize;
    for _ in 0..group_count.min(4096) {
        if offset
            .checked_add(12)
            .map(|end| end > bytes.len())
            .unwrap_or(true)
        {
            break;
        }
        let Some(id_set_offset) = u32_at(bytes, offset + 4).map(|v| v as usize) else {
            break;
        };
        let Some(key_params) = u32_at(bytes, offset + 8).map(|v| v as usize) else {
            break;
        };
        offset = match offset
            .checked_add(12)
            .and_then(|v| v.checked_add(key_params.saturating_mul(8)))
        {
            Some(v) => v,
            None => break,
        };
        let Some(count_offset) = id_set_offset.checked_add(4) else {
            continue;
        };
        let Some(id_count) = u32_at(bytes, count_offset) else {
            continue;
        };
        let mut cursor = id_set_offset.saturating_add(8);
        for _ in 0..id_count.min(65_536) {
            let Some(id) = u32_at(bytes, cursor) else {
                break;
            };
            let Some(data_offset) = u32_at(bytes, cursor + 4).map(|v| v as usize) else {
                break;
            };
            cursor = cursor.saturating_add(8);
            parse_id_item(bytes, data_offset, id, strings, media_by_id);
        }
    }
}

fn parse_id_item(
    bytes: &[u8],
    offset: usize,
    id: u32,
    strings: &mut HashMap<String, String>,
    media_by_id: &mut HashMap<u32, String>,
) {
    let Some(resource_type) = i32_at(bytes, offset + 4) else {
        return;
    };
    let Some(value_len) = u16_at(bytes, offset + 12).map(|v| v as usize) else {
        return;
    };
    if !(2..=8192).contains(&value_len) {
        return;
    }
    let value_start = offset.saturating_add(14);
    let value_end = value_start.saturating_add(value_len);
    if value_end + 2 > bytes.len() || bytes[value_end - 1] != 0 {
        return;
    }
    let Ok(value) = std::str::from_utf8(&bytes[value_start..value_end - 1]) else {
        return;
    };
    let Some(name_len) = u16_at(bytes, value_end).map(|v| v as usize) else {
        return;
    };
    if !(2..=1024).contains(&name_len) {
        return;
    }
    let name_start = value_end + 2;
    let name_end = name_start.saturating_add(name_len);
    if name_end > bytes.len() || bytes[name_end - 1] != 0 {
        return;
    }
    let Ok(name) = std::str::from_utf8(&bytes[name_start..name_end - 1]) else {
        return;
    };
    if resource_type == 9 {
        strings
            .entry(name.to_owned())
            .or_insert_with(|| value.to_owned());
    }
    media_by_id.entry(id).or_insert_with(|| value.to_owned());
}

fn resolve_icon_name<R: std::io::Read + std::io::Seek>(
    archive: &mut zip::ZipArchive<R>,
    meta: &HapMeta,
    icon_ref: &str,
) -> Result<Option<(usize, String)>> {
    let Some(name) = icon_ref.strip_prefix("$media:") else {
        return Ok(None);
    };
    if let Ok(id) = name.parse::<u32>() {
        if let Some(path) = meta.media_by_id.get(&id) {
            return find_media_path(archive, path);
        }
    }
    let media = media_entries(archive)?;
    let lower = name.to_ascii_lowercase();
    if let Some((index, entry)) = media.iter().find(|(_, entry)| media_stem(entry) == lower) {
        if entry.to_ascii_lowercase().ends_with(".json") {
            return resolve_layered(archive, meta, *index);
        }
        return Ok(Some((*index, entry.clone())));
    }
    Ok(None)
}

fn resolve_layered<R: std::io::Read + std::io::Seek>(
    archive: &mut zip::ZipArchive<R>,
    meta: &HapMeta,
    index: usize,
) -> Result<Option<(usize, String)>> {
    let bytes = safe_zip::read_index(archive, index, Limits::default())?;
    let value: Value = serde_json::from_slice(&bytes)?;
    for key in ["foreground", "background"] {
        if let Some(reference) = value
            .pointer(&format!("/layered-image/{key}"))
            .and_then(Value::as_str)
        {
            if let Some(found) = resolve_icon_name(archive, meta, reference)? {
                return Ok(Some(found));
            }
        }
    }
    Ok(None)
}

fn fallback_icon<R: std::io::Read + std::io::Seek>(
    archive: &mut zip::ZipArchive<R>,
) -> Result<Option<(usize, String)>> {
    let media = media_entries(archive)?;
    for name in ["foreground", "icon", "app_icon", "starticon"] {
        if let Some((index, entry)) = media.iter().find(|(_, entry)| media_stem(entry) == name) {
            return Ok(Some((*index, entry.clone())));
        }
    }
    Ok(media.into_iter().find(|(_, entry)| is_image(entry)))
}

fn media_entries<R: std::io::Read + std::io::Seek>(
    archive: &mut zip::ZipArchive<R>,
) -> Result<Vec<(usize, String)>> {
    let mut out = Vec::new();
    for i in 0..archive.len() {
        let entry = archive.by_index(i)?;
        let name = entry.name().to_owned();
        let lower = name.to_ascii_lowercase();
        if (lower.starts_with("resources/") || lower.contains("/resources/"))
            && lower.contains("/media/")
            && (is_image(&lower) || lower.ends_with(".json"))
        {
            out.push((i, name));
        }
    }
    Ok(out)
}

fn find_media_path<R: std::io::Read + std::io::Seek>(
    archive: &mut zip::ZipArchive<R>,
    path: &str,
) -> Result<Option<(usize, String)>> {
    let normalized = path
        .find("/resources/")
        .map(|i| &path[i + 1..])
        .unwrap_or(path);
    if let Some(i) = safe_zip::find_ordinal_ignore_case(archive, normalized) {
        return Ok(Some((i, normalized.to_owned())));
    }
    let suffix = path.to_ascii_lowercase();
    for i in 0..archive.len() {
        let entry = archive.by_index(i)?;
        if entry.name().to_ascii_lowercase().ends_with(&suffix) {
            return Ok(Some((i, entry.name().to_owned())));
        }
    }
    Ok(None)
}

fn resolve_string(value: &str, strings: &HashMap<String, String>) -> String {
    if let Some(name) = value.strip_prefix("$string:") {
        strings.get(name).cloned().unwrap_or_default()
    } else if value.starts_with('$') {
        String::new()
    } else {
        value.to_owned()
    }
}

fn object_string<'a>(
    object: Option<&'a serde_json::Map<String, Value>>,
    key: &str,
) -> Option<&'a str> {
    object?.get(key)?.as_str()
}

fn object_value_string(object: Option<&serde_json::Map<String, Value>>, key: &str) -> String {
    object
        .and_then(|o| o.get(key))
        .map(|v| match v {
            Value::String(v) => v.clone(),
            Value::Number(v) => v.to_string(),
            _ => String::new(),
        })
        .unwrap_or_default()
}

fn media_stem(name: &str) -> String {
    name.rsplit('/')
        .next()
        .unwrap_or(name)
        .rsplit_once('.')
        .map(|v| v.0)
        .unwrap_or(name)
        .to_ascii_lowercase()
}

fn is_image(name: &str) -> bool {
    let lower = name.to_ascii_lowercase();
    [".png", ".webp", ".jpg", ".jpeg"]
        .iter()
        .any(|e| lower.ends_with(e))
}

fn u16_at(bytes: &[u8], offset: usize) -> Option<u16> {
    Some(u16::from_le_bytes(
        bytes.get(offset..offset + 2)?.try_into().ok()?,
    ))
}
fn u32_at(bytes: &[u8], offset: usize) -> Option<u32> {
    Some(u32::from_le_bytes(
        bytes.get(offset..offset + 4)?.try_into().ok()?,
    ))
}
fn i32_at(bytes: &[u8], offset: usize) -> Option<i32> {
    Some(i32::from_le_bytes(
        bytes.get(offset..offset + 4)?.try_into().ok()?,
    ))
}
