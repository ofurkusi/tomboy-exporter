using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using Mono.Unix;
using Gtk;
using Tomboy;

namespace Tomboy.ExportToLaTeX {
	
	public partial class ExportToLaTeXDialog : Gtk.Dialog {

		// Set up global variables
			string tomboy_datadir = Tomboy.DefaultNoteManager.NoteDirectoryPath;
			
			string defaultFileName = "tomboy"; 				// Default filename
			string outputPath;								// Eventually holds the complete path for the output file
			string fileExtension;
			
			List<MyNotes> Notes = new List<MyNotes>();		// a list of notes
			List<string> Notegroups = new List<string>();	// a list of notebooks
			
			int NumNotegroups = 1;
		
		public ExportToLaTeXDialog () {
			
			// Runs when the window is displayed
			this.Build ();
			
			// Assign events to the buttons
			buttonExecute.Clicked += ButtonExecuteClicked;
			buttonCancel.Clicked += ButtonCancelClicked;
			
			// Load the notebooks
			PrepareVariables();
			
			// Load the list of notebooks into the dropdown selector
			foreach (string notebook in Notegroups) {
				notebook_selector.AppendText(notebook);
				notebook_selector.Active = 0;
			}

			// Make a list of available formats
			dropdownFormat.AppendText("LaTeX (.tex)");
			//dropdownFormat.AppendText("Rich Text Format (.rtf)");
			dropdownFormat.Active = 0;
			
		}
		
		public void PrepareVariables () {
			// Get a list of all the Tomboy notes
			// TODO: use "Tomboy.DefaultNoteManager.notes" instead?
			DirectoryInfo di = new DirectoryInfo(tomboy_datadir);
			FileInfo[] rgFiles = di.GetFiles("*.note");

			// Get information on all the notes and put everything into a list
			foreach (FileInfo fi in rgFiles) {
				string[] Info = FetchInfo(System.IO.Path.Combine(tomboy_datadir, fi.Name));
				if (!Info[0].Contains("Notebook Template")) {
					Notes.Add(new MyNotes(Info[0], Info[1], Info[2], Info[3]));
				}
				/* Info[0] = Filename;
				   Info[1] = Note title;
				   Info[2] = Note last update date;
				   Info[3] = Note "group" or Notebook;
				*/
			}
			
			// Sort the list of notes alphabetically
			Notes.Sort(delegate(MyNotes p1, MyNotes p2)
				{ return p1.title.CompareTo(p2.title); });

			// Make a list of notebooks
			Notegroups.Add("All notebooks"); // include this option

			Notes.ForEach(delegate(MyNotes p) {
				if(!Notegroups.Contains(p.notegroup)) {
					Notegroups.Add(p.notegroup);
					NumNotegroups++;
				}
			});
		}
		
