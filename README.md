# Authorized Recovery Agent

Visible, pairing-code-protected agent for a Windows PC the operator is authorized to administer.

## Build

Open PowerShell in this folder and run:

dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o dist

The executable will be:

dist\\RecoveryAgentBuild.exe

## Local test

1. Run RecoveryAgentBuild.exe.
2. Copy the six-digit pairing code.
3. Run the matching RecoveryController.exe.
4. Enter 127.0.0.1 as the Agent IP/hostname.
5. Enter the pairing code.
6. Select Status first.

Restart and Shutdown actually affect the computer. Use only on a PC you are authorized to administer.

Do not expose TCP 47821 directly to the public Internet. Use an administrator-approved private network/VPN for remote access.
