.SHELLFLAGS := -ec

build:
	rm -rf build/Cs2PracticeMode
	dotnet publish ./src --output build/Cs2PracticeMode
     
.PHONY: build
