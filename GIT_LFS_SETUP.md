# Git LFS Setup

This repository uses Git Large File Storage (LFS) to efficiently handle large library files.

## What is Being Tracked with LFS

The following files are tracked using Git LFS:

- `CableConcentricityCalculator/Libraries/*.json` - Cable, heat shrink, and braid library files (~134 MB total)
- `Datasheets/*.json` - Cable datasheet files
- `publish/**/*.exe` - Windows release executables (~56 MB each)
- `publish/**/*.dll` - Native library DLLs
- `publish/**/CableConcentricityCalculator.Gui` - macOS/Linux release executables (~42-50 MB each)

## Setup for Contributors

If you're cloning this repository for the first time:

### 1. Install Git LFS

#### Windows
```bash
# Using Chocolatey
choco install git-lfs

# Or download from https://git-lfs.github.com/
```

#### macOS
```bash
brew install git-lfs
```

#### Linux (Debian/Ubuntu)
```bash
sudo apt-get install git-lfs
```

### 2. Initialize Git LFS
```bash
git lfs install
```

### 3. Clone the Repository
```bash
git clone <repository-url>
cd CableConcentricityCalculator
```

Git LFS will automatically download the large files during clone.

## Verifying LFS Setup

Check if LFS is working correctly:

```bash
# List files tracked by LFS
git lfs ls-files

# Check LFS status
git lfs status
```

## For Existing Clones

If you cloned the repository before LFS was set up:

```bash
# Install Git LFS
git lfs install

# Fetch LFS files
git lfs pull
```

## File Organization

### Tracked by Git LFS (Large Files)
- ✅ `CableConcentricityCalculator/Libraries/*.json` (134 MB)
- ✅ `Datasheets/*.json`
- ✅ `publish/**/*.exe` (Windows executables)
- ✅ `publish/**/*.dll` (Native libraries)
- ✅ `publish/**/CableConcentricityCalculator.Gui` (macOS/Linux executables)

### Tracked by Git (Regular Files)
- ✅ Source code (*.cs, *.csproj, *.sln)
- ✅ UI files (*.axaml)
- ✅ Documentation (*.md)
- ✅ Build scripts (build.py)
- ✅ Configuration files (.gitignore, .gitattributes)

### Ignored by Git
- ❌ Build output (bin/, obj/)
- ❌ IDE files (.vs/, .idea/, .vscode/)
- ❌ Generated files (*.pdf, *.png)
- ❌ Python bytecode (*.pyc)

### Special Cases
- ✅ `publish/` directory is tracked (was previously ignored)
- ✅ `build.py` is tracked (other *.py files are ignored)

## Git Attributes

The `.gitattributes` file ensures consistent behavior across platforms:

- **Text files**: Normalized to LF line endings
- **Windows scripts**: Use CRLF line endings (*.ps1, *.bat, *.cmd)
- **Binary files**: Marked as binary (*.png, *.jpg, *.pdf, *.dll, *.exe)
- **LFS files**: Large JSON files tracked with Git LFS

## Troubleshooting

### LFS files not downloading
```bash
git lfs pull
```

### Large repository size
If your `.git` folder is very large, the LFS files might not be using LFS:
```bash
# Check what's using space
git lfs migrate info --everything

# Migrate existing files to LFS (if needed)
git lfs migrate import --include="*.json" --everything
```

### Bandwidth/Storage Limits
Git LFS uses your Git provider's LFS storage. Check your provider's limits:
- **GitHub**: 1 GB storage, 1 GB bandwidth per month (free tier)
- **GitLab**: 10 GB storage per project
- **Bitbucket**: 1 GB storage, 1 GB bandwidth per month (free tier)

## Best Practices

1. **Don't commit generated files**: Build outputs should not be committed
2. **Use LFS for large binaries**: Files over 1 MB should generally use LFS
3. **Update .gitattributes**: When adding new large file types, update .gitattributes
4. **Test locally**: Always test LFS changes locally before pushing

## Commands Reference

```bash
# Install LFS hooks
git lfs install

# Track a file pattern
git lfs track "*.json"

# List tracked patterns
git lfs track

# List LFS files in repository
git lfs ls-files

# Fetch LFS files
git lfs fetch

# Pull LFS files for current branch
git lfs pull

# Get LFS status
git lfs status

# See LFS file info
git lfs migrate info
```

## CI/CD Integration

Most CI/CD systems support Git LFS automatically:

### GitHub Actions
```yaml
- name: Checkout with LFS
  uses: actions/checkout@v3
  with:
    lfs: true
```

### GitLab CI
```yaml
variables:
  GIT_LFS_SKIP_SMUDGE: "1"

before_script:
  - git lfs pull
```

## More Information

- [Git LFS Documentation](https://git-lfs.github.com/)
- [GitHub LFS Guide](https://docs.github.com/en/repositories/working-with-files/managing-large-files)
