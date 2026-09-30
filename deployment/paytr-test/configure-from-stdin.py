#!/usr/bin/env python3
"""Accept setup over an authenticated SSH stdin; never log input or credentials."""
import ipaddress
import json
import os
from pathlib import Path
import secrets
import sys

root = Path(__file__).resolve().parent
os.umask(0o077)

def configure():
    raw = sys.stdin.buffer.read(4097)
    if len(raw) > 4096:
        raise ValueError()
    data = json.loads(raw)
    customer_ip = ipaddress.IPv4Address(data["customerIp"])
    if not customer_ip.is_global:
        raise ValueError()
    key, salt = data.get("key", ""), data.get("salt", "")
    if not all(isinstance(value, str) and len(value) <= 256 and
               not any(ord(char) < 32 or ord(char) == 127 for char in value)
               for value in (key, salt)):
        raise ValueError()
    if bool(key) != bool(salt):
        raise ValueError()
    base = root / ".env"
    secret_file = root / ".env.secrets"
    if secret_file.exists() and secret_file.stat().st_size:
        raise ValueError()
    if not base.exists():
        with base.open("x", encoding="utf-8", newline="\n") as target:
            target.write("PAYTR_TEST_HOST=paytr-test.systemcel.app\n"
                         "PAYTR_MERCHANT_ID=753709\n"
                         "PRODUCTION_EDGE_NETWORK=systemcel_edge\n"
                         f"TEST_CUSTOMER_IP={customer_ip}\n"
                         f"POSTGRES_PASSWORD={secrets.token_hex(32)}\n")
    pending = root / ".env.secrets.pending"
    with pending.open("x", encoding="utf-8", newline="\n") as target:
        if key:
            target.write(f"SYSTEMCEL_PAYTR_MERCHANT_KEY={key}\n"
                         f"SYSTEMCEL_PAYTR_MERCHANT_SALT={salt}\n")
    os.replace(pending, secret_file)
    os.chmod(base, 0o600)
    os.chmod(secret_file, 0o600)
    print("CONFIGURED" if key else "BASE_READY")

try:
    configure()
except (ValueError, KeyError, TypeError, OSError):
    print("SETUP_REJECTED", file=sys.stderr)
    sys.exit(1)
