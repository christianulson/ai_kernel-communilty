use serde::{Deserialize, Serialize};
use std::sync::{
    atomic::{AtomicBool, Ordering},
    Arc,
};
#[cfg(feature = "camera")]
use tauri::Emitter;

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct FaceRect {
    pub x: i32,
    pub y: i32,
    pub width: i32,
    pub height: i32,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct CameraInfo {
    pub index: i32,
    pub name: String,
}

pub struct CameraCapture {
    running: Arc<AtomicBool>,
}

impl CameraCapture {
    pub fn new() -> Self {
        Self {
            running: Arc::new(AtomicBool::new(false)),
        }
    }

    pub fn is_running(&self) -> bool {
        self.running.load(Ordering::SeqCst)
    }

    pub fn start_camera(&self, app_handle: tauri::AppHandle) -> Result<(), String> {
        #[cfg(not(feature = "camera"))]
        {
            let _ = app_handle;
            return Err("Camera feature not enabled. Rebuild with --features camera.".to_string());
        }

        #[cfg(feature = "camera")]
        self.start_camera_impl(app_handle)
    }

    pub fn stop_camera(&self) -> Result<(), String> {
        #[cfg(not(feature = "camera"))]
        return Err("Camera feature not enabled. Rebuild with --features camera.".to_string());

        #[cfg(feature = "camera")]
        self.stop_camera_impl()
    }
}

// ── Real implementation behind `camera` feature ───────────────────────────────

#[cfg(feature = "camera")]
impl CameraCapture {
    fn start_camera_impl(&self, app_handle: tauri::AppHandle) -> Result<(), String> {
        use opencv::prelude::*;

        if self.running.load(Ordering::SeqCst) {
            return Err("Camera is already running".to_string());
        }

        let mut cam = opencv::videoio::VideoCapture::new(0, opencv::videoio::CAP_ANY)
            .map_err(|e| format!("Failed to open camera: {}", e))?;

        if !cam
            .is_opened()
            .map_err(|e| format!("Camera error: {}", e))?
        {
            return Err("Could not open camera device".to_string());
        }

        self.running.store(true, Ordering::SeqCst);
        let running = self.running.clone();

        std::thread::spawn(move || {
            while running.load(Ordering::SeqCst) {
                let mut frame = opencv::core::Mat::default();
                match cam.read(&mut frame) {
                    Ok(true) if !frame.empty() => {
                        let mut buf = opencv::core::Vector::<u8>::new();
                        if let Ok(true) = opencv::imgcodecs::imencode(
                            ".jpg",
                            &frame,
                            &mut buf,
                            &opencv::core::Vector::<i32>::new(),
                        ) {
                            let bytes: Vec<u8> = buf.iter().collect();
                            let _ = app_handle.emit("camera-frame", &bytes);
                        }
                    }
                    _ => {}
                }
                std::thread::sleep(std::time::Duration::from_millis(33));
            }
        });

        Ok(())
    }

    fn stop_camera_impl(&self) -> Result<(), String> {
        if !self.running.load(Ordering::SeqCst) {
            return Err("Camera is not running".to_string());
        }
        self.running.store(false, Ordering::SeqCst);
        Ok(())
    }
}

// ── Stateless functions ───────────────────────────────────────────────────────

pub fn detect_faces(image_data: Vec<u8>) -> Result<Vec<FaceRect>, String> {
    #[cfg(not(feature = "camera"))]
    {
        let _ = image_data;
        return Err("Camera feature not enabled. Rebuild with --features camera.".to_string());
    }

    #[cfg(feature = "camera")]
    detect_faces_impl(image_data)
}

pub fn list_cameras() -> Result<Vec<CameraInfo>, String> {
    #[cfg(not(feature = "camera"))]
    return Err("Camera feature not enabled. Rebuild with --features camera.".to_string());

    #[cfg(feature = "camera")]
    list_cameras_impl()
}

#[cfg(feature = "camera")]
fn detect_faces_impl(image_data: Vec<u8>) -> Result<Vec<FaceRect>, String> {
    let buf: opencv::core::Vector<u8> = image_data.into_iter().collect();
    let img = opencv::imgcodecs::imdecode(&buf, opencv::imgcodecs::IMREAD_COLOR)
        .map_err(|e| format!("Failed to decode image: {}", e))?;

    let mut gray = opencv::core::Mat::default();
    opencv::imgproc::cvt_color(&img, &mut gray, opencv::imgproc::COLOR_BGR2GRAY, 0)
        .map_err(|e| format!("Failed to convert to grayscale: {}", e))?;

    let cascade_path = find_cascade_file()?;

    let mut cascade = opencv::objdetect::CascadeClassifier::new(&cascade_path)
        .map_err(|e| format!("Failed to load Haar cascade: {}", e))?;

    let mut faces = opencv::core::Vector::<opencv::core::Rect>::new();
    cascade
        .detect_multi_scale(
            &gray,
            &mut faces,
            1.1,
            3,
            0,
            opencv::core::Size::new(30, 30),
            opencv::core::Size::new(0, 0),
        )
        .map_err(|e| format!("Face detection failed: {}", e))?;

    let result: Vec<FaceRect> = faces
        .iter()
        .map(|r| FaceRect {
            x: r.x,
            y: r.y,
            width: r.width,
            height: r.height,
        })
        .collect();

    Ok(result)
}

#[cfg(feature = "camera")]
fn list_cameras_impl() -> Result<Vec<CameraInfo>, String> {
    let mut cameras = Vec::new();
    for index in 0..10 {
        match opencv::videoio::VideoCapture::new(index, opencv::videoio::CAP_ANY) {
            Ok(mut cam) => {
                if cam.is_opened().unwrap_or(false) {
                    cameras.push(CameraInfo {
                        index,
                        name: format!("Camera {}", index),
                    });
                }
            }
            Err(_) => break,
        }
    }
    Ok(cameras)
}

#[cfg(feature = "camera")]
fn find_cascade_file() -> Result<String, String> {
    let candidates = [
        #[cfg(target_os = "windows")]
        r"C:\opencv\etc\haarcascades\haarcascade_frontalface_default.xml",
        #[cfg(target_os = "windows")]
        r"C:\Program Files\opencv\etc\haarcascades\haarcascade_frontalface_default.xml",
        #[cfg(any(target_os = "linux", target_os = "macos"))]
        "/usr/share/opencv4/haarcascades/haarcascade_frontalface_default.xml",
        #[cfg(any(target_os = "linux", target_os = "macos"))]
        "/usr/local/share/opencv4/haarcascades/haarcascade_frontalface_default.xml",
        #[cfg(target_os = "macos")]
        "/opt/homebrew/share/opencv4/haarcascades/haarcascade_frontalface_default.xml",
        #[cfg(target_os = "macos")]
        "/usr/local/opt/opencv/share/opencv4/haarcascades/haarcascade_frontalface_default.xml",
    ];

    for path in &candidates {
        if std::path::Path::new(path).exists() {
            return Ok(path.to_string());
        }
    }

    Err("Haar cascade file not found. Install OpenCV with Haar cascade data.".to_string())
}
