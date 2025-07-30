# CyberVault - Secure Offline Password Manager for Windows
![Project Status: Stable](https://img.shields.io/badge/status-stable-brightgreen.svg)

**CyberVault** is a free, open-source password manager and secure credential vault for Windows users who prioritize privacy and local data control. Built with C# and military-grade encryption, CyberVault stores all your passwords offline on your computer - no cloud, no third-party servers, complete privacy.

> **Perfect for**: Security-conscious users, privacy advocates, businesses requiring offline password storage, and anyone seeking a reliable alternative to cloud-based password managers like LastPass, Bitwarden, or 1Password.

Developed by **CyberHansen** and **CyberNilsen** | Featured in *Laagendalsposten* newspaper 🇳🇴

### ✅ Project Status: Stable  
CyberVault is production-ready and actively maintained. While major new features are not being added right now, we continue releasing security updates and bug fixes.

![CyberVault Program](https://github.com/user-attachments/assets/ad8568e6-56e0-4c2e-8e61-1d12526c7188)

## 🎯 Why Choose CyberVault?

**🔐 Ultimate Privacy**: Unlike online password managers, your sensitive data never leaves your device  
**🏠 100% Offline**: No internet required after installation - perfect for air-gapped systems  
**🛡️ Military-Grade Security**: AES encryption with industry-standard cryptographic algorithms  
**💻 Windows Native**: Optimized specifically for Windows 10/11 with native WPF interface  
**🆓 Completely Free**: Open-source MIT license - inspect the code yourself  
**🌐 Browser Integration**: Chrome extension available for seamless web password management  

## 🚀 Key Features

### Password Management
- **Secure Password Storage**: AES-256 encryption with PBKDF2 key derivation
- **Password Generator**: Create strong, unique passwords with customizable complexity
- **Auto-Fill Integration**: Chrome extension for automatic login form completion
- **Import/Export**: Migrate from other password managers (JSON support)
- **Search & Filter**: Quickly find credentials with built-in search functionality

### Security Features  
- **Zero-Knowledge Architecture**: Only you have access to your master password
- **Secure Memory Handling**: Credentials cleared from memory after use
- **Backup & Restore**: Encrypted backup files for data protection
- **Two-Factor Authentication Ready**: Supports TOTP codes and 2FA workflows

### User Experience
- **Intuitive Interface**: Clean, modern WPF design optimized for Windows
- **Fast Performance**: Native C# application with minimal resource usage  
- **Portable Mode**: Run from USB drive without installation
- **Multiple Vaults**: Organize credentials into separate encrypted databases

## 🛠️ Technology Stack
- **Language**: C# (.NET Framework/Core)
- **UI Framework**: WPF (Windows Presentation Foundation)  
- **Cryptography**: System.Security.Cryptography, AES-256, PBKDF2
- **Database**: Encrypted file storage in user's AppData folder - no external database dependencies
- **Browser Extension**: JavaScript/Chrome Extensions API
- **Build System**: Visual Studio 2022, MSBuild

## 🚀 Quick Start Installation

### Method 1: Download Release (Recommended)
1. Visit our [Releases page](https://github.com/CyberNilsen/CyberVault/releases)
2. Download the latest
3. Run installer and follow setup wizard
4. Launch CyberVault from Start Menu

### Method 2: Build from Source
```bash
# Clone the repository
git clone https://github.com/CyberNilsen/CyberVault.git
cd CyberVault

# Open in Visual Studio 2022
# Build > Build Solution (Ctrl+Shift+B)
# Debug > Start Without Debugging (Ctrl+F5)
```

### System Requirements
- **OS**: Windows 10 version 1809 or later / Windows 11
- **Framework**: .NET Framework 4.8 or .NET 6.0 Runtime
- **Memory**: 512 MB RAM minimum (1 GB recommended)
- **Storage**: 300 MB free disk space
- **Architecture**: x64 (64-bit) systems only

## 💡 Usage Guide

### First Time Setup
1. **Launch CyberVault** from desktop shortcut or Start Menu
2. **Create Master Password**: Choose a strong, memorable master password
3. **Set Security Settings**: Configure auto-lock timeout and backup preferences
4. **Add Your First Password**: Click "Add New" to store your first credential

### Daily Workflow  
- **Quick Access**: Use Ctrl+Alt+V hotkey to open CyberVault from anywhere
- **Auto-Fill**: Install Chrome extension for seamless web form filling  
- **Search**: Type in search box to instantly find any stored password
- **Generate**: Use built-in generator for new account passwords
- **Backup**: Regular encrypted backups ensure you never lose data

## 🌐 Browser Extension
Install our **official Chrome extension** for seamless password management:
- 🔗 [Download from Chrome Web Store](https://chromewebstore.google.com/detail/cybervault-extension/apoijcgjdomcddnogcfjecfbgnnnhmdd)
- 🔗 [Extension Source Code](https://github.com/CyberNilsen/CyberVaultExtension)

**Extension Features:**
- Auto-detect login forms on websites
- One-click password filling
- Generate passwords directly in browser
- Secure communication with desktop app
- Works with all Chromium browsers

## 🔒 Security & Privacy

### Encryption Details
- **Algorithm**: AES-256 in CBC mode with PKCS7 padding
- **Key Derivation**: PBKDF2-SHA256 with 1,000,000 iterations and cryptographically secure random salt
- **Storage**: Encrypted files stored locally in `%AppData%\Roaming\CyberVault` - no database required
- **Memory Protection**: Sensitive data cleared from memory after use with secure memory handling

### Privacy Commitments
- ✅ **No Data Collection**: We never collect, store, or transmit your data
- ✅ **No Analytics**: No tracking, no telemetry, no usage statistics  
- ✅ **No Network Calls**: Application works 100% offline after installation
- ✅ **Open Source**: All code is publicly auditable on GitHub
- ✅ **Local Storage Only**: Everything stays on your computer, always

### Security Auditing
- Regular security reviews by the development team
- Automated dependency scanning with GitHub Security Advisories

## 📊 Comparisons

| Feature | CyberVault | LastPass | Bitwarden | 1Password |
|---------|------------|----------|-----------|-----------|
| **Offline Storage** | ✅ Complete | ❌ Cloud-only | ❌ Cloud-only | ❌ Cloud-only |
| **Open Source** | ✅ MIT License | ❌ Proprietary | ✅ GPL | ❌ Proprietary |
| **Windows Native** | ✅ WPF App | ❌ Web-based | ❌ Electron | ❌ Electron |
| **Cost** | ✅ Free Forever | 💰 $3/month | 💰 $10/year | 💰 $36/year |
| **Zero-Knowledge** | ✅ True Offline | ⚠️ Server-side | ⚠️ Server-side | ⚠️ Server-side |

## 🤝 Contributing

We welcome contributions from the cybersecurity and open-source community!

### Ways to Contribute
- 🐛 **Bug Reports**: Found an issue? [Open an issue](https://github.com/CyberNilsen/CyberVault/issues)
- 💡 **Feature Requests**: Suggest improvements or new features
- 🔧 **Code Contributions**: Submit pull requests for bug fixes or enhancements
- 📖 **Documentation**: Help improve our documentation and guides
- 🌐 **Translations**: Localize CyberVault for your language
- 🔐 **Security Research**: Responsible disclosure of security vulnerabilities

### Development Setup
```bash
git clone https://github.com/CyberNilsen/CyberVault.git
cd CyberVault
# Open CyberVault.sln in Visual Studio 2022
# Install NuGet packages and build
```

Please read our [Contributing Guidelines](CONTRIBUTING.md) and [Code of Conduct](CODE_OF_CONDUCT.md).

## 📜 License & Legal
This project is licensed under the [MIT License](LICENSE) - see the LICENSE file for details.

**Legal Notice**: CyberVault is provided "as-is" without warranty. Users are responsible for maintaining backups of their passwords.

## 🗞️ Media & Recognition
**Featured in Laagendalsposten**: CyberVault was highlighted in Norway Kongsberg's local news.  
🔗 [Read the full article (Norwegian)](https://www.laagendalsposten.no/andreas-og-mathias-vil-unnga-hackere-laget-losning-for-sikker-lagring/s/5-64-1548360)

## 🌐 Links & Resources
- 🌍 **Official Website**: [cybernilsen.github.io/CyberVault-website](https://cybernilsen.github.io/CyberVault-website/index.html)
- 📱 **Chrome Extension**: [CyberVault Extension Repository](https://github.com/CyberNilsen/CyberVaultExtension)
- 📧 **Support Email**: cyberbrothershq@gmail.com
- 🐛 **Bug Reports**: [GitHub Issues](https://github.com/CyberNilsen/CyberVault/issues)
- 💬 **Discussions**: [GitHub Discussions](https://github.com/CyberNilsen/CyberVault/discussions)

## 🙌 Acknowledgments

### Beta Testers & Contributors
Special thanks to our dedicated testers who helped make CyberVault secure and reliable:
- [**Matvey**](https://github.com/JahBoiMat) - Security testing and penetration testing
- [**Ådne**](https://github.com/Adnelilleskare) - UI/UX feedback and Windows compatibility testing  
- [**Fredrik**](https://github.com/JahnTeigen) - Performance optimization and feature testing

### Open Source Dependencies
- **System.Security.Cryptography** - .NET cryptographic APIs

---

## ⭐ Support the Project

If CyberVault helps keep your passwords secure, please:
- ⭐ **Star this repository** on GitHub
- 🔄 **Share** with friends and colleagues who value privacy  
- 🐛 **Report bugs** to help us improve security
- 💡 **Suggest features** for future versions
- 🤝 **Contribute code** to the open-source community

**Download CyberVault today and take control of your digital security!**

---
*Keywords: password manager, offline password manager, Windows password vault, secure credential storage, open source security, privacy-focused password manager, local password storage, encrypted password database, cybersecurity tools, password generator, Chrome extension password manager*
