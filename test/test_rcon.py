"""
uv venv
uv pip install rcon
uv run python ./test_rcon.py [--host HOST] [--port PORT] [--pass PASSWORD]
"""

import time
import argparse
# from rcon.source import rcon
from rcon.source import Client


def main():
    parser = argparse.ArgumentParser(description='CS2 RCON 测试脚本')
    parser.add_argument('--host', default='127.0.1.1')
    parser.add_argument('--port', type=int, default=60730)
    parser.add_argument('--pass', dest='password', default='test67890', help='RCON password')
    args = parser.parse_args()

    print(f'🔌 连接 {args.host}:{args.port}  RCON 密码: {args.password}')
    print('=' * 60)

    commands = [
        ('status', '查看服务器状态、玩家列表'),
        ('users',  '列出所有玩家 SteamID + UserID'),
        ('ping',   '所有玩家当前延迟'),
        ('stats',  '服务器 FPS / CPU / 网络流量'),
    ]

    try:
        # 使用标准的 Client 类作为同步上下文管理器，接收主机、端口和 passwd 关键字
        with Client(args.host, args.port, passwd=args.password) as conn:
            print('[+] RCON 认证成功\n')

            for cmd, desc in commands:
                print(f'▶ {cmd}  ({desc})')
                print('-' * 40)
                
                # 执行指令
                response = conn.run(cmd)
                
                print(response.strip() if response and response.strip() else '(无输出)')
                print()
                time.sleep(0.3)

    except ConnectionRefusedError:
        print(f'[❌] 无法连接到 {args.host}:{args.port}，请检查服务器是否运行、防火墙是否放行 TCP')
    except Exception as e:
        print(f'[❌] 捕获到异常异常: {e}')

    print('=' * 60)
    print('✅ 测试完成')


if __name__ == '__main__':
    main()
