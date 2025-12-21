# Documentation Verification Guide

**Purpose:** Ensure documentation accuracy through systematic verification methods

---

## Verification Methods

### 1. Automated Code Verification

#### A. Provider Status Verification

**Check:** Provider enum vs. actual implementations

**Script:**
```bash
# Verify all providers in enum have corresponding directories
# This should be run from the project root

# Extract provider names from enum
grep -o "ProviderType\.[A-Za-z]*OASIS" OASIS\ Architecture/NextGenSoftware.OASIS.API.Core/Enums/ProviderType.cs | \
  sed 's/ProviderType\.//' | \
  sort > /tmp/enum_providers.txt

# Find actual provider directories
find Providers -type d -name "*OASIS" | \
  sed 's/.*NextGenSoftware.OASIS.API.Providers.//' | \
  sed 's|/.*||' | \
  sort > /tmp/actual_providers.txt

# Compare
diff /tmp/enum_providers.txt /tmp/actual_providers.txt
```

**Expected Result:** List of providers in enum but not implemented, and vice versa

---

#### B. API Endpoint Verification

**Check:** API documentation vs. actual controller methods

**Manual Process:**
1. List all controllers in `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers/`
2. Extract HTTP method attributes (HttpGet, HttpPost, etc.)
3. Compare with documented endpoints in `Docs/Devs/API Documentation/`

**Automated Check (PowerShell/.NET):**
```powershell
# Get all controller methods
Get-ChildItem -Path "ONODE\*.WebAPI\Controllers\*.cs" | 
  Select-String -Pattern "\[HttpGet|\[HttpPost|\[HttpPut|\[HttpDelete" |
  Measure-Object | 
  Select-Object -ExpandProperty Count
```

---

#### C. HyperDrive Feature Verification

**Check:** Verify HyperDrive methods exist in code

**Script:**
```bash
# Check for key HyperDrive methods
grep -r "public.*FailoverRequestAsync\|ReplicateRequestAsync\|LoadBalanceRequestAsync" \
  "OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/OASIS HyperDrive/"
```

**Expected:** Should find all three methods documented

---

#### D. Manager Verification

**Check:** Verify manager classes exist and have documented methods

**Script:**
```bash
# List all manager classes
find "OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers" \
  -name "*Manager.cs" \
  -not -path "*/OASIS HyperDrive/*" \
  -exec basename {} .cs \;
```

**Compare with:** `Docs/Devs/OASIS-Managers-Complete-Guide.md`

---

### 2. Configuration Verification

#### A. OASIS_DNA.json Structure

**Check:** Verify documented configuration keys exist in actual DNA file

**Script:**
```bash
# Extract all provider config keys from OASIS_DNA.json
jq '.OASIS.StorageProviders | keys[]' OASIS_DNA.json | sort

# Compare with documented providers
```

**Expected:** All documented providers should have config keys (or null if not configured)

---

#### B. HyperDrive Configuration

**Check:** Verify HyperDrive config structure matches documentation

```bash
# Check if HyperDrive config exists in DNA
jq '.OASIS.OASISHyperDriveConfig' OASIS_DNA.json

# Verify documented config keys exist
jq '.OASIS.OASISHyperDriveConfig | keys[]' OASIS_DNA.json
```

---

### 3. Code Example Verification

#### A. Test Code Examples

Create unit tests that verify code examples in documentation actually work:

```csharp
[Test]
public void TestProviderActivationExample()
{
    // From docs/concepts/PROVIDERS.md
    var provider = new MongoDBOASIS();
    var result = provider.ActivateProviderAsync().Result;
    
    Assert.IsTrue(result.Result);
    Assert.IsTrue(provider.IsProviderActivated);
}
```

---

#### B. API Example Verification

Test API endpoint examples from documentation:

