# Publishing Scripts for @longhl104/pawfect-match-ng

This directory contains scripts to publish the Angular library `@longhl104/pawfect-match-ng` to npm.

## Scripts Available

### 1. `publish-pawfect-match-ng.sh` (Full Featured)

A comprehensive publishing script with all safety checks and options.

**Features:**
- ✅ Prerequisites checking (directory, npm auth, git status)
- ✅ Dependency installation
- ✅ Optional test running
- ✅ Optional lint checking
- ✅ Library building
- ✅ Version bumping (patch/minor/major/custom)
- ✅ Package validation
- ✅ Dry run capability
- ✅ Interactive confirmations

**Usage:**
```bash
# Full publish with all checks
./scripts/publish-pawfect-match-ng.sh

# Skip tests and lint
./scripts/publish-pawfect-match-ng.sh --skip-tests --skip-lint

# Dry run only (no actual publish)
./scripts/publish-pawfect-match-ng.sh --dry-run

# Skip build (use existing dist)
./scripts/publish-pawfect-match-ng.sh --skip-build

# Show help
./scripts/publish-pawfect-match-ng.sh --help
```

### 2. `quick-publish-pawfect-match-ng.sh` (Simplified)

A minimal script for quick publishing when you're confident everything is ready.

**Features:**
- 🚀 Build library
- 📦 Publish to npm
- ⚡ Fast execution

**Usage:**
```bash
./scripts/quick-publish-pawfect-match-ng.sh
```

## Prerequisites

Before running either script, ensure:

1. **Node.js and npm** are installed
2. **Angular CLI** is installed globally: `npm install -g @angular/cli`
3. **npm authentication**: Run `npm login` to authenticate with npm
4. **Permissions**: You have publish permissions for the `@longhl104` scope

## First Time Setup

1. Make sure you're logged into npm:
   ```bash
   npm login
   ```

2. Verify your npm user:
   ```bash
   npm whoami
   ```

3. Check if you have access to the scope:
   ```bash
   npm access list packages @longhl104
   ```

## Version Management

The full script (`publish-pawfect-match-ng.sh`) provides interactive version bumping:

- **Patch** (1.0.0 → 1.0.1): Bug fixes
- **Minor** (1.0.0 → 1.1.0): New features (backward compatible)
- **Major** (1.0.0 → 2.0.0): Breaking changes
- **Custom**: Specify your own version
- **Keep current**: Don't change version

## Publishing Process

### Using the Full Script (Recommended)

1. Navigate to the project root
2. Run the script:
   ```bash
   ./scripts/publish-pawfect-match-ng.sh
   ```
3. Follow the interactive prompts
4. Review the dry run output
5. Confirm publication

### Using the Quick Script

1. Navigate to the project root
2. Run the script:
   ```bash
   ./scripts/quick-publish-pawfect-match-ng.sh
   ```

## Manual Steps (Alternative)

If you prefer manual control:

```bash
# Navigate to workspace
cd Shared/pawfect-match-angular

# Install dependencies
npm install

# Build the library
ng build pawfect-match-ng --configuration production

# Navigate to dist
cd dist/longhl104/pawfect-match-ng

# Publish
npm publish --access public
```

## Troubleshooting

### Common Issues

1. **Authentication Error**
   ```
   npm ERR! 401 Unauthorized
   ```
   Solution: Run `npm login` and authenticate

2. **Permission Denied**
   ```
   npm ERR! 403 Forbidden
   ```
   Solution: Ensure you have publish permissions for the `@longhl104` scope

3. **Version Already Exists**
   ```
   npm ERR! 403 You cannot publish over the previously published versions
   ```
   Solution: Bump the version number

4. **Build Errors**
   - Check TypeScript compilation errors
   - Ensure all dependencies are installed
   - Verify Angular version compatibility

### Debug Mode

For debugging, you can run commands manually:

```bash
# Check npm configuration
npm config list

# Check package details
npm view @longhl104/pawfect-match-ng

# Test build locally
ng build pawfect-match-ng --configuration production

# Dry run publish
cd dist/longhl104/pawfect-match-ng
npm publish --dry-run
```

## Post-Publication

After successful publication:

1. **Verify the package**: Visit https://www.npmjs.com/package/@longhl104/pawfect-match-ng
2. **Test installation**: Try installing in a test project
3. **Update documentation**: Update any consuming applications
4. **Tag the release**: Consider creating a git tag for the version

## Security Notes

- Never commit npm authentication tokens
- Use `--access public` for public packages
- Review package contents before publishing
- Consider using npm's two-factor authentication
