# RAR – File Compression and Decompression Utility

## Overview

RAR is a Windows-based C# application for **compressing and decompressing files and folders** using **Huffman** and **Shannon-Fano** algorithms. It provides optional encryption, handles multi-file archives, and supports threaded, pause/resume operations.

## Features

* Compression & Decompression of files and folders
* Algorithms Supported: Huffman & Shannon-Fano
* AES-based password-protected archives
* Multi-file and folder operations
* Threaded operations & pause/resume
* Handles edge cases: empty files, single-byte files
* Windows GUI with context menu support

## Compression & Decompression Workflow

1. Validate input files and folders (check existence).
2. Read file bytes and create file metadata (paths, sizes, offsets).
3. Compress using the selected algorithm:
   - Huffman: build frequency table → construct Huffman tree → generate codes → encode data.
   - Shannon-Fano: build frequency table → generate prefix codes → encode data.
4. Encrypt the compressed data if a password is provided.
5. Write the archive to disk with metadata and encoded data.
6. Decompression reverses these steps: read metadata → decrypt (if needed) → decode data → reconstruct original files.

## Data Structures

* **File Metadata** – Stores file paths, sizes, offsets for multi-file archives.
* **Huffman Node / BitString** – Represents tree nodes and bit-level encoding.
* **Shannon-Fano Codes** – Stores prefix codes for symbols.

## Technical Details

* **Language:** C# (.NET Framework 4.7+)
* **UI:** Windows Forms
* **Threading:** Multi-threaded operations
* **Encryption:** AES via `EncryptionHelper.cs`
* **Edge Cases:** Empty files, single-byte files, cancellation support

## License

MIT License
