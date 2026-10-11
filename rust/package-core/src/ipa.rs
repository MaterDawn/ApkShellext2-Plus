use std::{collections::BTreeSet, io::Cursor, path::Path};

use plist::{Dictionary, Value};

use crate::{safe_zip, IconData, Limits, PackageError, PackageFormat, PackageInfo, Result};

struct IpaMeta {
    root: String,
    plist: Dictionary,
    format: PackageFormat,
}

pub(crate) fn inspect(path: &Path, limits: Limits) -> Result<PackageInfo> {
    let mut archive = safe_zip::open(path, limits)?;
    let meta = read_meta(path, &mut archive, limits)?;
    let icon_source = resolve_icon_index(&mut archive, &meta)?.map(|(_, name)| name);
    Ok(PackageInfo {
        format: meta.format,
        package_id: string(&meta.plist, "CFBundleIdentifier"),
        app_name: first_non_empty([
            string(&meta.plist, "CFBundleDisplayName"),
            string(&meta.plist, "CFBundleName"),
            string(&meta.plist, "FacebookDisplayName"),
        ]),
        version_name: string(&meta.plist, "CFBundleShortVersionString"),
        version_code: string(&meta.plist, "CFBundleVersion"),
        icon_source,
    })
}

pub(crate) fn extract_icon(path: &Path, limits: Limits) -> Result<IconData> {
    let mut archive = safe_zip::open(path, limits)?;
    let meta = read_meta(path, &mut archive, limits)?;
    let (index, source) =
        resolve_icon_index(&mut archive, &meta)?.ok_or(PackageError::IconNotFound)?;
    let bytes = safe_zip::read_index(&mut archive, index, limits)?;
    Ok(IconData {
        bytes,
        media_type: safe_zip::media_type(&source),
        source,
    })
}

fn read_meta<R: std::io::Read + std::io::Seek>(
    path: &Path,
    archive: &mut zip::ZipArchive<R>,
    limits: Limits,
) -> Result<IpaMeta> {
    let mut best: Option<(usize, String, usize)> = None;
    for i in 0..archive.len() {
        let entry = archive.by_index(i)?;
        let name = entry.name();
        let lower = name.to_ascii_lowercase();
        if lower.starts_with("payload/") && lower.ends_with(".app/info.plist") {
            let root_len = name.len() - "Info.plist".len();
            if best.as_ref().map(|b| root_len < b.2).unwrap_or(true) {
                best = Some((i, name[..root_len].to_owned(), root_len));
            }
        }
    }
    let (index, root, _) =
        best.ok_or_else(|| PackageError::EntryNotFound("Payload/*.app/Info.plist".into()))?;
    let bytes = safe_zip::read_index(archive, index, limits)?;
    let value = Value::from_reader(Cursor::new(bytes))?;
    let plist = value
        .into_dictionary()
        .ok_or_else(|| PackageError::InvalidData("Info.plist root is not a dictionary".into()))?;
    let format = if path
        .extension()
        .and_then(|s| s.to_str())
        .unwrap_or_default()
        .eq_ignore_ascii_case("tipa")
    {
        PackageFormat::Tipa
    } else {
        PackageFormat::Ipa
    };
    Ok(IpaMeta {
        root,
        plist,
        format,
    })
}

fn resolve_icon_index<R: std::io::Read + std::io::Seek>(
    archive: &mut zip::ZipArchive<R>,
    meta: &IpaMeta,
) -> Result<Option<(usize, String)>> {
    let candidates = icon_candidates(&meta.plist);
    for base in candidates {
        for variant in variants(&base) {
            let full = format!("{}{}", meta.root, variant);
            if let Some(index) = safe_zip::find_ordinal_ignore_case(archive, &full) {
                return Ok(Some((index, full)));
            }
        }
    }

    let root = meta.root.to_ascii_lowercase();
    let mut fallback: Vec<(u8, usize, String)> = Vec::new();
    for i in 0..archive.len() {
        let entry = archive.by_index(i)?;
        let name = entry.name().to_owned();
        let lower = name.to_ascii_lowercase();
        if !lower.starts_with(&root) || !is_image(&lower) {
            continue;
        }
        let score = if lower.contains("appicon") {
            0
        } else if lower.contains("icon") {
            1
        } else {
            2
        };
        fallback.push((score, i, name));
    }
    fallback.sort_by(|a, b| a.0.cmp(&b.0).then_with(|| b.2.len().cmp(&a.2.len())));
    Ok(fallback.into_iter().next().map(|(_, i, n)| (i, n)))
}

fn icon_candidates(plist: &Dictionary) -> Vec<String> {
    let mut out = Vec::new();
    for scope in ["CFBundleIcons", "CFBundleIcons~ipad"] {
        if let Some(primary) = plist
            .get(scope)
            .and_then(Value::as_dictionary)
            .and_then(|v| v.get("CFBundlePrimaryIcon"))
            .and_then(Value::as_dictionary)
        {
            add_value(&mut out, primary.get("CFBundleIconFiles"));
            add_value(&mut out, primary.get("CFBundleIconFile"));
            add_value(&mut out, primary.get("CFBundleIconName"));
        }
    }
    add_value(&mut out, plist.get("CFBundleIconFiles"));
    add_value(&mut out, plist.get("CFBundleIconFile"));
    add_value(&mut out, plist.get("CFBundleIconName"));
    out.extend(["AppIcon".into(), "Icon".into()]);
    let mut seen = BTreeSet::new();
    out.retain(|v| seen.insert(v.to_ascii_lowercase()));
    out
}

fn add_value(out: &mut Vec<String>, value: Option<&Value>) {
    match value {
        Some(Value::String(v)) if !v.is_empty() => out.push(v.clone()),
        Some(Value::Array(values)) => {
            for value in values {
                if let Some(v) = value.as_string() {
                    if !v.is_empty() {
                        out.push(v.to_owned());
                    }
                }
            }
        }
        _ => {}
    }
}

fn variants(name: &str) -> Vec<String> {
    let lower = name.to_ascii_lowercase();
    let has_image_ext = [".png", ".jpg", ".jpeg"].iter().any(|e| lower.ends_with(e));
    let has_scale = ["@1x", "@2x", "@3x"].iter().any(|s| lower.contains(s));
    let mut out = if has_image_ext {
        vec![name.to_owned()]
    } else if has_scale {
        vec![format!("{name}.png")]
    } else {
        vec![
            format!("{name}@3x.png"),
            format!("{name}@2x.png"),
            format!("{name}@1x.png"),
            format!("{name}.png"),
        ]
    };
    let ipad: Vec<String> = out
        .iter()
        .filter_map(|v| v.rsplit_once('.').map(|(a, b)| format!("{a}~ipad.{b}")))
        .collect();
    out.extend(ipad);
    out
}

fn string(plist: &Dictionary, key: &str) -> String {
    plist
        .get(key)
        .and_then(|v| match v {
            Value::String(v) => Some(v.clone()),
            Value::Integer(v) => Some(v.to_string()),
            _ => None,
        })
        .unwrap_or_default()
}

fn first_non_empty<const N: usize>(values: [String; N]) -> String {
    values
        .into_iter()
        .find(|v| !v.is_empty())
        .unwrap_or_default()
}

fn is_image(name: &str) -> bool {
    [".png", ".jpg", ".jpeg", ".webp"]
        .iter()
        .any(|e| name.ends_with(e))
}
