#!/usr/bin/env python3
"""Verify the asset ledger or generate CREDITS.md.

  license_ledger.py verify  [--ledger Assets/ThirdParty/ledger.json] [--base .]
  license_ledger.py credits [--ledger Assets/ThirdParty/ledger.json] [--out CREDITS.md]
"""
import argparse
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import ledger  # noqa: E402


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("cmd", choices=["verify", "credits"])
    parser.add_argument("--ledger", default=os.path.join("Assets", "ThirdParty", "ledger.json"))
    parser.add_argument("--base", default=".")
    parser.add_argument("--out", default="CREDITS.md")
    args = parser.parse_args()
    if args.cmd == "verify":
        problems = ledger.verify(args.ledger, args.base)
        if problems:
            print("Ledger problems:")
            for p in problems:
                print(" - " + p)
            return 1
        print("Ledger OK (%d entries)." % len(ledger.load_ledger(args.ledger)["entries"]))
        return 0
    text = ledger.credits_markdown(args.ledger)
    with open(args.out, "w", encoding="utf-8") as handle:
        handle.write(text)
    print("wrote " + args.out)
    return 0


if __name__ == "__main__":
    sys.exit(main())
