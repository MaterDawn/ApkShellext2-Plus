use std::{fs::File, io::Read, path::Path};

use zip::ZipArchive;

use crate::{Limits, PackageError, Result};

pub(crate) fn open(path: &Path, limits: Limits) -> Result<ZipArchive<File>> {
    let file = File::open(path).map_err(|e| PackageError::io(path, e))?;
    let mut archive = ZipArchive::new(file)?;
    if archive.len() > limits.max_entries {
        return Err(PackageError::TooManyEntries {
            count: archive.len(),
            limit: limits.max_entries,
        });
    }
    let mut declared = 0u64;
    for i in 0..archive.len() {
        let entry = archive.by_index(i)?;
        if entry.size() > limits.max_entry_size {
            return Err(PackageError::EntryTooLarge {
                name: entry.name().to_owned(),
                size: entry.size(),
                limit: limits.max_entry_size,
            });
        }
        declared = declared.saturating_add(entry.size());
        if declared > limits.max_declared_size {
            return Err(PackageError::ArchiveTooLarge {
                size: declared,
                limit: limits.max_declared_size,
            });
        }
    }
    Ok(archive)
}

pub(crate) fn find_ordinal_ignore_case<R: std::io::Read + std::io::Seek>(
    archive: &mut ZipArchive<R>,
    name: &str,
) -> Option<usize> {
    let target = name.to_ascii_lowercase();
    (0..archive.len()).find(|&i| {
        archive
            .by_index(i)
            .map(|e| e.name().to_ascii_lowercase() == target)
            .unwrap_or(false)
    })
}

pub(crate) fn read_index<R: std::io::Read + std::io::Seek>(
    archive: &mut ZipArchive<R>,
    index: usize,
    limits: Limits,
) -> Result<Vec<u8>> {
    let mut entry = archive.by_index(index)?;
    let name = entry.name().to_owned();
    if entry.size() > limits.max_entry_size {
        return Err(PackageError::EntryTooLarge {
            name,
            size: entry.size(),
            limit: limits.max_entry_size,
        });
    }
    let mut out = Vec::with_capacity(entry.size() as usize);
    (&mut entry)
        .take(limits.max_entry_size + 1)
        .read_to_end(&mut out)
        .map_err(|e| PackageError::InvalidData(e.to_string()))?;
    if out.len() as u64 > limits.max_entry_size {
        return Err(PackageError::EntryTooLarge {
            name,
            size: out.len() as u64,
            limit: limits.max_entry_size,
        });
    }
    Ok(out)
}

pub(crate) fn media_type(name: &str) -> &'static str {
    let lower = name.to_ascii_lowercase();
    if lower.ends_with(".webp") {
        "image/webp"
    } else if lower.ends_with(".jpg") || lower.ends_with(".jpeg") {
        "image/jpeg"
    } else {
        "image/png"
    }
}
