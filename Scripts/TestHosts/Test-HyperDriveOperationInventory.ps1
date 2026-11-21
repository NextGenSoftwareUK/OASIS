$ErrorActionPreference = 'Stop'
$verifier = Join-Path $PSScriptRoot '../verify_hyperdrive_v2_operation_contracts.ps1'
$source = @'
        private bool Classifier() {
            switch (operation) { case "ClassifierOnly": return true; }
        }
        private Result RouteUnpreparedStorageOperation<T>(Request request) {
            switch (request.Operation) { case "Read": return Read(); }
        }
        private Result RouteUnpreparedStorageOperationAsync<T>(Request request) {
            switch (request.Operation) { case "Read": return ReadAsync(); }
        }
        private bool End() { return true; }
'@
$inventory = & $verifier -RouterText $source -InventoryOnly
if ($inventory.DirectOperations.Count -ne 1 -or $inventory.DirectOperations[0] -ne 'Read') { throw 'Classifier label was treated as a handler.' }
$failed = $false
try { & $verifier -RouterText ($source.Replace('case "Read": return ReadAsync();','case "Other": return ReadAsync();')) -InventoryOnly | Out-Null }
catch { if ($_.Exception.Message -notmatch 'only one sync/async handler') { throw }; $failed=$true }
if (!$failed) { throw 'A real handler asymmetry was accepted.' }
$failed=$false
try { & $verifier -RouterText ($source.Replace('RouteUnpreparedStorageOperationAsync<T>','RemovedHandler<T>')) -InventoryOnly | Out-Null }
catch { if ($_.Exception.Message -notmatch 'Expected one router method') { throw }; $failed=$true }
if (!$failed) { throw 'A missing handler was accepted.' }
$conditional = @'
        private Request CreateConditionalRequest(Request request) {
            if (request.Operation == "DeleteHolons") return Delete(request);
        }
        private Result RouteStorageOperation<T>(Request request) {
            if (request.ConditionalWrite != null) {
                ResolveConditionalPreparation(request);
                provider.ExecuteConditionalHolonWrite(request);
                return ConvertConditionalResult<T>(request);
            }
        }
        private Result RouteStorageOperationAsync<T>(Request request) {
            if (request.ConditionalWrite != null) {
                ResolveConditionalPreparationAsync(request);
                provider.ExecuteConditionalHolonWriteAsync(request);
                return ConvertConditionalResult<T>(request);
            }
        }
        private bool EndConditional() { return true; }
'@
$withConditional = $source + "`n" + $conditional
$inventory = & $verifier -RouterText $withConditional -InventoryOnly
if ($inventory.ConditionalOperations.Count -ne 1 -or $inventory.ConditionalOperations[0] -ne 'DeleteHolons') { throw 'Shared conditional operation was not inventoried.' }
$failed=$false
try { & $verifier -RouterText ($withConditional.Replace('provider.ExecuteConditionalHolonWriteAsync','provider.MissingConditionalCallAsync')) -InventoryOnly | Out-Null }
catch { if ($_.Exception.Message -notmatch 'Conditional dispatch.*missing') { throw }; $failed=$true }
if (!$failed) { throw 'A missing conditional async dispatch was accepted.' }
Write-Host 'Operation inventory parser passed classifier exclusion, asymmetry, missing-handler, conditional dispatch and conditional asymmetry checks. No provider responses were mocked.'
