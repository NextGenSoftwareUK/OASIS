use std::collections::BTreeMap;
use std::io::Write;
use std::sync::Arc;

use anyhow::anyhow;
use clap::Args;
use crossterm::event::KeyCode;
use futures_util::FutureExt;
use incrementalmerkletree::{Address, Level};
use ratatui::{
    layout::{Constraint, Layout},
    style::Color,
    widgets::{
        canvas::{Canvas, Circle, Context, Line},
        Block, Paragraph, Widget,
    },
    Frame,
};
use shardtree::{error::ShardTreeError, store::ShardStore, LocatedTree, RetentionFlags};
use tokio::sync::{mpsc, oneshot};
use tracing::{info, warn};
use zcash_client_backend::data_api::{WalletCommitmentTrees, WalletRead};
use zcash_client_sqlite::{wallet::commitment_tree::SqliteShardStore, WalletDb};
use zcash_primitives::merkle_tree::HashSer;
use zcash_protocol::{
    consensus::{BlockHeight, Network},
    ShieldedProtocol,
};

use crate::{
    config::get_wallet_network,
    data::get_db_paths,
    tui::{self, Tui},
    ShutdownListener,
};

fn parse_pool(data: &str) -> Result<ShieldedProtocol, String> {
    match data {
        "s" | "sapling" => Ok(ShieldedProtocol::Sapling),
        "o" | "orchard" => Ok(ShieldedProtocol::Orchard),
        _ => Err(format!("Unknown pool '{data}'")),
    }
}

fn parse_address(data: &str) -> Result<Address, String> {
    match data.split_once(':') {
        Some((level, index)) => Ok(Address::from_parts(
            level
                .parse::<u8>()
                .map_err(|_| format!("Unknown address level '{level}'"))?
                .into(),
            index
                .parse()
                .map_err(|_| format!("Unknown address index '{index}'"))?,
        )),
        None => Ok(Address::from_parts(
            Level::ZERO,
            data.parse()
                .map_err(|_| format!("Unknown address '{data}'"))?,
        )),
    }
}

// Options accepted for the `wallet tree show` command
#[derive(Debug, Args)]
pub(crate) struct Command {
    #[arg(short, long, value_parser = parse_pool)]
    pool: ShieldedProtocol,

    /// A node address `level:index` or leaf position.
    #[arg(short, long, value_parser = parse_address)]
    address: Option<Address>,

    /// Set to show block boundaries.
    #[arg(long)]
    show_block_boundaries: bool,
}

impl Command {
    pub(crate) async fn run(
        self,
        mut shutdown: ShutdownListener,
        wallet_dir: Option<String>,
        tui: Tui,
    ) -> anyhow::Result<()> {
        let params = get_wallet_network(wallet_dir.as_ref())?;
        let (_, db_data) = get_db_paths(wallet_dir.as_ref());
        let db_data = WalletDb::for_path(db_data, params, (), ())?;

        let mut app = App::new(
            shutdown.tui_quit_signal(),
            db_data,
            self.pool,
            self.address,
            self.show_block_boundaries,
        )?;
        if let Err(e) = app.run(tui).await {
            tracing::error!("Error while running TUI: {e}");
        }

        Ok(())
    }
}

pub(super) struct App {
    should_quit: bool,
    notify_shutdown: Option<oneshot::Sender<()>>,
    db_data: WalletDb<rusqlite::Connection, Network, (), ()>,
    pool: ShieldedProtocol,
    address: Address,
    action_tx: mpsc::UnboundedSender<Action>,
    action_rx: mpsc::UnboundedReceiver<Action>,
    block_boundaries: Option<BTreeMap<u32, BlockHeight>>,
    region: Option<Region>,
}