		void ButtonExecuteClicked (object sender, EventArgs e) {
			// Find out which extension to use by looking at the selected format
			switch (dropdownFormat.Active) {
			    case 0: 
			        fileExtension = ".tex";
			        break;
			    case 1:
			        fileExtension = ".rtf";
			        break;
			    default:
			        fileExtension = ".tex";
			        break;
			}
			
			// Create a file chooser named "chooser"
		    FileChooserDialog chooser = new FileChooserDialog(
		    	"Please select where you would like your file saved",
		        this,
		        FileChooserAction.Save,
		        "Cancel", ResponseType.Cancel,
		        "Save", ResponseType.Accept );
			
			chooser.SetFilename(defaultFileName + fileExtension); // these two lines don't seem to do anything...?
			chooser.SelectFilename(defaultFileName + fileExtension);
			
			// Open the file chooser dialog and see if the user chose "Save" (note: "Save" will not return here if no filename is set)
		    if( chooser.Run() == ( int )ResponseType.Accept ) {
				// Get the path and filename of the chosen file
				Uri fileUri = new Uri (chooser.Uri);
				outputPath = fileUri.LocalPath;
				
				// Make sure the file ending is correct, else add it
				if (outputPath.EndsWith(fileExtension) == false){
					outputPath += fileExtension;
				}
				
				// Close the file chooser dialog
			    chooser.Destroy();
				
				if (File.Exists(outputPath)) {
					MessageDialog mdConfirm = new Gtk.MessageDialog (null, Gtk.DialogFlags.DestroyWithParent, MessageType.Question, 
                                      ButtonsType.YesNo, "This file already exists, do you want to overwrite it?");
					ResponseType mdResult = (ResponseType)mdConfirm.Run ();
					mdConfirm.Destroy();
					
					if (mdResult == ResponseType.Yes) {
						// The user wishes to overwrite the file
						try {
							File.Delete(outputPath);
							Logger.Info ("Tomboy Exporter: Existing file deleted by user request '{0}'", outputPath);
						} catch {
							Logger.Error ("Tomboy Exporter: User requested overwrite failed for the file '{0}'", outputPath);
							MessageDialog mdError = new Gtk.MessageDialog (null, Gtk.DialogFlags.DestroyWithParent, MessageType.Error,
                                      ButtonsType.Close, "Could not remove existing file. Check that you have write access to the file.");
							int result = mdError.Run ();
							mdError.Destroy();
							return;
						}
					} else {
						// Since the user does not want to overwrite the file, return to the Tomboy Exporter dialog
						return;
					}
                }

				Logger.Info ("Tomboy Exporter: exporting '{0}' to '{1}' as '{2}'", notebook_selector.ActiveText, outputPath, dropdownFormat.ActiveText);
	
				string notebookName;
				
				// Initiate the note export
				if (notebook_selector.Active == 0) {
					notebookName = "all";
				} else {
					notebookName = notebook_selector.ActiveText;
				}

				// switch the export type
				exporterLaTeX myExporter = new exporterLaTeX(outputPath, notebookName,checkHyperlinking.Active, checkIcelandic.Active, Notes);
				myExporter.beginExport();
				
				// and close the Tomboy Exporter dialog
				base.Destroy();
		    }
		    chooser.Destroy(); // user chose "Cancel" or some other action, whatever it may be - return to the Tomboy Exporter dialog
			
    	}

		void ButtonCancelClicked (object sender, EventArgs e) {
			base.Destroy();
    	}
		
		// <summary>
		// A function for fetching information from an XML file containing a note
		// </summary>
	 	public static string[] FetchInfo (string FileName) {
	
			// Define all the variables
			string[] Info = new string[4];
	
			string element_catch = "";
			string page_title = "NO-TITLE";
			string page_date = "NO_DATE";
			string page_tag = "Unfiled Notes";
			
			// Open the XML file
	        try {
				XmlTextReader reader = new XmlTextReader(FileName);
				reader.WhitespaceHandling = WhitespaceHandling.None;
			
				// Start reading the XML file
	        	while (reader.Read()) {
	
	                switch (reader.NodeType) 
	                {
	                    case XmlNodeType.Text: //Display the text in each element.
							switch (element_catch) {
							// The ones I am interested in
								case "title":
									page_title = reader.Value;
									break;
								case "last-change-date":
									page_date = reader.Value;
									break;
								case "tag":
									if (reader.Value.StartsWith("system:notebook")) {
										page_tag = reader.Value.Remove(0,16);
									}
									//page_tag = reader.Value;
									break;
								}
	                        break;
	                    case XmlNodeType.Element: // The start of an element.
							element_catch = reader.Name;
	                        break;
	                    case XmlNodeType.EndElement: // The end of an element.
							element_catch = "";
	                        break;
	                }
				}
			
				reader.Close();
				Info[0] = FileName;
				Info[1] = page_title;
				Info[2] = page_date;
				Info[3] = page_tag;
				return Info;
				
			} catch {
				//Console.WriteLine ("Unable to process file: " + FileName);
				return Info;
			}

		}

	}	

	// <summary>
	// This class holds information on a particular note, such as the title, filename & notegroup
	// </summary>
	public class MyNotes {

		public string filename;
		public string title;
		public string date;
		public string notegroup;

		public MyNotes(string filename, string title, string date, string notegroup) {
			this.filename = filename;
			this.title = title;
			this.date = date;
			this.notegroup = notegroup;
		}
	}	
	
}

