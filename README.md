# Authorized Recovery Controller

A controlled Windows recovery and administration tool for systems that the operator owns or has explicit authorization to administer.

> **Important:** Use this software only on systems where you have permission to perform administration or recovery operations.

## Features

- Pairing-code authentication.
- Explicit agent/controller connection.
- Windows system status checks.
- Controlled restart operation.
- Controlled shutdown operation.
- Local operational logging.
- Recovery-oriented workflow.
- .NET 8 / Windows x64 support.

## Architecture

```text
┌─────────────────────────┐
│   Recovery Controller   │
│                         │
│  Operator Interface     │
│  Pairing Authentication │
└────────────┬────────────┘
             │
             │ Authorized connection
             │
┌────────────▼────────────┐
│    Recovery Agent       │
│                         │
│  Pairing Verification   │
│  Status                 │
│  Restart                │
│  Shutdown               │
└─────────────────────────┘