impl App {
    pub(super) fn new(
        notify_shutdown: oneshot::Sender<()>,
        db_data: WalletDb<rusqlite::Connection, Network, (), ()>,
        pool: ShieldedProtocol,
        address: Option<Address>,
        show_block_boundaries: bool,
    ) -> anyhow::Result<Self> {
        let address =
            address.unwrap_or_else(|| Address::above_position(SHARD_ROOT_LEVEL, 0.into()));
        let (action_tx, action_rx) = mpsc::unbounded_channel();

        let block_boundaries = if show_block_boundaries {
            println!("Caching block boundaries for performance");
            let scanned_range = match (db_data.get_wallet_birthday()?, db_data.block_max_scanned()?)
            {
                (Some(birthday_height), Some(max_scanned)) => {
                    Ok(u32::from(birthday_height)..=u32::from(max_scanned.block_height()))
                }
                _ => Err(anyhow!("Tree is empty")),
            }?;

            // Crude progress bar.
            let mut last_pos = 0;
            let max = (scanned_range.end() - scanned_range.start()) as f64;

            let mut block_boundaries = BTreeMap::new();
            for (i, height) in scanned_range.enumerate() {
                let cur_pos = ((i * 100) as f64 / max) as u8;
                if cur_pos > last_pos {
                    last_pos = cur_pos;
                    print!(".");
                    std::io::stdout().flush()?;
                }

                if let Some(block) = db_data.block_metadata(height.into())? {
                    if let Some(key) = match pool {
                        ShieldedProtocol::Sapling => block.sapling_tree_size(),
                        ShieldedProtocol::Orchard => block.orchard_tree_size(),
                    } {
                        block_boundaries.entry(key).or_insert(block.block_height());
                    }
                }
            }
            println!();

            Some(block_boundaries)
        } else {
            None
        };

        let mut app = Self {
            should_quit: false,
            notify_shutdown: Some(notify_shutdown),
            db_data,
            pool,
            address,
            action_tx,
            action_rx,
            block_boundaries,
            region: None,
        };
        app.reload_region();
        Ok(app)
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
                    Action::Move(direction) => if self.move_through_tree(direction) {
                        self.reload_region();
                    },
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

    fn move_through_tree(&mut self, direction: Direction) -> bool {
        match direction {
            Direction::Up => {
                // Don't move above the tree root.
                if self.address.level() < 32.into() {
                    self.set_address_if_valid(self.address.parent())
                } else {
                    false
                }
            }
            Direction::Left => self.set_address_if_valid(Address::from_parts(
                self.address.level(),
                self.address.index().saturating_sub(1),
            )),
            Direction::Right => self.set_address_if_valid(self.address.next_at_level()),
            Direction::DownLeft => {
                if let Some((left, _)) = self.address.children() {
                    self.set_address_if_valid(left)
                } else {
                    false
                }
            }
            Direction::DownRight => {
                if let Some((_, right)) = self.address.children() {
                    self.set_address_if_valid(right)
                } else {
                    false
                }
            }
        }
    }

    fn set_address_if_valid(&mut self, address: Address) -> bool {
        match self.pool {
            ShieldedProtocol::Sapling => match self.db_data.with_sapling_tree_mut(|tree| {
                NodeFetcher {
                    store: tree.store(),
                }
                .get(address)
                .map(|opt| opt.map(|node| node.address))
            }) {
                Ok(Some(valid)) => {
                    assert_eq!(address, valid);
                    self.address = valid;
                    true
                }
                Ok(None) => false,
                Err(e) => todo!("{}", e),
            },
            ShieldedProtocol::Orchard => match self.db_data.with_orchard_tree_mut(|tree| {
                NodeFetcher {
                    store: tree.store(),
                }
                .get(address)
                .map(|opt| opt.map(|node| node.address))
            }) {
                Ok(Some(valid)) => {
                    assert_eq!(address, valid);
                    self.address = valid;
                    true
                }
                Ok(None) => false,
                Err(e) => todo!("{}", e),
            },
        }
    }

    fn reload_region(&mut self) {
        let mut get_region = |address| match self.pool {
            ShieldedProtocol::Sapling => self
                .db_data
                .with_sapling_tree_mut(move |tree| {
                    Region::get(
                        ShieldedProtocol::Sapling,
                        NodeFetcher {
                            store: tree.store(),
                        },
                        address,
                    )
                })
                .unwrap(),
            ShieldedProtocol::Orchard => self
                .db_data
                .with_orchard_tree_mut(move |tree| {
                    Region::get(
                        ShieldedProtocol::Orchard,
                        NodeFetcher {
                            store: tree.store(),
                        },
                        address,
                    )
                })
                .unwrap(),
        };

        let mut address = self.address;
        let mut region = get_region(address);

        while region.is_none() {
            // Find a parent that does exist.
            address = address.parent();
            region = get_region(address);
        }
        if address != self.address {
            warn!(
                "{:?} does not exist, moving to parent {:?}",
                self.address, address
            );
        }

        let mut region = region.expect("subtree roots exist");

        if let Some(block_boundaries) = &self.block_boundaries {
            region.add_block_boundaries(block_boundaries);
        }

        self.region = Some(region);
    }

    fn ui(&mut self, frame: &mut Frame) {
        let [upper_area, lower_area] =
            Layout::vertical([Constraint::Min(0), Constraint::Length(5)]).areas(frame.area());
        let [detail_area, debug_area] =
            Layout::horizontal([Constraint::Percentage(50), Constraint::Fill(1)]).areas(lower_area);

        let region = self.region.clone().expect("always set");

        frame.render_widget(region.render(), upper_area);
        frame.render_widget(
            Paragraph::new(
                [
                    Some(ratatui::text::Line::from(if region.node.is_nil {
                        "Nil node".into()
                    } else if region.node.flags.is_some() {
                        format!(
                            "Leaf node{}",
                            if let Some(v) = region.node.value {
                                format!(": {v}")
                            } else {
                                " (no value)".into()
                            }
                        )
                    } else {
                        format!(
                            "Parent node{}",
                            if let Some(a) = region.node.annotation {
                                format!(": {a}")
                            } else {
                                " (no annotation)".into()
                            }
                        )
                    })),
                    region.node.flags.and_then(|flags| {
                        (flags == RetentionFlags::EPHEMERAL).then(|| "Ephemeral".into())
                    }),
                    region
                        .node
                        .flags
                        .and_then(|flags| flags.is_checkpoint().then(|| "Checkpoint".into())),
                    region
                        .node
                        .flags
                        .and_then(|flags| flags.is_marked().then(|| "Marked".into())),
                    region.node.flags.and_then(|flags| {
                        flags
                            .contains(RetentionFlags::REFERENCE)
                            .then(|| "Reference".into())
                    }),
                ]
                .into_iter()
                .flatten()
                .collect::<Vec<_>>(),
            )
            .block(Block::bordered().title("Details")),
            detail_area,
        );
        frame.render_widget(
            Paragraph::new(vec![
                ratatui::text::Line::from(format!("{:?}", self.address)),
                ratatui::text::Line::from(format!("{:?}", region.node.flags)),
            ])
            .block(Block::bordered().title("Debug")),
            debug_area,
        );
    }
}

#[derive(Clone)]
enum Direction {
    Up,
    Left,
    Right,
    DownLeft,
    DownRight,
}

#[derive(Clone)]
enum Action {
    Quit,
    Tick,
    Move(Direction),
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
                KeyCode::Char('w') => Some(Action::Move(Direction::Up)),
                KeyCode::Char('a') => Some(Action::Move(Direction::Left)),
                KeyCode::Char('d') => Some(Action::Move(Direction::Right)),
                KeyCode::Char('z') => Some(Action::Move(Direction::DownLeft)),
                KeyCode::Char('x') => Some(Action::Move(Direction::DownRight)),
                KeyCode::Up => Some(Action::Move(Direction::Up)),
                KeyCode::Left => Some(Action::Move(Direction::Left)),
                KeyCode::Right => Some(Action::Move(Direction::Right)),
                KeyCode::Down => Some(Action::Move(Direction::DownLeft)),
                _ => None,
            },
            _ => None,
        }
    }
}

