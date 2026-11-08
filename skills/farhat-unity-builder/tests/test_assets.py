import hashlib
import io
import json
import os
import sys
import tempfile
import threading
import unittest
import zipfile
from http.server import BaseHTTPRequestHandler, HTTPServer
from urllib.parse import urlparse

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
sys.path.insert(0, os.path.join(ROOT, "scripts"))

import asset_fetch  # noqa: E402
import ledger  # noqa: E402


def md5(data):
    return hashlib.md5(data).hexdigest()


def make_zip(entries):
    buffer = io.BytesIO()
    with zipfile.ZipFile(buffer, "w") as archive:
        for name, data in entries.items():
            archive.writestr(name, data)
    return buffer.getvalue()


class MockServer:
    """A tiny local HTTP server that mimics the shapes of the Poly Haven and Sketchfab APIs (as documented in the vendors' schemas)."""

    def __init__(self):
        self.files = {}
        self.routes = {}
        self.requests = []
        outer = self

        class Handler(BaseHTTPRequestHandler):
            def log_message(self, *args):
                pass

            def do_GET(self):
                outer.requests.append((self.path, dict(self.headers)))
                path = urlparse(self.path).path
                if path in outer.files:
                    self._send(200, outer.files[path], "application/octet-stream")
                    return
                for prefix, handler in outer.routes.items():
                    if self.path.startswith(prefix):
                        status, payload = handler(self.headers)
                        self._send(status, json.dumps(payload).encode(), "application/json")
                        return
                self._send(404, b"{}", "application/json")

            def _send(self, status, body, ctype):
                self.send_response(status)
                self.send_header("Content-Type", ctype)
                self.send_header("Content-Length", str(len(body)))
                self.end_headers()
                self.wfile.write(body)

        self.httpd = HTTPServer(("127.0.0.1", 0), Handler)
        self.base = "http://127.0.0.1:%d" % self.httpd.server_port
        self.thread = threading.Thread(target=self.httpd.serve_forever, daemon=True)
        self.thread.start()

    def close(self):
        self.httpd.shutdown()
        self.httpd.server_close()


class LicenseTests(unittest.TestCase):
    def test_normalize(self):
        cases = {
            "cc0": "cc0", "CC0 Public Domain": "cc0", "CC0-1.0": "cc0", "Public Domain": "cc0",
            "by": "cc-by", "CC Attribution": "cc-by", "CC-BY-4.0": "cc-by", "Creative Commons - Attribution": "cc-by",
            "by-nc": None, "CC Attribution-NonCommercial": None, "by-nd": None, "by-sa": None, "CC-BY-SA-4.0": None,
            "by-nc-sa": None, "SIL Open Font License 1.1": "ofl", "OFL-1.1": "ofl", "editorial": None, "standard": None, "": None, None: None, "All rights reserved": None,
        }
        for label, expected in cases.items():
            self.assertEqual(ledger.normalize_license(label), expected, label)


