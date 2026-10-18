{
  pkgs ? import <nixpkgs> { },
}:

let
  dotnet =
    with pkgs.dotnetCorePackages;
    combinePackages [
      sdk_10_0
      aspnetcore_10_0
    ];
in
pkgs.mkShell {
  packages = [
    dotnet
    pkgs.powershell
  ];
  DOTNET_ROOT = "${dotnet}/share/dotnet";
}
