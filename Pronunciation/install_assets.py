import argparse
import hashlib
import shutil
import time
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
from urllib.request import Request, urlopen
from zipfile import ZipFile


SOURCE_REVISION = "1165ce7a60b79e465aac2daeb5214966d190c2a9"
SOURCE_URL = f"https://codeload.github.com/chenhongwen2005/mandarin-ipa-asr/zip/{SOURCE_REVISION}"
SOURCE_HASH = "0344da53b6ba7c031dc8a4377e00bcb7bedbcf7d0d0deaa970774ea4cc12fbbd"
MODEL_URL = ("https://www.modelscope.cn/models/QiGuanFuChen/mandarin-ipa-asr/resolve/"
             "314ef09c41f12fd8b6cfcfe3709b2bb4df456fa5/base.pt?download=true")
MODEL_HASH = "00b9e6a78857811ccb24b1dde40f7c1e95f75ab2f7d1a00c435bfd4facfdd4cb"
MODEL_SIZE = 940_307_312
SENSEVOICE_URL = "https://huggingface.co/FunAudioLLM/SenseVoiceSmall/resolve/3847d57b6bdf2dd8875cb1508d2af43d80a16bf7/"
SENSEVOICE_FILES = {
    "am.mvn": "29b3c740a2c0cfc6b308126d31d7f265fa2be74f3bb095cd2f143ea970896ae5",
    "config.yaml": "f71e239ba36705564b5bf2d2ffd07eece07b8e3f2bbf6d2c99d8df856339ac19",
    "chn_jpn_yue_eng_ko_spectok.bpe.model": "aa87f86064c3730d799ddf7af3c04659151102cba548bce325cf06ba4da4e6a8",
}


def has_hash(path, digest):
    if not path.is_file():
        return False
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest() == digest


def download(url, path, digest, large=False):
    if has_hash(path, digest):
        return
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_suffix(path.suffix + ".download")
    try:
        if large:
            download_checkpoint(url, temporary)
        else:
            with urlopen(url, timeout=45) as response, temporary.open("wb") as output:
                shutil.copyfileobj(response, output)
        if not has_hash(temporary, digest):
            raise RuntimeError(f"Checksum mismatch for {path.name}")
        temporary.replace(path)
    finally:
        temporary.unlink(missing_ok=True)


def download_checkpoint(url, temporary):
    # ModelScope's CDN supports ranges. Retry individual chunks of the 940 MB checkpoint.
    chunk_size = 16 * 1024 * 1024
    starts = range(0, MODEL_SIZE, chunk_size)

    def read_chunk(start):
        end = min(start + chunk_size, MODEL_SIZE) - 1
        request = Request(f"{url}&offset={start}", headers={"Range": f"bytes={start}-{end}"})
        for attempt in range(3):
            try:
                with urlopen(request, timeout=45) as response:
                    if response.headers.get("Content-Range") != f"bytes {start}-{end}/{MODEL_SIZE}":
                        raise OSError(f"Unexpected download range at {start}")
                    data = response.read(end - start + 1)
                if len(data) != end - start + 1:
                    raise OSError(f"Incomplete download range at {start}")
                return data
            except OSError:
                if attempt == 2:
                    raise
                time.sleep(attempt + 1)

    print("Downloading the Mandarin IPA checkpoint (940 MB)...", flush=True)
    with temporary.open("wb") as output, ThreadPoolExecutor(max_workers=6) as pool:
        # Keep the in-flight buffer bounded even if one range needs a retry.
        for offset in range(0, len(starts), 6):
            for data in pool.map(read_chunk, starts[offset:offset + 6]):
                output.write(data)
            print(f"{output.tell() * 100 // MODEL_SIZE}%", flush=True)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("directory", type=Path)
    root = parser.parse_args().directory
    archive = root / "upstream.zip"
    download(SOURCE_URL, archive, SOURCE_HASH)
    filenames = (
        "src/converter.py", "src/utils.py", "vocab/vocab_mandarin_ipa_combined.json",
        "vocab/vocab_mandarin_ipa_tone_combined.json", "vendor/putonghua_converter/putonghua.js", "LICENSE",
    )
    with ZipFile(archive) as package:
        prefix = package.namelist()[0]
        for filename in filenames:
            destination = root / "upstream" / filename
            destination.parent.mkdir(parents=True, exist_ok=True)
            destination.write_bytes(package.read(prefix + filename))

    for filename, digest in SENSEVOICE_FILES.items():
        download(SENSEVOICE_URL + filename, root / "sensevoice" / filename, digest)
    download(MODEL_URL, root / "model" / "base.pt", MODEL_HASH, large=True)


if __name__ == "__main__":
    main()
