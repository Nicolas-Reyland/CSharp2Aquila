@echo off
echo [!] CREATING RELEASE
dotnet publish -r win-x64 -c Release /p:PublishSingleFile=true --self-contained true
echo [!] COPYING RELEASE
XCopy /E /I /Y "C:\Users\Nicolas\Documents\EPITA\Code Vultus\scratch\CSharp2Aquila\CSharp2Aquila\bin\Release\net5.0\win-x64\publish" "C:\Users\Nicolas\Code Vultus\Assets\Translators\CSharpTranslatorBinaries"
echo [!] FINISHED
pause