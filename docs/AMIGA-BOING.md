# Classic-style Boing (development)

The reference is the original red/white checker ball, grey room, violet grid and
translucent offset shadow, as preserved in the
[Robert J. Mical / Dale Luck demo archive](https://www.randelshofer.ch/animations/anims/robert_j_mical/boing3.ilbm.html).
This is an original procedural implementation, not a copied demo executable or
artwork. No original audio is bundled.

The shared renderer prepares 32 immutable 112×112 rotation sprites and a room
image once per process. Runtime frames only draw the cached room/ball, a shadow
and optional scrolling text. The same code is used by Avalonia and the native
macOS core. The floor has a flat wall/floor junction and a consistent perspective.

All Kickstarts uses a fixed startup slot long enough for the longest original
prompt animation, rotating 1.3 → 2.0 → 3.1 on each program cycle. All animations
cycles Runner → Plasma → Boing. An individual animation can still be combined
with All Kickstarts. Saved numeric IDs for existing selections are unchanged;
Boing uses new ID 6, while removed historical IDs 1/2/3 still migrate to Cycle.

Tests cover moving sprites/scroller, frame cost, full playlist wrap, original
Kickstart pixels across two full rotations, transition/reflection timing,
companion persistence, and reachable footer buttons at 800×480.
