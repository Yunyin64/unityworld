import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..'))

from tokenizer import clean_markdown, extract_ordered_tokens, make_bigrams, extract_all_tokens, extract_summary

class TestCleanMarkdown:
    def test_strips_fenced_code_blocks(self):
        text = "前言\n```lua\nlocal x = 1\n```\n后文"
        result = clean_markdown(text)
        assert "local x" not in result
        assert "前言" in result and "后文" in result

    def test_strips_headers(self):
        result = clean_markdown("# 标题\n## 子标题\n正文")
        assert "#" not in result
        assert "正文" in result

    def test_strips_markdown_links_keeps_label(self):
        result = clean_markdown("[ArkPipe](arkpipe.module.md)")
        assert "ArkPipe" in result
        assert "arkpipe.module.md" not in result

    def test_strips_backticks_keeps_content(self):
        result = clean_markdown("调用 `CNetworkMgr::StartUp` 函数")
        assert "`" not in result
        assert "CNetworkMgr" in result

class TestExtractOrderedTokens:
    def test_extracts_english_words(self):
        tokens = extract_ordered_tokens("ArkPipe network manager")
        assert "ark" in tokens and "pipe" in tokens
        assert "network" in tokens

    def test_filters_english_stopwords(self):
        tokens = extract_ordered_tokens("this is the manager")
        assert "this" not in tokens
        assert "is" not in tokens
        assert "the" not in tokens
        assert "manager" in tokens

    def test_extracts_chinese_terms(self):
        tokens = extract_ordered_tokens("连接管理是核心模块")
        joined = " ".join(tokens)
        assert any(t in joined for t in ["连接", "管理", "核心", "模块"])

    def test_splits_camel_case(self):
        tokens = extract_ordered_tokens("CNetworkMgr StartUp")
        assert "cnetworkmgr" in tokens or "network" in tokens

    def test_preserves_order(self):
        tokens = extract_ordered_tokens("arkpipe rpc dispatch")
        assert tokens.index("arkpipe") < tokens.index("rpc") < tokens.index("dispatch")

class TestMakeBigrams:
    def test_basic_bigrams(self):
        bigrams = make_bigrams(["arkpipe", "rpc", "dispatch"])
        assert "arkpipe rpc" in bigrams
        assert "rpc dispatch" in bigrams
        assert len(bigrams) == 2

    def test_empty_returns_empty(self):
        assert make_bigrams([]) == []
        assert make_bigrams(["single"]) == []

class TestExtractAllTokens:
    def test_contains_unigrams_and_bigrams(self):
        tokens = extract_all_tokens("ArkPipe RPC 注册")
        assert "ark" in tokens and "pipe" in tokens
        assert any(" " in t for t in tokens)  # at least one bigram

    def test_deduplicates(self):
        tokens = extract_all_tokens("lua lua lua")
        assert tokens.count("lua") == 1

class TestExtractSummary:
    def test_extracts_summary_line(self):
        content = "# ArkPipe 网络组件\n> summary: 进程间通信底座，负责连接生命周期。\n\n正文..."
        assert extract_summary(content) == "进程间通信底座，负责连接生命周期。"

    def test_returns_empty_if_missing(self):
        content = "# ArkPipe 网络组件\n\n正文..."
        assert extract_summary(content) == ""

    def test_only_checks_first_10_lines(self):
        content = "# Title\n\n" + "\n".join(["line"] * 10) + "\n> summary: late"
        assert extract_summary(content) == ""
