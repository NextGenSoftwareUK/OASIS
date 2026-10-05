use std::time::Duration;

use anyhow::anyhow;
use clap::Args;
use image::buffer::ConvertBuffer;
use minicbor::data::{Int, Type};
use nokhwa::{
    nokhwa_check, nokhwa_initialize,
    pixel_format::RgbFormat,
    utils::{RequestedFormat, RequestedFormatType, Resolution},
    Camera,
};
use pczt::Pczt;
use qrcode::{render::unicode, QrCode};
use tokio::io::{stdin, stdout, AsyncReadExt, AsyncWriteExt, Stdout};

use crate::ShutdownListener;

#[cfg(feature = "tui")]
use crate::tui::Tui;

#[cfg(feature = "tui")]
mod tui;

const ZCASH_PCZT: &str = "zcash-pczt";
const UR_ZCASH_PCZT: &str = "ur:zcash-pczt";

// Options accepted for the `pczt to-qr` command
#[cfg(feature = "pczt-qr")]
#[derive(Debug, Args)]
pub(crate) struct Send {
    /// The duration in milliseconds to wait between QR codes (default is 500)
    #[arg(long)]
    #[arg(default_value_t = 500)]
    interval: u64,

    #[cfg(feature = "tui")]
    #[arg(long)]
    pub(crate) tui: bool,
}

impl Send {
    pub(crate) async fn run(
        self,
        mut shutdown: ShutdownListener,
        #[cfg(feature = "tui")] tui: Tui,
    ) -> Result<(), anyhow::Error> {
        let mut buf = vec![];
        stdin().read_to_end(&mut buf).await?;

        let pczt = Pczt::parse(&buf).map_err(|e| anyhow!("Failed to read PCZT: {:?}", e))?;

        let mut pczt_packet = vec![];
        minicbor::encode(
            &ZcashPczt {
                data: pczt.serialize(),
            },
            &mut pczt_packet,
        )
        .map_err(|e| anyhow!("Failed to encode PCZT packet: {:?}", e))?;

        #[cfg(feature = "tui")]
        let tui_handle = if self.tui {
            let mut app = tui::App::new(shutdown.tui_quit_signal());
            let handle = app.handle();
            tokio::spawn(async move {
                if let Err(e) = app.run(tui).await {
                    tracing::error!("Error while running TUI: {e}");
                }
            });
            Some(handle)
        } else {
            None
        };

        let mut encoder = ur::Encoder::new(&pczt_packet, 100, ZCASH_PCZT)
            .map_err(|e| anyhow!("Failed to build UR encoder: {e}"))?;

        let mut stdout = stdout();
        let mut interval = tokio::time::interval(Duration::from_millis(self.interval));
        loop {
            interval.tick().await;

            if shutdown.requested() {
                return Ok(());
            }

            let ur = encoder
                .next_part()
                .map_err(|e| anyhow!("Failed to encode PCZT part: {e}"))?;

            async fn render_cli(stdout: &mut Stdout, ur: String) -> anyhow::Result<()> {
                let code = QrCode::new(ur.to_ascii_uppercase())?;
                let string = code
                    .render::<unicode::Dense1x2>()
                    .dark_color(unicode::Dense1x2::Light)
                    .light_color(unicode::Dense1x2::Dark)
                    .quiet_zone(true)
                    .build();

                stdout.write_all(format!("{string}\n").as_bytes()).await?;
                stdout.write_all(format!("{ur}\n\n\n\n").as_bytes()).await?;
                stdout.flush().await?;

                Ok(())
            }

            #[cfg(feature = "tui")]
            if let Some(handle) = tui_handle.as_ref() {
                if handle.set_ur(ur) {
                    // TUI exited.
                    return Ok(());
                }
            } else {
                render_cli(&mut stdout, ur).await?;
            }

            #[cfg(not(feature = "tui"))]
            render_cli(&mut stdout, ur).await?;
        }
    }
}

// Options accepted for the `pczt from-qr` command
#[cfg(feature = "pczt-qr")]
#[derive(Debug, Args)]
pub(crate) struct Receive {
    /// The duration in milliseconds to wait between scanning for QR codes (default is 500)
    #[arg(long)]
    #[arg(default_value_t = 500)]
    interval: u64,
}

