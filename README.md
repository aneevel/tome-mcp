# ToME MCP Server

An [MCP (Model Context Protocol)](https://modelcontextprotocol.io/) server for [Tales of Maj'Eyal](https://te4.org/) that enables AI-assisted addon development and engine exploration.

## What It Does

This server acts as a bridge between an MCP-capable AI assistant (such as Cursor) and the T-Engine4 game engine. It provides tools to:

- **Explore the engine** — Browse and search T-Engine4 source code, understand class hierarchies, and look up API usage patterns
- **Analyze existing addons** — Ingest community and official addons to learn conventions, patterns, and best practices
- **Generate addon code** — Scaffold new addons and write well-structured Lua code that follows ToME conventions
- **Monitor the game** — Watch game logs and output in real-time to verify that addon behavior matches expectations

## Why

Tales of Maj'Eyal runs on T-Engine4, a powerful but sparsely documented Lua-based engine. Learning to write addons means reading source code, reverse-engineering patterns from existing addons, and a lot of trial and error. This MCP server aims to make that process faster and more accessible by giving an AI assistant direct access to the engine internals and a structured way to interact with them.

## Tech Stack

- **C#** / .NET — MCP server implementation
- **Lua** — T-Engine4 addon language (what the server helps you write)
- **MCP** — Communication protocol between the server and AI clients

## Status

Early development. The project structure and MCP tool surface are being designed.

## License

TBD
