import sys, os, sqlite3
from pathlib import Path
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..'))

import kb_index_write as W
import kb_index_query as Q

def build(tmp_path, dirs: dict[str, dict[str, str]]):
    """
    dirs: {subpath: {filename: content}}
    Returns (conn, root=tmp_path)
    """
    conn = sqlite3.connect(":memory:")
    W.init_schema(conn)
    for subpath, files in dirs.items():
        d = tmp_path / subpath
        d.mkdir(parents=True, exist_ok=True)
        for name, content in files.items():
            (d / name).write_text(content, encoding="utf-8")
        (d / "entry.md").write_text(
            f"# {subpath}\n\n| 文件 | 类型 | 简介 |\n|------|------|------|\n",
            encoding="utf-8",
        )
    W.rebuild(conn, tmp_path, min_keywords=1)
    return conn

class TestSearch:
    def test_finds_directory(self, tmp_path):
        # Need at least 2 dirs so IDF is non-zero for discriminating terms
        conn = build(tmp_path, {
            "infrastructure/arkpipe": {
                "a.md": "# ArkPipe\narkpipe rpc dispatch network unique1 unique2",
            },
            "infrastructure/other": {
                "b.md": "# Other\nother module content unique3 unique4 unique5",
            },
        })
        results = Q.search(conn, ["arkpipe"])
        assert len(results) == 1
        path, summary = results[0]
        assert "arkpipe" in path
        assert path.endswith("entry.md")

    def test_and_requires_both_terms(self, tmp_path):
        conn = build(tmp_path, {
            "infrastructure/arkpipe": {
                "a.md": "# ArkPipe\narkpipe connection network unique1 unique2 unique3",
            },
            "infrastructure/dbproxy": {
                "b.md": "# DBProxy\ndbproxy database cache unique4 unique5 unique6",
            },
        })
        # Both dirs have unique terms; AND of arkpipe+dbproxy term should hit only
        # a dir that has both (none here)
        results = Q.search(conn, ["arkpipe", "dbproxy"], use_and=True)
        assert len(results) == 0

    def test_or_finds_either(self, tmp_path):
        conn = build(tmp_path, {
            "infrastructure/arkpipe": {
                "a.md": "# ArkPipe\narkpipe network unique1 unique2 unique3",
            },
            "infrastructure/dbproxy": {
                "b.md": "# DBProxy\ndbproxy database unique4 unique5 unique6",
            },
        })
        results = Q.search(conn, ["arkpipe", "dbproxy"], use_and=False)
        assert len(results) == 2

    def test_empty_returns_empty(self, tmp_path):
        conn = build(tmp_path, {
            "infrastructure/alpha": {"a.md": "# Alpha\ncontent unique1"}
        })
        assert Q.search(conn, []) == []

class TestSuggest:
    def test_suggest_returns_related_keywords(self, tmp_path):
        conn = build(tmp_path, {
            "infrastructure/arkpipe": {
                "a.md": "# ArkPipe\narkpipe lua rpc registration unique1 unique2",
                "b.md": "# ArkPipe B\narkpipe lua class system unique3 unique4",
            },
            "infrastructure/dbproxy": {
                "c.md": "# DBProxy\ndbproxy cache persistence unique5 unique6 unique7",
            },
        })
        suggs = Q.suggest(conn, "ark", max_doc_count=5)
        kws = [s[0] for s in suggs]
        assert any("ark" in k for k in kws)

class TestAutoSearch:
    def test_docs_mode(self, tmp_path):
        # Need at least 2 dirs so IDF is non-zero for arkpipe
        conn = build(tmp_path, {
            "infrastructure/arkpipe": {
                "a.md": "# ArkPipe\narkpipe unique1 unique2 unique3 unique4",
            },
            "infrastructure/other": {
                "b.md": "# Other\nother content unique5 unique6 unique7",
            },
        })
        mode, result = Q.auto_search(conn, ["arkpipe"], broad_threshold=50)
        assert mode == "docs"
        assert len(result) >= 1
        assert isinstance(result[0], tuple)

    def test_suggest_mode_on_miss(self, tmp_path):
        conn = build(tmp_path, {
            "infrastructure/arkpipe": {
                "a.md": "# ArkPipe\narkpipe rpc unique1 unique2 unique3",
            }
        })
        # Search for something not in index
        mode, result = Q.auto_search(conn, ["zzznomatch"], broad_threshold=50)
        assert mode == "suggest"
