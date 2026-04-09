Write-Host "🚀 Activating MentoringApp Swarm..." -ForegroundColor Cyan

# 2. Launch Background Agent: The "Watcher" (Build & Health)
# Opens in a new window/tab so it doesn't clutter your main screen
Start-Process powershell -ArgumentList "-NoExit", "-Command", "Host.UI.RawUI.WindowTitle = 'BUILD WATCHER'; claude `"/loop 5m --read-only 'Check dotnet build MentoringApp.sln. If it fails, alert me with the error.'`""

# 3. Launch Background Agent: The "Architect" (Docs & Patterns)
Start-Process powershell -ArgumentList "-NoExit", "-Command", "Host.UI.RawUI.WindowTitle = 'DOCS AGENT'; claude `"/loop 15m --read-only 'Review new code for Result<T> pattern consistency and XML docs.'`""

# 4. Stay in the current terminal for the "Implementer"
Write-Host "✅ Swarm is ON AIR." -ForegroundColor Green
Write-Host "💡 Use THIS window for 'Plan then implement' tasks." -ForegroundColor White
claude