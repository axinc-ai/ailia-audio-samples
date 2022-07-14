ifndef OSTYPE
#OSTYPE=Windows
OSTYPE=Mac
endif

ifeq ($(OSTYPE),Windows)
COMPILER=cl
OPTION=/EHsc /I ./include
LIBS=ailia.lib ailia_audio.lib ole32.lib
EXT=.exe
#chcp 65001
else
COMPILER=clang++
OPTION=-I ./include -stdlib=libc++ -std=c++11 -Wl,-rpath,./
LIBS=libailia.dylib libailia_audio.dylib
EXT=
endif

all:
	$(COMPILER) crnn_audio_classification.cpp wave_reader.cpp $(LIBS) $(OPTION) -o crnn_audio_classification$(EXT)
