"""Capture ProjectHub using PrintWindow, including when another app covers it."""
import ctypes
from ctypes import wintypes
from pathlib import Path
import os
import sys
from PIL import Image

user = ctypes.windll.user32
gdi = ctypes.windll.gdi32
handle = int(sys.argv[2]) if len(sys.argv) > 2 else user.FindWindowW(None, "ProjectHub")
if not handle:
    raise SystemExit("ProjectHub window unavailable")
rect = wintypes.RECT()
user.GetWindowRect(handle, ctypes.byref(rect))
width, height = rect.right - rect.left, rect.bottom - rect.top
dc = user.GetWindowDC(handle)
memory = gdi.CreateCompatibleDC(dc)
bitmap = gdi.CreateCompatibleBitmap(dc, width, height)
previous = gdi.SelectObject(memory, bitmap)
try:
    if not user.PrintWindow(handle, memory, 2):
        raise OSError("PrintWindow failed")
    header = (40).to_bytes(4, "little") + width.to_bytes(4, "little") + (-height).to_bytes(4, "little", signed=True) + bytes.fromhex("01002000") + bytes(24)
    info = ctypes.create_string_buffer(header)
    buffer = ctypes.create_string_buffer(width * height * 4)
    gdi.GetDIBits(memory, bitmap, 0, height, buffer, info, 0)
    destination = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(os.environ["TEMP"]) / "projecthub-native.png"
    destination.parent.mkdir(parents=True, exist_ok=True)
    Image.frombuffer("RGB", (width, height), buffer.raw, "raw", "BGRX", 0, 1).save(destination)
    print(destination)
finally:
    gdi.SelectObject(memory, previous)
    gdi.DeleteObject(bitmap)
    gdi.DeleteDC(memory)
    user.ReleaseDC(handle, dc)
