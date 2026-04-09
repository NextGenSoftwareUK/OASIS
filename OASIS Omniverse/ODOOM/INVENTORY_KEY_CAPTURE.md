# ODOOM inventory key capture (OQuake-style)

When the inventory popup is open, key bindings for **Up, Down, Left, Right, W, S, A, D, E, Z, X, I, O, P** are temporarily cleared so the game does not receive them. When the popup closes, default bindings are restored.

So that the inventory UI still receives those keys (arrows to select, E to use, Z/X to send, I to toggle, O/P for tabs), the integration reads **raw key state** each frame when `odoom_inventory_open` is set and writes **odoom_key_*** CVars for ZScript.

## Implementation

- **Windows:** `ODOOM_InventoryInputCaptureFrame()` uses `GetAsyncKeyState()` for VK_UP, VK_DOWN, VK_LEFT, VK_RIGHT, and keys E, Z, X, I, O, P, and calls `ODOOM_InventorySetKeyState()` every frame while the inventory is open. No engine patch is required.
- **Non-Windows (Linux/macOS):** Raw key state is not read in the integration (returns 0). To get arrows/keys in the popup on those platforms, the engine would need to call `ODOOM_InventorySetKeyState()` from its input/ticcmd code and pass 1/0 per key; see engine input layer for where to hook.

## CVARINFO

The CVars `odoom_inventory_open` and `odoom_key_*` are defined in **odoom_cvarinfo.txt**. The build script merges them into the engine's cvarinfo so ZScript can set `odoom_inventory_open` and read the key CVars.
