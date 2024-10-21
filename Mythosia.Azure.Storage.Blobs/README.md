**Description**:  
This package provides an extension method to automatically convert strings into valid Azure Blob Storage container names. Azure Blob Storage has strict naming rules that require lowercase letters, numbers, and hyphens only, with specific constraints on hyphen placement and length. This extension method helps developers avoid manual naming errors by automatically transforming arbitrary input strings into compliant container names.

**Key Features**:  
- Converts camelCase or PascalCase strings to lowercase with hyphens.
- Replaces disallowed characters with hyphens.
- Ensures the name meets Azure's length and character restrictions.

**Usage**:  
```csharp
using Mythosia.Azure.Storage.Blobs;

// Example usage
string validName = "PhonoMaster@123".ToBlobContainerName();
// Output: "phono-master-123"
```

This utility simplifies compliance with Azure Blob Storage naming rules, reducing errors and improving consistency in storage management.

---

**BlobServiceClient and Azure Key Vault Integration**  
The BlobServiceClient is a part of the Azure SDK designed to interact with Azure Blob Storage. It provides methods for working with containers and blobs, such as uploading, downloading, deleting, and generating Shared Access Signatures (SAS). However, BlobServiceClient does not natively include integration with Azure Key Vault for secret management, such as storing or retrieving credentials or connection strings.

Azure Key Vault, on the other hand, is a service that provides centralized secret management, enabling secure storage and access to sensitive information such as API keys, connection strings, and certificates. It allows secure access using Azure Managed Identity or Service Principal Authentication.

While BlobServiceClient itself does not handle secrets or authentication via Azure Key Vault, you can integrate the two services by retrieving secrets (such as Blob Storage connection strings or SAS tokens) from Azure Key Vault and using them to initialize BlobServiceClient.

**Key Points**:  
- **BlobServiceClient**:
  - Used to perform operations on Azure Blob Storage (upload, download, delete, etc.).
  - Does not have native support for managing secrets or authentication via Azure Key Vault.
- **Azure Key Vault**:
  - A service for managing and securing sensitive information like connection strings and credentials.
  - Can store secrets required for securely accessing services like Azure Blob Storage.

**Integration**:  
You can retrieve connection strings or SAS tokens from Azure Key Vault using Azure SDKs (e.g., SecretClient) and use them to instantiate BlobServiceClient.

**Example**:  
```csharp
// create BlobServiceClient with key vault information
new ExtendBlobServiceClient("https://mythosia-key-vault.vault.azure.net/", "blob");
```
