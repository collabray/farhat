"""Shared helpers for the Farhat Unity Builder scripts. Standard library only."""
import hashlib
import json
import os
import sys
import time
import urllib.error
import urllib.request

SKILL_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
DATA_DIR = os.path.join(SKILL_ROOT, "data")
USER_AGENT = "FarhatUnityBuilder/1.0 (Unity game build skill; contact: see repository README)"

EXPECTED_VERIFIED = [1, 2, 3, 5, 7, 8, 11, 13, 14, 15, 17, 18, 19, 21, 22, 23, 25, 26, 33, 35, 39]


def load_json(path):
    with open(path, "r", encoding="utf-8") as handle:
        return json.load(handle)


def save_json(path, data):
    directory = os.path.dirname(os.path.abspath(path))
    os.makedirs(directory, exist_ok=True)
    tmp = path + ".tmp"
    with open(tmp, "w", encoding="utf-8") as handle:
        json.dump(data, handle, indent=2, ensure_ascii=False)
        handle.write("\n")
    os.replace(tmp, path)


def load_chapters(path=None):
    return load_json(path or os.path.join(DATA_DIR, "chapters.json"))


def sha256_file(path):
    digest = hashlib.sha256()
    with open(path, "rb") as handle:
        for block in iter(lambda: handle.read(1 << 20), b""):
            digest.update(block)
    return digest.hexdigest()


def md5_file(path):
    digest = hashlib.md5()
    with open(path, "rb") as handle:
        for block in iter(lambda: handle.read(1 << 20), b""):
            digest.update(block)
    return digest.hexdigest()


def http_get(url, headers=None, retries=3, timeout=60):
    """GET with a descriptive User-Agent and exponential backoff. Returns bytes."""
    request_headers = {"User-Agent": USER_AGENT}
    if headers:
        request_headers.update(headers)
    last_error = None
    for attempt in range(retries):
        try:
            request = urllib.request.Request(url, headers=request_headers)
            with urllib.request.urlopen(request, timeout=timeout) as response:
                return response.read()
        except urllib.error.HTTPError as error:
            last_error = error
            if error.code in (401, 403, 404):
                break
        except (urllib.error.URLError, TimeoutError) as error:
            last_error = error
        time.sleep(1.5 ** attempt)
    raise RuntimeError("GET failed for %s: %s" % (url, last_error))


def http_get_json(url, headers=None):
    return json.loads(http_get(url, headers).decode("utf-8"))


def download_file(url, destination, headers=None, max_bytes=2 * 1024 ** 3):
    """Download to destination via a temporary file. Refuses files larger than max_bytes."""
    os.makedirs(os.path.dirname(os.path.abspath(destination)), exist_ok=True)
    request_headers = {"User-Agent": USER_AGENT}
    if headers:
        request_headers.update(headers)
    request = urllib.request.Request(url, headers=request_headers)
    tmp = destination + ".part"
    total = 0
    with urllib.request.urlopen(request, timeout=120) as response, open(tmp, "wb") as handle:
        while True:
            block = response.read(1 << 20)
            if not block:
                break
            total += len(block)
            if total > max_bytes:
                handle.close()
                os.remove(tmp)
                raise RuntimeError("Download exceeds the %d byte limit: %s" % (max_bytes, url))
            handle.write(block)
    os.replace(tmp, destination)
    return destination


def fail(message, code=1):
    sys.stderr.write("error: %s\n" % message)
    sys.exit(code)
