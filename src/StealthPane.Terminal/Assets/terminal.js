const term = new Terminal({
    theme: {
        background: '#1a1a1a',
        foreground: '#d4d4d4',
        cursor: '#d4d4d4',
        cursorAccent: '#1a1a1a',
        selectionBackground: '#264f78',
        black: '#1e1e1e',
        red: '#f44747',
        green: '#6a9955',
        yellow: '#d7ba7d',
        blue: '#569cd6',
        magenta: '#c586c0',
        cyan: '#4ec9b0',
        white: '#d4d4d4',
        brightBlack: '#808080',
        brightRed: '#f44747',
        brightGreen: '#6a9955',
        brightYellow: '#d7ba7d',
        brightBlue: '#569cd6',
        brightMagenta: '#c586c0',
        brightCyan: '#4ec9b0',
        brightWhite: '#e5e5e5'
    },
    fontFamily: "'Cascadia Code', 'Cascadia Mono', 'SF Mono', 'JetBrains Mono', Consolas, monospace",
    fontSize: 14,
    lineHeight: 1.2,
    cursorBlink: true,
    cursorStyle: 'bar',
    allowProposedApi: true
});

const fitAddon = new FitAddon.FitAddon();
term.loadAddon(fitAddon);
term.open(document.getElementById('terminal'));
fitAddon.fit();

term.onData(function(data) {
    sendMessage({ type: 'input', data: data });
});

term.onResize(function(size) {
    sendMessage({ type: 'resize', cols: size.cols, rows: size.rows });
});

const resizeObserver = new ResizeObserver(function() {
    fitAddon.fit();
});
resizeObserver.observe(document.getElementById('terminal'));

function termWrite(base64Data) {
    const binary = atob(base64Data);
    const bytes = new Uint8Array(binary.length);
    for (let i = 0; i < binary.length; i++) {
        bytes[i] = binary.charCodeAt(i);
    }
    term.write(bytes);
}

function termResize(cols, rows) {
    term.resize(cols, rows);
}

function termReset() {
    term.reset();
}

function sendMessage(msg) {
    if (typeof invokeCSharpAction === 'function') {
        invokeCSharpAction(JSON.stringify(msg));
    }
}

sendMessage({ type: 'ready', cols: term.cols, rows: term.rows });
