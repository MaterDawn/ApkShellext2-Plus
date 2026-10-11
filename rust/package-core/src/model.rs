use serde::Serialize;

#[derive(Clone, Copy, Debug, Serialize)]
#[serde(rename_all = "lowercase")]
pub enum PackageFormat {
    Ipa,
    Tipa,
    Hap,
    Appx,
    AppxBundle,
}

#[derive(Clone, Debug, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct PackageInfo {
    pub format: PackageFormat,
    pub package_id: String,
    pub app_name: String,
    pub version_name: String,
    pub version_code: String,
    pub icon_source: Option<String>,
}

#[derive(Clone, Debug)]
pub struct IconData {
    pub bytes: Vec<u8>,
    pub source: String,
    pub media_type: &'static str,
}
