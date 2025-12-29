use std::{collections::BTreeMap, ops::Range};

use crossterm::event::KeyCode;
use futures_util::FutureExt;
use ratatui::{
    prelude::*,
    widgets::{Block, Paragraph},
};
use roaring::RoaringBitmap;
use tokio::sync::{mpsc, oneshot};
use tracing::{error, info, warn};
use tui_logger::{TuiLoggerLevelOutput, TuiLoggerSmartWidget};
use zcash_client_backend::data_api::{
    scanning::{ScanPriority, ScanRange},
    WalletSummary,
};
use zcash_client_sqlite::AccountUuid;
use zcash_protocol::consensus::BlockHeight;

use crate::tui;

pub(super) struct AppHandle {
    action_tx: mpsc::UnboundedSender<Action>,
}

impl AppHandle {
    /// Returns `true` if the TUI exited.
    pub(super) fn set_scan_ranges(
        &self,
        scan_ranges: &[ScanRange],
        chain_tip: BlockHeight,
    ) -> bool {
        match self.action_tx.send(Action::UpdateScanRanges {
            scan_ranges: scan_ranges.to_vec(),
            chain_tip,
        }) {
            Ok(()) => false,
            Err(e) => {
                error!("Failed to send: {}", e);
                true
            }
        }
    }

    /// Returns `true` if the TUI exited.
    pub(super) fn set_fetching_range(&self, fetching_range: Option<Range<BlockHeight>>) -> bool {
        match self.action_tx.send(Action::SetFetching(fetching_range)) {
            Ok(()) => false,
            Err(e) => {
                error!("Failed to send: {}", e);
                true
            }
        }
    }

    /// Returns `true` if the TUI exited.
    pub(super) fn set_fetched(&self, fetched_height: BlockHeight) -> bool {
        match self.action_tx.send(Action::SetFetched(fetched_height)) {
            Ok(()) => false,
            Err(e) => {
                error!("Failed to send: {}", e);
                true
            }
        }
    }

    /// Returns `true` if the TUI exited.
    pub(super) fn set_scanning_range(&self, scanning_range: Option<Range<BlockHeight>>) -> bool {
        match self.action_tx.send(Action::SetScanning(scanning_range)) {
            Ok(()) => false,
            Err(e) => {
                error!("Failed to send: {}", e);
                true
            }
        }
    }

    /// Returns `true` if the TUI exited.
    pub(super) fn set_wallet_summary(
        &self,
        wallet_summary: Option<WalletSummary<AccountUuid>>,
    ) -> bool {
        match self
            .action_tx
            .send(Action::SetWalletSummary(wallet_summary))
        {
            Ok(()) => false,
            Err(e) => {
                error!("Failed to send: {}", e);
                true
            }
        }
    }
}

pub(super) struct App {
    should_quit: bool,
    notify_shutdown: Option<oneshot::Sender<()>>,
    wallet_birthday: BlockHeight,
    wallet_summary: Option<WalletSummary<AccountUuid>>,
    scan_ranges: BTreeMap<BlockHeight, ScanPriority>,
    fetching_set: RoaringBitmap,
    fetched_set: RoaringBitmap,
    scanning_range: Option<Range<BlockHeight>>,
    action_tx: mpsc::UnboundedSender<Action>,
    action_rx: mpsc::UnboundedReceiver<Action>,
    logger_state: tui_logger::TuiWidgetState,
}

impl App {
    pub(super) fn new(notify_shutdown: oneshot::Sender<()>, wallet_birthday: BlockHeight) -> Self {
        let (action_tx, action_rx) = mpsc::unbounded_channel();
        Self {
            should_quit: false,
            notify_shutdown: Some(notify_shutdown),
            wallet_birthday,
            wallet_summary: None,
            scan_ranges: BTreeMap::new(),
            fetching_set: RoaringBitmap::new(),
            fetched_set: RoaringBitmap::new(),
            scanning_range: None,
            action_tx,
            action_rx,
            logger_state: tui_logger::TuiWidgetState::new(),
        }
    }

    pub(super) fn handle(&self) -> AppHandle {
        AppHandle {
            action_tx: self.action_tx.clone(),
        }
    }

