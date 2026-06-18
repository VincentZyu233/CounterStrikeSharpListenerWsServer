"""共享工具 — 两个测试脚本的参数解析 & 默认命令列表"""

import argparse

DEFAULT_COMMANDS = [
    ('status', '查看服务器状态、玩家列表'),
    ('users',  '列出所有玩家 SteamID + UserID'),
    ('stats',  '服务器 FPS / CPU / 网络流量'),
]


def make_ws_parser(desc: str) -> argparse.ArgumentParser:
    """创建 WS 测试脚本的参数解析器"""
    parser = argparse.ArgumentParser(description=desc)
    parser.add_argument('--host', default='127.0.0.1')
    parser.add_argument('--port', type=int, default=60618)
    parser.add_argument('--token', default='test12345')
    parser.add_argument('command', nargs='?', default=None)
    return parser


def make_rcon_parser(desc: str) -> argparse.ArgumentParser:
    """创建 RCON 测试脚本的参数解析器"""
    parser = argparse.ArgumentParser(description=desc)
    parser.add_argument('--host', default='127.0.1.1')
    parser.add_argument('--port', type=int, default=60730)
    parser.add_argument('--pass', dest='password', default='test67890', help='RCON password')
    parser.add_argument('command', nargs='?', default=None)
    return parser


def resolve_commands(args) -> list[tuple[str, str]]:
    """有传参跑一条，没传跑全部 4 条默认指令"""
    if args.command:
        return [(args.command, '')]
    return DEFAULT_COMMANDS.copy()