impl Receive {
    pub(crate) async fn run(self, mut shutdown: ShutdownListener) -> Result<(), anyhow::Error> {
        nokhwa_initialize(|_| ());
        if !nokhwa_check() {
            return Err(anyhow!("Failed to obtain macOS camera permissions"));
        }

        let cameras = nokhwa::query(nokhwa::utils::ApiBackend::Auto)?;
        let camera = if cameras.len() > 1 {
            eprintln!("Available cameras:");
            for (i, camera) in cameras.iter().enumerate() {
                eprintln!("{}: {}", i, camera.human_name());
            }
            eprint!("Select a camera: ");
            cameras
                .get(usize::from(stdin().read_u8().await?) - 48)
                .ok_or(anyhow!("Invalid camera"))
        } else {
            cameras.first().ok_or(anyhow!("No camera"))
        }?;

        eprintln!("Creating camera");
        let mut camera = Camera::new(
            camera.index().clone(),
            RequestedFormat::new::<RgbFormat>(RequestedFormatType::AbsoluteHighestResolution),
        )?;

        eprintln!("Opening camera stream");
        camera
            .open_stream()
            .map_err(|e| anyhow!("Could not open camera stream: {e}"))?;

        eprintln!("Starting detection loop");
        let mut decoder = ur::Decoder::default();
        let mut interval = tokio::time::interval(Duration::from_millis(self.interval));

        while !decoder.complete() {
            interval.tick().await;

            if shutdown.requested() {
                camera.stop_stream()?;
                return Ok(());
            }

            let frame = camera.frame()?;
            // Doesn't work in nokhwa 0.10: https://github.com/l1npengtul/nokhwa/issues/100
            // let decoded = frame.decode_image::<RgbFormat>()?;
            let decoded = convert_buffer_to_image(frame)?;

            let mut detect_grids = |mut img: rqrr::PreparedImage<
                image::ImageBuffer<image::Luma<u8>, Vec<u8>>,
            >|
             -> anyhow::Result<()> {
                let grids = img.detect_grids();
                if let Some(grid) = grids.first() {
                    let (_, content) = grid.decode()?;
                    let content = content.to_ascii_lowercase();
                    if content.starts_with(UR_ZCASH_PCZT) {
                        eprintln!("{content}");
                        decoder
                            .receive(&content)
                            .map_err(|e| anyhow!("Failed to parse QR code: {:?}", e))?;
                    } else {
                        eprintln!("Unexpected UR type: {content}");
                    }
                }
                Ok(())
            };

            if let Err(e) = detect_grids(rqrr::PreparedImage::prepare(decoded.convert())) {
                eprintln!("Error while detecting grids: {e}");
            }
        }

        camera.stop_stream()?;

        let pczt_packet = decoder
            .message()
            .map_err(|e| anyhow!("Failed to extract full message from QR codes: {:?}", e))?
            .expect("complete");

        let pczt = Pczt::parse(
            &minicbor::decode::<'_, ZcashPczt>(&pczt_packet)
                .map_err(|e| anyhow!("Failed to decode PCZT packet: {:?}", e))?
                .data,
        )
        .map_err(|e| anyhow!("Failed to read PCZT from QR codes: {:?}", e))?;

        stdout().write_all(&pczt.serialize()).await?;

        Ok(())
    }
}

const DATA: u8 = 1;

struct ZcashPczt {
    data: Vec<u8>,
}

impl<C> minicbor::Encode<C> for ZcashPczt {
    fn encode<W: minicbor::encode::Write>(
        &self,
        e: &mut minicbor::Encoder<W>,
        _ctx: &mut C,
    ) -> Result<(), minicbor::encode::Error<W::Error>> {
        e.map(1)?;

        e.int(Int::from(DATA))?.bytes(&self.data)?;

        Ok(())
    }
}

impl<'b, C> minicbor::Decode<'b, C> for ZcashPczt {
    fn decode(
        d: &mut minicbor::Decoder<'b>,
        _ctx: &mut C,
    ) -> Result<Self, minicbor::decode::Error> {
        let mut result = ZcashPczt { data: vec![] };
        cbor_map(d, &mut result, |key, obj, d| {
            let key =
                u8::try_from(key).map_err(|e| minicbor::decode::Error::message(e.to_string()))?;
            if key == DATA {
                obj.data = d.bytes()?.to_vec();
            }
            Ok(())
        })?;
        Ok(result)
    }
}

fn cbor_map<'b, F, T>(
    d: &mut minicbor::Decoder<'b>,
    obj: &mut T,
    mut cb: F,
) -> Result<(), minicbor::decode::Error>
where
    F: FnMut(Int, &mut T, &mut minicbor::Decoder<'b>) -> Result<(), minicbor::decode::Error>,
{
    let entries = d.map()?;
    let mut index = 0;
    loop {
        let key = d.int()?;
        (cb)(key, obj, d)?;
        index += 1;
        if let Some(len) = entries {
            if len == index {
                break;
            }
        }
        if let Type::Break = d.datatype()? {
            d.skip()?;
            break;
        }
    }
    Ok(())
}

fn convert_buffer_to_image(
    buffer: nokhwa::Buffer,
) -> anyhow::Result<image::ImageBuffer<image::Rgb<u8>, Vec<u8>>> {
    let Resolution {
        width_x: width,
        height_y: height,
    } = buffer.resolution();
    let mut image_buffer = image::ImageBuffer::<image::Rgb<u8>, Vec<u8>>::new(width, height);
    let data = buffer.buffer();

    for (y, chunk) in data
        .chunks_exact((width * 2) as usize)
        .enumerate()
        .take(height as usize)
    {
        for (x, pixel) in chunk.chunks_exact(4).enumerate() {
            let [u, y1, v, y2] = [
                pixel[0] as f32,
                pixel[1] as f32,
                pixel[2] as f32,
                pixel[3] as f32,
            ];
            let x = (x * 2) as u32;
            image_buffer.put_pixel(x, y as u32, yuv_to_rgb(y1, u, v));
            image_buffer.put_pixel(x + 1, y as u32, yuv_to_rgb(y2, u, v));
        }
    }

    Ok(image_buffer)
}

//YUV to RGB conversion BT.709
fn yuv_to_rgb(y: f32, u: f32, v: f32) -> image::Rgb<u8> {
    let r = y + 1.5748 * (v - 128.0);
    let g = y - 0.1873 * (u - 128.0) - 0.4681 * (v - 128.0);
    let b = y + 1.8556 * (u - 128.0);

    image::Rgb([r as u8, g as u8, b as u8])
}
