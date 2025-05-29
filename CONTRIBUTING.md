# Contributing to CyberVault

Thank you for your interest in contributing to CyberVault! We welcome contributions from the community to help make this password manager even better and more secure.

## 🤝 How to Contribute

### Reporting Bugs
If you find a bug, please create an issue with the following information:
- A clear and descriptive title
- Steps to reproduce the issue
- Expected behavior vs actual behavior
- Your operating system and .NET version
- Screenshots if applicable

### Suggesting Features
We love new ideas! When suggesting a feature:
- Check if the feature has already been requested
- Clearly describe the feature and its benefits
- Explain how it fits with CyberVault's security-first approach
- Consider the impact on local-only storage principle

### Security Issues
**⚠️ IMPORTANT:** For security vulnerabilities, please follow our [Security Policy](SECURITY.md) and do NOT create a public issue. Contact us privately first.

## 🛠️ Development Setup

### Prerequisites
- Visual Studio 2019 or later (Community edition is fine)
- .NET Framework or .NET Core (check project file for specific version)
- Git for version control

### Getting Started
1. Fork the repository
2. Clone your fork:
   ```bash
   git clone https://github.com/YOUR-USERNAME/CyberVault.git
   ```
3. Open `CyberVault.sln` in Visual Studio
4. Build the solution to ensure everything works
5. Create a new branch for your feature:
   ```bash
   git checkout -b feature/your-feature-name
   ```

## 📝 Coding Standards

### Code Style
- Follow standard C# naming conventions
- Use meaningful variable and method names
- Add XML documentation comments for public methods
- Keep methods focused and reasonably sized

### Security Guidelines
Since CyberVault handles sensitive data, please ensure:
- **Never log passwords or sensitive data**
- Use secure coding practices for cryptographic operations
- Validate all user inputs
- Follow the principle of least privilege
- Test cryptographic functions thoroughly

### Testing
- Write unit tests for new functionality
- Ensure existing tests still pass
- Test edge cases and error conditions
- Manual testing on different Windows versions is appreciated

## 🔄 Pull Request Process

1. **Before submitting:**
   - Ensure your code builds without errors
   - Run existing tests
   - Update documentation if needed
   - Test your changes thoroughly

2. **Pull Request Guidelines:**
   - Use a clear, descriptive title
   - Reference any related issues
   - Describe what your changes do and why
   - Include screenshots for UI changes
   - Keep PRs focused on a single feature/fix

3. **Review Process:**
   - CyberHansen or CyberNilsen will review your PR
   - Address any feedback promptly
   - Be patient - security reviews take time
   - Once approved, we'll merge your contribution

## 🎯 Priority Areas

We're especially interested in contributions for:
- **Security improvements** and code audits
- **UI/UX enhancements** for better usability
- **Performance optimizations**
- **Cross-platform compatibility** improvements
- **Documentation** and code comments
- **Testing** and quality assurance

## 💡 Project Philosophy

When contributing, please keep in mind CyberVault's core principles:
- **Local-only storage** - no cloud dependencies
- **Security first** - every change should maintain or improve security
- **Simplicity** - keep the UI clean and intuitive
- **Open source** - transparent and auditable code

## 🚫 What We Don't Accept

- Features that require internet connectivity for core functionality
- Changes that weaken security or encryption
- Code that introduces unnecessary dependencies
- Contributions that don't follow our coding standards

## 🌟 Recognition

Contributors will be:
- Added to our README acknowledgments
- Credited in release notes for significant contributions

## 📞 Getting Help

- Open a discussion for questions about the codebase
- Tag @CyberHansen or @CyberNilsen for urgent matters
- You can also reach us at our mail
- Check existing issues and discussions first

## 📜 Code of Conduct

By participating in this project, you agree to abide by our code of conduct. Be respectful, constructive, and help us maintain a welcoming community.

---

**Thank you for contributing to CyberVault and helping keep everyone's passwords secure! 🔒**
