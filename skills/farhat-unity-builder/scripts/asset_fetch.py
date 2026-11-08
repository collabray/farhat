#!/usr/bin/env python3
"""Search and download licensed 3D assets, textures and HDRIs, and record every download in the licence ledger.

Sources
  polyhaven   CC0 models, HDRIs and textures through the public API (https://api.polyhaven.com). No key needed.
  sketchfab   Search is public. Download needs a personal API token in SKETCHFAB_API_TOKEN. Only CC0 and CC BY models are accepted.
  url         Any direct download link (for example a Kenney or Quaternius CC0 pack). You must state the licence and author.

Examples
  asset_fetch.py polyhaven search --type models --query barrel
  asset_fetch.py polyhaven download --type models --id <asset id> --res 1k --out Assets/ThirdParty/polyhaven
  asset_fetch.py sketchfab search --query "desert ruins" --license cc0,by
  asset_fetch.py sketchfab download --uid <model uid> --out Assets/ThirdParty/sketchfab
  asset_fetch.py url download --url <zip link> --name "Pack name" --author "Author" --license cc0 --source-url <page> --out Assets/ThirdParty/packs

Never download assets of the licensed characters (Farhat, Tilda, Wings, Ben and the rest) from fan or marketplace sites: those are
third-party copies of someone else's artwork. Characters are modelled from the rights holder's model sheets.
"""
import argparse
import os
import sys
import urllib.parse
import zipfile

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import ledger  # noqa: E402
from _common import download_file, fail, http_get_json, md5_file, sha256_file  # noqa: E402

POLYHAVEN_BASE = os.environ.get("POLYHAVEN_API_BASE", "https://api.polyhaven.com")
SKETCHFAB_BASE = os.environ.get("SKETCHFAB_API_BASE", "https://api.sketchfab.com/v3")
DEFAULT_LEDGER = os.path.join("Assets", "ThirdParty", "ledger.json")
BLOCKED_NAME_WORDS = ["farhat", "tilda swanson", "egokhan", "mondo tv", "ikaghan", "snarchkoff"]


def check_not_licensed_character(name):
    lowered = (name or "").lower()
    for word in BLOCKED_NAME_WORDS:
        if word in lowered:
            fail("'%s' looks like fan or third-party artwork of a licensed character. Characters are modelled from the rights holder's model sheets, not downloaded." % name)


def safe_extract(zip_path, destination):
    """Extract a zip while refusing paths that escape the destination (zip-slip)."""
    root = os.path.realpath(destination)
    extracted = []
    with zipfile.ZipFile(zip_path) as archive:
        for member in archive.infolist():
            target = os.path.realpath(os.path.join(destination, member.filename))
            if target != root and not target.startswith(root + os.sep):
                raise RuntimeError("Unsafe path in archive: %s" % member.filename)
        archive.extractall(destination)
        for member in archive.infolist():
            if not member.is_dir():
                extracted.append(os.path.join(destination, member.filename))
    return extracted


def file_records(paths, base):
    return [{"path": os.path.relpath(p, base).replace(os.sep, "/"), "sha256": sha256_file(p)} for p in paths]


# ---------------------------------------------------------------- Poly Haven
def ph_search(kind, query, limit):
    data = http_get_json("%s/assets?type=%s&t=%s" % (POLYHAVEN_BASE, kind, kind))
    tokens = [t for t in (query or "").lower().split() if t]
    rows = []
    for asset_id, info in data.items():
        hay = " ".join([asset_id, info.get("name", "")] + list(info.get("tags", [])) + list(info.get("categories", []))).lower()
        if all(t in hay for t in tokens):
            rows.append((asset_id, info))
    rows.sort(key=lambda r: -int(r[1].get("download_count", 0)))
    return rows[:limit]


def ph_files_for(kind, files, res, fmt):
    """Return a list of (relative_path, url, md5) for the chosen asset kind."""
    out = []
    if kind == "models":
        entry = files[fmt][res][fmt]
        out.append((os.path.basename(entry["url"]), entry["url"], entry.get("md5")))
        for rel, inc in entry.get("include", {}).items():
            out.append((rel, inc["url"], inc.get("md5")))
    elif kind == "hdris":
        node = files["hdri"][res]
        chosen = node.get(fmt) or node.get("hdr") or node.get("exr")
        out.append((os.path.basename(chosen["url"]), chosen["url"], chosen.get("md5")))
    else:
        for map_name, by_res in files.items():
            if not isinstance(by_res, dict) or res not in by_res:
                continue
            node = by_res[res]
            chosen = node.get("jpg") or node.get("png") or node.get(fmt)
            if chosen and "url" in chosen:
                out.append((map_name + "_" + os.path.basename(chosen["url"]), chosen["url"], chosen.get("md5")))
    return out


