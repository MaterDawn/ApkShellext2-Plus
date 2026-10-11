use std::{fs, io::Write, path::Path};

use package_icon_core::{
    extract_icon, inspect, inspect_with_limits, Limits, PackageError, PackageFormat,
};
use plist::{Dictionary, Value};
use zip::{write::SimpleFileOptions, ZipWriter};

fn temp_package(ext: &str, name: &str) -> std::path::PathBuf {
    let root = std::env::temp_dir().join(format!(
        "package-icon-tests-{}-{}",
        std::process::id(),
        name
    ));
    fs::create_dir_all(&root).unwrap();
    root.join(format!("fixture.{ext}"))
}

fn write_zip(path: &Path, files: &[(&str, &[u8])]) {
    let file = fs::File::create(path).unwrap();
    let mut zip = ZipWriter::new(file);
    for (name, bytes) in files {
        zip.start_file(*name, SimpleFileOptions::default()).unwrap();
        zip.write_all(bytes).unwrap();
    }
    zip.finish().unwrap();
}

fn ipa_plist_bytes(binary: bool) -> Vec<u8> {
    let mut plist = Dictionary::new();
    plist.insert(
        "CFBundleIdentifier".into(),
        Value::String("com.example.main".into()),
    );
    plist.insert(
        "CFBundleDisplayName".into(),
        Value::String("Main App".into()),
    );
    plist.insert(
        "CFBundleShortVersionString".into(),
        Value::String("1.2.3".into()),
    );
    plist.insert("CFBundleVersion".into(), Value::String("42".into()));
    plist.insert(
        "CFBundleIconFiles".into(),
        Value::Array(vec![Value::String("AppIcon60x60".into())]),
    );
    let mut out = Vec::new();
    if binary {
        Value::Dictionary(plist).to_writer_binary(&mut out).unwrap();
    } else {
        Value::Dictionary(plist).to_writer_xml(&mut out).unwrap();
    }
    out
}

#[test]
fn inspects_xml_ipa_and_chooses_main_bundle() {
    let path = temp_package("ipa", "xml-ipa");
    let plist = ipa_plist_bytes(false);
    let extension_plist = b"<?xml version=\"1.0\"?><plist version=\"1.0\"><dict><key>CFBundleIdentifier</key><string>com.example.watch</string></dict></plist>";
    let icon = b"\x89PNG\r\n\x1a\nfixture";
    write_zip(
        &path,
        &[
            ("Payload/Main.app/Info.plist", &plist),
            (
                "Payload/Main.app/PlugIns/Watch.app/Info.plist",
                extension_plist,
            ),
            ("payload/main.app/APPICON60X60@3X.PNG", icon),
        ],
    );

    let info = inspect(&path).unwrap();
    assert!(matches!(info.format, PackageFormat::Ipa));
    assert_eq!(info.package_id, "com.example.main");
    assert_eq!(info.app_name, "Main App");
    assert_eq!(info.version_name, "1.2.3");
    assert_eq!(info.version_code, "42");
    assert_eq!(
        info.icon_source.as_deref(),
        Some("Payload/Main.app/AppIcon60x60@3x.png")
    );
    let extracted = extract_icon(&path).unwrap();
    assert_eq!(extracted.bytes, icon);
    assert_eq!(extracted.media_type, "image/png");
    fs::remove_dir_all(path.parent().unwrap()).ok();
}

#[test]
fn inspects_binary_tipa() {
    let path = temp_package("tipa", "binary-tipa");
    let plist = ipa_plist_bytes(true);
    write_zip(
        &path,
        &[
            ("Payload/Main.app/Info.plist", &plist),
            ("Payload/Main.app/AppIcon60x60@2x.png", b"icon"),
        ],
    );
    let info = inspect(&path).unwrap();
    assert!(matches!(info.format, PackageFormat::Tipa));
    assert_eq!(info.package_id, "com.example.main");
    fs::remove_dir_all(path.parent().unwrap()).ok();
}

