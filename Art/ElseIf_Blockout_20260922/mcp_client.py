import json,socket,sys
from pathlib import Path

def call(kind,params):
 try:
  with socket.create_connection(('127.0.0.1',9876),timeout=10) as connection:
   connection.settimeout(300);connection.sendall(json.dumps({'type':kind,'params':params}).encode());buffer=bytearray()
   while True:
    chunk=connection.recv(1048576)
    if not chunk:raise ConnectionError('Blender MCP closed before responding')
    buffer.extend(chunk)
    try:return json.loads(buffer)
    except (json.JSONDecodeError,UnicodeDecodeError):continue
 except OSError as e:raise RuntimeError('Blender MCP通信に失敗しました') from e
if __name__=='__main__':
 path=Path(sys.argv[1]);result=call('execute_code',{'code':path.read_text(encoding='utf-8-sig')});path.with_suffix('.response.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8');print(json.dumps(result,ensure_ascii=False,indent=2))
 if result.get('status')!='success':sys.exit(1)
