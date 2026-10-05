import zlib
import re
import sys

if hasattr(sys.stdout, 'reconfigure'):
    sys.stdout.reconfigure(encoding='utf-8')

def encode6bit(b):
    b = b & 0x3f
    if b < 10: return chr(48 + b)
    b -= 10
    if b < 26: return chr(65 + b)
    b -= 26
    if b < 26: return chr(97 + b)
    b -= 26
    if b == 0: return '-'
    if b == 1: return '_'
    return '?'

def encode64(data):
    res = []
    for i in range(0, len(data), 3):
        b1 = data[i]
        b2 = data[i+1] if i+1 < len(data) else 0
        b3 = data[i+2] if i+2 < len(data) else 0
        res.append(encode6bit(b1 >> 2))
        res.append(encode6bit(((b1 & 0x3) << 4) | (b2 >> 4)))
        if i+1 < len(data): res.append(encode6bit(((b2 & 0xf) << 2) | (b3 >> 6)))
        if i+2 < len(data): res.append(encode6bit(b3 & 0x3f))
    return ''.join(res)

with open(r'c:\Users\Van Thien\Downloads\ĐỒ ÁN HTTTDN\Fashion_store\baocao\generate_diagrams.py', 'r', encoding='utf-8') as f:
    text = f.read()

matches = re.findall(r'diagrams\["([^"]+)"\] = """(.*?)"""', text, re.DOTALL)
for name, code in matches:
    comp = zlib.compress(code.encode('utf-8'))[2:-4]
    print(name + '|||https://www.plantuml.com/plantuml/png/~1' + encode64(comp))
