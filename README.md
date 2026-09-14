# School PC Setup

Authorized Windows remote-access setup.

## Build
The GitHub Actions workflow builds a single `SchoolPC_Setup.exe` for Windows x64 whenever a tag beginning with `v` is pushed.

## What it configures
- Windows Remote Desktop (if Windows edition can host RDP)
- Network Level Authentication
- Windows Remote Desktop firewall rules
- Wake-on-Magic-Packet where Windows supports it
- Connection information on the Public Desktop

## Limitations
- Windows Home cannot host incoming Microsoft Remote Desktop.
- BIOS/UEFI Wake-on-LAN settings may need manual configuration.
- RDP should not be exposed directly to the public Internet.
- Use only on systems and networks you are authorized to administer.
