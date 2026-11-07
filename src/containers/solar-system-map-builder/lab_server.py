import argparse
import datetime
import json
import pathlib
import re
import threading
import urllib.parse
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from io import BytesIO

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt  # noqa: E402
import yaml  # noqa: E402

from main import draw_figure  # noqa: E402

EPOCH_PATTERN = re.compile(r"^\d{4}-\d{2}-\d{2}( \d{2}:\d{2}(:\d{2})?)?$")
VIEW_PATTERN = re.compile(r"^[a-z0-9-]+$")
LIVE_ROUND_MINUTES = 10
MAX_CACHED_RENDERS = 400
DRAW_LOCK = threading.Lock()


def live_epoch():
    now = datetime.datetime.now(datetime.timezone.utc)
    now = now.replace(minute=now.minute - now.minute % LIVE_ROUND_MINUTES, second=0, microsecond=0)
    return now.strftime("%Y-%m-%d %H:%M:00")


def normalized_epoch(epoch):
    if len(epoch) == 10:
        return f"{epoch} 00:00:00"
    if len(epoch) == 16:
        return f"{epoch}:00"
    return epoch


class LabHandler(BaseHTTPRequestHandler):
    config_folder: pathlib.Path
    cache_folder: pathlib.Path
    index_html: bytes

    def views(self):
        return sorted(p.stem for p in self.config_folder.glob("*.yaml"))

    def send(self, status, content_type, body, cache_seconds=0):
        self.send_response(status)
        self.send_header("Content-Type", content_type)
        self.send_header("Content-Length", str(len(body)))
        self.send_header("Cache-Control", f"public, max-age={cache_seconds}" if cache_seconds else "no-store")
        self.end_headers()
        self.wfile.write(body)

    def do_GET(self):
        url = urllib.parse.urlparse(self.path)
        query = urllib.parse.parse_qs(url.query)
        if url.path in ("/", "/index.html") and "epoch" not in query:
            self.send(200, "text/html; charset=utf-8", self.index_html)
        elif url.path == "/views":
            self.send(200, "application/json", json.dumps({"views": self.views(), "live": live_epoch()}).encode())
        elif url.path in ("/map.png", "/"):
            self.map(query)
        else:
            self.send(404, "text/plain", b"not found")

    def map(self, query):
        epoch = query.get("epoch", [""])[0].strip()
        view = query.get("view", ["overview"])[0]
        if epoch in ("", "now"):
            epoch = live_epoch()
        if not EPOCH_PATTERN.match(epoch):
            self.send(400, "text/plain", b"epoch must look like YYYY-MM-DD or YYYY-MM-DD HH:MM[:SS]")
            return
        if not VIEW_PATTERN.match(view) or view not in self.views():
            self.send(404, "text/plain", f"unknown view '{view}'; known: {', '.join(self.views())}".encode())
            return

        epoch = normalized_epoch(epoch)
        cached = self.cache_folder / "renders" / view / (epoch.replace(" ", "T").replace(":", "-") + ".png")
        if cached.exists():
            self.send(200, "image/png", cached.read_bytes(), cache_seconds=86400)
            return

        try:
            with DRAW_LOCK:
                with open(self.config_folder / f"{view}.yaml", encoding="utf-8") as config_file:
                    config = yaml.load(config_file, Loader=yaml.FullLoader)
                figure = draw_figure(epoch, config=config, cache_folder=str(self.cache_folder))
                buffer = BytesIO()
                figure.savefig(buffer, format="png")
                plt.close(figure)
        except Exception as error:  # noqa: BLE001 - every failure is reported to the viewer as text
            self.send(500, "text/plain", f"{view} at {epoch}: {error}".encode())
            return

        body = buffer.getvalue()
        cached.parent.mkdir(parents=True, exist_ok=True)
        cached.write_bytes(body)
        self.prune()
        self.send(200, "image/png", body, cache_seconds=86400)

    def prune(self):
        renders = sorted((self.cache_folder / "renders").rglob("*.png"), key=lambda p: p.stat().st_mtime)
        for old in renders[:-MAX_CACHED_RENDERS]:
            old.unlink(missing_ok=True)


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--config-folder", required=True)
    parser.add_argument("--cache-folder", required=True)
    parser.add_argument("--port", type=int, default=80)
    args = parser.parse_args()

    LabHandler.config_folder = pathlib.Path(args.config_folder)
    LabHandler.cache_folder = pathlib.Path(args.cache_folder)
    LabHandler.index_html = (pathlib.Path(__file__).parent / "index.html").read_bytes()
    ThreadingHTTPServer(("0.0.0.0", args.port), LabHandler).serve_forever()