const SHARD_ROOT_LEVEL: Level = Level::new(16);

struct NodeFetcher<'a, H> {
    store: &'a SqliteShardStore<&'a rusqlite::Transaction<'a>, H, 16>,
}

impl<H: Clone + HashSer> NodeFetcher<'_, H> {
    fn get(
        &self,
        address: Address,
    ) -> Result<Option<Node>, ShardTreeError<zcash_client_sqlite::wallet::commitment_tree::Error>>
    {
        if address.level() > SHARD_ROOT_LEVEL {
            // Address is within the cap.
            self.store
                .get_cap()
                .map_err(ShardTreeError::Storage)
                .and_then(|cap| {
                    LocatedTree::from_parts(Address::from_parts(32.into(), 0), cap)
                        .map(|tree| tree.subtree(address))
                        .map_err(|addr| {
                            ShardTreeError::Query(shardtree::error::QueryError::NotContained(addr))
                        })
                })
        } else {
            // Address is within a shard.
            self.store
                .get_shard(Address::above_position(
                    SHARD_ROOT_LEVEL,
                    address.position_range_start(),
                ))
                .map_err(ShardTreeError::Storage)
                .map(|opt| opt.and_then(|shard| shard.subtree(address)))
        }
        .map(|opt| opt.map(Node::new))
    }
}

