 # 🌌 Mythosia 
 ![Nuget version](https://img.shields.io/nuget/v/Mythosia.svg?style=flat-square) ![Downloads](https://img.shields.io/nuget/dt/Mythosia.svg?style=flat-square) ![Stars](https://img.shields.io/github/stars/AJ-comp/Mythosia?style=flat-square) ![License](https://img.shields.io/github/license/AJ-comp/Mythosia?style=flat-square)

`Mythosia` is a versatile .NET library offering custom functionality not natively provided by .NET. Whether you're looking to enhance your application's integrity, cryptography, or simply streamline your codebase with handy extensions, Mythosia has got you covered.

---

## 🌟 **Features**

- 🔄 Provides functions to convert numeric data to binary strings and byte arrays.
- 🔗 Enhances development convenience with various extension methods.
- 🔐 Includes encryption algorithms for security and reliability.

---

## 📂 **Project Structure**

### [🌌 **Mythosia**](https://github.com/AJ-comp/Mythosia/tree/master/Mythosia)
⚙️ **Productivity Amplified**: Make the most of extensive extensions that streamline everything from threading and synchronization to intricate collection manipulations.

### [🛡 **Mythosia.Integrity**](https://github.com/AJ-comp/Mythosia/tree/master/Mythosia.Integrity)
🔍 **Error Detection Excellence**: Dive into advanced error detection methodologies supporting CRC16, CRC32, and beyond.

### [🔒 **Mythosia.Security.Cryptography**](https://github.com/AJ-comp/Mythosia/tree/master/Mythosia.Security.Cryptography)
🛠 **Enhanced Cryptographic Convenience**: Simplify your integration of standard encryption techniques like AES, DES, and 3DES with our streamlined functionality.

🔎 **Explore Lesser-known Cryptography**: Experience the power and versatility of less common encryption algorithms like SEED, empowering your application with unique cryptographic options.

🔍 **Hashing Prowess**: Dive into robust hashing capabilities with support for algorithms like MD2 and MD4, ensuring data integrity and security.

### [🧪 **Mythosia.Test**](https://github.com/AJ-comp/Mythosia/tree/master/Mythosia.Test)
🧐 **Comprehensive Testing**: Harness the power of Xunit for systematic testing to ensure every aspect of your project shines.

---

## 🚀 **Getting Started**

Install the .NET 8 SDK, then run:

```powershell
git clone https://github.com/AJ-comp/Mythosia.git
cd Mythosia
dotnet restore Mythosia.sln -p:EnableWindowsTargeting=true
dotnet build Mythosia.sln -c Release --no-restore -p:EnableWindowsTargeting=true
dotnet test Mythosia.sln -c Release --no-build --no-restore -p:EnableWindowsTargeting=true
```

These commands support Windows and Linux checkouts. WPF can be cross-compiled
on Linux with `EnableWindowsTargeting`, but running WPF requires Windows.
See [build and validation instructions](eng/README.md) for the Integrity CI,
benchmark and package checks. Dependencies are restored from NuGet; existing
local build outputs are not required.

Development notes and helpers are preserved in the
[Integrity research archive](eng/research/integrity/README.md).

---

## 📝 **Usage**

To install `Mythosia` using NuGet Package Manager in Visual Studio, use the following command:

```powershell
PM> NuGet\Install-Package Mythosia
```
```powershell
PM> NuGet\Install-Package Mythosia.Integrity
```
```powershell
PM> NuGet\Install-Package Mythosia.Integrity
```
---

## 🤝 **Contributing**

If you wish to contribute to the project, please send a Pull Request or submit an issue.

---

## 📜 **License**

This project is distributed under the MIT license.