    pub(super) async fn run(&mut self, mut tui: tui::Tui) -> anyhow::Result<()> {
        tui.enter()?;

        loop {
            let action_queue_len = self.action_rx.len();
            if action_queue_len >= 50 {
                warn!("Action queue lagging! Length: {}", action_queue_len);
            }

            let next_event = tui.next().fuse();
            let next_action = self.action_rx.recv().fuse();
            tokio::select! {
                Some(event) = next_event => if let Some(action) = Action::for_event(event) {
                    self.action_tx.send(action)?;
                },
                Some(action) = next_action => match action {
                    Action::Quit => {
                        info!("Quit requested");
                        self.should_quit = true;
                        let _ = self.notify_shutdown.take().expect("should only occur once").send(());
                        break;
                    }
                    Action::Tick => {}
                    Action::LoggerEvent(event) => self.logger_state.transition(event),
                    Action::UpdateScanRanges { scan_ranges, chain_tip } => {
                        self.update_scan_ranges(scan_ranges, chain_tip);
                    }
                    Action::SetFetching(fetching_range) => {
                        self.fetching_set.clear();
                        self.fetched_set.clear();
                        if let Some(range) = fetching_range {
                            self.fetching_set.insert_range(u32::from(range.start)..u32::from(range.end));
                        }
                    }
                    Action::SetFetched(fetched_height) => {
                        self.fetching_set.remove(u32::from(fetched_height));
                        self.fetched_set.insert(u32::from(fetched_height));
                    },
                    Action::SetScanning(scanning_range) => self.scanning_range = scanning_range,
                    Action::SetWalletSummary(wallet_summary) => self.wallet_summary = wallet_summary,
                    Action::Render => {
                        tui.draw(|f| self.ui(f))?;
                    }
                }
            }

            if self.should_quit {
                break;
            }
        }

        self.action_rx.close();
        tui.exit()?;

        Ok(())
    }

    fn update_scan_ranges(&mut self, mut scan_ranges: Vec<ScanRange>, chain_tip: BlockHeight) {
        scan_ranges.sort_by_key(|range| range.block_range().start);
        let mempool_height = chain_tip + 1;

        self.scan_ranges = scan_ranges
            .into_iter()
            .flat_map(|range| {
                [
                    (range.block_range().start, range.priority()),
                    // If this range is followed by an adjacent range, this will be
                    // overwritten. Otherwise, this is either a gap between unscanned
                    // ranges (which by definition is scanned), or heights at or above the
                    // "mempool height" which we coerce down to that height.
                    (
                        range.block_range().end.min(mempool_height),
                        ScanPriority::Scanned,
                    ),
                ]
            })
            .collect();

        // If we weren't passed a ScanRange starting at the wallet birthday, it means we
        // have scanned that height.
        self.scan_ranges
            .entry(self.wallet_birthday)
            .or_insert(ScanPriority::Scanned);

        // If we inserted the mempool height above, mark it as ignored (because we can't
        // scan blocks that don't yet exist). If we didn't insert it above, do so here.
        self.scan_ranges
            .entry(mempool_height)
            .and_modify(|e| *e = ScanPriority::Ignored)
            .or_insert(ScanPriority::Ignored);
    }

    fn ui(&mut self, frame: &mut Frame) {
        let [upper_area, mid_area, log_area] = Layout::vertical([
            Constraint::Min(0),
            Constraint::Length(3),
            Constraint::Length(15),
        ])
        .areas(frame.area());

        let defrag_area = {
            let block = Block::bordered().title("Wallet Defragmentor");
            let inner_area = block.inner(upper_area);
            frame.render_widget(block, upper_area);
            inner_area
        };

        if let Some(block_count) = self
            .scan_ranges
            .last_key_value()
            .map(|(&last, _)| last - self.wallet_birthday)
        {
            // Determine the density of blocks we will be rendering. Use ceiling division
            // to ensure we don't require more cells than we have (which would cause the
            // blocks around the chain tip to never be rendered).
            let area = defrag_area.area();
            let blocks_per_cell = block_count.div_ceil(area);
            let blocks_per_row = blocks_per_cell * u32::from(defrag_area.width);

            // Split the area into cells.
            for i in 0..defrag_area.width {
                for j in 0..defrag_area.height {
                    // Determine the priority of the cell.
                    let cell_start = u32::from(self.wallet_birthday)
                        + (blocks_per_row * u32::from(j))
                        + (blocks_per_cell * u32::from(i));
                    let cell_end = cell_start + blocks_per_cell;

                    let cell = if self.fetching_set.range_cardinality(cell_start..cell_end) > 0 {
                        Some(("↓", Color::Magenta))
                    } else if self
                        .scanning_range
                        .as_ref()
                        .map(|range| {
                            u32::from(range.start) < cell_end && cell_start < u32::from(range.end)
                        })
                        .unwrap_or(false)
                    {
                        Some(("@", Color::Magenta))
                    } else if self.fetched_set.range_cardinality(cell_start..cell_end) > 0 {
                        Some((" ", Color::Magenta))
                    } else {
                        let cell_priority = self
                            .scan_ranges
                            .range(
                                BlockHeight::from_u32(cell_start)..BlockHeight::from_u32(cell_end),
                            )
                            .fold(None, |acc: Option<ScanPriority>, (_, &priority)| {
                                if let Some(acc) = acc {
                                    Some(acc.max(priority))
                                } else {
                                    Some(priority)
                                }
                            })
                            .or_else(|| {
                                self.scan_ranges
                                    .range(..=BlockHeight::from_u32(cell_start))
                                    .next_back()
                                    .map(|(_, &priority)| priority)
                            })
                            .or_else(|| {
                                self.scan_ranges
                                    .range(BlockHeight::from_u32(cell_end - 1)..)
                                    .next()
                                    .map(|(_, &priority)| priority)
                            })
                            .unwrap_or(ScanPriority::Ignored);

                        match cell_priority {
                            ScanPriority::Ignored => None,
                            ScanPriority::Scanned => Some(Color::Green),
                            ScanPriority::Historic => Some(Color::Black),
                            ScanPriority::OpenAdjacent => Some(Color::LightBlue),
                            ScanPriority::FoundNote => Some(Color::Yellow),
                            ScanPriority::ChainTip => Some(Color::Blue),
                            ScanPriority::Verify => Some(Color::Red),
                        }
                        .map(|color| (" ", color))
                    };

                    if let Some((cell_text, cell_color)) = cell {
                        frame.render_widget(
                            Paragraph::new(cell_text).bg(cell_color),
                            Rect::new(defrag_area.x + i, defrag_area.y + j, 1, 1),
                        );
                    }
                }
            }
        }

        let stats = Line::from_iter(
            self.wallet_summary
                .as_ref()
                .iter()
                .flat_map(|wallet_summary| {
                    let scan_progress = wallet_summary.progress().scan();
                    let synced = Span::raw(format!(
                        "Synced: {:0.3}%",
                        (*scan_progress.numerator() as f64) * 100f64
                            / (*scan_progress.denominator() as f64)
                    ));

                    let recovered = wallet_summary.progress().recovery().map(|progress| {
                        Span::raw(format!(
                            "Recovered: {:0.3}%",
                            (*progress.numerator() as f64) * 100f64
                                / (*progress.denominator() as f64)
                        ))
                    });

                    let separator = (recovered.is_some()).then(|| Span::raw(" | "));

                    [Some(synced), separator, recovered]
                })
                .flatten(),
        );
        frame.render_widget(
            Paragraph::new(stats).block(Block::bordered().title("Stats")),
            mid_area,
        );

        frame.render_widget(
            TuiLoggerSmartWidget::default()
                .title_log("Log Entries")
                .title_target("Log Target Selector")
                .style_error(Style::default().fg(Color::Red))
                .style_debug(Style::default().fg(Color::Green))
                .style_warn(Style::default().fg(Color::Yellow))
                .style_trace(Style::default().fg(Color::Magenta))
                .style_info(Style::default().fg(Color::Cyan))
                .output_separator(':')
                .output_timestamp(Some("%H:%M:%S".to_string()))
                .output_level(Some(TuiLoggerLevelOutput::Abbreviated))
                .output_target(true)
                .output_file(true)
                .output_line(true)
                .state(&self.logger_state),
            log_area,
        );
    }
}

