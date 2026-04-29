/**
 * Canvas 地图渲染器
 */

// 渲染配置
const TILE_SIZE = 12;        // 基础地块大小（六边形外接圆半径）
const SQRT3 = 1.732;         // sqrt(3)

// 渲染状态
let canvas = null;
let ctx = null;
let tileMap = new Map();     // TileId -> TileDto
let npcPositions = [];       // NPC 位置列表
let offsetX = 0;             // 视图偏移
let offsetY = 0;
let zoom = 1;                // 缩放
let isDragging = false;
let dragStartX = 0;
let dragStartY = 0;

// 地形颜色映射
const TERRAIN_COLORS = {
    0: '#4a7c59',  // Plain - 绿色平原
    1: '#8b7355',  // Hill - 棕色丘陵
    2: '#5a5a5a',  // Mountain - 灰色山地
    3: '#4a90b0',  // RiverLake - 蓝色河湖
    4: '#2a6090',  // Ocean - 深蓝海洋
    5: '#c9a857',  // Desert - 黄色荒漠
    6: '#2d5a27',  // Forest - 深绿森林
};

// 初始化渲染器
function initRenderer() {
    canvas = document.getElementById("mapCanvas");
    ctx = canvas.getContext("2d");
    
    resizeCanvas();
    window.addEventListener("resize", resizeCanvas);
    
    // 鼠标事件
    canvas.addEventListener("mousedown", onMouseDown);
    canvas.addEventListener("mousemove", onMouseMove);
    canvas.addEventListener("mouseup", onMouseUp);
    canvas.addEventListener("wheel", onWheel);
    canvas.addEventListener("click", onClick);
    
    // 设置变更
    document.getElementById("zoomLevel").addEventListener("input", (e) => {
        zoom = parseFloat(e.target.value);
    });
}

function resizeCanvas() {
    const container = canvas.parentElement;
    canvas.width = container.clientWidth;
    canvas.height = container.clientHeight;
    
    if (worldState) {
        renderMap(worldState);
    }
}

// 主渲染函数
function renderMap(state) {
    if (!ctx || !state.tiles) return;
    
    // 清空画布
    ctx.fillStyle = '#1a1a2e';
    ctx.fillRect(0, 0, canvas.width, canvas.height);
    
    // 计算地块大小
    const tileSize = TILE_SIZE * zoom;
    
    // 重置 tileMap
    tileMap.clear();
    npcPositions = state.npcs || [];
    
    // 使用视野中心（而非世界中心）来定位
    // 地图中心始终是画布中心
    const mapCenterX = canvas.width / 2;
    const mapCenterY = canvas.height / 2;
    
    // 视野中心（主 NPC 位置）
    const viewCenterX = state.viewCenterX || 0;
    const viewCenterY = state.viewCenterY || 0;
    
    // 六边形尺寸（flat-top）
    const hexWidth = tileSize * 2;
    const hexHeight = tileSize * SQRT3;
    const horizSpacing = tileSize * 1.5;   // 水平间距
    const vertSpacing = hexHeight;         // 垂直间距
    const oddColOffset = hexHeight / 2;    // 奇数列垂直偏移

    // 渲染所有地块
    for (const tile of state.tiles) {
        const key = `${tile.x},${tile.y}`;
        tileMap.set(key, tile);
        
        // 计算相对于视野中心的偏移
        const relX = tile.x - viewCenterX;
        const relY = tile.y - viewCenterY;
        
        // 计算屏幕坐标（odd-r offset 坐标系）
        const screenX = mapCenterX + relX * horizSpacing + offsetX;
        const screenY = mapCenterY + relY * vertSpacing + (tile.x % 2 === 1 ? oddColOffset : 0) + offsetY;
        
        // 视口裁剪
        if (screenX < -tileSize || screenX > canvas.width + tileSize ||
            screenY < -tileSize || screenY > canvas.height + tileSize) {
            continue;
        }
        
        // 绘制六边形
        drawHexagon(screenX, screenY, tileSize, tile);
    }
    
    // 渲染 NPC
    if (document.getElementById("showNpcs")?.checked !== false) {
        renderNpcs(tileSize, mapCenterX, mapCenterY, viewCenterX, viewCenterY);
    }
}

