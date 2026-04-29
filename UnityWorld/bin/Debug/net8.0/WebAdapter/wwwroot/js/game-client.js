/**
 * SignalR 客户端：与服务端通信
 */

// 全局状态
let connection = null;
let worldState = null;
let selectedint = null;

// 初始化 SignalR 连接
async function initConnection() {
    connection = new signalR.HubConnectionBuilder()
        .withUrl("/gameHub")
        .withAutomaticReconnect()
        .build();

    // 接收世界状态
    connection.on("ReceiveWorldState", (state) => {
        worldState = state;
        updateUI(state);
    });

    // 接收地块详情
    connection.on("ReceiveTileDetail", (tile) => {
        showTileDetail(tile);
    });

    // 连接状态变化
    connection.onclose(() => updateConnectionStatus(false));
    connection.onreconnecting(() => updateConnectionStatus(false, "重连中..."));
    connection.onreconnected(() => {
        updateConnectionStatus(true);
        connection.invoke("RequestWorldState");
    });

    try {
        await connection.start();
        updateConnectionStatus(true);
        console.log("SignalR 连接成功");
    } catch (err) {
        console.error("SignalR 连接失败:", err);
        updateConnectionStatus(false);
    }
}

// 更新连接状态显示
function updateConnectionStatus(connected, text = null) {
    const statusDot = document.querySelector(".status-dot");
    const statusText = document.querySelector(".status-text");
    
    statusDot.className = "status-dot " + (connected ? "connected" : "disconnected");
    statusText.textContent = text || (connected ? "已连接" : "断开连接");
}

// ── 发送命令到服务端 ──────────────────────────────────────

async function togglePause() {
    if (connection) {
        await connection.invoke("TogglePause");
    }
}

async function setTimeScale(scale) {
    if (connection) {
        await connection.invoke("SetTimeScale", scale);
    }
}

async function selectNpc(npcId) {
    selectedint = npcId;
    if (connection) {
        await connection.invoke("SelectNpc", npcId);
    }
}

async function deselectNpc() {
    selectedint = null;
    if (connection) {
        await connection.invoke("DeselectNpc");
    }
}

async function selectTile(x, y) {
    if (connection) {
        await connection.invoke("SelectTile", x, y);
    }
}

async function useBehaviorCard(cardId, npcId) {
    if (connection) {
        await connection.invoke("UseBehaviorCard", cardId, npcId);
    }
}

async function stepTick() {
    if (connection) {
        await connection.invoke("StepTick");
    }
}

// ── UI 更新函数 ──────────────────────────────────────

function updateUI(state) {
    // 更新时间显示
    document.getElementById("gameTime").textContent = 
        `第 ${state.gameYear} 年 ${state.gameMonth} 月 ${state.gameDay} 日 ${String(state.gameHour).padStart(2, '0')}:00`;
    
    // 更新速度显示
    document.getElementById("speedDisplay").textContent = state.timeScale + "x";
    
    // 更新暂停按钮
    document.getElementById("btnPause").textContent = state.runState === "Running" ? "⏸" : "▶";
    
    // 更新地图
    if (window.renderMap && state.tiles) {
        renderMap(state);
    }
    
    // 更新行为卡
    updateBehaviorCards(state.behaviorCards);
    
    // 更新日志
    updateLogs(state.logs);
}

function updateBehaviorCards(cards) {
    const container = document.getElementById("BehaviorCards");
    
    if (!cards || cards.length === 0) {
        container.innerHTML = '<p class="hint">选中 NPC 后可查看可用的行为卡</p>';
        return;
    }
    
    container.innerHTML = cards.map(card => `
        <div class="action-card" onclick="useBehaviorCard('${card.defineId}', ${card.ownerId})">
            <span class="action-card-icon">${getCardIcon(card.tags)}</span>
            <span class="action-card-name">${card.displayName}</span>
            <span class="action-card-desc">${card.tags.slice(0, 2).join(' · ')}</span>
        </div>
    `).join('');
}

function getCardIcon(tags) {
    const iconMap = {
        '种植': '🌾', '农耕': '🌾',
        '钓鱼': '🎣',
        '伐木': '🪓', '采集': '🪓',
        '休息': '💤', '睡眠': '💤',
        '交谈': '💬', '社交': '💬',
        '烹饪': '🍳', '烹饪': '🍳',
        '战斗': '⚔️', '训练': '⚔️',
        '修炼': '🧘', '冥想': '🧘',
        '探索': '🗺️',
        '建造': '🔨',
        '治疗': '💚', '恢复': '💚'
    };
    
    for (const tag of tags || []) {
        if (iconMap[tag]) return iconMap[tag];
    }
    return '🎴';
}

function updateLogs(logs) {
    const container = document.getElementById("logList");
    if (!container || !logs) return;
    
    container.innerHTML = logs.slice().reverse().map(log => `
        <div class="log-entry">
            <span class="log-time">${log.gameTime}</span>
            <span class="log-level ${log.level}">${log.level}</span>
            <span class="log-message">${log.message}</span>
        </div>
    `).join('');
}