def ph_download(kind, asset_id, res, fmt, out_dir, ledger_path, base):
    info = http_get_json("%s/info/%s" % (POLYHAVEN_BASE, urllib.parse.quote(asset_id)))
    files = http_get_json("%s/files/%s" % (POLYHAVEN_BASE, urllib.parse.quote(asset_id)))
    check_not_licensed_character(info.get("name", asset_id))
    target = os.path.join(out_dir, asset_id)
    saved = []
    for rel, url, md5 in ph_files_for(kind, files, res, fmt):
        destination = os.path.join(target, rel)
        download_file(url, destination)
        if md5 and md5_file(destination) != md5:
            os.remove(destination)
            fail("Checksum mismatch for %s" % rel)
        saved.append(destination)
    if not saved:
        fail("No files found for %s at resolution %s" % (asset_id, res))
    authors = ", ".join(sorted(info.get("authors", {}).keys())) or "Poly Haven"
    entry = {"id": asset_id, "name": info.get("name", asset_id), "source": "polyhaven", "sourceUrl": "https://polyhaven.com/a/" + asset_id,
             "author": authors, "license": "cc0", "licenseUrl": "https://creativecommons.org/publicdomain/zero/1.0/",
             "attribution": "%s by %s, CC0, via Poly Haven (https://polyhaven.com/a/%s)" % (info.get("name", asset_id), authors, asset_id),
             "kind": kind, "files": file_records(saved, base)}
    ledger.add_entry(ledger_path, entry)
    return saved


# ----------------------------------------------------------------- Sketchfab
def sf_license_label(model):
    lic = model.get("license")
    if isinstance(lic, dict):
        return lic.get("slug") or lic.get("label") or lic.get("fullName")
    return lic


def sf_search(query, licenses, count):
    wanted = [ledger.normalize_license(x) or x for x in licenses] if licenses else []
    params = {"type": "models", "q": query, "downloadable": "true", "count": str(min(max(count, 1), 24))}
    results = http_get_json("%s/search?%s" % (SKETCHFAB_BASE, urllib.parse.urlencode(params))).get("results", [])
    rows = []
    for model in results:
        label = sf_license_label(model)
        if label is None:
            model = http_get_json("%s/models/%s" % (SKETCHFAB_BASE, model["uid"]))
            label = sf_license_label(model)
        normal = ledger.normalize_license(label)
        if normal is None:
            continue
        if wanted and normal not in wanted:
            continue
        rows.append((model, normal))
    return rows


def sf_download(uid, out_dir, ledger_path, base):
    token = os.environ.get("SKETCHFAB_API_TOKEN")
    if not token:
        fail("Set SKETCHFAB_API_TOKEN to a personal Sketchfab API token to download models.")
    model = http_get_json("%s/models/%s" % (SKETCHFAB_BASE, uid))
    check_not_licensed_character(model.get("name", uid))
    label = sf_license_label(model)
    normal = ledger.normalize_license(label)
    if normal is None:
        fail("Licence %r is not allowed (only CC0 and CC BY). Pick another model." % label)
    links = http_get_json("%s/models/%s/download" % (SKETCHFAB_BASE, uid), headers={"Authorization": "Token " + token})
    if "gltf" not in links:
        fail("The model has no glTF download.")
    zip_path = os.path.join(out_dir, uid + ".zip")
    download_file(links["gltf"]["url"], zip_path)
    target = os.path.join(out_dir, uid)
    extracted = safe_extract(zip_path, target)
    os.remove(zip_path)
    user = (model.get("user") or {})
    author = user.get("displayName") or user.get("username") or "unknown author"
    url = model.get("viewerUrl") or "https://sketchfab.com/3d-models/" + uid
    license_label = (model.get("license") or {}).get("label") if isinstance(model.get("license"), dict) else str(label)
    entry = {"id": uid, "name": model.get("name", uid), "source": "sketchfab", "sourceUrl": url, "author": author, "license": normal,
             "licenseUrl": (model.get("license") or {}).get("url", "") if isinstance(model.get("license"), dict) else "",
             "attribution": '"%s" by %s (%s) is licensed under %s' % (model.get("name", uid), author, url, license_label),
             "kind": "model", "files": file_records(extracted, base)}
    ledger.add_entry(ledger_path, entry)
    return extracted


