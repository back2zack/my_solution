# OCR Function App (.NET 8 - Azure Functions)

This project is an Azure Functions application that accepts a PDF document and extracts its content using an open source OCR tool (Tesseract). The extracted text is returned as JSON.

## Tech Stack

- .NET 8 (Isolated Worker)
- Azure Functions
- Tesseract OCR
- Magick.NET-Q8-AnyCPU (image handling)
- HttpMultipartParser (file upload handling)

## How It Works

1. Receives a PDF file via HTTP POST
2. Saves the file to a local directory
3. Converts PDF pages to images
4. Applies OCR to each image using Tesseract
5. Returns the extracted text in a structured JSON response

## Installed Packages

These packages must be installed manually using the .NET CLI:

```bash
dotnet add package Tesseract
dotnet add package Magick.NET-Q8-AnyCPU
dotnet add package HttpMultipartParser


