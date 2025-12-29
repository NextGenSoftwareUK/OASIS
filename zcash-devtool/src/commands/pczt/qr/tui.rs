use crossterm::event::KeyCode;
use futures_util::FutureExt;
use qrcode::{render::unicode, QrCode};
use ratatui::{
    prelude::*,
    widgets::{Block, Paragraph},
};
use tokio::sync::{mpsc, oneshot};
use tracing::{error, info, warn};
use tui_logger::{TuiLoggerLevelOutput, TuiLoggerSmartWidget};

use crate::tui;

pub(super) struct AppHandle {
    action_tx: mpsc::UnboundedSender<Action>,
}

impl AppHandle {
    /// Returns `true` if the TUI exited.
    pub(super) fn set_ur(&self, ur: String) -> bool {
        match self.action_tx.send(Action::SetUr(ur)) {
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
    ur: Option<String>,
    action_tx: mpsc::UnboundedSender<Action>,
    action_rx: mpsc::UnboundedReceiver<Action>,
    logger_state: tui_logger::TuiWidgetState,
}

impl App {
    pub(super) fn new(notify_shutdown: oneshot::Sender<()>) -> Self {
        let (action_tx, action_rx) = mpsc::unbounded_channel();
        Self {
            should_quit: false,
            notify_shutdown: Some(notify_shutdown),
            ur: None,
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
                    Action::SetUr(ur) => self.ur = Some(ur),
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

        if let Some(ur) = &self.ur {
            let code = QrCode::new(ur.to_ascii_uppercase()).unwrap();
            let string = code
                .render::<unicode::Dense1x2>()
                .dark_color(unicode::Dense1x2::Light)
                .light_color(unicode::Dense1x2::Dark)
                .quiet_zone(true)
                .build();

            let lines = string.lines();

            for (i, line) in lines.into_iter().enumerate() {
                frame.render_widget(
                    Paragraph::new(line),
                    Rect::new(
                        defrag_area.x,
                        defrag_area.y + i as u16,
                        line.len() as u16,
                        1,
                    ),
                );
            }

            frame.render_widget(
                Paragraph::new(Line::from_iter(Some(ur))).block(Block::bordered().title("UR")),
                mid_area,
            );
        }

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

#[derive(Clone)]
pub(super) enum Action {
    Quit,
    Tick,
    LoggerEvent(tui_logger::TuiWidgetEvent),
    SetUr(String),
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
