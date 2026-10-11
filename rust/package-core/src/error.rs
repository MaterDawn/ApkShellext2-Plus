use std::path::PathBuf;

pub type Result<T> = std::result::Result<T, PackageError>;

#[derive(Debug, thiserror::Error)]
pub enum PackageError {
    #[error("unsupported package format: {0}")]
    UnsupportedFormat(String),
    #[error("required entry not found: {0}")]
    EntryNotFound(String),
    #[error("archive contains too many entries: {count} > {limit}")]
    TooManyEntries { count: usize, limit: usize },
    #[error("archive entry is too large: {name} ({size} > {limit})")]
    EntryTooLarge { name: String, size: u64, limit: u64 },
    #[error("archive declared size is too large: {size} > {limit}")]
    ArchiveTooLarge { size: u64, limit: u64 },
    #[error("invalid package data: {0}")]
    InvalidData(String),
    #[error("icon not found")]
    IconNotFound,
    #[error("I/O error for {path}: {source}")]
    Io {
        path: PathBuf,
        #[source]
        source: std::io::Error,
    },
    #[error(transparent)]
    Zip(#[from] zip::result::ZipError),
    #[error(transparent)]
    Plist(#[from] plist::Error),
    #[error(transparent)]
    Json(#[from] serde_json::Error),
}

impl PackageError {
    pub(crate) fn io(path: impl Into<PathBuf>, source: std::io::Error) -> Self {
        Self::Io { path: path.into(), source }
    }
}