```bash
# Test quickstart example
curl -X POST "https://api.oasisweb4.com/api/avatar/register" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "test_user",
    "email": "test@example.com",
    "password": "test123",
    "firstName": "Test",
    "lastName": "User",
    "avatarType": "User",
    "acceptTerms": true
  }'
```

**Expected:** Should return success response matching documented format

---

### 4. Cross-Reference Verification

#### A. Internal Link Checking

**Check:** All internal documentation links are valid

**Script:**
```bash
# Extract all markdown links from docs
grep -r "\[.*\](.*\.md)" docs/ | \
  sed 's/.*\[.*\](\(.*\))/\1/' | \
  sed 's/#.*//' | \
  sort -u > /tmp/doc_links.txt

# Verify each link exists
while read link; do
  if [ ! -f "$link" ]; then
    echo "BROKEN: $link"
  fi
done < /tmp/doc_links.txt
```

---

#### B. Source Code Reference Verification

**Check:** All file paths referenced in docs actually exist

**Script:**
```bash
# Extract code file references from docs
grep -r "OASIS Architecture\|Providers\|ONODE" docs/ | \
  grep "\.cs\|\.json" | \
  sed 's/.*`\([^`]*\)`.*/\1/' | \
  sed 's/^[^/]*//' | \
  sort -u | \
  while read file; do
    if [ ! -f "$file" ]; then
      echo "MISSING: $file"
    fi
  done
```

---

### 5. Semantic Verification

#### A. Concept Accuracy

**Manual Review Checklist:**

For each concept document, verify:
- [ ] Definitions match actual code interfaces
- [ ] Examples use correct method signatures
- [ ] Described behavior matches implementation
- [ ] No contradictory information

**Example for Providers:**
```bash
# Verify IOASISProvider interface matches documentation
grep -A 20 "interface IOASISProvider" \
  "OASIS Architecture/NextGenSoftware.OASIS.API.Core/Interfaces/Providers/IOASISProvider.cs"

# Compare with docs/concepts/PROVIDERS.md
```

---

#### B. Architecture Accuracy

**Check:** Architecture descriptions match actual code structure

**Process:**
1. Review architecture diagrams/documentation
2. Trace actual code flow for a simple operation (e.g., save holon)
3. Verify documented flow matches actual flow

**Example Flow Check:**
```
Documented: Client → Manager → HyperDrive → Provider
Actual: Check code in HolonManager.SaveHolonAsync()
```

---

### 6. Version Consistency

#### A. Date Verification

**Check:** All "Last Updated" dates are reasonable

**Script:**
```bash
# Find all "Last Updated" dates
grep -r "Last Updated:" docs/ | \
  sed 's/.*Last Updated: //' | \
  sort -u
```

**Expected:** Dates should be current or recent (not future dates)

---

#### B. Version Number Consistency

**Check:** Documentation references correct versions

**Manual:** Review version references in docs against:
- Git tags
- Release notes
- Package versions

---

## Verification Checklist

### Before Publishing Documentation

- [ ] Run provider status verification script
- [ ] Verify all code file paths exist
- [ ] Test at least one code example
- [ ] Check all internal links work
- [ ] Verify configuration examples match actual config
- [ ] Review for outdated information
- [ ] Check dates are current
- [ ] Verify method signatures match code

### Quarterly Review

- [ ] Full provider status audit
- [ ] API endpoint comparison
- [ ] Test all code examples
- [ ] Update outdated information
- [ ] Verify new features are documented
- [ ] Check for deprecated features
- [ ] Review accuracy of examples

---

## Automated Verification Script

Create a verification script (`docs/verify.sh` or `docs/verify.ps1`):

```bash
#!/bin/bash
# Documentation Verification Script

echo "=== OASIS Documentation Verification ==="
echo ""

# 1. Check provider enum vs implementations
echo "1. Checking Provider Status..."
# ... provider check code ...

# 2. Verify code file references
echo "2. Checking Code File References..."
# ... file check code ...

# 3. Check internal links
echo "3. Checking Internal Links..."
# ... link check code ...

# 4. Verify dates
echo "4. Checking Dates..."
# ... date check code ...

echo ""
echo "=== Verification Complete ==="
```

---

## Continuous Integration

### GitHub Actions / CI Pipeline

Add documentation verification to CI:

```yaml
name: Documentation Verification

on:
  pull_request:
    paths:
      - 'docs/**'
      - 'OASIS Architecture/**'
      - 'Providers/**'

jobs:
  verify:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      
      - name: Verify Provider Status
        run: |
          # Run provider verification script
          bash docs/scripts/verify_providers.sh
      
      - name: Check Documentation Links
        run: |
          # Check markdown links
          npx markdown-link-check docs/**/*.md
      
      - name: Verify Code References
        run: |
          # Verify file paths exist
          bash docs/scripts/verify_code_refs.sh
```

---

## Manual Verification Process

### Step-by-Step Manual Check

1. **Pick a Documentation Section**
   - Example: "Providers Concept"

2. **Identify Claims**
   - "60 provider types defined"
   - "Provider interface has X methods"

3. **Verify Against Code**
   - Count providers in enum: ✓ 60
   - Check interface: ✓ Methods match

4. **Test Examples**
   - Run code examples
   - Verify they work

5. **Check Cross-References**
   - Follow links
   - Verify they point to correct sections

6. **Document Findings**
   - Note any discrepancies
   - Update documentation if needed

---

## Reporting Issues

### When Finding Inaccuracies

1. **Document the Issue**
   - What documentation says
   - What code actually does
   - File path and line numbers

2. **Verify the Discrepancy**
   - Double-check code
   - Confirm it's not a misunderstanding

3. **Fix or Flag**
   - Fix if documentation is wrong
   - Flag if code changed and docs need update

---

## Tools for Verification

### Recommended Tools

1. **Markdown Link Checker**
   - `markdown-link-check` (npm)
   - Checks all links in markdown files

2. **Code Search**
   - `grep` / `ripgrep`
   - Find references in code

3. **JSON Validator**
   - `jq` for OASIS_DNA.json validation
   - Verify structure

4. **AST Parsers**
   - For C#: Roslyn analyzers
   - Extract method signatures programmatically

5. **Documentation Generators**
   - DocFX for C# (can verify code references)
   - Sphinx for Python

---

## Best Practices

### For Maintainers

1. **Update Docs with Code**
   - When changing code, update docs
   - Include in PR checklist

2. **Use Version Control**
   - Track documentation changes
   - Link docs changes to code changes

3. **Automate Where Possible**
   - CI checks catch many issues
   - Regular automated audits

4. **Document Assumptions**
   - Note when documentation is aspirational
   - Clearly mark "planned" vs "implemented"

### For Contributors

1. **Verify Before Submitting**
   - Run verification scripts
   - Test code examples

2. **Include Verification**
   - Show how you verified accuracy
   - Include test results

---

## Quick Verification Commands

### Common Checks

```bash
# Count providers in enum
grep -c "OASIS" "OASIS Architecture/.../ProviderType.cs"

# List all manager classes
find "OASIS Architecture/.../Managers" -name "*Manager.cs" -exec basename {} \;

# Check HyperDrive methods exist
grep -l "FailoverRequestAsync\|ReplicateRequestAsync" "OASIS Architecture/.../OASISHyperDrive.cs"

# Verify DNA config structure
jq '.OASIS.StorageProviders | keys' OASIS_DNA.json

# Find broken markdown links
find docs -name "*.md" -exec markdown-link-check {} \;
```

---

## Related Documentation

- [Documentation Progress](./DOCUMENTATION_PROGRESS.md) - Current documentation status
- [Documentation Analysis Report](./DOCUMENTATION_ANALYSIS_REPORT.md) - Initial analysis
- [Provider Status Reference](./reference/PROVIDERS/STATUS.md) - Provider verification results

---

**Last Updated:** December 2025

