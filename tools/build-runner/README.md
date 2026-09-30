# Build runner

`Start-BuildRunner.ps1` lets a tool that can only write files into this repository (for example a coding assistant
working through a sandbox) build, test and pack Dapplo.Windows on a real Windows machine.

Start it once from the repository root and leave the window open:

```powershell
powershell -ExecutionPolicy Bypass -File tools\build-runner\Start-BuildRunner.ps1
```

It watches `.build-runner\requests` for `*.json` files and runs one of a fixed set of actions. Nothing from the request
is executed as a command: it can only choose the action, the configuration, a target framework and a validated
test filter.

| Field | Values |
|---|---|
| `id` | letters, digits, `_ . -`; names the result files |
| `action` | `build`, `test`, `verify` (build then test), `pack` (Release, into `.build-runner\packages`) |
| `configuration` | `Debug` (default) or `Release` |
| `framework` | optional, e.g. `net10.0-windows` or `net480` |
| `filter` | optional `dotnet test --filter` expression |
| `interactive` | `true` to include tests with `Category=Interactive` (they send input, replace the clipboard or write to the registry) |

Example `.build-runner\requests\baseline.json`:

```json
{ "id": "baseline", "action": "verify", "configuration": "Debug" }
```

Results land in `.build-runner\results`: `<id>.log` (full output), one `<id>_<framework>_<time>.trx` per target framework,
and `<id>.json` (exit codes, test counts per framework, names of failed tests). Create the file `.build-runner\cancel` to stop a running request. Hanging tests are aborted
after 3 minutes and named in the log. The runner restarts itself when the script changes.

`.build-runner` is ignored by git.
