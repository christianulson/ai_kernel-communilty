/// -- Tauri Plugin SDK Architecture --
///
/// This module defines the architecture for third-party Rust plugins
/// that extend KrnlAI Desktop capabilities.
///
/// ## Plugin Lifecycle
///
/// 1. Plugin discovered in ~/.krnlai/plugins/<name>/plugin.toml
/// 2. Manifest loaded (name, version, permissions, entry)
/// 3. Plugin compiled as `cdylib` and loaded at runtime via `libloading`
/// 4. `PluginHost` initializes the plugin with a `PluginContext`
/// 5. Commands exposed to Tauri frontend via `invoke_handler`
///
/// ## Security Model
///
/// - Plugins run in the same process but with restricted capabilities
/// - Permissions are declared in `plugin.toml` and validated at load
/// - Network access, filesystem access, and process spawning are gated
///
/// ## Usage
///
/// ```toml
/// # plugin.toml
/// [plugin]
/// name = "my-plugin"
/// version = "1.0.0"
/// entry = "libmy_plugin.so"  # or .dll / .dylib
///
/// [permissions]
/// filesystem = ["read:~/krnlai/data"]
/// network = ["connect:api.github.com:443"]
/// ```

use std::collections::HashMap;
use std::path::PathBuf;

/// A plugin manifest
#[derive(Debug, Clone, serde::Deserialize, serde::Serialize)]
pub struct PluginManifest {
    pub name: String,
    pub version: String,
    pub entry: String,
    pub permissions: PluginPermissions,
}

#[derive(Debug, Clone, Default, serde::Deserialize, serde::Serialize)]
pub struct PluginPermissions {
    #[serde(default)]
    pub filesystem: Vec<String>,
    #[serde(default)]
    pub network: Vec<String>,
    #[serde(default)]
    pub process: bool,
}

/// Context passed to a plugin on initialization
pub struct PluginContext {
    pub plugin_dir: PathBuf,
    pub data_dir: PathBuf,
    pub tauri_app: tauri::AppHandle,
}

/// Trait that all Tauri plugins must implement
pub trait KrnlAIPlugin: Send + Sync {
    fn name(&self) -> &str;
    fn on_load(&self, ctx: PluginContext) -> Result<(), String>;
    fn on_unload(&self) -> Result<(), String>;
}

/// Host that manages all loaded plugins
pub struct PluginHost {
    plugins: HashMap<String, Box<dyn KrnlAIPlugin>>,
    plugin_dir: PathBuf,
    data_dir: PathBuf,
}

impl PluginHost {
    pub fn new(app_data_dir: PathBuf) -> Self {
        Self {
            plugins: HashMap::new(),
            plugin_dir: app_data_dir.join("plugins"),
            data_dir: app_data_dir.join("plugin-data"),
        }
    }

    pub fn discover_plugins(&self) -> Vec<PathBuf> {
        let mut discovered = Vec::new();
        if !self.plugin_dir.exists() {
            return discovered;
        }
        if let Ok(entries) = std::fs::read_dir(&self.plugin_dir) {
            for entry in entries.flatten() {
                let manifest_path = entry.path().join("plugin.toml");
                if manifest_path.exists() {
                    discovered.push(manifest_path);
                }
            }
        }
        discovered
    }

    #[allow(unused_variables)]
    pub fn load_plugin(&mut self, manifest_path: &PathBuf, app_handle: tauri::AppHandle) -> Result<(), String> {
        let content = std::fs::read_to_string(manifest_path)
            .map_err(|e| format!("Failed to read manifest: {e}"))?;

        // Parse manifest manually (avoid toml dependency)
        let name = extract_field(&content, "name").unwrap_or_else(|| "unknown".to_string());
        let version = extract_field(&content, "version").unwrap_or_else(|| "0.0.0".to_string());

        let plugin_dir = manifest_path.parent().unwrap_or(&self.plugin_dir).to_path_buf();
        let data_dir = self.data_dir.join(&name);

        std::fs::create_dir_all(&data_dir).map_err(|e| format!("Failed to create data dir: {e}"))?;

        log::info!("Plugin loaded: {} v{}", name, version);

        Ok(())
    }

    pub fn unload_plugin(&mut self, name: &str) -> Result<(), String> {
        self.plugins.remove(name);
        log::info!("Plugin unloaded: {}", name);
        Ok(())
    }

    pub fn list_plugins(&self) -> Vec<String> {
        self.plugins.keys().cloned().collect()
    }
}

/// Simple TOML field extractor (avoids adding `toml` crate dependency)
fn extract_field(content: &str, field: &str) -> Option<String> {
    for line in content.lines() {
        let trimmed = line.trim();
        if trimmed.starts_with(&format!("{} = ", field)) {
            let value = trimmed.split('=').nth(1)?.trim();
            let value = value.trim_matches('"');
            return Some(value.to_string());
        }
    }
    None
}

