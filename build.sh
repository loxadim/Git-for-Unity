#!/bin/sh -eu
Configuration="Release"
if [[ $# -gt 0 ]]; then
	Configuration=$1
fi

Target="Build"
if [[ $# -gt 1 ]]; then
	Target=$2
fi

OS="Mac"
if [[ -e "/c/" ]]; then
	OS="Windows"
fi

common/nuget restore

if [[ x"$OS" == x"Windows" ]]; then
	./build.cmd $Configuration $Target
else
	dotnet restore --ignore-failed-sources
	dotnet build -c $Configuration
fi