#[derive(Clone)]
struct Node {
    address: Address,
    annotation: Option<String>,
    value: Option<String>,
    flags: Option<RetentionFlags>,
    is_nil: bool,
    block_boundary: Option<BlockHeight>,
}

impl Node {
    fn new<H: HashSer>(node: LocatedTree<Option<Arc<H>>, (H, RetentionFlags)>) -> Self {
        Self {
            address: node.root_addr(),
            annotation: node.root().annotation().and_then(|a| a.as_ref()).map(|a| {
                let mut data = vec![];
                a.write(&mut data).unwrap();
                hex::encode(data)
            }),
            value: node.root().leaf_value().map(|(v, _)| {
                let mut data = vec![];
                v.write(&mut data).unwrap();
                hex::encode(data)
            }),
            flags: node.root().leaf_value().map(|(_, flags)| *flags),
            is_nil: node.root().is_nil(),
            block_boundary: None,
        }
    }

    fn is_checkpoint(&self) -> bool {
        self.flags.is_some_and(|flags| flags.is_checkpoint())
    }

    fn add_block_boundary(&mut self, block_boundaries: &BTreeMap<u32, BlockHeight>) {
        // If the maximum position within this node is the last note in a block, render
        // a block boundary.
        if let Some(height) =
            block_boundaries.get(&(u64::from(self.address.max_position()) as u32 + 1))
        {
            self.block_boundary = Some(*height);
        }
        // TODO: Once `BTreeMap::upper_bound` stabilises, show inexact block boundaries in
        // addition to exact ones.
    }
}

const NODE_RADIUS: f64 = 2.0;

const X_NODE: f64 = 0.0;
const CHILD_GAP: f64 = 20.0;
const CHILD_OFFSET: f64 = CHILD_GAP / 2.0;
const NODE_GAP: f64 = CHILD_GAP * 2.0;
const NODE_OFFSET: f64 = NODE_GAP / 2.0;
const PARENT_GAP: f64 = NODE_GAP * 2.0;

const ROW_SPACING: f64 = 20.0;
const Y_GRANDPARENT: f64 = ROW_SPACING * 2.0;
const Y_PARENT: f64 = ROW_SPACING;
const Y_NODE: f64 = 0.0;
const Y_CHILD: f64 = -ROW_SPACING;

/// Metadata for the following region around node `N`:
/// ```text
///    ____g____        || og___               ____g
///   /         \       ||      \             /
/// OP           p      ||       OP          p
///    \       /   \    ||         \       /   \
///     o     N     s   ||          o     N     s
///    / \   / \   / \  ||         / \   / \   / \
///   ol or l   r sl sr ||        ol or l   r sl sr
/// ```
///
/// `Direction` lets us move from `N` to `p`, `o`, `s`, `l`, or `r`.
#[derive(Clone)]
struct Region {
    pool: ShieldedProtocol,
    node: Node,
    l: Option<Node>,
    r: Option<Node>,
    p: Option<Parent>,
    op: Option<Parent>,
}

