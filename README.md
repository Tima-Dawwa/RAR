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
* Windows GUI and context menu support

## Compression & Decompression Workflow

1. Validate input files/folders.
2. Read data and metadata.
3. Compress using chosen algorithm (Huffman/Shannon-Fano).
4. Encrypt data if a password is set.
5. Write archive to disk.
6. Decompression reverses the steps, reconstructing original files.

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
