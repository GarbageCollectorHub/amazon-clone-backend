param(
    [Parameter(Mandatory = $true)]
    [uri]$BaseUri,
    [ValidateSet('Exhaust', 'CheckSeparateClient')]
    [string]$Mode = 'Exhaust'
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Net.Http
$client = New-Object System.Net.Http.HttpClient
$client.Timeout = [TimeSpan]::FromSeconds(60)
$loginUri = [uri]::new($BaseUri, '/api/auth/login')

function Send-InvalidLogin([string]$ForwardedFor = '') {
    $request = New-Object System.Net.Http.HttpRequestMessage([System.Net.Http.HttpMethod]::Post, $loginUri)
    $request.Content = New-Object System.Net.Http.StringContent('{}', [Text.Encoding]::UTF8, 'application/json')
    if ($ForwardedFor) {
        [void]$request.Headers.TryAddWithoutValidation('X-Forwarded-For', $ForwardedFor)
    }
    try {
        $response = $client.SendAsync($request).GetAwaiter().GetResult()
        try { return [int]$response.StatusCode }
        finally { $response.Dispose() }
    }
    finally { $request.Dispose() }
}

try {
    if ($Mode -eq 'CheckSeparateClient') {
        # Выполняется из другой сети, пока первый IP ещё ограничен.
        $status = Send-InvalidLogin
        if ($status -ne 400) {
            throw "Expected 400 from the second client, got $status. Check that the public IP is different and has no active limit."
        }
        Write-Output 'PASS: the second client is not blocked. Confirm that the first client is still receiving 429.'
        return
    }

    # Пустое JSON-тело останавливается на валидации DTO, до проверки аккаунта в БД.
    for ($attempt = 1; $attempt -le 5; $attempt++) {
        $status = Send-InvalidLogin
        if ($status -ne 400) {
            throw "Request ${attempt}: expected 400, got $status. Wait for the current window to expire before retrying."
        }
    }

    $status = Send-InvalidLogin
    if ($status -ne 429) {
        throw "Request 6: expected 429, got $status. Check the deployed version and login rate-limit policy."
    }

    # Подмена X-Forwarded-For не должна давать новый лимит тому же посетителю.
    foreach ($fakeIp in @('198.51.100.20', '198.51.100.21')) {
        $status = Send-InvalidLogin $fakeIp
        if ($status -ne 429) {
            throw "Changing X-Forwarded-For bypassed the limit: expected 429, got $status."
        }
    }
    Write-Output 'PASS: login is limited after five requests; changing X-Forwarded-For does not reset the counter.'
    Write-Output 'Next: run CheckSeparateClient from a different network within this one-minute window.'
}
finally { $client.Dispose() }