function drawHexagon(x, y, size, tile) {
    ctx.beginPath();
    
    // 六边形顶点（旋转 90°）
    for (let i = 0; i < 6; i++) {
        const angle = (Math.PI / 3) * i - Math.PI / 6 + Math.PI / 2;
        const px = x + size * Math.cos(angle);
        const py = y + size * Math.sin(angle);
        
        if (i === 0) ctx.moveTo(px, py);
        else ctx.lineTo(px, py);
    }
    ctx.closePath();
    
    // 填充颜色（基于地形或元气）
    let fillColor = TERRAIN_COLORS[tile.terrain] || '#4a7c59';
    
    // 如果显示元气，叠加颜色
    if (document.getElementById("showAura")?.checked) {
        const aura = tile;
        const maxAura = 5;
        const intensity = Math.min(1, (aura.Jin + aura.mu + aura.shui + aura.huo + aura.tu) / maxAura / 5);
        
        // 根据最强元气调整颜色
        const elements = [
            { v: aura.huo, c: '#ff6b6b' },  // 火
            { v: aura.shui, c: '#4ecdc4' }, // 水
            { v: aura.mu, c: '#a8e6cf' },   // 木
            { v: aura.Jin, c: '#ffd93d' }, // 金
            { v: aura.tu, c: '#c9b1ff' },   // 土
        ];
        elements.sort((a, b) => b.v - a.v);
        
        if (elements[0].v > 1.5) {
            fillColor = blendColors(fillColor, elements[0].c, 0.3 + intensity * 0.2);
        }
    }
    
    ctx.fillStyle = fillColor;
    ctx.fill();
    
    // 边框
    ctx.strokeStyle = 'rgba(255,255,255,0.1)';
    ctx.lineWidth = 0.5;
    ctx.stroke();
}

function renderNpcs(tileSize, mapCenterX, mapCenterY, viewCenterX, viewCenterY) {
    // 六边形尺寸
    const hexHeight = tileSize * SQRT3;
    const horizSpacing = tileSize * 1.5;
    const vertSpacing = hexHeight;
    const oddColOffset = hexHeight / 2;

    for (const npc of npcPositions) {
        // 计算相对于视野中心的偏移
        const relX = npc.x - viewCenterX;
        const relY = npc.y - viewCenterY;
        
        // 计算屏幕坐标（odd-r offset 坐标系）
        const screenX = mapCenterX + relX * horizSpacing + offsetX;
        const screenY = mapCenterY + relY * vertSpacing + (npc.x % 2 === 1 ? oddColOffset : 0) + offsetY;
        
        // 视口裁剪
        if (screenX < -tileSize || screenX > canvas.width + tileSize ||
            screenY < -tileSize || screenY > canvas.height + tileSize) {
            continue;
        }
        
        // 绘制 NPC 图标
        ctx.beginPath();
        ctx.arc(screenX, screenY, tileSize * 0.6, 0, Math.PI * 2);
        ctx.fillStyle = selectedint === npc.id ? '#ffd700' : '#ffffff';
        ctx.fill();
        ctx.strokeStyle = '#333';
        ctx.lineWidth = 2;
        ctx.stroke();
        
        // 绘制 NPC 名称
        ctx.font = `bold ${Math.max(10, 12 * zoom)}px sans-serif`;
        ctx.fillStyle = '#fff';
        ctx.textAlign = 'center';
        ctx.textBaseline = 'middle';
        
        // 绘制背景
        const name = npc.name?.slice(0, 4) || 'NPC';
        const textWidth = ctx.measureText(name).width;
        ctx.fillStyle = 'rgba(0,0,0,0.6)';
        ctx.fillRect(screenX - textWidth/2 - 4, screenY - tileSize * 0.6 - 14, textWidth + 8, 16);
        
        ctx.fillStyle = '#fff';
        ctx.fillText(name, screenX, screenY - tileSize * 0.6 - 6);
    }
}

// 颜色混合
function blendColors(color1, color2, ratio) {
    const parseHex = (hex) => {
        const r = parseInt(hex.slice(1, 3), 16);
        const g = parseInt(hex.slice(3, 5), 16);
        const b = parseInt(hex.slice(5, 7), 16);
        return [r, g, b];
    };
    
    const [r1, g1, b1] = parseHex(color1);
    const [r2, g2, b2] = parseHex(color2);
    
    const r = Math.round(r1 + (r2 - r1) * ratio);
    const g = Math.round(g1 + (g2 - g1) * ratio);
    const b = Math.round(b1 + (b2 - b1) * ratio);
    
    return `rgb(${r},${g},${b})`;
}

// 鼠标交互
function onMouseDown(e) {
    isDragging = true;
    dragStartX = e.clientX - offsetX;
    dragStartY = e.clientY - offsetY;
}