function showTileDetail(tile) {
    const content = document.getElementById("tileContent");
    const maxAura = 5; // 用于计算百分比
    
    content.innerHTML = `
        <div class="tile-info">
            <div class="tile-coord">📍 (${tile.x}, ${tile.y})</div>
            <div class="tile-terrain">
                <span class="terrain-icon">${getTerrainIcon(tile.terrain)}</span>
                <span class="terrain-name">${tile.terrainName}</span>
            </div>
            
            <div class="aura-section">
                <h4>五行元气</h4>
                <div class="aura-bars">
                    <div class="aura-bar">
                        <span class="aura-label">金</span>
                        <div class="aura-bar-fill">
                            <div class="aura-fill Jin" style="width: ${Math.min(100, tile.Jin / maxAura * 100)}%"></div>
                        </div>
                        <span class="aura-value">${tile.Jin.toFixed(1)}</span>
                    </div>
                    <div class="aura-bar">
                        <span class="aura-label">木</span>
                        <div class="aura-bar-fill">
                            <div class="aura-fill mu" style="width: ${Math.min(100, tile.mu / maxAura * 100)}%"></div>
                        </div>
                        <span class="aura-value">${tile.mu.toFixed(1)}</span>
                    </div>
                    <div class="aura-bar">
                        <span class="aura-label">水</span>
                        <div class="aura-bar-fill">
                            <div class="aura-fill shui" style="width: ${Math.min(100, tile.shui / maxAura * 100)}%"></div>
                        </div>
                        <span class="aura-value">${tile.shui.toFixed(1)}</span>
                    </div>
                    <div class="aura-bar">
                        <span class="aura-label">火</span>
                        <div class="aura-bar-fill">
                            <div class="aura-fill huo" style="width: ${Math.min(100, tile.huo / maxAura * 100)}%"></div>
                        </div>
                        <span class="aura-value">${tile.huo.toFixed(1)}</span>
                    </div>
                    <div class="aura-bar">
                        <span class="aura-label">土</span>
                        <div class="aura-bar-fill">
                            <div class="aura-fill tu" style="width: ${Math.min(100, tile.tu / maxAura * 100)}%"></div>
                        </div>
                        <span class="aura-value">${tile.tu.toFixed(1)}</span>
                    </div>
                </div>
            </div>
            
            ${tile.modifierCount > 0 ? `<p style="margin-top:12px;color:var(--accent)">✦ ${tile.modifierCount} 个修正源</p>` : ''}
            ${tile.regionId ? `<p style="margin-top:8px">所属区域: ${tile.regionId}</p>` : ''}
            ${tile.landMarkId ? `<p style="margin-top:8px">地标: ${tile.landMarkId}</p>` : ''}
        </div>
    `;
}

function showNpcDetail(npc) {
    const content = document.getElementById("npcContent");
    const lifespanRatio = npc.lifespanMax > 0 ? npc.age / npc.lifespanMax : 0;
    
    content.innerHTML = `
        <div class="npc-info">
            <div class="npc-name">👤 ${npc.name || 'NPC #' + npc.id}</div>
            
            <div class="npc-stat">
                <span class="npc-stat-label">坐标</span>
                <span class="npc-stat-value">(${npc.x}, ${npc.y})</span>
            </div>
            <div class="npc-stat">
                <span class="npc-stat-label">年龄</span>
                <span class="npc-stat-value">${npc.age.toFixed(1)} 岁</span>
            </div>
            <div class="npc-stat">
                <span class="npc-stat-label">寿命</span>
                <span class="npc-stat-value">${npc.lifespanMax.toFixed(0)} 岁</span>
            </div>
            <div class="npc-stat">
                <span class="npc-stat-label">寿元</span>
                <span class="npc-stat-value">${((1 - lifespanRatio) * 100).toFixed(0)}%</span>
            </div>
            <div class="progress-bar">
                <div class="progress-fill" style="width: ${((1 - lifespanRatio) * 100)}%"></div>
            </div>
            
            ${npc.roles && npc.roles.length > 0 ? `
                <div class="npc-roles">
                    <h4>角色</h4>
                    ${npc.roles.map(r => `<span class="role-tag">${r}</span>`).join('')}
                </div>
            ` : ''}
            
            ${npc.traits && npc.traits.length > 0 ? `
                <div class="npc-traits">
                    <h4>特性</h4>
                    ${npc.traits.map(t => `<span class="trait-tag">${t}</span>`).join('')}
                </div>
            ` : ''}
        </div>
    `;
}

function getTerrainIcon(terrain) {
    const icons = ['🌾', '⛰️', '🏔️', '🌊', '🌊', '🏜️', '🌲'];
    return icons[terrain] || '❓';
}

// ── 按钮事件绑定 ──────────────────────────────────────

document.getElementById("btnPause").addEventListener("click", togglePause);

document.getElementById("btnSpeedDown").addEventListener("click", () => {
    if (worldState) {
        const newScale = Math.max(0.25, worldState.timeScale / 2);
        setTimeScale(newScale);
    }
});

document.getElementById("btnSpeedUp").addEventListener("click", () => {
    if (worldState) {
        const newScale = Math.min(10, worldState.timeScale * 2);
        setTimeScale(newScale);
    }
});

document.getElementById("btnStep").addEventListener("click", stepTick);

document.getElementById("btnLogs").addEventListener("click", () => {
    const modal = document.getElementById("logModal");
    modal.classList.toggle("hidden");
});

document.getElementById("btnSettings").addEventListener("click", () => {
    const modal = document.getElementById("settingsModal");
    modal.classList.toggle("hidden");
});

// ── 辅助函数 ──────────────────────────────────────

function closePanel(panelId) {
    document.getElementById(panelId).style.display = "none";
}

function closeModal(modalId) {
    document.getElementById(modalId).classList.add("hidden");
}

// 页面加载完成后初始化
document.addEventListener("DOMContentLoaded", initConnection);
