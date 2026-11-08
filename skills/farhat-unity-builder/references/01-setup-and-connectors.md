# Setup and connectors

## 1. Prerequisites the human provides

| Item | Why | How the agent checks |
| --- | --- | --- |
| Unity 6.3 LTS installed through Unity Hub, licence activated (Personal is free under the revenue limit in `references/project/04-tech-stack.md`) | Everything builds in Unity | `python3 scripts/check_env.py` |
| Git, and Git LFS recommended | Large binaries | `check_env.py` |
| `uv` (provides `uvx`) | Runs the Unity MCP server and the Blender MCP server | `check_env.py` |
| Blender (optional) | Asset conversion, Blender MCP | `check_env.py` |
| `SKETCHFAB_API_TOKEN` (optional) | Downloading Sketchfab models | `check_env.py` shows whether it is set |

The agent never installs Unity, activates a licence, or creates tokens.

## 2. Unity MCP

The game uses the open-source Unity MCP server by CoplayDev ("MCP for Unity").

1. In the Unity project, add the package from the Git URL (Window > Package Manager > + > Add package from git URL):
   `https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main`
2. Open its window (Window > MCP for Unity) and start the server. It listens on `http://localhost:8080/mcp` by default and needs `uv` on the PATH.
3. Register the server with your agent client (section 4). Re-check that the editor window shows "connected".

If a version of the package changes its menu names or default port, trust what the editor window shows and update `mcp/*.json` in
the repository. Tool names also change between versions, so list the tools your client exposes instead of assuming names.

What to use it for: creating and editing scenes, GameObjects, components and prefabs; reading the console; running the test
runner; entering Play mode to look at a result. What not to use it for: anything that must be repeatable in CI (use `unity_cli.py`
and the editor scripts) and anything that edits `.meta` files by hand.

## 3. Blender MCP

Use the Blender MCP server (`uvx blender-mcp`; if the package has been renamed on PyPI, use the name shown by the project's README, which was published as `mcp-for-blender` at the time of writing).

1. Install the Blender add-on from the project's repository into Blender (Edit > Preferences > Add-ons > Install).
2. In Blender's side panel, start the connection (default port 9876).
3. Register the server with your client (section 4).

Use it to open, clean, retarget and re-export downloaded models and to import Poly Haven or Sketchfab content that the add-on exposes.
Every file it brings in must still go through the ledger: record it with `asset_fetch.py url download` or add a ledger entry from the
add-on's source page, never leave a downloaded asset unrecorded.

## 4. Registering the servers

### Claude Code

Project-scoped file `.mcp.json` in the game project root (copy `mcp/claude-code.mcp.json`):

```json
{
  "mcpServers": {
    "unityMCP": { "type": "http", "url": "http://localhost:8080/mcp" },
    "blender": { "command": "uvx", "args": ["blender-mcp"] }
  }
}
```

Or from a terminal: `claude mcp add --transport http unityMCP http://localhost:8080/mcp` and
`claude mcp add blender -- uvx blender-mcp`. Skills live in `.claude/skills/<name>/` (project) or `~/.claude/skills/<name>/` (user).

### Antigravity

Workspace config `.agents/mcp_config.json` (copy `mcp/antigravity.mcp_config.json`), remote servers use `serverUrl`:

```json
{
  "mcpServers": {
    "unityMCP": { "serverUrl": "http://localhost:8080/mcp" },
    "blender": { "command": "uvx", "args": ["blender-mcp"] }
  }
}
```

Skills live in `.agents/skills/<name>/` (workspace; older builds used `.agent/skills/`) or in the global skills folder under `~/.gemini/`
(`~/.gemini/config/skills/`, or `~/.gemini/antigravity/skills/` on older builds). Antigravity may need an MCP toggle enabled in its
settings before it lists the Unity server.

### Installing everything at once

`python3 install.py` at the repository root copies the skill into the right folders for both clients and merges the MCP files without
overwriting existing servers. Run `python3 install.py --help`.

## 5. After setup

Run in the game project: `python3 scripts/check_env.py`, then `python3 scripts/unity_cli.py compile --project .`. If MCP tools are
not visible to the agent, restart the client and confirm the Unity window shows the server running. Report which connectors are live
before you start work that depends on them.
