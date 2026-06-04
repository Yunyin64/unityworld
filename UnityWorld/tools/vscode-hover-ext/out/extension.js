"use strict";
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || (function () {
    var ownKeys = function(o) {
        ownKeys = Object.getOwnPropertyNames || function (o) {
            var ar = [];
            for (var k in o) if (Object.prototype.hasOwnProperty.call(o, k)) ar[ar.length] = k;
            return ar;
        };
        return ownKeys(o);
    };
    return function (mod) {
        if (mod && mod.__esModule) return mod;
        var result = {};
        if (mod != null) for (var k = ownKeys(mod), i = 0; i < k.length; i++) if (k[i] !== "default") __createBinding(result, mod, k[i]);
        __setModuleDefault(result, mod);
        return result;
    };
})();
Object.defineProperty(exports, "__esModule", { value: true });
exports.activate = activate;
exports.deactivate = deactivate;
const vscode = __importStar(require("vscode"));
const child_process_1 = require("child_process");
// ─── Cache ────────────────────────────────────────────────────────────────
const cache = new Map();
const CACHE_TTL_MS = 30000; // 30s cache
// ─── Long-running process handle ──────────────────────────────────────────
let daemonProc = null;
let pendingResolve = null;
function getConfig() {
    const cfg = vscode.workspace.getConfiguration('unityworld-hover');
    return {
        cliPath: cfg.get('cliPath') || '',
        timeoutMs: cfg.get('timeoutMs') || 3000,
        useLongRunning: cfg.get('useLongRunning') || false,
    };
}
// ─── One-shot mode: spawn CLI per hover ───────────────────────────────────
function queryOneShot(cliPath, text, timeoutMs) {
    return new Promise((resolve, reject) => {
        const proc = (0, child_process_1.execFile)(cliPath, [text], { timeout: timeoutMs, shell: true }, (err, stdout) => {
            if (err)
                return reject(err);
            resolve(stdout.trim());
        });
    });
}
// ─── Long-running mode: stdin/stdout line protocol ────────────────────────
// Protocol: write one line to stdin → read one line from stdout as response.
// Use \n as delimiter. Response must be single line (use \\n for literal newlines in content).
function ensureDaemon(cliPath) {
    if (daemonProc && !daemonProc.killed)
        return daemonProc;
    daemonProc = (0, child_process_1.spawn)(cliPath, ['--daemon'], { stdio: ['pipe', 'pipe', 'ignore'], shell: true });
    let buffer = '';
    daemonProc.stdout.on('data', (chunk) => {
        buffer += chunk.toString();
        const idx = buffer.indexOf('\n');
        if (idx !== -1) {
            const line = buffer.slice(0, idx);
            buffer = buffer.slice(idx + 1);
            if (pendingResolve) {
                // Unescape \\n → real newlines for display
                pendingResolve(line.replace(/\\n/g, '\n'));
                pendingResolve = null;
            }
        }
    });
    daemonProc.on('exit', () => { daemonProc = null; });
    return daemonProc;
}
function queryDaemon(cliPath, text, timeoutMs) {
    return new Promise((resolve, reject) => {
        const proc = ensureDaemon(cliPath);
        pendingResolve = resolve;
        proc.stdin.write(text + '\n');
        setTimeout(() => {
            if (pendingResolve === resolve) {
                pendingResolve = null;
                reject(new Error('daemon timeout'));
            }
        }, timeoutMs);
    });
}
// ─── Hover Provider ───────────────────────────────────────────────────────
class JsonHoverProvider {
    async provideHover(doc, pos) {
        const config = getConfig();
        console.log('[UnityWorld Hover] provideHover called, cliPath:', config.cliPath);
        if (!config.cliPath) {
            console.log('[UnityWorld Hover] no cliPath configured, skipping');
            return;
        }
        // Match a JSON string value (between quotes) — manual scan for reliability
        const line = doc.lineAt(pos.line).text;
        const col = pos.character;
        let start = -1;
        let end = -1;
        // Search left for opening quote
        for (let i = col; i >= 0; i--) {
            if (line[i] === '"') {
                start = i;
                break;
            }
        }
        // Search right for closing quote
        for (let i = Math.max(col, start + 1); i < line.length; i++) {
            if (line[i] === '"' && i !== start) {
                end = i;
                break;
            }
        }
        if (start === -1 || end === -1 || start === end) {
            console.log('[UnityWorld Hover] no string matched at position', pos.line, col);
            return;
        }
        const range = new vscode.Range(pos.line, start, pos.line, end + 1);
        const raw = doc.getText(range);
        const text = raw.slice(1, -1); // strip quotes
        console.log('[UnityWorld Hover] querying:', text);
        if (!text)
            return;
        // Cache check
        const cached = cache.get(text);
        if (cached && Date.now() - cached.ts < CACHE_TTL_MS) {
            if (!cached.result)
                return;
            return new vscode.Hover(new vscode.MarkdownString(cached.result));
        }
        // Query CLI
        try {
            const result = config.useLongRunning
                ? await queryDaemon(config.cliPath, text, config.timeoutMs)
                : await queryOneShot(config.cliPath, text, config.timeoutMs);
            console.log('[UnityWorld Hover] CLI returned:', result.slice(0, 200));
            cache.set(text, { result, ts: Date.now() });
            if (!result)
                return;
            return new vscode.Hover(new vscode.MarkdownString(result));
        }
        catch (e) {
            console.error('[UnityWorld Hover] CLI error:', e?.message || e);
            cache.set(text, { result: '', ts: Date.now() });
            return;
        }
    }
}
// ─── Activation ───────────────────────────────────────────────────────────
function activate(ctx) {
    const selector = [
        { scheme: 'file', language: 'json' },
        { scheme: 'file', language: 'jsonc' },
    ];
    ctx.subscriptions.push(vscode.languages.registerHoverProvider(selector, new JsonHoverProvider()));
    // Cleanup daemon on deactivate
    ctx.subscriptions.push({
        dispose() {
            if (daemonProc && !daemonProc.killed) {
                daemonProc.kill();
                daemonProc = null;
            }
        }
    });
    console.log('[UnityWorld Hover] activated');
}
function deactivate() {
    if (daemonProc && !daemonProc.killed) {
        daemonProc.kill();
        daemonProc = null;
    }
}
//# sourceMappingURL=extension.js.map