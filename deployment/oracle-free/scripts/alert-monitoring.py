#!/usr/bin/env python3
"""Send disk, offsite backup, and collector alerts from local metrics."""

import argparse
import json
import math
import os
import smtplib
import ssl
import time
from email.message import EmailMessage
from pathlib import Path


METRICS = Path("/var/lib/systemcel-monitoring/systemcel.prom")
STATE = Path("/var/lib/systemcel-monitoring/alert-state.json")
REPEAT_SECONDS = 30 * 60
STALE_SECONDS = 3 * 60


def read_metrics(path):
    values = {}
    for line in path.read_text(encoding="utf-8").splitlines():
        if not line or line.startswith("#"):
            continue
        name, _, raw = line.partition(" ")
        if name in (
            "systemcel_host_disk_usage_percent",
            "systemcel_offsite_backup_state_valid",
            "systemcel_offsite_backup_age_seconds",
        ):
            value = float(raw.strip())
            if not math.isfinite(value):
                raise ValueError(f"Invalid metric: {name}")
            values[name] = value
    return values


def evaluate(metrics, fresh):
    if not fresh:
        return {"collector": ("critical", "İzleme ölçümleri üç dakikadan eski veya okunamıyor.")}

    disk = metrics.get("systemcel_host_disk_usage_percent")
    backup_valid = metrics.get("systemcel_offsite_backup_state_valid")
    backup_age = metrics.get("systemcel_offsite_backup_age_seconds")
    results = {"collector": ("ok", "İzleme ölçümleri yeniden güncel.")}
    if disk is None or disk < 0 or disk > 100:
        results["disk"] = ("critical", "Disk kullanım ölçümü geçersiz.")
    elif disk > 85:
        results["disk"] = ("critical", f"Disk kullanımı %{disk:g} (kritik eşik >%85).")
    elif disk > 70:
        results["disk"] = ("warning", f"Disk kullanımı %{disk:g} (uyarı eşiği >%70).")
    else:
        results["disk"] = ("ok", f"Disk kullanımı yeniden normal: %{disk:g}.")

    if backup_valid != 1 or backup_age is None or backup_age < 0:
        results["backup"] = ("critical", "Doğrulanmış uzak yedek bilgisi yok veya geçersiz.")
    elif backup_age > 36 * 3600:
        results["backup"] = ("critical", f"Son doğrulanmış uzak yedek {backup_age / 3600:.1f} saat önce (kritik eşik >36 saat).")
    elif backup_age > 26 * 3600:
        results["backup"] = ("warning", f"Son doğrulanmış uzak yedek {backup_age / 3600:.1f} saat önce (uyarı eşiği >26 saat).")
    else:
        results["backup"] = ("ok", f"Uzak yedek yaşı yeniden normal: {backup_age / 3600:.1f} saat.")
    return results


def pending_events(results, state, now):
    events = []
    for name, (level, description) in results.items():
        previous = state.get(name, {})
        previous_level = previous.get("level", "ok")
        last_sent = previous.get("last_sent", 0)
        if level != previous_level or (level == "critical" and now - last_sent >= REPEAT_SECONDS):
            if level != "ok" or previous_level != "ok":
                events.append((name, level, description))
    return events


def send_alert(events):
    host = os.environ["SYSTEMCEL_SMTP_HOST"]
    port = int(os.environ.get("SYSTEMCEL_SMTP_PORT", "587"))
    sender = os.environ["SYSTEMCEL_SMTP_FROM_ADDRESS"]
    recipient = os.environ["SYSTEMCEL_ALERT_EMAIL"]
    username = os.environ["SYSTEMCEL_SMTP_USERNAME"]
    password = os.environ["SYSTEMCEL_SMTP_PASSWORD"]
    message = EmailMessage()
    message["From"] = sender
    message["To"] = recipient
    levels = {level for _, level, _ in events}
    status = "kritik" if "critical" in levels else "uyarı" if "warning" in levels else "düzeldi"
    message["Subject"] = f"Systemcel izleme: {status}"
    message.set_content("\n".join(f"{name}: {description}" for name, _, description in events))
    context = ssl.create_default_context()
    if port == 465:
        with smtplib.SMTP_SSL(host, port, timeout=15, context=context) as smtp:
            smtp.login(username, password)
            smtp.send_message(message)
    else:
        with smtplib.SMTP(host, port, timeout=15) as smtp:
            smtp.starttls(context=context)
            smtp.login(username, password)
            smtp.send_message(message)


def write_state(path, state):
    path.parent.mkdir(mode=0o750, parents=True, exist_ok=True)
    temporary = path.with_suffix(".tmp")
    with temporary.open("w", encoding="utf-8") as output:
        json.dump(state, output)
    temporary.chmod(0o600)
    temporary.replace(path)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--metrics", type=Path, default=METRICS)
    parser.add_argument("--state", type=Path, default=STATE)
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()
    now = time.time()
    try:
        metrics = read_metrics(args.metrics)
        metric_age = now - args.metrics.stat().st_mtime
        fresh = -60 <= metric_age <= STALE_SECONDS
    except (OSError, ValueError):
        metrics, fresh = {}, False
    results = evaluate(metrics, fresh)
    try:
        state = json.loads(args.state.read_text(encoding="utf-8"))
        if not isinstance(state, dict):
            state = {}
    except (OSError, ValueError):
        state = {}
    events = pending_events(results, state, now)
    if args.dry_run:
        for name, level, description in events:
            print(f"{name}: {level}: {description}")
        return
    if events:
        send_alert(events)
    for name, level, _ in events:
        state[name] = {"level": level, "last_sent": now}
    if events:
        write_state(args.state, state)


if __name__ == "__main__":
    main()
