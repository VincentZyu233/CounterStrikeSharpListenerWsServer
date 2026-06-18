"""模拟 Koishi WS 客户端 — 发送 external_command_to_server 并等待 command_result
用法: uv pip install websockets && uv run python ./test_exec_command.py [COMMAND] [--host HOST] [--port PORT] [--token TOKEN]
"""

import asyncio
import json
import websockets
from common import make_ws_parser, resolve_commands

# ANSI styles
BOLD    = "\033[1m"
DIM     = "\033[2m"
ITALIC  = "\033[3m"
RST     = "\033[0m"
RED     = "\033[31m"
GREEN   = "\033[32m"
YELLOW  = "\033[33m"
BLUE    = "\033[34m"
MAGENTA = "\033[35m"
CYAN    = "\033[36m"
GRAY    = "\033[90m"


async def exec_command(host: str, port: int, token: str, command: str, idx: int, total: int):
    url = f"ws://{host}:{port}"
    if token:
        url += f"?token={token}"

    async with websockets.connect(url) as ws:
        request_id = f"test-{command}"
        payload = {"type": "external_command_to_server", "request_id": request_id, "command": command}
        await ws.send(json.dumps(payload))
        print(f"{GREEN}[→]{RST}  {BOLD}{command}{RST}  {DIM}(request_id={request_id}){RST}")

        try:
            async with asyncio.timeout(15):
                async for raw in ws:
                    msg = json.loads(raw)
                    if msg.get("type") == "command_result" and msg.get("request_id") == request_id:
                        if msg.get("ok"):
                            result = msg.get("result", "")
                            if result:
                                print(f"{BLUE}[←]{RST}  {BOLD}ok{RST}  {DIM}({len(result)} chars){RST}\n{result}")
                            else:
                                print(f"{BLUE}[←]{RST}  {BOLD}ok{RST}  {DIM}(empty){RST}")
                        else:
                            print(f"{RED}[←]{RST}  {BOLD}FAIL{RST}: {msg.get('error', 'unknown error')}")
                        return msg
        except TimeoutError:
            print(f"{RED}[⚠] 超时 (15s)，未收到 command_result — 检查 EnableRemoteExecCommand 是否为 true{RST}")


async def main_async():
    parser = make_ws_parser('WS 命令执行测试 (模拟 Koishi 客户端)')
    args = parser.parse_args()

    print(f'{BOLD}{CYAN}🔌 WS 连接 ws://{args.host}:{args.port}  token: {args.token}{RST}')
    print('=' * 60)

    cmds = resolve_commands(args)

    if not args.command:
        print(f'{YELLOW}⚠️  未传参，执行默认 {len(cmds)} 条指令{RST}\n')

    for i, (cmd, desc) in enumerate(cmds, 1):
        tag = f'{ITALIC}{GRAY}{desc}{RST}' if desc else ''
        print(f'\n{BOLD}{MAGENTA}▶ [{i}/{len(cmds)}] {cmd}{RST} {tag}')
        print('─' * 40)
        await exec_command(args.host, args.port, args.token, cmd, i, len(cmds))

    print('\n' + '=' * 60)
    print(f'{BOLD}{GREEN}✅ 测试完成{RST}')


def main():
    asyncio.run(main_async())


if __name__ == '__main__':
    main()