#[test]
fn inspects_hap_and_extracts_direct_icon() {
    let path = temp_package("hap", "direct-hap");
    let module = br#"{"app":{"bundleName":"com.example.hap","label":"Harmony App","versionName":"2.0","versionCode":7,"icon":"$media:app_icon"}}"#;
    let icon = b"RIFFfixtureWEBP";
    write_zip(
        &path,
        &[
            ("module.json", module),
            ("resources/base/media/app_icon.webp", icon),
        ],
    );
    let info = inspect(&path).unwrap();
    assert!(matches!(info.format, PackageFormat::Hap));
    assert_eq!(info.package_id, "com.example.hap");
    assert_eq!(info.app_name, "Harmony App");
    assert_eq!(info.version_name, "2.0");
    assert_eq!(info.version_code, "7");
    let extracted = extract_icon(&path).unwrap();
    assert_eq!(extracted.bytes, icon);
    assert_eq!(extracted.media_type, "image/webp");
    fs::remove_dir_all(path.parent().unwrap()).ok();
}

#[test]
fn rejects_archive_with_too_many_entries() {
    let path = temp_package("hap", "limit");
    write_zip(
        &path,
        &[
            ("module.json", br#"{"app":{"bundleName":"x"}}"#),
            ("extra", b"x"),
        ],
    );
    let error = inspect_with_limits(
        &path,
        Limits {
            max_entries: 1,
            ..Limits::default()
        },
    )
    .unwrap_err();
    assert!(matches!(error, PackageError::TooManyEntries { .. }));
    fs::remove_dir_all(path.parent().unwrap()).ok();
}

#[test]
fn rejects_missing_hap_module() {
    let path = temp_package("hap", "missing-module");
    write_zip(&path, &[("other.json", b"{}")]);
    let error = inspect(&path).unwrap_err();
    assert!(matches!(error, PackageError::EntryNotFound(_)));
    fs::remove_dir_all(path.parent().unwrap()).ok();
}

fn appx_manifest() -> &'static [u8] {
    br#"<?xml version="1.0" encoding="utf-8"?>
<Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10" xmlns:mp="http://schemas.microsoft.com/appx/2014/phone/manifest">
  <Identity Name="MaterDawn.Sample" Publisher="CN=MaterDawn" Version="3.2.1.0" />
  <Properties><DisplayName>Sample Appx</DisplayName><Logo>Assets\StoreLogo.png</Logo></Properties>
  <mp:PhoneIdentity PhoneProductId="test" />
</Package>"#
}

#[test]
fn inspects_appx_and_selects_largest_scaled_logo() {
    let path = temp_package("appx", "appx");
    write_zip(
        &path,
        &[
            ("AppxManifest.xml", appx_manifest()),
            ("Assets/StoreLogo.scale-100.png", b"100"),
            ("Assets/StoreLogo.scale-400.png", b"400"),
        ],
    );
    let info = inspect(&path).unwrap();
    assert!(matches!(info.format, PackageFormat::Appx));
    assert_eq!(info.package_id, "MaterDawn.Sample");
    assert_eq!(info.app_name, "Sample Appx");
    assert_eq!(info.version_name, "3.2.1.0");
    assert_eq!(
        info.icon_source.as_deref(),
        Some("Assets/StoreLogo.scale-400.png")
    );
    assert_eq!(extract_icon(&path).unwrap().bytes, b"400");
    fs::remove_dir_all(path.parent().unwrap()).ok();
}

#[test]
fn inspects_appxbundle_nested_application() {
    let path = temp_package("appxbundle", "bundle");
    let nested_path = temp_package("appx", "nested");
    write_zip(
        &nested_path,
        &[
            ("AppxManifest.xml", appx_manifest()),
            ("Assets/StoreLogo.png", b"bundle-icon"),
        ],
    );
    let nested = fs::read(&nested_path).unwrap();
    let bundle_manifest = br#"<?xml version="1.0"?><Bundle><Packages><Package Type="resource" FileName="resources.appx"/><Package Type="application" FileName="main.appx"/></Packages></Bundle>"#;
    write_zip(
        &path,
        &[
            ("AppxMetadata/AppxBundleManifest.xml", bundle_manifest),
            ("main.appx", &nested),
        ],
    );
    let info = inspect(&path).unwrap();
    assert!(matches!(info.format, PackageFormat::AppxBundle));
    assert_eq!(info.package_id, "MaterDawn.Sample");
    assert_eq!(extract_icon(&path).unwrap().bytes, b"bundle-icon");
    fs::remove_dir_all(path.parent().unwrap()).ok();
    fs::remove_dir_all(nested_path.parent().unwrap()).ok();
}
