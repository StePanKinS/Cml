NAME := cml

BUILD := build
OUT := $(BUILD)/$(NAME).vsix

.PHONY: all clean package prepare

all: package

package: clean prepare
	cd $(BUILD) && zip -r $(NAME).vsix extension

prepare:
	mkdir -p $(BUILD)/extension
	cp -r package.json $(BUILD)/extension/
	cp -r icon.png $(BUILD)/extension/ || true
	cp -r syntaxes $(BUILD)/extension/ || true
	cp -r language-configuration $(BUILD)/extension/ || true

clean:
	rm -rf $(BUILD)