#[derive(Clone, Debug)]
pub(super) enum Action {
    Quit,
    Tick,
    LoggerEvent(tui_logger::TuiWidgetEvent),
    UpdateScanRanges {
        scan_ranges: Vec<ScanRange>,
        chain_tip: BlockHeight,
    },
    SetFetching(Option<Range<BlockHeight>>),
    SetFetched(BlockHeight),
    SetScanning(Option<Range<BlockHeight>>),
    SetWalletSummary(Option<WalletSummary<AccountUuid>>),
    Render,
}

impl Action {
    fn for_event(event: tui::Event) -> Option<Self> {
        match event {
            tui::Event::Error => None,
            tui::Event::Tick => Some(Action::Tick),
            tui::Event::Render => Some(Action::Render),
            tui::Event::Key(key) => match key.code {
                KeyCode::Char('q') => Some(Action::Quit),
                KeyCode::Char(' ') => {
                    Some(Action::LoggerEvent(tui_logger::TuiWidgetEvent::SpaceKey))
                }
                KeyCode::Up => Some(Action::LoggerEvent(tui_logger::TuiWidgetEvent::UpKey)),
                KeyCode::Down => Some(Action::LoggerEvent(tui_logger::TuiWidgetEvent::DownKey)),
                KeyCode::Left => Some(Action::LoggerEvent(tui_logger::TuiWidgetEvent::LeftKey)),
                KeyCode::Right => Some(Action::LoggerEvent(tui_logger::TuiWidgetEvent::RightKey)),
                KeyCode::Char('+') => {
                    Some(Action::LoggerEvent(tui_logger::TuiWidgetEvent::PlusKey))
                }
                KeyCode::Char('-') => {
                    Some(Action::LoggerEvent(tui_logger::TuiWidgetEvent::MinusKey))
                }
                KeyCode::Char('h') => {
                    Some(Action::LoggerEvent(tui_logger::TuiWidgetEvent::HideKey))
                }
                KeyCode::Char('f') => {
                    Some(Action::LoggerEvent(tui_logger::TuiWidgetEvent::FocusKey))
                }
                KeyCode::PageUp => {
                    Some(Action::LoggerEvent(tui_logger::TuiWidgetEvent::PrevPageKey))
                }
                KeyCode::PageDown => {
                    Some(Action::LoggerEvent(tui_logger::TuiWidgetEvent::NextPageKey))
                }
                KeyCode::Esc => Some(Action::LoggerEvent(tui_logger::TuiWidgetEvent::EscapeKey)),
                _ => None,
            },
            _ => None,
        }
    }
}
