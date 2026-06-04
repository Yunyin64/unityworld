import * as vscode from 'vscode';
import { execFile, ChildProcess, spawn } from 'child_process';
import { Readable } from 'stream';

// ─── Cache ────────────────────────────────────────────────────────────────
const cache = new Map<string, { result: string; ts: number }>();
const CACHE_TTL_MS = 30_000; // 30s cache

// ─── Long-running process handle ──────────────────────────────────────────
let daemonProc: ChildProcess | null = null;
let pendingResolve: ((value: string) => void) | null = null;

function getConfig() {
    const cfg = vscode.workspace.getConfiguration('unityworld-hover');
    return {
        cliPath: cfg.get<string>('cliPath') || '',
        timeoutMs: cfg.get<number>('timeoutMs') || 3000,
        useLongRunning: cfg.get<boolean>('useLongRunning') || false,
    };
}

// ─── One-shot mode: spawn CLI per hover ───────────────────────────────────
function queryOneShot(cliPath: string, text: string, timeoutMs: number): Promise<string> {
    return new Promise((resolve, reject) => {
        const proc = execFile(cliPath, [text], { timeout: timeoutMs, shell: true }, (err, stdout) => {
            if (err) return reject(err);
            resolve(stdout.trim());
        });
    });
}

// ─── Long-running mode: stdin/stdout line protocol ────────────────────────
// Protocol: write one line to stdin → read one line from stdout as response.
// Use \n as delimiter. Response must be single line (use \\n for literal newlines in content).
function ensureDaemon(cliPath: string): ChildProcess {
    if (daemonProc && !daemonProc.killed) return daemonProc;

    daemonProc = spawn(cliPath, ['--daemon'], { stdio: ['pipe', 'pipe', 'ignore'], shell: true });

    let buffer = '';
    daemonProc.stdout!.on('data', (chunk: Buffer) => {
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

function queryDaemon(cliPath: string, text: string, timeoutMs: number): Promise<string> {
    return new Promise((resolve, reject) => {
        const proc = ensureDaemon(cliPath);
        pendingResolve = resolve;
        proc.stdin!.write(text + '\n');

        setTimeout(() => {
            if (pendingResolve === resolve) {
                pendingResolve = null;
                reject(new Error('daemon timeout'));
            }
        }, timeoutMs);
    });
}

// ─── Hover Provider ───────────────────────────────────────────────────────
class JsonHoverProvider implements vscode.HoverProvider {
    async provideHover(
        doc: vscode.TextDocument,
        pos: vscode.Position
    ): Promise<vscode.Hover | undefined> {
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
            if (line[i] === '"') { start = i; break; }
        }
        // Search right for closing quote
        for (let i = Math.max(col, start + 1); i < line.length; i++) {
            if (line[i] === '"' && i !== start) { end = i; break; }
        }
        if (start === -1 || end === -1 || start === end) {
            console.log('[UnityWorld Hover] no string matched at position', pos.line, col);
            return;
        }
        const range = new vscode.Range(pos.line, start, pos.line, end + 1);

        const raw = doc.getText(range);
        const text = raw.slice(1, -1); // strip quotes
        console.log('[UnityWorld Hover] querying:', text);
        if (!text) return;

        // Cache check
        const cached = cache.get(text);
        if (cached && Date.now() - cached.ts < CACHE_TTL_MS) {
            if (!cached.result) return;
            return new vscode.Hover(new vscode.MarkdownString(cached.result));
        }

        // Query CLI
        try {
            const result = config.useLongRunning
                ? await queryDaemon(config.cliPath, text, config.timeoutMs)
                : await queryOneShot(config.cliPath, text, config.timeoutMs);

            console.log('[UnityWorld Hover] CLI returned:', result.slice(0, 200));
            cache.set(text, { result, ts: Date.now() });
            if (!result) return;
            return new vscode.Hover(new vscode.MarkdownString(result));
        } catch (e: any) {
            console.error('[UnityWorld Hover] CLI error:', e?.message || e);
            cache.set(text, { result: '', ts: Date.now() });
            return;
        }
    }
}

// ─── Activation ───────────────────────────────────────────────────────────
export function activate(ctx: vscode.ExtensionContext) {
    const selector: vscode.DocumentSelector = [
        { scheme: 'file', language: 'json' },
        { scheme: 'file', language: 'jsonc' },
    ];

    ctx.subscriptions.push(
        vscode.languages.registerHoverProvider(selector, new JsonHoverProvider())
    );

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

export function deactivate() {
    if (daemonProc && !daemonProc.killed) {
        daemonProc.kill();
        daemonProc = null;
    }
}