impl Region {
    fn get<H: Clone + HashSer>(
        pool: ShieldedProtocol,
        node_fetcher: NodeFetcher<'_, H>,
        address: Address,
    ) -> Result<Option<Self>, ShardTreeError<zcash_client_sqlite::wallet::commitment_tree::Error>>
    {
        let node = match node_fetcher.get(address)? {
            Some(node) => node,
            None => return Ok(None),
        };

        let (l, r) = match address.children() {
            None => (None, None),
            Some((left, right)) => (node_fetcher.get(left)?, node_fetcher.get(right)?),
        };

        let sibling = address.sibling();
        let other_sibling = if address.is_left_child() {
            address
                .index()
                .checked_sub(1)
                .map(|index| Address::from_parts(address.level(), index))
        } else {
            Some(address.next_at_level())
        };

        let p = Parent::get(&node_fetcher, true, sibling, address.is_right_child())?;
        let op = match other_sibling {
            Some(addr) => Parent::get(&node_fetcher, false, addr, address.is_left_child())?,
            None => None,
        };

        Ok(Some(Self {
            pool,
            node,
            l,
            r,
            p,
            op,
        }))
    }

    fn add_block_boundaries(&mut self, block_boundaries: &BTreeMap<u32, BlockHeight>) {
        // Annotate the lowest-level nodes in the region with block boundaries.
        if let Some(right) = &mut self.r {
            right.add_block_boundary(block_boundaries);
            if let Some(left) = &mut self.l {
                left.add_block_boundary(block_boundaries);
            }
        } else {
            self.node.add_block_boundary(block_boundaries);
        }
        if let Some(parent) = &mut self.p {
            if let Some(sibling) = &mut parent.sibling_child {
                if let Some(right) = &mut sibling.r {
                    right.add_block_boundary(block_boundaries);
                    if let Some(left) = &mut sibling.l {
                        left.add_block_boundary(block_boundaries);
                    }
                } else {
                    sibling.node.add_block_boundary(block_boundaries);
                }
            } else {
                parent.node.add_block_boundary(block_boundaries);
            }
        }
        if let Some(parent) = &mut self.op {
            if let Some(sibling) = &mut parent.sibling_child {
                if let Some(right) = &mut sibling.r {
                    right.add_block_boundary(block_boundaries);
                    if let Some(left) = &mut sibling.l {
                        left.add_block_boundary(block_boundaries);
                    }
                } else {
                    sibling.node.add_block_boundary(block_boundaries);
                }
            } else {
                parent.node.add_block_boundary(block_boundaries);
            }
        }
    }

    fn render(&self) -> impl Widget + use<'_> {
        Canvas::default()
            .block(Block::bordered().title(match self.pool {
                ShieldedProtocol::Sapling => "Sapling tree",
                ShieldedProtocol::Orchard => "Orchard tree",
            }))
            .x_bounds([-90.0, 90.0])
            .y_bounds([-30.0, 50.0])
            .paint(move |ctx| {
                // Draw lines on the lower layer.
                if self.node.address.level() == SHARD_ROOT_LEVEL {
                    draw_shard_boundary(ctx, Y_NODE);
                } else if self
                    .l
                    .as_ref()
                    .or(self.r.as_ref())
                    .is_some_and(|node| node.address.level() == SHARD_ROOT_LEVEL)
                {
                    draw_shard_boundary(ctx, Y_CHILD);
                } else if let Some(parent) = &self.p {
                    if parent.node.address.level() == SHARD_ROOT_LEVEL {
                        draw_shard_boundary(ctx, Y_PARENT);
                    } else if parent
                        .grandparent
                        .as_ref()
                        .is_some_and(|node| node.address.level() == SHARD_ROOT_LEVEL)
                    {
                        draw_shard_boundary(ctx, Y_GRANDPARENT);
                    }
                }
                if let Some(height) = self.node.block_boundary {
                    draw_block_boundary(ctx, X_NODE, height);
                } else if self.node.is_checkpoint() {
                    draw_checkpoint(ctx, X_NODE);
                }
                if let Some(left) = &self.l {
                    let x_left = X_NODE - CHILD_OFFSET;
                    draw_edge(ctx, X_NODE, Y_NODE, x_left, Y_CHILD);
                    if let Some(height) = left.block_boundary {
                        draw_block_boundary(ctx, x_left, height);
                    } else if left.is_checkpoint() {
                        draw_checkpoint(ctx, x_left);
                    }
                }
                if let Some(right) = &self.r {
                    let x_right = X_NODE + CHILD_OFFSET;
                    draw_edge(ctx, X_NODE, Y_NODE, x_right, Y_CHILD);
                    if let Some(height) = right.block_boundary {
                        draw_block_boundary(ctx, x_right, height);
                    } else if right.is_checkpoint() {
                        draw_checkpoint(ctx, x_right);
                    }
                }
                if let Some(parent) = &self.p {
                    parent.draw_edges(ctx);
                }
                if let Some(parent) = &self.op {
                    parent.draw_edges(ctx);
                }
                ctx.layer();

                // Draw nodes on the upper layer.
                draw_node(ctx, X_NODE, Y_NODE, self.node.address);
                if let Some(left) = &self.l {
                    draw_node(ctx, X_NODE - CHILD_OFFSET, Y_CHILD, left.address);
                }
                if let Some(right) = &self.r {
                    draw_node(ctx, X_NODE + CHILD_OFFSET, Y_CHILD, right.address);
                }
                if let Some(parent) = &self.p {
                    parent.draw_nodes(ctx);
                }
                if let Some(parent) = &self.op {
                    parent.draw_nodes(ctx);
                }

                // Highlight where we are.
                ctx.draw(&Circle {
                    x: X_NODE,
                    y: Y_NODE,
                    radius: NODE_RADIUS * 2.0,
                    color: Color::Blue,
                });
            })
    }
}

