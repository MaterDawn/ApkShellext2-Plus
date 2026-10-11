use std::{io::Cursor, path::Path};

use quick_xml::{events::Event, Reader};

use crate::{safe_zip, IconData, Limits, PackageError, PackageFormat, PackageInfo, Result};

#[derive(Default)]
struct Manifest {
    name: String,
    version: String,
    display_name: String,
    logo: String,
}

pub(crate) fn inspect(path: &Path, limits: Limits, bundle: bool) -> Result<PackageInfo> {
    let (manifest, icon_source) = if bundle {
        read_bundle(path, limits, false)?
    } else {
        let mut archive = safe_zip::open(path, limits)?;
        let manifest = read_appx_manifest(&mut archive, limits)?;
        let icon_source = find_logo(&mut archive, &manifest.logo)?.map(|(_, n)| n);
        (manifest, icon_source)
    };
    Ok(PackageInfo {
        format: if bundle {
            PackageFormat::AppxBundle
        } else {
            PackageFormat::Appx
        },
        package_id: manifest.name,
        app_name: manifest.display_name,
        version_name: manifest.version.clone(),
        version_code: manifest.version,
        icon_source,
    })
}

pub(crate) fn extract_icon(path: &Path, limits: Limits, bundle: bool) -> Result<IconData> {
    if bundle {
        let (manifest, _) = read_bundle(path, limits, true)?;
        let mut outer = safe_zip::open(path, limits)?;
        let nested_name = bundle_application_name(&mut outer, limits)?;
        let nested_index = safe_zip::find_ordinal_ignore_case(&mut outer, &nested_name)
            .ok_or_else(|| PackageError::EntryNotFound(nested_name.clone()))?;
        let nested = safe_zip::read_index(&mut outer, nested_index, limits)?;
        let mut inner = zip::ZipArchive::new(Cursor::new(nested))?;
        validate_archive(&mut inner, limits)?;
        let (index, source) =
            find_logo(&mut inner, &manifest.logo)?.ok_or(PackageError::IconNotFound)?;
        let bytes = safe_zip::read_index(&mut inner, index, limits)?;
        return Ok(IconData {
            bytes,
            media_type: safe_zip::media_type(&source),
            source: format!("{nested_name}!/{source}"),
        });
    }
    let mut archive = safe_zip::open(path, limits)?;
    let manifest = read_appx_manifest(&mut archive, limits)?;
    let (index, source) =
        find_logo(&mut archive, &manifest.logo)?.ok_or(PackageError::IconNotFound)?;
    let bytes = safe_zip::read_index(&mut archive, index, limits)?;
    Ok(IconData {
        bytes,
        media_type: safe_zip::media_type(&source),
        source,
    })
}

fn read_bundle(path: &Path, limits: Limits, _for_icon: bool) -> Result<(Manifest, Option<String>)> {
    let mut outer = safe_zip::open(path, limits)?;
    let nested_name = bundle_application_name(&mut outer, limits)?;
    let nested_index = safe_zip::find_ordinal_ignore_case(&mut outer, &nested_name)
        .ok_or_else(|| PackageError::EntryNotFound(nested_name.clone()))?;
    let nested = safe_zip::read_index(&mut outer, nested_index, limits)?;
    let mut inner = zip::ZipArchive::new(Cursor::new(nested))?;
    validate_archive(&mut inner, limits)?;
    let manifest = read_appx_manifest(&mut inner, limits)?;
    let icon =
        find_logo(&mut inner, &manifest.logo)?.map(|(_, name)| format!("{nested_name}!/{name}"));
    Ok((manifest, icon))
}

fn validate_archive<R: std::io::Read + std::io::Seek>(
    archive: &mut zip::ZipArchive<R>,
    limits: Limits,
) -> Result<()> {
    if archive.len() > limits.max_entries {
        return Err(PackageError::TooManyEntries {
            count: archive.len(),
            limit: limits.max_entries,
        });
    }
    let mut total = 0u64;
    for i in 0..archive.len() {
        let entry = archive.by_index(i)?;
        if entry.size() > limits.max_entry_size {
            return Err(PackageError::EntryTooLarge {
                name: entry.name().to_owned(),
                size: entry.size(),
                limit: limits.max_entry_size,
            });
        }
        total = total.saturating_add(entry.size());
        if total > limits.max_declared_size {
            return Err(PackageError::ArchiveTooLarge {
                size: total,
                limit: limits.max_declared_size,
            });
        }
    }
    Ok(())
}

