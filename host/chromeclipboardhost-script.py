#!python

import struct
import sys
import json
import win32clipboard
import base64

if sys.platform == "win32":
    import os
    import msvcrt
    msvcrt.setmode(sys.stdin.fileno(), os.O_BINARY)
    msvcrt.setmode(sys.stdout.fileno(), os.O_BINARY)


def send_message(message: str):
    sys.stdout.buffer.write(struct.pack('I', len(message)))
    sys.stdout.buffer.write(message.encode('utf-8'))
    sys.stdout.flush()


def main():
    """Reads Chrome host messages from the stdin."""
    while 1:
        text_length_bytes = sys.stdin.buffer.read(4)
        if len(text_length_bytes) == 0:
            return

        text_length = struct.unpack('i', text_length_bytes)[0]
        text = sys.stdin.buffer.read(text_length).decode('utf-8')
        obj = json.loads(text)
        try:
            win32clipboard.OpenClipboard()
            win32clipboard.EmptyClipboard()
            fmt = win32clipboard.RegisterClipboardFormat(obj['format'])
            win32clipboard.SetClipboardData(fmt, base64.b64decode(obj['base64']))
            win32clipboard.CloseClipboard()
            send_message(json.dumps(True))
        except Exception as ex:
            send_message(json.dumps(str(ex)))


if __name__ == '__main__':
    main()
