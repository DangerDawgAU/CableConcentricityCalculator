# Security Policy

Security considerations and vulnerability reporting procedures for Cable Concentricity Calculator.

## Supported Versions

| Version | Supported |
|---------|-----------|
| 1.0.x   | Yes       |
| < 1.0   | No        |

## Security Considerations

### Application Scope
This is a desktop engineering tool designed for local use in trusted environments. It processes cable assembly specifications and generates technical documentation. The application:

- Does not connect to external networks or services
- Does not handle sensitive user credentials
- Does not process personally identifiable information
- Does not implement authentication or authorisation mechanisms
- Operates entirely on local filesystem

### File Operations
The application reads and writes files in the following locations:

**Read Operations:**
- `Libraries/*.json` - Component database files
- User-specified assembly configuration files (`.json`)
- Project configuration files

**Write Operations:**
- `output/` directory - Generated reports, images, and STL files
- User-specified save locations for assembly configurations

**Security Notes:**
- File paths are not sanitised for untrusted input
- Application assumes operation in trusted filesystem environment
- No validation of file path traversal attacks
- Designed for use with trusted input files only

### JSON Deserialization
The application deserialises JSON from:
- Component library files (`CableLibrary.json`, `HeatShrinkLibrary.json`, `OverBraidLibrary.json`)
- Assembly configuration files (user-created `.json` files)

**Risks:**
- Malformed JSON may cause parsing exceptions
- Large JSON files may cause memory exhaustion
- No validation against malicious payloads in JSON content

**Mitigations:**
- Uses System.Text.Json with standard deserialisation settings
- Application designed for trusted input only
- Operates in isolated desktop environment

### PDF Generation
Uses QuestPDF library (version 2024.10.2) for PDF generation.

**Security Notes:**
- QuestPDF Community Licence applied
- No user-supplied code execution in PDF generation
- Generated PDFs contain only application-generated content
- No external resource embedding from untrusted sources

### Third-Party Dependencies

**Core Dependencies:**
- QuestPDF 2024.10.2
- SkiaSharp 2.88.8
- System.Text.Json 9.0.0
- Spectre.Console 0.49.1

**GUI Dependencies:**
- Avalonia 11.2.1
- CommunityToolkit.Mvvm 8.3.2

**Dependency Management:**
- Review dependency security advisories regularly
- Update dependencies when security patches are released
- Monitor NuGet package vulnerability reports

### Code Execution
The application:
- Does not execute user-supplied code
- Does not evaluate expressions from configuration files
- Does not load external assemblies dynamically
- Does not implement plugin or extension mechanisms

### Data Validation
Limited input validation is performed on:
- Numeric values (diameters, thicknesses, etc.)
- Enum values (cable types, twist directions)
- JSON structure compliance

**Not Validated:**
- File path traversal
- Resource exhaustion (memory, disk space)
- Malicious JSON content beyond structure validation

## Vulnerability Reporting

### Reporting Process
If you discover a security vulnerability:

1. **Do Not** open a public issue
2. Contact project maintainers directly via secure channel
3. Provide detailed description including:
   - Vulnerability type and impact
   - Steps to reproduce
   - Affected versions
   - Suggested mitigation if known

### Response Timeline
- **Initial Response:** Within 5 business days
- **Assessment:** Within 10 business days
- **Fix Development:** Timeline depends on severity and complexity
- **Disclosure:** Coordinated disclosure after fix is available

### Severity Classification

**Critical:**
- Remote code execution
- Privilege escalation
- Data exfiltration to external systems

**High:**
- Local code execution via malicious input files
- Denial of service via resource exhaustion
- Arbitrary file read/write outside intended directories

**Medium:**
- Application crash via malformed input
- Information disclosure from error messages
- Dependency vulnerabilities with available patches

**Low:**
- Minor information leaks
- Non-exploitable edge cases
- Theoretical vulnerabilities requiring significant preconditions

## Security Best Practices

### For Users

**File Handling:**
- Only open assembly configuration files from trusted sources
- Review JSON files before loading if received from external parties
- Maintain backups of critical assembly configurations

**Environment:**
- Run application with standard user privileges (not administrator/root)
- Ensure output directory has appropriate filesystem permissions
- Regularly update .NET runtime to latest stable version

**Library Management:**
- Verify integrity of component library JSON files
- Restrict write access to `Libraries/` directory in production
- Validate custom component definitions before adding to libraries

### For Developers

**Code Changes:**
- Validate all user input at application boundaries
- Sanitise file paths when handling user-specified locations
- Implement resource limits for memory-intensive operations
- Review dependency updates for security advisories

**Build Process:**
- Use official .NET SDK from Microsoft
- Verify NuGet package signatures
- Scan dependencies for known vulnerabilities
- Build in clean, isolated environment

**Deployment:**
- Sign executables with valid code signing certificate
- Provide checksums (SHA-256) for distributed binaries
- Document third-party dependency versions and licences
- Include software bill of materials (SBOM) with releases

## Known Limitations

### By Design
The following are known limitations accepted by design scope:

1. **No Authentication:** Application does not implement user authentication
2. **No Encryption:** Files are stored unencrypted on local filesystem
3. **No Network Security:** Application does not communicate over network
4. **No Sandboxing:** File operations are not sandboxed or restricted
5. **No Input Sanitisation:** File paths are not validated for traversal attacks
6. **No Resource Limits:** No hard limits on memory or CPU usage

These limitations are acceptable because:
- Application is designed for trusted, local desktop use
- Target users are engineering professionals in controlled environments
- No handling of sensitive or regulated data
- No multi-user or network deployment scenarios

### Not Security Vulnerabilities
The following are not considered security vulnerabilities:

- Application crash from malformed JSON files
- Resource exhaustion from extremely large assemblies
- File system errors from invalid paths
- Exceptions from missing dependencies
- Performance degradation with complex assemblies

## Compliance

### Data Protection
- No personally identifiable information (PII) is collected or processed
- No telemetry or analytics data is transmitted
- All data remains on local filesystem
- No compliance requirements for GDPR, HIPAA, or similar regulations

### Export Control
- Application is engineering calculation software
- No encryption components beyond standard .NET libraries
- No military or dual-use technology restrictions known
- Users are responsible for compliance with local export regulations

## Security Updates

Security updates will be released as:
- Patch versions (1.0.x) for critical and high severity issues
- Minor versions (1.x.0) for medium severity issues
- Major versions (x.0.0) may include breaking changes for security improvements

Users should:
- Monitor repository for security announcements
- Subscribe to release notifications
- Update to latest stable version regularly
- Review changelog for security-related fixes

## Contact

For security-related enquiries:
- Review existing security documentation first
- Contact project maintainers through established channels
- Use encrypted communication for sensitive vulnerability details
- Allow reasonable time for assessment and response

## Disclaimer

This software is provided "as is" without warranty of any kind. See LICENCE file for complete terms.

Users are responsible for:
- Assessing suitability for their specific use case
- Implementing additional security controls as needed
- Compliance with applicable laws and regulations
- Verifying output accuracy and integrity

The maintainers make no guarantees regarding:
- Security of third-party dependencies
- Suitability for safety-critical applications
- Fitness for specific regulated environments
- Absence of undiscovered vulnerabilities