fn read_appx_manifest<R: std::io::Read + std::io::Seek>(
    archive: &mut zip::ZipArchive<R>,
    limits: Limits,
) -> Result<Manifest> {
    let index = safe_zip::find_ordinal_ignore_case(archive, "AppxManifest.xml")
        .ok_or_else(|| PackageError::EntryNotFound("AppxManifest.xml".into()))?;
    let bytes = safe_zip::read_index(archive, index, limits)?;
    parse_manifest(&bytes)
}

fn parse_manifest(bytes: &[u8]) -> Result<Manifest> {
    let mut reader = Reader::from_reader(bytes);
    reader.config_mut().trim_text(true);
    let mut manifest = Manifest::default();
    let mut current = String::new();
    loop {
        match reader.read_event() {
            Ok(Event::Start(event)) | Ok(Event::Empty(event)) => {
                let local = local_name(event.name().as_ref());
                if local == "Identity" {
                    for attr in event.attributes().flatten() {
                        let key = local_name(attr.key.as_ref());
                        let value = attr.value.as_ref().to_owned();
                        if key == "Name" {
                            manifest.name = value;
                        } else if key == "Version" {
                            manifest.version = value;
                        }
                    }
                }
                current = local;
            }
            Ok(Event::Text(text)) => {
                let value = text.as_ref();
                if current == "DisplayName" {
                    manifest.display_name = value.to_owned();
                } else if current == "Logo" {
                    manifest.logo = value.replace('\\', "/");
                }
            }
            Ok(Event::End(_)) => current.clear(),
            Ok(Event::Eof) => break,
            Err(e) => return Err(PackageError::InvalidData(e.to_string())),
            _ => {}
        }
    }
    if manifest.name.is_empty() {
        return Err(PackageError::InvalidData(
            "AppxManifest has no Identity/Name".into(),
        ));
    }
    Ok(manifest)
}

fn bundle_application_name<R: std::io::Read + std::io::Seek>(
    archive: &mut zip::ZipArchive<R>,
    limits: Limits,
) -> Result<String> {
    let index = safe_zip::find_ordinal_ignore_case(archive, "AppxMetadata/AppxBundleManifest.xml")
        .ok_or_else(|| PackageError::EntryNotFound("AppxMetadata/AppxBundleManifest.xml".into()))?;
    let bytes = safe_zip::read_index(archive, index, limits)?;
    let mut reader = Reader::from_reader(bytes.as_slice());
    loop {
        match reader.read_event() {
            Ok(Event::Start(event)) | Ok(Event::Empty(event))
                if local_name(event.name().as_ref()) == "Package" =>
            {
                let mut file = None;
                let mut package_type = None;
                for attr in event.attributes().flatten() {
                    let key = local_name(attr.key.as_ref());
                    let value = attr.value.as_ref().to_owned();
                    if key == "FileName" {
                        file = Some(value);
                    } else if key == "Type" {
                        package_type = Some(value);
                    }
                }
                if package_type
                    .as_deref()
                    .map(|v| v.eq_ignore_ascii_case("application"))
                    == Some(true)
                {
                    return file.ok_or_else(|| {
                        PackageError::InvalidData("application package has no FileName".into())
                    });
                }
            }
            Ok(Event::Eof) => break,
            Err(e) => return Err(PackageError::InvalidData(e.to_string())),
            _ => {}
        }
    }
    Err(PackageError::EntryNotFound(
        "application package in bundle manifest".into(),
    ))
}

fn find_logo<R: std::io::Read + std::io::Seek>(
    archive: &mut zip::ZipArchive<R>,
    declared: &str,
) -> Result<Option<(usize, String)>> {
    if declared.is_empty() {
        return Ok(None);
    }
    let declared = declared.replace('\\', "/");
    if let Some(index) = safe_zip::find_ordinal_ignore_case(archive, &declared) {
        return Ok(Some((index, declared)));
    }
    let (stem, ext) = declared.rsplit_once('.').unwrap_or((&declared, "png"));
    let stem = stem.to_ascii_lowercase();
    let ext = format!(".{ext}").to_ascii_lowercase();
    let mut candidates = Vec::new();
    for i in 0..archive.len() {
        let entry = archive.by_index(i)?;
        let name = entry.name().to_owned();
        let lower = name.to_ascii_lowercase();
        if lower.ends_with(&ext) && lower.starts_with(&stem) {
            let scale = lower
                .strip_prefix(&format!("{stem}.scale-"))
                .and_then(|v| v.strip_suffix(&ext))
                .and_then(|v| v.parse::<u32>().ok())
                .unwrap_or(0);
            candidates.push((scale, i, name));
        }
    }
    candidates.sort_by_key(|a| std::cmp::Reverse(a.0));
    Ok(candidates.into_iter().next().map(|(_, i, n)| (i, n)))
}

fn local_name(name: &str) -> String {
    name.rsplit(':').next().unwrap_or(name).to_owned()
}
