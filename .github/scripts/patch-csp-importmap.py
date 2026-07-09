"""Deploy-time CSP patcher: allows Blazor's publish-time inline importmap
via a script-src hash-source, keeping the CSP free of 'unsafe-inline'."""
import hashlib, base64, re, pathlib, sys

root = pathlib.Path(sys.argv[1] if len(sys.argv) > 1 else "publish/wwwroot")
for name in ("index.html", "404.html"):
    p = root / name
    html = p.read_text(encoding="utf-8")
    m = re.search(r'<script type="importmap">(.*?)</script>', html, re.S)
    if not m:
        raise SystemExit(f"{name}: importmap script not found")
    digest = base64.b64encode(hashlib.sha256(m.group(1).encode()).digest()).decode()
    needle = "script-src 'self' 'wasm-unsafe-eval'"
    if needle not in html:
        raise SystemExit(f"{name}: expected CSP script-src directive not found")
    html = html.replace(needle, f"{needle} 'sha256-{digest}'")
    p.write_text(html, encoding="utf-8")
    print(f"{name}: importmap allowed via 'sha256-{digest}'")