#[derive(Clone)]
struct Parent {
    x: f64,
    y: f64,
    is_parent_of_region: bool,
    node: Node,
    grandparent: Option<Node>,
    sibling_child: Option<Sibling>,
}

impl Parent {
    fn get<H: Clone + HashSer>(
        node_fetcher: &NodeFetcher<'_, H>,
        is_parent_of_region: bool,
        sibling: Address,
        sibling_is_left_of_region: bool,
    ) -> Result<Option<Self>, ShardTreeError<zcash_client_sqlite::wallet::commitment_tree::Error>>
    {
        let address = sibling.parent();
        if let Some(node) = node_fetcher.get(address)? {
            Ok(Some(Parent {
                x: if is_parent_of_region {
                    if sibling_is_left_of_region {
                        X_NODE - NODE_OFFSET
                    } else {
                        X_NODE + NODE_OFFSET
                    }
                } else if sibling_is_left_of_region {
                    X_NODE + NODE_OFFSET - PARENT_GAP
                } else {
                    X_NODE - NODE_OFFSET + PARENT_GAP
                },
                y: Y_PARENT,
                is_parent_of_region,
                node,
                grandparent: node_fetcher.get(address.parent())?,
                sibling_child: Sibling::get(node_fetcher, sibling, sibling_is_left_of_region)?,
            }))
        } else {
            // Parent is pruned.
            Ok(None)
        }
    }

    fn draw_edges(&self, ctx: &mut Context<'_>) {
        if self.is_parent_of_region {
            draw_edge(ctx, X_NODE, Y_NODE, self.x, self.y);
        }
        if let Some(height) = self.node.block_boundary {
            draw_block_boundary(ctx, self.x, height);
        } else if self.node.is_checkpoint() {
            draw_checkpoint(ctx, self.x);
        }
        if let Some(grandparent) = &self.grandparent {
            let grandparent_x = if self.node.address.is_left_child() {
                self.x + NODE_GAP
            } else {
                self.x - NODE_GAP
            };
            draw_edge(ctx, self.x, self.y, grandparent_x, Y_GRANDPARENT);
            if grandparent.is_checkpoint() {
                draw_checkpoint(ctx, grandparent_x);
            }
        }
        if let Some(sibling) = &self.sibling_child {
            sibling.draw_edges(ctx, self.x, self.y);
        }
    }

    fn draw_nodes(&self, ctx: &mut Context<'_>) {
        draw_node(ctx, self.x, self.y, self.node.address);
        if let Some(grandparent) = &self.grandparent {
            let grandparent_x = if self.node.address.is_left_child() {
                self.x + NODE_GAP
            } else {
                self.x - NODE_GAP
            };
            draw_node(ctx, grandparent_x, Y_GRANDPARENT, grandparent.address);
        }
        if let Some(sibling) = &self.sibling_child {
            sibling.draw_nodes(ctx);
        }
    }
}

