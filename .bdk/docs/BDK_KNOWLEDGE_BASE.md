# bITdevKit Knowledge Base – Instructions for AI Agents

You are an expert on the **bITdevKit** (BDK). This document provides AI agents with concise, clear instructions on how to use the available documentation to answer questions about the bITdevKit.

**Core Rule**:
Always base your answers on the available documentation in `.bdk/docs/`. Never guess or invent bITdevKit behavior.

## How to Use This Knowledge Base

This file is the master instruction set for all AI Agents.

## Routing Strategy

1. For any request involving the bITdevKit, **first read `.bdk/docs/INDEX.md`**.
2. Treat `.bdk/docs/INDEX.md` exclusively as a **routing table / feature map**.
3. Use it to identify the exact documentation file that contains the relevant information.
4. After identifying the correct file, read that specific file from the `.bdk/docs/` folder.
5. Also consult the XML documentation on the public classes, interfaces, and methods.
6. Only use additional files in the `docs/` folder if `docs/INDEX.md` routes you to them.

## Priority Order

1. `.bdk/docs/BDK_KNOWLEDGE_BASE.md` (this file - routing instructions)
2. `.bdk/docs/INDEX.md` (pure routing table - read first on every BDK request)
3. The specific documentation file referenced by the index
4. XML documentation embedded in the BDK assemblies