#!/usr/bin/env node

// Self-signed sertifika uyarılarını bypass et
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';

const readline = require('readline');

const rl = readline.createInterface({
    input: process.stdin,
    output: process.stdout,
    terminal: false
});

rl.on('line', async (line) => {
    try {
        const request = JSON.parse(line);
        
        if (request.method === 'initialize') {
            console.log(JSON.stringify({
                jsonrpc: "2.0",
                id: request.id,
                result: {
                    protocolVersion: "2024-11-05",
                    serverInfo: { name: "ArchGuard-BIM-MCP-Server", version: "1.0.0" },
                    capabilities: { tools: {} }
                }
            }));
            return;
        }

        const response = await fetch('https://localhost:7002/api/Mcp', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(request)
        });

        const data = await response.json();
        console.log(JSON.stringify(data));
    } catch (err) {
        console.error(JSON.stringify({
            jsonrpc: "2.0",
            error: { code: -32603, message: err.message }
        }));
    }
});