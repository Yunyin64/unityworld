import sys, os, sqlite3, math
from pathlib import Path
from collections import Counter
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..'))

import kb_index_write as W

def make_db():
    conn = sqlite3.connect(":memory:")
    W.init_schema(conn)
    return conn

def make_dir(tmp_path, subpath, files: dict[str, str]) -> Path:
    """Create a directory with MD files and an entry.md."""
    d = tmp_path / subpath
    d.mkdir(parents=True, exist_ok=True)
    for name, content in files.items():
        (d / name).write_text(content, encoding="utf-8")
    (d / "entry.md").write_text(f"# {subpath} — 目录概览\n\n| 文件 | 类型 | 简介 |\n|------|------|------|\n", encoding="utf-8")
    return d

class TestInitSchema:
    def test_tables_exist(self):
        conn = make_db()
        tables = {r[0] for r in conn.execute(
            "SELECT name FROM sqlite_master WHERE type='table'"
        ).fetchall()}
        assert {"doc_meta", "doc_keywords", "pruned_keywords"} <= tables
        cols = {r[1] for r in conn.execute("PRAGMA table_info(doc_meta)").fetchall()}
        assert {"id", "rel_path", "sha1", "summary", "indexed_at"} <= cols
        kw_cols = {r[1] for r in conn.execute("PRAGMA table_info(doc_keywords)").fetchall()}
        assert "doc_id" in kw_cols and "rel_path" not in kw_cols

class TestRebuild:
    def test_indexes_directories_not_files(self, tmp_path):
        conn = make_db()
        make_dir(tmp_path, "infrastructure/alpha", {
            "alpha.module.md": "# Alpha Module\nalpha specific unique content",
        })
        W.rebuild(conn, tmp_path, min_keywords=1)
        count = conn.execute("SELECT COUNT(*) FROM doc_meta").fetchone()[0]
        assert count == 1
        row = conn.execute("SELECT rel_path FROM doc_meta").fetchone()
        assert row[0].endswith("entry.md")

    def test_tfidf_keeps_discriminating_terms(self, tmp_path):
        conn = make_db()
        # Two directories with distinct content
        make_dir(tmp_path, "infrastructure/arkpipe", {
            "a.module.md": "# ArkPipe\narkpipe network connection rpc dispatch",
            "b.flow.md":   "# ArkPipe Flow\narkpipe connection lifecycle startup",
        })
        make_dir(tmp_path, "infrastructure/dbproxy", {
            "c.module.md": "# DBProxy\ndbproxy database dirty cache persistence",
            "d.flow.md":   "# DBProxy Flow\ndbproxy mysql redis read write path",
        })
        W.rebuild(conn, tmp_path, min_keywords=1)

        def kws_for(rel_like):
            rows = conn.execute("""
                SELECT dk.keyword FROM doc_keywords dk
                JOIN doc_meta dm ON dk.doc_id = dm.id
                WHERE dm.rel_path LIKE ?
            """, (f"%{rel_like}%",)).fetchall()
            return {r[0] for r in rows}

        ark_kws = kws_for("arkpipe")
        db_kws  = kws_for("dbproxy")

        # Each directory should have its own characteristic term
        assert "arkpipe" in ark_kws
        assert "dbproxy" in db_kws
        # Terms should not bleed across directories
        assert "arkpipe" not in db_kws
        assert "dbproxy" not in ark_kws

    def test_common_terms_not_in_top_keywords(self, tmp_path):
        conn = make_db()
        # 'common' appears in both directories → low IDF → should not be top keyword
        make_dir(tmp_path, "infrastructure/alpha", {
            "a.md": "# Alpha\ncommon alpha unique1 unique2 unique3 unique4 unique5",
        })
        make_dir(tmp_path, "infrastructure/beta", {
            "b.md": "# Beta\ncommon beta unique6 unique7 unique8 unique9 unique10",
        })
        W.rebuild(conn, tmp_path, min_keywords=1, token_ratio=0.1)

        all_kws = {r[0] for r in conn.execute("SELECT keyword FROM doc_keywords").fetchall()}
        # 'common' appears in both dirs → IDF = log(2/2) = 0 → excluded
        assert "common" not in all_kws

class TestUpsertDoc:
    def test_upsert_indexes_directory(self, tmp_path):
        conn = make_db()
        d = make_dir(tmp_path, "infrastructure/alpha", {
            "a.module.md": "# Alpha\nalpha specific content unique",
        })
        md = d / "a.module.md"
        W.upsert_doc(conn, md, root=tmp_path)
        count = conn.execute("SELECT COUNT(*) FROM doc_meta").fetchone()[0]
        assert count == 1
        row = conn.execute("SELECT rel_path FROM doc_meta").fetchone()
        assert "entry.md" in row[0]

    def test_upsert_skips_unchanged_directory(self, tmp_path):
        conn = make_db()
        d = make_dir(tmp_path, "infrastructure/alpha", {
            "a.module.md": "# Alpha\nalpha content unique",
        })
        W.upsert_doc(conn, d / "a.module.md", root=tmp_path)
        c1 = conn.execute("SELECT COUNT(*) FROM doc_keywords").fetchone()[0]
        W.upsert_doc(conn, d / "a.module.md", root=tmp_path)
        c2 = conn.execute("SELECT COUNT(*) FROM doc_keywords").fetchone()[0]
        assert c1 == c2

class TestRemoveDoc:
    def test_remove_reindexes_directory(self, tmp_path):
        conn = make_db()
        d = make_dir(tmp_path, "infrastructure/alpha", {
            "a.module.md": "# Alpha\nalpha content unique",
            "b.flow.md":   "# Alpha Flow\nalpha flow content different",
        })
        W.upsert_doc(conn, d / "a.module.md", root=tmp_path)
        # Remove one file (physically delete it to simulate removal)
        (d / "a.module.md").unlink()
        W.remove_doc(conn, d / "a.module.md", root=tmp_path)
        # Directory still has b.flow.md, so entry should still exist
        count = conn.execute("SELECT COUNT(*) FROM doc_meta").fetchone()[0]
        assert count == 1

class TestSummaryStorage:
    def test_summary_from_entry_md_title(self, tmp_path):
        conn = make_db()
        make_dir(tmp_path, "infrastructure/alpha", {
            "a.module.md": "# Alpha Module\nalpha unique content",
        })
        W.rebuild(conn, tmp_path, min_keywords=1)
        row = conn.execute("SELECT summary FROM doc_meta").fetchone()
        assert row[0] != ""  # has some summary (title from entry.md)
