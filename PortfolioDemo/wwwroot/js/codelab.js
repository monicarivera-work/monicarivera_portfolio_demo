/**
 * codelab.js — Code Lab interactive execution & stack-trace visualizer
 *
 * Architecture:
 *   1. User writes JavaScript in the editor textarea.
 *   2. On "Run", code is wrapped with a trace harness and injected into a
 *      sandboxed <iframe> via srcdoc. The sandbox has no access to the
 *      parent page (sandbox="allow-scripts").
 *   3. The iframe posts trace snapshots + console lines back via postMessage.
 *   4. The visualizer renders each snapshot, auto-detecting the shape of
 *      each variable (scalar, array, stack, linked list, binary tree, map).
 *   5. The step navigator lets the user step through snapshots one by one
 *      or play them as an animation.
 */

(function () {
    'use strict';

    // ── DOM refs ──────────────────────────────────────────────────────────────
    const editor      = document.getElementById('cl-editor');
    const runBtn      = document.getElementById('cl-run-btn');
    const clearBtn    = document.getElementById('cl-clear-btn');
    const exampleBtn  = document.getElementById('cl-example-btn');
    const consoleEl   = document.getElementById('cl-console');
    const sandbox     = document.getElementById('cl-sandbox');
    const vizEmpty    = document.getElementById('cl-viz-empty');
    const vizSteps    = document.getElementById('cl-viz-steps');
    const snapDisplay = document.getElementById('cl-snapshot-display');
    const stepLabel   = document.getElementById('cl-step-label');
    const btnFirst    = document.getElementById('cl-step-first');
    const btnPrev     = document.getElementById('cl-step-prev');
    const btnPlay     = document.getElementById('cl-step-play');
    const btnNext     = document.getElementById('cl-step-next');
    const btnLast     = document.getElementById('cl-step-last');

    // ── State ─────────────────────────────────────────────────────────────────
    let snapshots = [];
    let currentStep = 0;
    let playTimer = null;
    const PLAY_INTERVAL_MS = 1200;

    // ── Example programs ─────────────────────────────────────────────────────
    const EXAMPLES = [
`// Binary Search example
function binarySearch(arr, target) {
    let lo = 0, hi = arr.length - 1;
    while (lo <= hi) {
        let mid = Math.floor((lo + hi) / 2);
        trace("iteration", { lo, mid, hi, arr, current: arr[mid], target });
        if (arr[mid] === target) return mid;
        if (arr[mid] < target) lo = mid + 1;
        else hi = mid - 1;
    }
    return -1;
}

const arr = [2, 5, 8, 12, 16, 23, 38, 56, 72, 91];
const idx = binarySearch(arr, 23);
trace("result", { idx, found: arr[idx] });`,

`// Bubble Sort step-by-step
function bubbleSort(arr) {
    const a = [...arr];
    for (let i = 0; i < a.length; i++) {
        for (let j = 0; j < a.length - i - 1; j++) {
            trace("compare", { array: [...a], i, j, comparing: [a[j], a[j+1]] });
            if (a[j] > a[j+1]) {
                [a[j], a[j+1]] = [a[j+1], a[j]];
            }
        }
    }
    trace("sorted", { array: a });
    return a;
}
bubbleSort([64, 34, 25, 12, 22, 11, 90]);`,

`// Build a linked list
function buildLinkedList(values) {
    let head = null, tail = null;
    for (const v of values) {
        const node = { val: v, next: null };
        if (!head) { head = tail = node; }
        else { tail.next = node; tail = node; }
        trace("append " + v, { head });
    }
    return head;
}
buildLinkedList([1, 2, 3, 4, 5]);`,

`// Build a binary tree and traverse
function insert(root, val) {
    if (!root) return { val, left: null, right: null };
    if (val < root.val) root.left  = insert(root.left,  val);
    else               root.right = insert(root.right, val);
    return root;
}
let tree = null;
for (const v of [5, 3, 7, 1, 4, 6, 8]) {
    tree = insert(tree, v);
    trace("insert " + v, { tree });
}`,

`// Stack-based balanced parentheses checker
function isBalanced(s) {
    const stack = [];
    const pairs = { ')':'(', ']':'[', '}':'{' };
    for (const ch of s) {
        if ('([{'.includes(ch)) {
            stack.push(ch);
            trace("push " + ch, { stack: [...stack], char: ch });
        } else if (pairs[ch]) {
            const top = stack.pop();
            trace("pop " + ch, { stack: [...stack], expected: pairs[ch], got: top, match: top === pairs[ch] });
        }
    }
    trace("done", { stack: [...stack], balanced: stack.length === 0 });
    return stack.length === 0;
}
isBalanced("({[]})");`,
    ];
    let exampleIdx = 0;

    // ── Default code ──────────────────────────────────────────────────────────
    editor.value = EXAMPLES[0];

    // ── Sandboxed execution harness ───────────────────────────────────────────
    /**
     * Builds the srcdoc for the iframe: injects the trace() helper + user code,
     * all output posted back as structured messages.
     */
    function buildSandboxDoc(userCode) {
        // We JSON-stringify to safely embed the user code as a string literal
        const escapedCode = JSON.stringify(userCode);
        return `<!DOCTYPE html><html><body><script>
(function() {
    var snapshots = [];
    var consoleLogs = [];

    window.trace = function(label, vars) {
        try {
            snapshots.push({ label: String(label), state: JSON.parse(JSON.stringify(vars)) });
        } catch(e) {
            snapshots.push({ label: String(label), state: { error: 'State not serialisable: ' + e.message } });
        }
    };

    var origLog   = console.log.bind(console);
    var origError = console.error.bind(console);
    var origWarn  = console.warn.bind(console);

    function capture(type, args) {
        consoleLogs.push({ type, text: args.map(a => {
            try { return typeof a === 'object' ? JSON.stringify(a) : String(a); }
            catch(e) { return String(a); }
        }).join(' ') });
    }

    console.log   = function() { capture('log',   Array.from(arguments)); origLog.apply(console, arguments); };
    console.error = function() { capture('error', Array.from(arguments)); origError.apply(console, arguments); };
    console.warn  = function() { capture('warn',  Array.from(arguments)); origWarn.apply(console, arguments); };

    var userError = null;
    try {
        var __code__ = ${escapedCode};
        eval(__code__);
    } catch(e) {
        userError = e.toString();
    }

    window.parent.postMessage({
        type: 'codelab-result',
        snapshots: snapshots,
        consoleLogs: consoleLogs,
        error: userError
    }, '*');
})();
<\/script></body></html>`;
    }

    // ── Run ───────────────────────────────────────────────────────────────────
    function runCode() {
        snapshots = [];
        currentStep = 0;
        clearViz();
        clearConsole();

        const code = editor.value.trim();
        if (!code) {
            appendConsole('log', '// Nothing to run.');
            return;
        }

        runBtn.disabled = true;
        runBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Running…';

        sandbox.srcdoc = buildSandboxDoc(code);
    }

    // ── Handle message from sandbox ───────────────────────────────────────────
    window.addEventListener('message', function (e) {
        if (!e.data || e.data.type !== 'codelab-result') return;

        runBtn.disabled = false;
        runBtn.innerHTML = '<i class="fas fa-play"></i> Run';

        const { snapshots: snaps, consoleLogs, error } = e.data;

        // Console output
        (consoleLogs || []).forEach(l => appendConsole(l.type, l.text));
        if (error) appendConsole('error', error);

        snapshots = snaps || [];
        if (snapshots.length === 0) {
            if (!error) appendConsole('log', '// No trace() calls found. Add trace("label", {vars}) to visualize state.');
            showEmpty();
        } else {
            showSteps();
            renderStep(0);
        }
    });

    // ── Console helpers ───────────────────────────────────────────────────────
    function clearConsole() {
        consoleEl.innerHTML = '';
    }
    function appendConsole(type, text) {
        const line = document.createElement('div');
        if (type === 'error') line.className = 'cl-console-err';
        line.textContent = (type === 'error' ? '✗ ' : type === 'warn' ? '⚠ ' : '') + text;
        consoleEl.appendChild(line);
        consoleEl.scrollTop = consoleEl.scrollHeight;
    }

    // ── Visualization state helpers ───────────────────────────────────────────
    function clearViz() {
        stopPlay();
        vizEmpty.style.display  = 'flex';
        vizSteps.style.display  = 'none';
        snapDisplay.innerHTML   = '';
    }
    function showEmpty() {
        vizEmpty.style.display = 'flex';
        vizSteps.style.display = 'none';
    }
    function showSteps() {
        vizEmpty.style.display = 'none';
        vizSteps.style.display = '';
        updateNavButtons();
    }

    // ── Step navigation ───────────────────────────────────────────────────────
    function renderStep(idx) {
        currentStep = Math.max(0, Math.min(idx, snapshots.length - 1));
        const snap = snapshots[currentStep];
        stepLabel.textContent = `Step ${currentStep + 1} / ${snapshots.length}`;
        snapDisplay.innerHTML = '';
        snapDisplay.appendChild(buildSnapshotEl(snap));
        updateNavButtons();
    }

    function updateNavButtons() {
        const n = snapshots.length;
        btnFirst.disabled = currentStep === 0;
        btnPrev.disabled  = currentStep === 0;
        btnNext.disabled  = currentStep >= n - 1;
        btnLast.disabled  = currentStep >= n - 1;
    }

    btnFirst.addEventListener('click', () => { stopPlay(); renderStep(0); });
    btnPrev.addEventListener('click',  () => { stopPlay(); renderStep(currentStep - 1); });
    btnNext.addEventListener('click',  () => { stopPlay(); renderStep(currentStep + 1); });
    btnLast.addEventListener('click',  () => { stopPlay(); renderStep(snapshots.length - 1); });

    btnPlay.addEventListener('click', () => {
        if (playTimer) { stopPlay(); return; }
        startPlay();
    });

    function startPlay() {
        btnPlay.textContent = '⏸';
        playTimer = setInterval(() => {
            if (currentStep >= snapshots.length - 1) { stopPlay(); return; }
            renderStep(currentStep + 1);
        }, PLAY_INTERVAL_MS);
    }

    function stopPlay() {
        if (playTimer) { clearInterval(playTimer); playTimer = null; }
        btnPlay.textContent = '▶';
    }

    // ── Snapshot renderer ─────────────────────────────────────────────────────
    function buildSnapshotEl(snap) {
        const card = el('div', 'cl-snapshot');

        const lbl = el('div', 'cl-snap-label');
        lbl.innerHTML = `<span>📍</span> ${escHtml(snap.label)}`;
        card.appendChild(lbl);

        const vars = el('div', 'cl-snap-vars');

        for (const [name, value] of Object.entries(snap.state || {})) {
            const group = el('div', 'cl-var-group');

            const nameEl = el('div', 'cl-var-name');
            nameEl.textContent = name;
            group.appendChild(nameEl);

            group.appendChild(buildValueEl(value));
            vars.appendChild(group);
        }

        card.appendChild(vars);
        return card;
    }

    // ── Value renderer (auto-detects shape) ───────────────────────────────────
    function buildValueEl(value) {
        if (value === null || value === undefined) {
            return scalar('null', true);
        }
        if (typeof value !== 'object') {
            return scalar(String(value));
        }
        if (Array.isArray(value)) {
            return buildArrayEl(value);
        }
        // Linked list? {val, next}
        if (isLinkedList(value)) {
            return buildLinkedListEl(value);
        }
        // Binary tree? {val, left, right}
        if (isBinaryTree(value)) {
            return buildTreeEl(value);
        }
        // Plain object / map
        return buildObjectEl(value);
    }

    // ── Shape detectors ───────────────────────────────────────────────────────
    function isLinkedList(v) {
        if (v === null) return true;
        return typeof v === 'object' && !Array.isArray(v) && 'val' in v && 'next' in v && !('left' in v) && !('right' in v);
    }
    function isBinaryTree(v) {
        if (v === null) return true;
        return typeof v === 'object' && !Array.isArray(v) && 'val' in v && ('left' in v || 'right' in v);
    }

    // ── Array ─────────────────────────────────────────────────────────────────
    function buildArrayEl(arr) {
        const wrap = el('div', 'cl-array-viz');
        arr.forEach((item, i) => {
            const cell = el('div', 'cl-array-cell');
            const box = el('div', 'cl-array-box');
            box.textContent = typeof item === 'object' ? JSON.stringify(item) : String(item);
            const idx = el('div', 'cl-array-idx');
            idx.textContent = i;
            cell.appendChild(box);
            cell.appendChild(idx);
            wrap.appendChild(cell);
        });
        if (arr.length === 0) {
            const empty = el('span'); empty.style.cssText = 'color:var(--text-light);font-style:italic;font-size:0.8rem;'; empty.textContent = '(empty array)';
            wrap.appendChild(empty);
        }
        return wrap;
    }

    // ── Linked list ───────────────────────────────────────────────────────────
    function buildLinkedListEl(head) {
        const wrap = el('div', 'cl-ll-viz');
        let cur = head;
        let limit = 50;
        while (cur && limit-- > 0) {
            const node = el('div', 'cl-ll-node');
            const box = el('div', 'cl-ll-box');
            box.textContent = String(cur.val);
            node.appendChild(box);
            if (cur.next) {
                const arrow = el('span', 'cl-ll-arrow'); arrow.textContent = '→';
                node.appendChild(arrow);
            }
            wrap.appendChild(node);
            cur = cur.next;
        }
        const nil = el('span', 'cl-ll-null'); nil.textContent = '→ null';
        wrap.appendChild(nil);
        return wrap;
    }

    // ── Binary tree (SVG layout) ───────────────────────────────────────────────
    function buildTreeEl(root) {
        const wrap = el('div', 'cl-tree-viz');

        // Collect nodes with positions using a recursive layout
        const nodes = [];
        const edges = [];
        const XGAP = 44, YGAP = 56, R = 18;

        function layout(node, depth, leftBound, rightBound) {
            if (!node) return null;
            const mid = (leftBound + rightBound) / 2;
            const id = nodes.length;
            nodes.push({ id, val: node.val, x: mid, y: depth * YGAP + R + 10 });
            const lId = layout(node.left,  depth + 1, leftBound, mid);
            const rId = layout(node.right, depth + 1, mid, rightBound);
            if (lId !== null) edges.push([id, lId]);
            if (rId !== null) edges.push([id, rId]);
            return id;
        }

        // Determine width needed
        function treeDepth(node) { if (!node) return 0; return 1 + Math.max(treeDepth(node.left), treeDepth(node.right)); }
        function treeLeaves(node) { if (!node) return 1; if (!node.left && !node.right) return 1; return treeLeaves(node.left) + treeLeaves(node.right); }

        const depth = treeDepth(root);
        const leaves = treeLeaves(root);
        const width = Math.max(leaves * XGAP + XGAP, 100);
        const height = depth * YGAP + R * 2 + 20;

        layout(root, 0, 0, width);

        const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
        svg.setAttribute('width', width);
        svg.setAttribute('height', height);
        svg.setAttribute('viewBox', `0 0 ${width} ${height}`);
        svg.className.baseVal = 'cl-tree-svg';

        // Edges
        for (const [a, b] of edges) {
            const na = nodes[a], nb = nodes[b];
            const line = document.createElementNS('http://www.w3.org/2000/svg', 'line');
            line.setAttribute('x1', na.x); line.setAttribute('y1', na.y);
            line.setAttribute('x2', nb.x); line.setAttribute('y2', nb.y);
            line.setAttribute('stroke', 'rgba(212,181,232,0.7)'); line.setAttribute('stroke-width', '1.5');
            svg.appendChild(line);
        }

        // Node circles
        for (const n of nodes) {
            const circle = document.createElementNS('http://www.w3.org/2000/svg', 'circle');
            circle.setAttribute('cx', n.x); circle.setAttribute('cy', n.y); circle.setAttribute('r', R);
            circle.setAttribute('fill', 'rgba(212,181,232,0.35)'); circle.setAttribute('stroke', 'rgba(123,94,167,0.6)'); circle.setAttribute('stroke-width', '1.5');
            svg.appendChild(circle);

            const text = document.createElementNS('http://www.w3.org/2000/svg', 'text');
            text.setAttribute('x', n.x); text.setAttribute('y', n.y + 5);
            text.setAttribute('text-anchor', 'middle'); text.setAttribute('font-size', '11');
            text.setAttribute('font-family', 'Courier New, monospace'); text.setAttribute('fill', '#5a5a6e');
            text.textContent = String(n.val).length > 4 ? String(n.val).slice(0,3)+'…' : String(n.val);
            svg.appendChild(text);
        }

        wrap.appendChild(svg);
        return wrap;
    }

    // ── Plain object ──────────────────────────────────────────────────────────
    function buildObjectEl(obj) {
        const wrap = el('div', 'cl-obj-viz');
        const entries = Object.entries(obj);
        if (entries.length === 0) {
            const e = el('span'); e.style.cssText = 'color:var(--text-light);font-style:italic;font-size:0.8rem;'; e.textContent = '{}';
            wrap.appendChild(e);
            return wrap;
        }
        const MAX = 20;
        entries.slice(0, MAX).forEach(([k, v]) => {
            const row = el('div', 'cl-obj-row');
            const key = el('span', 'cl-obj-key'); key.textContent = k;
            const colon = el('span', 'cl-obj-colon'); colon.textContent = ':';
            const val = el('span', 'cl-obj-val');
            val.textContent = typeof v === 'object' ? JSON.stringify(v) : String(v);
            row.appendChild(key); row.appendChild(colon); row.appendChild(val);
            wrap.appendChild(row);
        });
        if (entries.length > MAX) {
            const more = el('span'); more.style.cssText = 'font-size:0.75rem;color:var(--text-light);'; more.textContent = `… +${entries.length - MAX} more`;
            wrap.appendChild(more);
        }
        return wrap;
    }

    // ── Scalar ────────────────────────────────────────────────────────────────
    function scalar(text, isNull) {
        const s = el('span', 'cl-var-scalar' + (isNull ? ' cl-var-null' : ''));
        s.textContent = text;
        return s;
    }

    // ── DOM helpers ───────────────────────────────────────────────────────────
    function el(tag, cls) {
        const e = document.createElement(tag);
        if (cls) e.className = cls;
        return e;
    }
    function escHtml(s) {
        return String(s).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;');
    }

    // ── Button handlers ───────────────────────────────────────────────────────
    runBtn.addEventListener('click', runCode);

    clearBtn.addEventListener('click', () => {
        editor.value = '';
        clearConsole();
        clearViz();
        editor.focus();
    });

    exampleBtn.addEventListener('click', () => {
        editor.value = EXAMPLES[exampleIdx % EXAMPLES.length];
        exampleIdx++;
        clearConsole();
        clearViz();
        editor.focus();
    });

    // Tab key → insert 2 spaces in editor
    editor.addEventListener('keydown', e => {
        if (e.key === 'Tab') {
            e.preventDefault();
            const start = editor.selectionStart;
            const end   = editor.selectionEnd;
            editor.value = editor.value.slice(0, start) + '  ' + editor.value.slice(end);
            editor.selectionStart = editor.selectionEnd = start + 2;
        }
        if ((e.ctrlKey || e.metaKey) && e.key === 'Enter') {
            runCode();
        }
    });

    // ── Session management ────────────────────────────────────────────────────
    if (window.clIsAuthenticated) {
        loadSessions();

        const saveBtn         = document.getElementById('cl-btn-save');
        const sessionNameInput = document.getElementById('cl-session-name');

        saveBtn && saveBtn.addEventListener('click', async () => {
            const name = (sessionNameInput && sessionNameInput.value.trim()) || '';
            if (!name) { alert('Please enter a session name.'); return; }
            const code = editor.value;
            if (!code.trim()) { alert('Nothing to save — write some code first.'); return; }

            try {
                saveBtn.disabled = true;
                saveBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Saving…';
                const res = await fetch('/api/sessions', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ sessionName: name, code, language: 'javascript' })
                });
                if (!res.ok) throw new Error(await res.text());
                appendConsole('log', '✓ Session "' + name + '" saved.');
                loadSessions();
            } catch (err) {
                appendConsole('error', 'Failed to save session: ' + err.message);
            } finally {
                saveBtn.disabled = false;
                saveBtn.innerHTML = '<i class="fas fa-save"></i> Save';
            }
        });
    }

    async function loadSessions() {
        const list = document.getElementById('cl-sessions-list');
        if (!list) return;
        try {
            const res = await fetch('/api/sessions');
            if (!res.ok) { list.innerHTML = '<p class="text-muted" style="font-size:0.85rem;">Could not load sessions.</p>'; return; }
            const sessions = await res.json();
            if (!sessions.length) { list.innerHTML = '<p class="text-muted" style="font-size:0.85rem;">No saved sessions yet.</p>'; return; }
            list.innerHTML = sessions.map(s => {
                const date = new Date(s.updatedAt).toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' });
                const nameEnc = encodeURIComponent(s.sessionName);
                return `<div class="cl-session-item">
                    <span class="cl-session-item-name">${escHtml(s.sessionName)}</span>
                    <span class="cl-session-item-date">${date}</span>
                    <button class="cl-btn-load" onclick="clLoadSession(${JSON.stringify(s.sessionName)})">Load</button>
                    <button class="cl-btn-del"  onclick="clDeleteSession(${JSON.stringify(s.sessionName)})">✕</button>
                </div>`;
            }).join('');
        } catch (err) {
            list.innerHTML = '<p style="color:#d9534f;font-size:0.85rem;">Error loading sessions.</p>';
        }
    }

    window.clLoadSession = async function (name) {
        try {
            const res = await fetch('/api/sessions/' + encodeURIComponent(name));
            if (!res.ok) throw new Error('Not found');
            const session = await res.json();
            editor.value = session.code;
            const inp = document.getElementById('cl-session-name');
            if (inp) inp.value = session.sessionName;
            clearViz();
            appendConsole('log', '✓ Session "' + name + '" loaded.');
            editor.focus();
        } catch (err) {
            appendConsole('error', 'Failed to load session: ' + err.message);
        }
    };

    window.clDeleteSession = async function (name) {
        if (!confirm('Delete session "' + name + '"?')) return;
        try {
            await fetch('/api/sessions/' + encodeURIComponent(name), { method: 'DELETE' });
            appendConsole('log', '✓ Session "' + name + '" deleted.');
            loadSessions();
        } catch (err) {
            appendConsole('error', 'Failed to delete session: ' + err.message);
        }
    };

})();
