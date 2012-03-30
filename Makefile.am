include $(top_srcdir)/Makefile.include

CSFLAGS = 		\
	-debug 		\
	-define:DEBUG 	\
	-target:library

ASSEMBLIES =  \
	$(GTKSHARP_LIBS) \
	$(LINK_MONO_ADDINS) \
	$(LINK_TOMBOY_EXE) \
	-r:../../../bin/Debug/Tomboy.exe \
	-r:Mono.Posix

ADDIN_NAME = ExportToLaTeX
TARGET = $(ADDIN_NAME).dll
CSFILES = \
	$(srcdir)/ExportToLaTeXAddin.cs		\
RESOURCES = \
	-resource:$(srcdir)/$(ADDIN_NAME).addin.xml \


$(TARGET).mdb: $(TARGET)

$(TARGET): $(CSFILES) $(top_builddir)/Tomboy/Tomboy.exe
	$(CSC) -out:$@ $(CSFLAGS) $(ASSEMBLIES) $(CSFILES) $(RESOURCES)


addinsdir = $(pkglibdir)/addins
addins_DATA = 			\
	$(TARGET)		\
	$(TARGET).mdb

EXTRA_DIST =            	\
	$(CSFILES) \
	$(srcdir)/$(ADDIN_NAME).addin.xml \

CLEANFILES =				\
	$(TARGET).mdb \
	$(TARGET)

