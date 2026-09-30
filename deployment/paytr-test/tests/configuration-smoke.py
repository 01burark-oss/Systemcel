"""Exercise the SSH configuration boundary using disposable files and fake secrets."""
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest


class ConfigurationBoundary(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        self.script = self.root / "configure-from-stdin.py"
        shutil.copyfile(Path(__file__).resolve().parents[1] / self.script.name, self.script)

    def tearDown(self):
        self.temp.cleanup()

    def configure(self, payload):
        raw = payload if isinstance(payload, bytes) else json.dumps(payload).encode()
        return subprocess.run([sys.executable, str(self.script)], input=raw, capture_output=True)

    def test_base_then_keys_and_no_overwrite(self):
        first = self.configure({"customerIp": "8.8.8.8"})
        self.assertEqual(first.returncode, 0)
        base = (self.root / ".env").read_bytes()
        self.assertEqual((self.root / ".env.secrets").read_bytes(), b"")
        configured = self.configure({"customerIp": "8.8.8.8", "key": "fake$test=key", "salt": "fake$test=salt"})
        self.assertEqual(configured.returncode, 0)
        self.assertEqual(configured.stdout.strip(), b"CONFIGURED")
        secrets_before = (self.root / ".env.secrets").read_bytes()
        self.assertIn(b"=fake$test=key\n", secrets_before)
        self.assertEqual((self.root / ".env").read_bytes(), base)
        rejected = self.configure({"customerIp": "8.8.8.8", "key": "different", "salt": "different"})
        self.assertNotEqual(rejected.returncode, 0)
        self.assertEqual((self.root / ".env.secrets").read_bytes(), secrets_before)
        self.assertNotIn(b"fake", configured.stdout + configured.stderr + rejected.stdout + rejected.stderr)
        if os.name != "nt":
            for name in (".env", ".env.secrets"):
                self.assertEqual((self.root / name).stat().st_mode & 0o777, 0o600)

    def test_rejects_injection_partial_secrets_and_nonpublic_ip(self):
        for data in (
            {"customerIp": "127.0.0.1"},
            {"customerIp": "10.0.0.1"},
            {"customerIp": "::1"},
            {"customerIp": "8.8.8.8", "key": "one"},
            {"customerIp": "8.8.8.8", "key": "one\nSYSTEMCEL_PAYTR_TEST_MODE=false", "salt": "two"},
            {"customerIp": "8.8.8.8", "key": "one", "salt": "two\r"},
            {"customerIp": "8.8.8.8", "key": "x" * 257, "salt": "two"},
            [],
            None,
        ):
            with self.subTest(data_type=type(data).__name__):
                result = self.configure(data)
                self.assertNotEqual(result.returncode, 0)
                self.assertFalse((self.root / ".env.secrets").exists())
                self.assertNotIn(b"SYSTEMCEL_PAYTR_TEST_MODE", result.stdout + result.stderr)

    def test_rejects_oversized_or_malformed_input(self):
        for raw in (b"x" * 4097, b"{invalid", b"\xff"):
            result = self.configure(raw)
            self.assertNotEqual(result.returncode, 0)
            self.assertFalse((self.root / ".env").exists())


if __name__ == "__main__":
    unittest.main()
