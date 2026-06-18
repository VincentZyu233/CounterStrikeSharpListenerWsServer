```bash
uv venv
uv pip install rcon
uv run python ./test_rcon.py
uv run python ./test_rcon.py [--host HOST] [--port PORT] [--pass PASSWORD] [COMMAND]

uv pip install websockets
uv run python ./test_exec_command.py
uv run python ./test_exec_command.py [--host HOST] [--port PORT] [--token TOKEN] [COMMAND]
```