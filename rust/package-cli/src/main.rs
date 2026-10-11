use std::{env, fs, path::PathBuf, process::ExitCode};

use package_icon_core::{extract_icon, inspect};

fn main() -> ExitCode {
    match run() {
        Ok(()) => ExitCode::SUCCESS,
        Err(error) => {
            eprintln!("package-icon: {error}");
            ExitCode::FAILURE
        }
    }
}

fn run() -> Result<(), Box<dyn std::error::Error>> {
    let args: Vec<String> = env::args().skip(1).collect();
    match args.as_slice() {
        [command, input] if command == "inspect" => {
            let info = inspect(PathBuf::from(input))?;
            println!("{}", serde_json::to_string_pretty(&info)?);
            Ok(())
        }
        [command, input, output] if command == "extract-icon" => {
            let icon = extract_icon(PathBuf::from(input))?;
            fs::write(output, &icon.bytes)?;
            eprintln!(
                "source={} mediaType={} bytes={}",
                icon.source,
                icon.media_type,
                icon.bytes.len()
            );
            Ok(())
        }
        _ => {
            eprintln!("Usage:");
            eprintln!("  package-icon inspect <package.ipa|tipa|hap|appx|appxbundle>");
            eprintln!(
                "  package-icon extract-icon <package.ipa|tipa|hap|appx|appxbundle> <output>"
            );
            Err("invalid arguments".into())
        }
    }
}