class PolyHavenTests(unittest.TestCase):
    def setUp(self):
        self.server = MockServer()
        self.tmp = tempfile.TemporaryDirectory()
        gltf, binary, tex = b'{"asset":{"version":"2.0"}}', b"\x00\x01\x02\x03", b"jpegdata"
        self.server.files = {"/f/barrel.gltf": gltf, "/f/barrel.bin": binary, "/f/diff.jpg": tex}
        base = self.server.base
        self.server.routes = {
            "/assets": lambda h: (200, {
                "wooden_barrel": {"name": "Wooden Barrel", "type": 2, "categories": ["props"], "tags": ["barrel", "wood"], "authors": {"Jane": "All"}, "download_count": 900},
                "stone_pillar": {"name": "Stone Pillar", "type": 2, "categories": ["architecture"], "tags": ["stone"], "authors": {"Joe": "All"}, "download_count": 100}}),
            "/info/wooden_barrel": lambda h: (200, {"name": "Wooden Barrel", "authors": {"Jane": "All"}}),
            "/files/wooden_barrel": lambda h: (200, {"gltf": {"1k": {"gltf": {
                "url": base + "/f/barrel.gltf", "md5": md5(gltf), "size": len(gltf),
                "include": {"barrel.bin": {"url": base + "/f/barrel.bin", "md5": md5(binary)},
                            "textures/diff.jpg": {"url": base + "/f/diff.jpg", "md5": md5(tex)}}}}}}),
            "/info/bad_sum": lambda h: (200, {"name": "Bad", "authors": {"X": "All"}}),
            "/files/bad_sum": lambda h: (200, {"gltf": {"1k": {"gltf": {"url": base + "/f/barrel.gltf", "md5": "0" * 32, "include": {}}}}}),
        }
        asset_fetch.POLYHAVEN_BASE = base
        self.ledger = os.path.join(self.tmp.name, "ledger.json")

    def tearDown(self):
        self.server.close()
        self.tmp.cleanup()

    def test_search_filters_by_every_query_word_and_sorts_by_popularity(self):
        rows = asset_fetch.ph_search("models", "wood barrel", 10)
        self.assertEqual([r[0] for r in rows], ["wooden_barrel"])
        rows = asset_fetch.ph_search("models", "", 10)
        self.assertEqual([r[0] for r in rows], ["wooden_barrel", "stone_pillar"])

    def test_download_verifies_checksums_writes_the_ledger_and_credits(self):
        out = os.path.join(self.tmp.name, "assets")
        saved = asset_fetch.ph_download("models", "wooden_barrel", "1k", "gltf", out, self.ledger, self.tmp.name)
        self.assertEqual(len(saved), 3)
        for path in saved:
            self.assertTrue(os.path.exists(path))
        self.assertTrue(os.path.exists(os.path.join(out, "wooden_barrel", "textures", "diff.jpg")))
        data = ledger.load_ledger(self.ledger)
        self.assertEqual(len(data["entries"]), 1)
        entry = data["entries"][0]
        self.assertEqual(entry["license"], "cc0")
        self.assertEqual(entry["author"], "Jane")
        self.assertEqual(ledger.verify(self.ledger, self.tmp.name), [])
        self.assertIn("Powered by Poly Haven", ledger.credits_markdown(self.ledger))
        with open(saved[0], "ab") as handle:
            handle.write(b"tampered")
        problems = ledger.verify(self.ledger, self.tmp.name)
        self.assertTrue(any("checksum changed" in p for p in problems))

    def test_checksum_mismatch_aborts_and_removes_the_file(self):
        out = os.path.join(self.tmp.name, "assets")
        with self.assertRaises(SystemExit):
            asset_fetch.ph_download("models", "bad_sum", "1k", "gltf", out, self.ledger, self.tmp.name)
        self.assertFalse(os.path.exists(os.path.join(out, "bad_sum", "barrel.gltf")))
        self.assertFalse(os.path.exists(self.ledger))

    def test_user_agent_is_sent(self):
        asset_fetch.ph_search("models", "", 1)
        agent = [h for p, h in self.server.requests if p.startswith("/assets")][0].get("User-Agent", "")
        self.assertIn("FarhatUnityBuilder", agent)


