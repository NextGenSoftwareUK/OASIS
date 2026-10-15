param([switch]$Apply, [string]$ResumeEndpointId)
$ErrorActionPreference = 'Stop'
trap { Write-Error ('Staging Stripe setup failed: ' + $_.Exception.GetType().Name + '. Secret values are withheld; reconcile endpoint/settings before retrying.') -ErrorAction Continue; exit 1 }
$url = 'https://staging.api.web4.oasisomniverse.one/api/subscription/webhooks/stripe'
$devUrl = 'https://dev.api.web4.oasisomniverse.one/api/subscription/webhooks/stripe'
$events = @('checkout.session.completed','customer.subscription.created','customer.subscription.updated','customer.subscription.deleted','invoice.paid','invoice.payment_succeeded','invoice.payment_failed')
$dev = railway variable list -s 'WEB4 OASIS API' -e dev --json | ConvertFrom-Json
if ($LASTEXITCODE -ne 0 -or -not ([string]$dev.STRIPE_SECRET_KEY).StartsWith('sk_test_') -or -not ([string]$dev.STRIPE_PUBLISHABLE_KEY).StartsWith('pk_test_')) { throw 'Verified existing test-mode credentials are required' }
$headers = @{Authorization="Bearer $($dev.STRIPE_SECRET_KEY)"}
$endpoints = Invoke-RestMethod 'https://api.stripe.com/v1/webhook_endpoints?limit=100' -Headers $headers
if ($endpoints.has_more) { throw 'Endpoint inventory is incomplete; do not create a duplicate' }
$existing = @($endpoints.data | Where-Object url -eq $url)
if ($existing.Count) {
    if ($existing.Count -ne 1 -or $existing[0].id -ne $ResumeEndpointId -or $existing[0].livemode -or
        $existing[0].status -ne 'disabled' -or
        [DateTimeOffset]::UtcNow.ToUnixTimeSeconds() - $existing[0].created -gt 23*3600) {
        throw 'Existing endpoint cannot be reconciled safely with the retained creation idempotency key'
    }
} elseif ($ResumeEndpointId) { throw 'Requested reconciliation endpoint does not exist' }
$reference = @($endpoints.data | Where-Object { $_.url -eq $devUrl -and $_.status -eq 'enabled' -and -not $_.livemode })
if ($reference.Count -ne 1 -or -not $reference[0].api_version) { throw 'Expected one verified working dev test webhook API contract' }
"Verified test credentials and webhook contract; explicit idempotent reconciliation=$([bool]$ResumeEndpointId); apply=$Apply"
if (-not $Apply) { exit 0 }

# Stripe idempotency prevents duplicate creation after an ambiguous transport result.
# Never print the response: signing secrets are returned only at creation.
$body = @{url=$url;api_version=$reference[0].api_version;description='OASIS isolated staging subscription gate (test mode, disabled until accepted rollout)'}
for ($i=0; $i -lt $events.Count; $i++) { $body["enabled_events[$i]"] = $events[$i] }
$createHeaders = $headers.Clone()
$createHeaders['Idempotency-Key'] = 'oasis-staging-subscription-gate-v1'
$endpoint = Invoke-RestMethod -Method Post 'https://api.stripe.com/v1/webhook_endpoints' -Headers $createHeaders -Body $body
if ($endpoint.livemode -or -not $endpoint.secret -or $endpoint.url -ne $url) { throw 'Unexpected test endpoint identity' }
if ($ResumeEndpointId -and $endpoint.id -ne $ResumeEndpointId) { throw 'Idempotent replay returned a different endpoint' }
$disabled = Invoke-RestMethod -Method Post "https://api.stripe.com/v1/webhook_endpoints/$($endpoint.id)" -Headers $headers -Body @{disabled='true'}
if ($disabled.livemode -or $disabled.status -ne 'disabled') { throw 'Test endpoint must remain disabled until isolated staging is active' }

$snapshot = railway status --json | ConvertFrom-Json
if ($LASTEXITCODE -ne 0 -or $snapshot.id -ne '712f50f6-3b7b-46d4-b229-44b49922f16b') { throw 'Wrong Railway project' }
$services = @((($snapshot.environments.edges.node | Where-Object name -eq staging).serviceInstances.edges.node) | Where-Object serviceName -Match '^WEB(4|5|6|7|8|9|10) ')
if ($services.Count -ne 7) { throw 'Expected seven staging services' }
function Read-Variables($service,$environment) {
    $values = railway variable list -s $service -e $environment --json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0) { throw 'Configuration read failed' }
    return $values
}
function Set-Stage($service,$name,$value) {
    $value | railway variable set $name --stdin --skip-deploys -s $service -e staging | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Staging configuration write failed' }
}
$devDna = $dev.OASIS_DNA_JSON | ConvertFrom-Json
foreach ($service in $services) {
    $before = Read-Variables $service.serviceName production
    $values = Read-Variables $service.serviceName staging
    $dna = $values.OASIS_DNA_JSON | ConvertFrom-Json
    $stripe = $dna.OASIS.SubscriptionConfig.Stripe
    if (-not $stripe) { throw 'Expected canonical subscription Stripe configuration' }
    $stripe.SecretKey = $dev.STRIPE_SECRET_KEY
    $stripe.WebhookSecret = $endpoint.secret
    foreach ($name in @('PriceBronze','PriceSilver','PriceGold','PriceEnterprise')) {
        $stripe.$name = $devDna.OASIS.SubscriptionConfig.Stripe.$name
    }
    Set-Stage $service.serviceName 'OASIS_DNA_JSON' ($dna | ConvertTo-Json -Depth 100 -Compress)
    if ($service.serviceName -eq 'WEB4 OASIS API' -or 'STRIPE_SECRET_KEY' -in $values.PSObject.Properties.Name) {
        Set-Stage $service.serviceName 'STRIPE_SECRET_KEY' $dev.STRIPE_SECRET_KEY
        Set-Stage $service.serviceName 'STRIPE_PUBLISHABLE_KEY' $dev.STRIPE_PUBLISHABLE_KEY
        Set-Stage $service.serviceName 'STRIPE_WEBHOOK_SECRET' $endpoint.secret
    }
    $readback = Read-Variables $service.serviceName staging
    $readbackStripe = ($readback.OASIS_DNA_JSON | ConvertFrom-Json).OASIS.SubscriptionConfig.Stripe
    if (-not ([string]$readbackStripe.SecretKey).StartsWith('sk_test_') -or $readbackStripe.WebhookSecret -cne $endpoint.secret) { throw 'Staging test configuration readback mismatch' }
    $after = Read-Variables $service.serviceName production
    foreach ($key in @('OASIS_DNA_JSON','STRIPE_SECRET_KEY','STRIPE_PUBLISHABLE_KEY','STRIPE_WEBHOOK_SECRET')) {
        if ($before.$key -cne $after.$key) { throw 'Production configuration changed concurrently; investigate without overwriting' }
    }
    "$($service.serviceName): test-mode configuration verified, production unchanged, deployment not triggered"
}
"Staging test webhook $($endpoint.id) created, disabled, API version $($endpoint.api_version); signing secret securely stored and withheld"