# ------------------------------------------------------------------- generic URL
def url_download(url, name, author, license_label, source_url, out_dir, ledger_path, base):
    check_not_licensed_character(name)
    normal = ledger.normalize_license(license_label)
    if normal is None:
        fail("Licence %r is not allowed (only CC0 and CC BY)." % license_label)
    file_name = os.path.basename(urllib.parse.urlparse(url).path) or "download.bin"
    destination = os.path.join(out_dir, file_name)
    download_file(url, destination)
    saved = [destination]
    if destination.lower().endswith(".zip"):
        folder = os.path.join(out_dir, os.path.splitext(file_name)[0])
        saved = safe_extract(destination, folder)
        os.remove(destination)
    entry = {"id": os.path.splitext(file_name)[0], "name": name, "source": "url", "sourceUrl": source_url, "author": author, "license": normal,
             "licenseUrl": "", "attribution": "%s by %s (%s), %s" % (name, author, source_url, {"cc0": "CC0", "cc-by": "CC BY", "ofl": "SIL OFL 1.1"}[normal]),
             "kind": "pack", "files": file_records(saved, base)}
    ledger.add_entry(ledger_path, entry)
    return saved


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--ledger", default=DEFAULT_LEDGER)
    parser.add_argument("--base", default=".", help="Project root; ledger file paths are stored relative to it")
    sub = parser.add_subparsers(dest="source", required=True)

    ph = sub.add_parser("polyhaven").add_subparsers(dest="action", required=True)
    ph_s = ph.add_parser("search")
    ph_s.add_argument("--type", choices=["models", "hdris", "textures"], required=True)
    ph_s.add_argument("--query", default="")
    ph_s.add_argument("--limit", type=int, default=15)
    ph_d = ph.add_parser("download")
    ph_d.add_argument("--type", choices=["models", "hdris", "textures"], required=True)
    ph_d.add_argument("--id", required=True)
    ph_d.add_argument("--res", default="1k")
    ph_d.add_argument("--format", default="gltf")
    ph_d.add_argument("--out", required=True)

    sf = sub.add_parser("sketchfab").add_subparsers(dest="action", required=True)
    sf_s = sf.add_parser("search")
    sf_s.add_argument("--query", required=True)
    sf_s.add_argument("--license", default="cc0,by")
    sf_s.add_argument("--count", type=int, default=24)
    sf_d = sf.add_parser("download")
    sf_d.add_argument("--uid", required=True)
    sf_d.add_argument("--out", required=True)

    ur = sub.add_parser("url").add_subparsers(dest="action", required=True)
    ur_d = ur.add_parser("download")
    ur_d.add_argument("--url", required=True)
    ur_d.add_argument("--name", required=True)
    ur_d.add_argument("--author", required=True)
    ur_d.add_argument("--license", required=True)
    ur_d.add_argument("--source-url", required=True)
    ur_d.add_argument("--out", required=True)

    args = parser.parse_args(argv)
    if args.source == "polyhaven" and args.action == "search":
        for asset_id, info in ph_search(args.type, args.query, args.limit):
            print("%-32s %-40s %s" % (asset_id, info.get("name", ""), ",".join(info.get("categories", []))))
    elif args.source == "polyhaven":
        for p in ph_download(args.type, args.id, args.res, args.format, args.out, args.ledger, args.base):
            print(p)
    elif args.source == "sketchfab" and args.action == "search":
        licenses = [x.strip() for x in args.license.split(",") if x.strip()]
        for model, normal in sf_search(args.query, licenses, args.count):
            print("%s  %-6s %-40s %s" % (model["uid"], normal, model.get("name", ""), model.get("viewerUrl", "")))
    elif args.source == "sketchfab":
        for p in sf_download(args.uid, args.out, args.ledger, args.base):
            print(p)
    else:
        for p in url_download(args.url, args.name, args.author, args.license, args.source_url, args.out, args.ledger, args.base):
            print(p)
    return 0


if __name__ == "__main__":
    sys.exit(main())