class SketchfabTests(unittest.TestCase):
    def setUp(self):
        self.server = MockServer()
        self.tmp = tempfile.TemporaryDirectory()
        base = self.server.base
        models = {
            "u1": {"uid": "u1", "name": "Ruined Wall", "license": {"slug": "cc0", "label": "CC0 Public Domain", "url": "https://creativecommons.org/publicdomain/zero/1.0/"}, "user": {"displayName": "Ana"}, "viewerUrl": "https://sketchfab.com/3d-models/u1"},
            "u2": {"uid": "u2", "name": "NC Statue", "license": {"slug": "by-nc", "label": "CC Attribution-NonCommercial"}, "user": {"displayName": "Bo"}},
            "u3": {"uid": "u3", "name": "Farhat Fan Model", "license": {"slug": "cc0", "label": "CC0 Public Domain"}, "user": {"displayName": "Fan"}},
            "u4": {"uid": "u4", "name": "Oil Lamp", "license": {"slug": "by", "label": "CC Attribution", "url": "https://creativecommons.org/licenses/by/4.0/"}, "user": {"displayName": "Cy"}, "viewerUrl": "https://sketchfab.com/3d-models/u4"},
        }
        self.server.files = {"/f/model.zip": make_zip({"scene.gltf": "{}", "scene.bin": "bin"})}

        def token_required(headers):
            if headers.get("Authorization") != "Token secret-token":
                return 401, {"detail": "no token"}
            return 200, {"gltf": {"url": base + "/f/model.zip", "size": 100, "expires": 300}}

        self.server.routes = {
            "/search": lambda h: (200, {"results": [{k: v for k, v in m.items() if k != "user"} for m in models.values()]}),
            "/models/u1/download": token_required, "/models/u4/download": token_required,
            "/models/u2/download": token_required, "/models/u3/download": token_required,
            "/models/u1": lambda h: (200, models["u1"]), "/models/u2": lambda h: (200, models["u2"]),
            "/models/u3": lambda h: (200, models["u3"]), "/models/u4": lambda h: (200, models["u4"]),
        }
        asset_fetch.SKETCHFAB_BASE = base
        self.ledger = os.path.join(self.tmp.name, "ledger.json")
        os.environ.pop("SKETCHFAB_API_TOKEN", None)

    def tearDown(self):
        os.environ.pop("SKETCHFAB_API_TOKEN", None)
        self.server.close()
        self.tmp.cleanup()

    def test_search_keeps_only_cc0_and_cc_by(self):
        rows = asset_fetch.sf_search("wall", ["cc0", "by"], 24)
        self.assertEqual(sorted(m["uid"] for m, _ in rows), ["u1", "u3", "u4"])
        only_cc0 = asset_fetch.sf_search("wall", ["cc0"], 24)
        self.assertEqual(sorted(m["uid"] for m, _ in only_cc0), ["u1", "u3"])

    def test_download_needs_a_token(self):
        with self.assertRaises(SystemExit):
            asset_fetch.sf_download("u1", self.tmp.name, self.ledger, self.tmp.name)

    def test_download_sends_the_token_extracts_and_records_attribution(self):
        os.environ["SKETCHFAB_API_TOKEN"] = "secret-token"
        files = asset_fetch.sf_download("u4", os.path.join(self.tmp.name, "out"), self.ledger, self.tmp.name)
        self.assertEqual(len(files), 2)
        entry = ledger.load_ledger(self.ledger)["entries"][0]
        self.assertEqual(entry["license"], "cc-by")
        self.assertIn("Oil Lamp", entry["attribution"])
        self.assertIn("Cy", entry["attribution"])
        self.assertEqual(ledger.verify(self.ledger, self.tmp.name), [])
        self.assertIn("Oil Lamp", ledger.credits_markdown(self.ledger))

    def test_non_commercial_models_are_refused(self):
        os.environ["SKETCHFAB_API_TOKEN"] = "secret-token"
        with self.assertRaises(SystemExit):
            asset_fetch.sf_download("u2", self.tmp.name, self.ledger, self.tmp.name)

    def test_fan_art_of_licensed_characters_is_refused(self):
        os.environ["SKETCHFAB_API_TOKEN"] = "secret-token"
        with self.assertRaises(SystemExit):
            asset_fetch.sf_download("u3", self.tmp.name, self.ledger, self.tmp.name)


class ArchiveAndUrlTests(unittest.TestCase):
    def test_zip_slip_is_rejected(self):
        with tempfile.TemporaryDirectory() as tmp:
            bad = os.path.join(tmp, "bad.zip")
            with open(bad, "wb") as handle:
                handle.write(make_zip({"../evil.txt": "x"}))
            with self.assertRaises(RuntimeError):
                asset_fetch.safe_extract(bad, os.path.join(tmp, "dest"))
            self.assertFalse(os.path.exists(os.path.join(tmp, "evil.txt")))

    def test_url_download_requires_an_allowed_licence(self):
        server = MockServer()
        try:
            server.files = {"/pack.zip": make_zip({"a.fbx": "fbx"})}
            with tempfile.TemporaryDirectory() as tmp:
                ledger_path = os.path.join(tmp, "ledger.json")
                with self.assertRaises(SystemExit):
                    asset_fetch.url_download(server.base + "/pack.zip", "Pack", "Author", "by-nc", "https://example.org/pack", os.path.join(tmp, "o"), ledger_path, tmp)
                files = asset_fetch.url_download(server.base + "/pack.zip", "Fantasy Pack", "Author", "CC0", "https://example.org/pack", os.path.join(tmp, "o"), ledger_path, tmp)
                self.assertEqual(len(files), 1)
                entry = ledger.load_ledger(ledger_path)["entries"][0]
                self.assertEqual(entry["license"], "cc0")
                self.assertEqual(ledger.verify(ledger_path, tmp), [])
        finally:
            server.close()


if __name__ == "__main__":
    unittest.main()