#[derive(Clone)]
struct Sibling {
    x: f64,
    y: f64,
    node: Node,
    l: Option<Node>,
    r: Option<Node>,
}

impl Sibling {
    fn get<H: Clone + HashSer>(
        node_fetcher: &NodeFetcher<'_, H>,
        address: Address,
        is_left_of_region: bool,
    ) -> Result<Option<Self>, ShardTreeError<zcash_client_sqlite::wallet::commitment_tree::Error>>
    {
        if let Some(node) = node_fetcher.get(address)? {
            let (l, r) = match address.children() {
                None => (None, None),
                Some((left, right)) => (node_fetcher.get(left)?, node_fetcher.get(right)?),
            };
            Ok(Some(Sibling {
                x: if is_left_of_region {
                    X_NODE - NODE_GAP
                } else {
                    X_NODE + NODE_GAP
                },
                y: Y_NODE,
                node,
                l,
                r,
            }))
        } else {
            // Sibling is pruned.
            Ok(None)
        }
    }

    fn draw_edges(&self, ctx: &mut Context<'_>, parent_x: f64, parent_y: f64) {
        draw_edge(ctx, parent_x, parent_y, self.x, self.y);
        if let Some(height) = self.node.block_boundary {
            draw_block_boundary(ctx, self.x, height);
        } else if self.node.is_checkpoint() {
            draw_checkpoint(ctx, self.x);
        }
        if let Some(left) = &self.l {
            let x_left = self.x - CHILD_OFFSET;
            draw_edge(ctx, self.x, self.y, x_left, self.y - ROW_SPACING);
            if let Some(height) = left.block_boundary {
                draw_block_boundary(ctx, x_left, height);
            } else if left.is_checkpoint() {
                draw_checkpoint(ctx, x_left);
            }
        }
        if let Some(right) = &self.r {
            let x_right = self.x + CHILD_OFFSET;
            draw_edge(ctx, self.x, self.y, x_right, self.y - ROW_SPACING);
            if let Some(height) = right.block_boundary {
                draw_block_boundary(ctx, x_right, height);
            } else if right.is_checkpoint() {
                draw_checkpoint(ctx, x_right);
            }
        }
    }

    fn draw_nodes(&self, ctx: &mut Context<'_>) {
        draw_node(ctx, self.x, self.y, self.node.address);
        if let Some(left) = &self.l {
            draw_node(
                ctx,
                self.x - CHILD_OFFSET,
                self.y - ROW_SPACING,
                left.address,
            );
        }
        if let Some(right) = &self.r {
            draw_node(
                ctx,
                self.x + CHILD_OFFSET,
                self.y - ROW_SPACING,
                right.address,
            );
        }
    }
}

fn draw_edge(ctx: &mut Context<'_>, x1: f64, y1: f64, x2: f64, y2: f64) {
    ctx.draw(&Line {
        x1,
        y1,
        x2,
        y2,
        color: Color::Gray,
    });
}

fn draw_node(ctx: &mut Context<'_>, x: f64, y: f64, addr: Address) {
    ctx.draw(&Circle {
        x,
        y,
        radius: NODE_RADIUS,
        color: if u8::from(addr.level()) & 1 == 0 {
            Color::White
        } else {
            Color::Red
        },
    });
    ctx.print(x, y, format!("{}:{}", u8::from(addr.level()), addr.index()));
}

fn draw_shard_boundary(ctx: &mut Context<'_>, y: f64) {
    let y = y + CHILD_OFFSET / 2.0;
    ctx.draw(&Line {
        x1: -90.0,
        y1: y,
        x2: 90.0,
        y2: y,
        color: Color::Green,
    });
}

fn draw_block_boundary(ctx: &mut Context<'_>, x: f64, height: BlockHeight) {
    draw_checkpoint(ctx, x);
    ctx.print(x, -30.0, format!("{}", u32::from(height)));
}

fn draw_checkpoint(ctx: &mut Context<'_>, x: f64) {
    let x = x + CHILD_OFFSET / 2.0;
    ctx.draw(&Line {
        x1: x,
        y1: -30.0,
        x2: x,
        y2: 50.0,
        color: Color::Yellow,
    });
}
