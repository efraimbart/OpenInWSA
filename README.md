# Open in WSA

Routes opened links through WSA (Windows Subsystem for Android™) and opens them in the applicable Android application, otherwise opens them in the default Windows browser.

## Getting Started

Instructions to set up `Open in WSA` either using the latest release executable or via cloning the repo.

### Prerequisites

Required:

* [WSA](http://aka.ms/AmazonAppstore) with `Developer mode` enabled under settings.
* [ADB](https://developer.android.com/studio/releases/platform-tools)

Suggested: 

* `Subsystem resources` set to `Continuous` under WSA settings.

### Installation

1. ####Download Executable: 
    Download the [latest release](https://github.com/efraimbart/OpenInWSA/releases/latest) executable.

or
2. ####Clone Repo
    
   * Download and set up [.Net 6.0](https://dotnet.microsoft.com/download/dotnet/6.0) 
   * Run `$ git clone https://github.com/efraimbart/OpenInWSA`
   * Run `$ dotnet build`


### Setup
1. Open OpenInWSA.exe.
2. If ADB is not automatically found, set the path to ADB.
3. If the default browser is not automatically detected, set a default browser.
4. The application will attempt to elevate itself and register as a browser. If it fails, attempt to run the application manually as an administrator.
5. Set `Open In WSA` as the default for `URL:HyperText Transfer Protocol` in Windows settings under `Apps > Default apps`

## Usage

Click a link from within a Windows application and it will route it through WSA and open it in the applicable Android application. if none are found it will open the link in the default Windows browser.

To route links within Chrome through `Open in WSA` install the [Open in WSA Chrome Extension](https://chrome.google.com/webstore/detail/nkfpikoflncblmlajlcagaflndiijhhl) | [Repo](https://github.com/efraimbart/OpenInWSAChromeExtension) and right click on the given link.