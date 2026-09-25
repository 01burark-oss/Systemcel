#!/usr/bin/env python3
"""Synthetic alert thresholds and notification state, without touching live metrics."""

import importlib.util
from pathlib import Path


script = Path(__file__).resolve().parents[1] / "scripts" / "alert-monitoring.py"
spec = importlib.util.spec_from_file_location("alert_monitoring", script)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


def check(metrics, expected_disk, expected_backup):
    result = module.evaluate(metrics, True)
    assert result["disk"][0] == expected_disk, result
    assert result["backup"][0] == expected_backup, result
    return result


base = {
    "systemcel_host_disk_usage_percent": 35,
    "systemcel_offsite_backup_state_valid": 1,
    "systemcel_offsite_backup_age_seconds": 3600,
}
check(base, "ok", "ok")
check({**base, "systemcel_host_disk_usage_percent": 70}, "ok", "ok")
check({**base, "systemcel_host_disk_usage_percent": 71}, "warning", "ok")
check({**base, "systemcel_host_disk_usage_percent": 86}, "critical", "ok")
check({**base, "systemcel_offsite_backup_age_seconds": 26 * 3600}, "ok", "ok")
check({**base, "systemcel_offsite_backup_age_seconds": 26 * 3600 + 1}, "ok", "warning")
check({**base, "systemcel_offsite_backup_age_seconds": 36 * 3600 + 1}, "ok", "critical")
check({**base, "systemcel_offsite_backup_state_valid": 0}, "ok", "critical")
assert module.evaluate({}, False)["collector"][0] == "critical"

critical = check({**base, "systemcel_host_disk_usage_percent": 86}, "critical", "ok")
events = module.pending_events(critical, {}, 1000)
assert [(name, level) for name, level, _ in events] == [("disk", "critical")]
state = {"disk": {"level": "critical", "last_sent": 1000}}
assert module.pending_events(critical, state, 1000 + module.REPEAT_SECONDS - 1) == []
assert module.pending_events(critical, state, 1000 + module.REPEAT_SECONDS)[0][:2] == ("disk", "critical")
recovered = module.pending_events(module.evaluate(base, True), state, 1001)
assert [(name, level) for name, level, _ in recovered] == [("disk", "ok")]
assert module.pending_events(module.evaluate(base, True), {}, 1001) == []
print("alert-monitoring smoke: OK")
