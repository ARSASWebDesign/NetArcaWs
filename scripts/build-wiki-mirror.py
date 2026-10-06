#!/usr/bin/env python3
"""Render repository Markdown as a flat, locally reviewable GitHub Wiki mirror."""

from __future__ import annotations

import re
import unicodedata
from pathlib import Path
from urllib.parse import quote, unquote, urlsplit


ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "docs" / "wiki-export"
REPOSITORY = "https://github.com/ARSASWebDesign/NetArcaWs"
LINK_RE = re.compile(r"(?P<prefix>!?\[[^\]]*\]\()(?P<target>[^)\s]+)(?P<suffix>[^)]*\))")
WIKI_LINK_RE = re.compile(r"\[\[(?P<target>.+?)\]\]", re.DOTALL)


def page_name(source: Path) -> str:
    relative = source.relative_to(ROOT).as_posix()
    stem = source.stem
    if relative == "README.md":
        return "Proyecto"
    if relative == "ARCHITECTURE.md":
        return "Arquitectura-general"
    if relative == "PROGRESS.md":
        return "Estado-del-proyecto"
    if relative == "CONTRIBUTING.md":
        return "Contribuir"
    if relative == "THIRD-PARTY-NOTICES.md":
        return "Avisos-de-terceros"
    if relative.startswith("docs/wiki/"):
        return stem
    if relative.startswith("docs/adr/"):
        return f"ADR-{stem}"
    if relative == "docs/releases.md":
        return "Publicar-versiones"
    if relative == "docs/certificates-cli.md":
        return "CLI-certificados"
    if relative.startswith("docs/plans/"):
        return f"Plan-{stem}"
    if relative.startswith("docs/reference/"):
        return f"Referencia-{stem}"
    return f"Documento-{stem}"


def build_sources() -> list[Path]:
    return sorted(
        path
        for path in ROOT.rglob("*.md")
        if OUTPUT not in path.parents
        and ".git" not in path.parts
        and "artifacts" not in path.parts
        and "bin" not in path.parts
        and "obj" not in path.parts
    )


def rewrite_link(source: Path, target: str, mapping: dict[Path, str], image: bool) -> str:
    parsed = urlsplit(target)
    if parsed.scheme or parsed.netloc or not parsed.path:
        return target

    source_target = (source.parent / unquote(parsed.path)).resolve()
    try:
        source_target.relative_to(ROOT)
    except ValueError:
        return target

    page = mapping.get(source_target)
    if page:
        suffix = f"#{parsed.fragment}" if parsed.fragment else ""
        return f"{quote(page, safe='-_.')}" + suffix

    relative = source_target.relative_to(ROOT).as_posix()
    base = "raw" if image else "blob"
    url = f"{REPOSITORY}/{base}/main/{quote(relative, safe='/-_.')}"
    if parsed.fragment:
        url += f"#{parsed.fragment}"
    return url


def normalized(value: str) -> str:
    plain = unicodedata.normalize("NFKD", value)
    plain = "".join(char for char in plain if not unicodedata.combining(char))
    return re.sub(r"[^a-z0-9]+", "-", plain.lower()).strip("-")


def render(source: Path, mapping: dict[Path, str], page_lookup: dict[str, str]) -> str:
    text = source.read_text(encoding="utf-8")

    def replace(match: re.Match[str]) -> str:
        target = match.group("target")
        image = match.group("prefix").startswith("!")
        return (
            match.group("prefix")
            + rewrite_link(source, target, mapping, image)
            + match.group("suffix")
        )

    text = LINK_RE.sub(replace, text)

    def replace_wiki(match: re.Match[str]) -> str:
        label = " ".join(match.group("target").split())
        target = page_lookup.get(normalized(label))
        if not target:
            return label
        return f"[{label}]({quote(target, safe='-_.')})"

    return WIKI_LINK_RE.sub(replace_wiki, text)


def main() -> None:
    sources = build_sources()
    names = {source: page_name(source) for source in sources}
    duplicates: dict[str, list[Path]] = {}
    for source, name in names.items():
        duplicates.setdefault(name, []).append(source)
    collisions = {name: paths for name, paths in duplicates.items() if len(paths) > 1}
    if collisions:
        details = "; ".join(f"{name}: {paths}" for name, paths in collisions.items())
        raise SystemExit(f"Wiki page name collision: {details}")

    if OUTPUT.exists():
        manifest = OUTPUT / "MIRROR-SOURCES.md"
        if manifest.exists():
            for old_name in re.findall(r"→ `([^`]+)`", manifest.read_text(encoding="utf-8")):
                old_page = (OUTPUT / f"{old_name}.md").resolve()
                if OUTPUT.resolve() in old_page.parents and old_page.is_file():
                    old_page.unlink()
        for generated in (OUTPUT / "_Sidebar.md", manifest):
            generated.unlink(missing_ok=True)
    OUTPUT.mkdir(parents=True, exist_ok=True)

    page_lookup = {normalized(name): name for name in names.values()}
    for source, name in names.items():
        source_header = (
            f"<!-- Source: {source.relative_to(ROOT).as_posix()}. "
            "Generated wiki mirror; edit the repository source. -->\n\n"
        )
        (OUTPUT / f"{name}.md").write_text(
            source_header + render(source, names, page_lookup), encoding="utf-8"
        )

    sidebar = ["# Pages", "", "- [Inicio](Home)"]
    sidebar.extend(
        f"- [{name}]({quote(name, safe='-_.')})"
        for name in sorted(set(names.values()))
        if name != "Home"
    )
    (OUTPUT / "_Sidebar.md").write_text("\n".join(sidebar) + "\n", encoding="utf-8")
    (OUTPUT / "MIRROR-SOURCES.md").write_text(
        "# Fuentes del mirror local\n\n"
        f"Generado por `python3 scripts/build-wiki-mirror.py`: {len(names)} páginas "
        "de contenido, más `_Sidebar.md` y este manifiesto. Cada página incluye "
        "su fuente y reescribe enlaces relativos para navegación de wiki. Los "
        "contratos ARCA archivados en el repositorio tienen fecha de snapshot "
        "2026-10-06. La salida refleja el árbol de trabajo y todavía no está "
        "asociada con un commit de publicación. Revisarla antes de publicar.\n\n"
        + "\n".join(f"- `{source.relative_to(ROOT).as_posix()}` → `{name}`" for source, name in names.items())
        + "\n",
        encoding="utf-8",
    )

    known_pages = set(names.values()) | {"_Sidebar", "MIRROR-SOURCES"}
    broken: list[str] = []
    for generated in OUTPUT.glob("*.md"):
        body = generated.read_text(encoding="utf-8")
        for match in LINK_RE.finditer(body):
            parsed = urlsplit(match.group("target"))
            if parsed.scheme or parsed.netloc or not parsed.path:
                continue
            destination = unquote(parsed.path).removesuffix(".md")
            if destination not in known_pages:
                broken.append(f"{generated.name}: {match.group('target')}")
    if broken:
        raise SystemExit("Unresolved wiki links:\n" + "\n".join(broken))


if __name__ == "__main__":
    main()