function onMouseMove(e) {
    if (isDragging) {
        offsetX = e.clientX - dragStartX;
        offsetY = e.clientY - dragStartY;
        if (worldState) renderMap(worldState);
    }
    
    // 更新 tooltip
    updateTooltip(e);
}

function onMouseUp(e) {
    isDragging = false;
}

function onWheel(e) {
    e.preventDefault();
    const delta = e.deltaY > 0 ? 0.9 : 1.1;
    zoom = Math.max(0.5, Math.min(3, zoom * delta));
    document.getElementById("zoomLevel").value = zoom;
    if (worldState) renderMap(worldState);
}

function onClick(e) {
    if (isDragging) return;
    
    const rect = canvas.getBoundingClientRect();
    const mouseX = e.clientX - rect.left;
    const mouseY = e.clientY - rect.top;
    
    const tileSize = TILE_SIZE * zoom;
    const mapCenterX = canvas.width / 2;
    const mapCenterY = canvas.height / 2;
    
    // 使用 worldState 中的视野中心
    const viewCenterX = worldState?.viewCenterX || 0;
    const viewCenterY = worldState?.viewCenterY || 0;
    
    // 六边形尺寸（与渲染一致）
    const hexHeight = tileSize * SQRT3;
    const horizSpacing = tileSize * 1.5;
    const vertSpacing = hexHeight;
    const oddColOffset = hexHeight / 2;
    
    // 检查是否点击了 NPC
    for (const npc of npcPositions) {
        const relX = npc.x - viewCenterX;
        const relY = npc.y - viewCenterY;
        const screenX = mapCenterX + relX * horizSpacing + offsetX;
        const screenY = mapCenterY + relY * vertSpacing + (npc.x % 2 === 1 ? oddColOffset : 0) + offsetY;
        
        const dist = Math.sqrt((mouseX - screenX) ** 2 + (mouseY - screenY) ** 2);
        if (dist < tileSize * 0.7) {
            selectNpc(npc.id);
            showNpcDetail(npc);
            return;
        }
    }
    
    // 检查是否点击了地块
    for (const [key, tile] of tileMap) {
        const relX = tile.x - viewCenterX;
        const relY = tile.y - viewCenterY;
        const screenX = mapCenterX + relX * horizSpacing + offsetX;
        const screenY = mapCenterY + relY * vertSpacing + (tile.x % 2 === 1 ? oddColOffset : 0) + offsetY;
        
        const dist = Math.sqrt((mouseX - screenX) ** 2 + (mouseY - screenY) ** 2);
        if (dist < tileSize) {
            selectTile(tile.x, tile.y);
            showTileDetail(tile);
            return;
        }
    }
}

function updateTooltip(e) {
    const tooltip = document.getElementById("tooltip");
    const rect = canvas.getBoundingClientRect();
    const mouseX = e.clientX - rect.left;
    const mouseY = e.clientY - rect.top;
    
    const tileSize = TILE_SIZE * zoom;
    const mapCenterX = canvas.width / 2;
    const mapCenterY = canvas.height / 2;
    
    // 使用 worldState 中的视野中心
    const viewCenterX = worldState?.viewCenterX || 0;
    const viewCenterY = worldState?.viewCenterY || 0;
    
    // 六边形尺寸（与渲染一致）
    const hexHeight = tileSize * SQRT3;
    const horizSpacing = tileSize * 1.5;
    const vertSpacing = hexHeight;
    const oddColOffset = hexHeight / 2;
    
    // 检查是否悬停在地块上
    for (const [key, tile] of tileMap) {
        const relX = tile.x - viewCenterX;
        const relY = tile.y - viewCenterY;
        const screenX = mapCenterX + relX * horizSpacing + offsetX;
        const screenY = mapCenterY + relY * vertSpacing + (tile.x % 2 === 1 ? oddColOffset : 0) + offsetY;
        
        const dist = Math.sqrt((mouseX - screenX) ** 2 + (mouseY - screenY) ** 2);
        if (dist < tileSize) {
            tooltip.classList.remove("hidden");
            tooltip.style.left = (e.clientX + 10) + "px";
            tooltip.style.top = (e.clientY + 10) + "px";
            
            // 构建 tooltip 内容
            let content = `(${tile.x}, ${tile.y}) ${tile.terrainName}`;
            if (tile.modifierCount > 0) {
                content += ` ✦${tile.modifierCount}`;
            }
            tooltip.innerHTML = content;
            return;
        }
    }
    
    tooltip.classList.add("hidden");
}

// 初始化
document.addEventListener("DOMContentLoaded", initRenderer);
