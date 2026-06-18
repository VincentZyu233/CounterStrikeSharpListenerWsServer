"""CS2 RCON 测试脚本 — 使用 python-rcon 库
用法: uv pip install rcon && uv run python ./test_rcon.py [COMMAND] [--host HOST] [--port PORT] [--pass PASSWORD]
"""

import time
from rcon.source import Client
from common import make_rcon_parser, resolve_commands

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


def main():
    parser = make_rcon_parser('CS2 RCON 测试脚本')
    args = parser.parse_args()

    print(f'{BOLD}{CYAN}🔌 连接 {args.host}:{args.port}  RCON 密码: {args.password}{RST}')
    print('=' * 60)

    cmds = resolve_commands(args)

    if not args.command:
        print(f'{YELLOW}⚠️  未传参，执行默认 {len(cmds)} 条指令{RST}\n')

    try:
        with Client(args.host, args.port, passwd=args.password) as conn:
            print(f'{BOLD}{GREEN}✅ RCON 认证成功{RST}\n')

            for i, (cmd, desc) in enumerate(cmds, 1):
                tag = f'{ITALIC}{GRAY}{desc}{RST}' if desc else ''
                print(f'{BOLD}{MAGENTA}▶ [{i}/{len(cmds)}] {cmd}{RST} {tag}')
                print('─' * 40)
                response = conn.run(cmd)
                if response and response.strip():
                    print(response.strip())
                else:
                    print(f'{DIM}(无输出){RST}')
                print()
                time.sleep(0.3)

    except ConnectionRefusedError:
        print(f'{RED}[❌] 无法连接到 {args.host}:{args.port}，请检查服务器是否运行、防火墙是否放行 TCP{RST}')
    except Exception as e:
        print(f'{RED}[❌] {e}{RST}')

    print('=' * 60)
    print(f'{BOLD}{GREEN}✅ 测试完成{RST}')


if __name__ == '__main__':
    main()
