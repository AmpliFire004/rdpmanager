§SHELL := /bin/sh
DOTNET ?= dotnet
PROJECT := RdpManager/RdpManager.csproj
RUNTIME ?= win-x64
SELF_CONTAINED ?= true
PUBLISH_DIR ?= publish

.PHONY: all build run clean publish

all: build

build:
	$(DOTNET) build $(PROJECT)

run:
	$(DOTNET) run --project $(PROJECT)

publish:
	$(DOTNET) publish $(PROJECT) -c Release -r $(RUNTIME) \
		--self-contained $(SELF_CONTAINED) \
		-p:PublishSingleFile=true \
		-p:IncludeNativeLibrariesForSelfExtract=true \
		-p:PublishTrimmed=false \
		-o $(PUBLISH_DIR)

clean:
	$(DOTNET) clean $(PROJECT)
