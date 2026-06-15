use tauri::App;

pub fn setup_updater(app: &mut App) -> Result<(), Box<dyn std::error::Error>> {
    // Updater is configured via tauri.conf.json "updater" section.
    // The tauri-plugin-updater is initialized in lib.rs.
    // No additional setup needed here.
    //
    // To trigger a manual check:
    // use tauri_plugin_updater::UpdaterExt;
    // app.updater().check().await?;
    Ok(())
}
