mod error;
mod hap;
mod ipa;
mod limits;
mod model;
mod safe_zip;

use std::path::Path;

pub use error::{PackageError, Result};
pub use limits::Limits;
pub use model::{IconData, PackageFormat, PackageInfo};

pub fn inspect(path: impl AsRef<Path>) -> Result<PackageInfo> {
    inspect_with_limits(path, Limits::default())
}

pub fn inspect_with_limits(path: impl AsRef<Path>, limits: Limits) -> Result<PackageInfo> {
    let path = path.as_ref();
    let ext = path
        .extension()
        .and_then(|s| s.to_str())
        .unwrap_or_default()
        .to_ascii_lowercase();
    match ext.as_str() {
        "ipa" | "tipa" => ipa::inspect(path, limits),
        "hap" => hap::inspect(path, limits),
        _ => Err(PackageError::UnsupportedFormat(ext)),
    }
}

pub fn extract_icon(path: impl AsRef<Path>) -> Result<IconData> {
    extract_icon_with_limits(path, Limits::default())
}

pub fn extract_icon_with_limits(path: impl AsRef<Path>, limits: Limits) -> Result<IconData> {
    let path = path.as_ref();
    let ext = path
        .extension()
        .and_then(|s| s.to_str())
        .unwrap_or_default()
        .to_ascii_lowercase();
    match ext.as_str() {
        "ipa" | "tipa" => ipa::extract_icon(path, limits),
        "hap" => hap::extract_icon(path, limits),
        _ => Err(PackageError::UnsupportedFormat(ext)),
    }
}
