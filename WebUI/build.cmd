SET BUILD_TARGET=GDIA
npm run build && powershell -Command "(gc build\index.html) -replace 'type=\"module\"', '' | Out-File -encoding UTF8 build\index.html" && powershell -Command "(gc build\index.html) -replace 'crossorigin=\"anonymous\"', '' | Out-File -encoding UTF8 build\index.html"
xcopy /e /y build ..\IAGrim\bin\Debug\Resources\

pause