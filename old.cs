//using System.Threading;
//using System.Runtime.InteropServices;
//using System.Diagnostics;

//using Mono.Unix;
//using Gtk;
//using Tomboy;


using System;
using System.Xml;
using System.IO;
using System.Collections.Generic;

namespace Tomboy.LaTeX_out {


public class MainClass {
    public static void Main() {
	
		Console.WriteLine ("     Welcome to Tomboy to LaTeX note exporter");
		Console.WriteLine ("--------------------------------------------------");
		
		string tomboy_datadir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Tomboy\\notes");
	
		try {

			// Get a list of all the Tomboy notes
			DirectoryInfo di = new DirectoryInfo(tomboy_datadir);
			FileInfo[] rgFiles = di.GetFiles("*.note");

			// Get information on all the notes and put everything into a list
			List<MyNotes> Notes = new List<MyNotes>();
			
			foreach(FileInfo fi in rgFiles)
			{
				string[] Info = FetchInfo(Path.Combine(tomboy_datadir, fi.Name));
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

			// Make a list of notegroups and ask which should be used, or all
			var Notegroups = new List<string>();
			int NumNotegroups = 0;
			Notes.ForEach(delegate(MyNotes p)
				{
					if(!Notegroups.Contains(p.notegroup)) {
						Notegroups.Add(p.notegroup);
						Console.WriteLine("  " + NumNotegroups + "\t-> " + p.notegroup);
						NumNotegroups++;
					}
					//Console.WriteLine(String.Format("{0} {1}", p.title, p.notegroup)); 
				});
			Console.WriteLine();
			Console.WriteLine("  a\t-> All notebooks");

			Console.Write("Please select the notebook you wish to export: ");
			string Answer = Console.ReadLine();
			int ChosenNotebook = -1; // define variable

			// Ask for the output filename
			Console.Write("Please type a name for the output file: ");
			string OutputFileName = Console.ReadLine();
			if (OutputFileName == "") {
				OutputFileName = "latex_out.tex"; // use default name
				Console.WriteLine("No output filename specified, using default name: " + OutputFileName);
			}

			// Start the process...
			
			InitiateTeX(OutputFileName);				
				
			try {
				// Make sure that the user input is a number
				ChosenNotebook = Convert.ToInt32(Answer); 
				//Notes.ForEach(
				// Go through all the notebooks and process those that belong to the specified group				
				Notes.ForEach(delegate(MyNotes p)
				{
					if(p.notegroup == Notegroups[ChosenNotebook]) {
						Console.WriteLine("Processing: " + p.title);
						GenerateTeX(Path.Combine(tomboy_datadir, p.filename), OutputFileName);						
					} 
				});
				
				}
			catch {
				if (Answer == "a") {
					// User chose all notebooks
					Notes.ForEach(delegate(MyNotes p)
					{
						Console.WriteLine("Processing: " + p.title);
						GenerateTeX(Path.Combine(tomboy_datadir, p.filename), OutputFileName);						
					});					
				} else {
					Console.WriteLine("No option was selected. Press any key to exit.");
					Console.ReadLine();
					Environment.Exit(0);
				}
				}
				
			FinalizeTeX(OutputFileName); // Write the "LaTeX footer"
			
			Console.WriteLine("The operation completed successfully");
			
			
		} catch {
			Console.WriteLine("Error! " + tomboy_datadir);
		}
			Console.ReadLine();
	}

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
			Console.WriteLine ("Unable to process file: " + FileName);
			return Info;
		}
			


	}

	public static string LaTeX_Prep_String (string CleanString, string ListItem) {
		/*	For LaTeX output, special characters must be dealt with, otherwise
		 	the LaTeX parser will output errors.
		 	**Missing** Do some error checking???
		*/ 
			
			string MathStart = "\\["; // "\[" marks the beginning of a formula
			string MathEnd	 = "\\]"; // "\]" marks the ending of a formula
			
			if ((CleanString.Contains(MathStart) == true) && (CleanString.Contains(MathEnd) == true)) {
				/* 	The string contains a LaTeX formula. Make sure these tags are not changed
					and that the formula between the tags is not modified (it should be seen as
					LaTeX code).
				*/

				int cur_pos = 0;
				int hit_pos = 0;
				int run_pos = 0;
				int i = 0;

				string ReadyString = "";
				
				while (i < CleanString.Length) {
            		
					hit_pos = CleanString.IndexOf(MathStart, i);	// Math begins at this position
					run_pos = CleanString.IndexOf(MathEnd, i);		// Math ends at this position
					if ((hit_pos == -1) || (run_pos == -1)) {
						// There is no more math, end
						ReadyString = ReadyString + LaTeX_Clean_String(CleanString.Substring(cur_pos, CleanString.Length - cur_pos), ListItem);
						break;
					}
					
					// All the text that comes before or between the math
					ReadyString = ReadyString + LaTeX_Clean_String(CleanString.Substring(cur_pos, hit_pos-cur_pos), ListItem);
					// The math
					ReadyString = ReadyString + CleanString.Substring(hit_pos, run_pos + MathEnd.Length - hit_pos );
					
					i 		= run_pos + MathEnd.Length;
					cur_pos = i;
					i++;
				}
				CleanString = ReadyString;
				
			} else {
				// There was no LaTeX formula, proceed as normal
				CleanString = LaTeX_Clean_String(CleanString, ListItem);
			}
			
			// Finish
			return CleanString;
	}
	public static string LaTeX_Clean_String (string CleanString, string ListItem) {
		/*	For LaTeX output, special characters must be dealt with, otherwise
		 	the LaTeX parser will output errors.
		  	This function cleans the string for the following characters:
		 		# $ % & ~ _ ^ \ { }
		 	There are also some special characters that should be dealt with
		 	so they are displayed properly. Most notably greek characters.
		 	
		 	1. First deal with \
		 	2. Then deal with the other illegal characters
		 	3. Deal with greeks
		 	4. Do some extra detailing
		 	
		 	**Missing** Do some error checking???
		*/ 

			// Step 1
			CleanString = CleanString.Replace("\\", "\\\\"); // make all \ => \\
			
			// Step 2
			CleanString = CleanString.Replace("#", "\\#");
			CleanString = CleanString.Replace("$", "\\$");
			CleanString = CleanString.Replace("%", "\\%");
			CleanString = CleanString.Replace("&", "\\&");
			CleanString = CleanString.Replace("~", "\\~");
			CleanString = CleanString.Replace("_", "\\_");
			CleanString = CleanString.Replace("^", "\\^");
			CleanString = CleanString.Replace("{", "\\{");
			CleanString = CleanString.Replace("}", "\\}");

			// Step 3
			CleanString = CleanString.Replace("α", "$\\alpha$");
			CleanString = CleanString.Replace("ß", "$\\beta$");
			CleanString = CleanString.Replace("β", "$\\beta$");
			CleanString = CleanString.Replace("γ", "$\\gamma$");
			CleanString = CleanString.Replace("Γ", "$\\Gamma$");
			CleanString = CleanString.Replace("δ", "$\\delta$");
			CleanString = CleanString.Replace("Δ", "$\\Delta$");
			CleanString = CleanString.Replace("ε", "$\\varepsilon$");
			CleanString = CleanString.Replace("θ", "$\\theta$");
			CleanString = CleanString.Replace("Θ", "$\\Theta$");
			CleanString = CleanString.Replace("σ", "$\\sigma$");
			CleanString = CleanString.Replace("Σ", "$\\Sigma$");
			CleanString = CleanString.Replace("ς", "$\\varsigma$");
			CleanString = CleanString.Replace("λ", "$\\lambda$");
			CleanString = CleanString.Replace("Λ", "$\\Lambda$");
			CleanString = CleanString.Replace("π", "$\\pi$");
			CleanString = CleanString.Replace("Π", "$\\Pi$");
			CleanString = CleanString.Replace("ρ", "$\\rho$");
			CleanString = CleanString.Replace("φ", "$\\phi$");
			CleanString = CleanString.Replace("Φ", "$\\Phi$");
			CleanString = CleanString.Replace("ψ", "$\\psi$");
			CleanString = CleanString.Replace("Ψ", "$\\Psi$");
			CleanString = CleanString.Replace("ω", "$\\omega$");
			CleanString = CleanString.Replace("Ω", "$\\Omega$");
			CleanString = CleanString.Replace("τ", "$\\tau$");
			CleanString = CleanString.Replace("μ", "$\\mu$");
			CleanString = CleanString.Replace("η", "$\\eta$");
			CleanString = CleanString.Replace("χ", "$\\chi$");
			CleanString = CleanString.Replace("ι", "$\\iota$");
			CleanString = CleanString.Replace("κ", "$\\kappa$");
			CleanString = CleanString.Replace("Ξ", "$\\Xi$");
			CleanString = CleanString.Replace("ξ", "$\\xi$");
			CleanString = CleanString.Replace("ζ", "$\\zeta$");
			CleanString = CleanString.Replace("Υ", "$\\Upsilon$");
			CleanString = CleanString.Replace("υ", "$\\upsilon$");
			CleanString = CleanString.Replace("ν", "$\\nu$");

			/* Missing greeks
				\epsilon (I like \varepsilon better and used that instead)
				\vartheta
				\varpi
				\varrho
				\varphi
			*/

			// Step 3
			CleanString = CleanString.Replace("¹", "$^{1}$");
			CleanString = CleanString.Replace("²", "$^{2}$");
			CleanString = CleanString.Replace("³", "$^{3}$");
			CleanString = CleanString.Replace("²", "$^{2}$");
			CleanString = CleanString.Replace("\"", "''");
			CleanString = CleanString.Replace("“", "``");
			CleanString = CleanString.Replace("”", "''");
			CleanString = CleanString.Replace("–", "-");
			
			if ((ListItem == "") && (CleanString != "\n")) {
				CleanString = CleanString.Replace("\n", "\\\\\n");
			} else {
				CleanString = CleanString.Replace("\n", "");
			}				
			// If two greeks are adjacent it outputs: $\lambda$$\tau$$^{2}$. The double $ doesn't cause
			// parsing problems per se but it does confuse some latex guis.
			// This could cause problems if someone writes "$Ω" (or some other greek) but what are the chances...
			CleanString = CleanString.Replace("$$", "");
			
			// Finish
			return CleanString;
		
	}
	public static string LaTeX_Prep_LinkInternal (string PrepString) {
		/* Takes titles of internal links, makes them lower case and removes spaces
		   so they can be used as tags
		*/
		PrepString = LaTeX_Prep_String(PrepString, "0");
		PrepString = PrepString.Replace(" ", "_");
		PrepString = PrepString.ToLower();
		return PrepString;
	}
	public static string LaTeX_CleanOutputFile (string OutputFileName) {
		/* Take the automatically generated LaTeX file and attempt to fix errors if needed
			- Remove "\\" at the end of list items
			- Remove "\\" at the end of lines that preceede lists
		*/

	}

	public static void InitiateTeX (string OutputFileName) {
		/* Write a header to the LaTeX file */

		Console.WriteLine("Preparing LaTeX file...");

		try {
			// Open an output file
			StreamWriter Tex = new StreamWriter(OutputFileName, true, System.Text.Encoding.UTF8);

			// Generate a header
			string LaTeX_Header =
				"\\documentclass[10pt,a4paper]{article} \n" +
				"\\usepackage{t1enc} \n" +
				"\\usepackage[icelandic]{babel} \n" +
				"\\selectlanguage{icelandic} \n" +
				"\\setlength{\\topmargin}{-.5in} \n" +
				"\\setlength{\\textheight}{9in} \n" +
				"\\setlength{\\oddsidemargin}{.125in} \n" +
				"\\setlength{\\textwidth}{6.25in} \n" +
				"\\usepackage{ucs} \n" +
				"\\usepackage{amsmath} \n" +
				"\\usepackage{amsfonts} \n" +
				"\\usepackage{amssymb} \n" +
				"\\usepackage[colorlinks, linkcolor=blue]{hyperref} \n" +
				"\\author{Tomboy to LaTeX note exporter} \n" +
				"\\begin{document} \n" +
				"\\begingroup \n" +
				"\\hypersetup{linkcolor=black} \n" +
				"\\tableofcontents \n" +
				"\\endgroup \n" +
				"\\newpage \n";
			
			Tex.WriteLine(LaTeX_Header);	
			Tex.Close();		
				
		} catch {
			Console.WriteLine ("Unable to write to output file");
		
			return;
		}
	}
	public static void GenerateTeX (string FileName, string OutputFileName) {

		// Define all the variables
		string contents = ""; 
		string element_catch = "";
		string page_title = "NO-TITLE";
		string page_date = "NO_DATE";
		string page_tag = "NO-TAG";
			
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
							case "link:internal":
								contents = contents + LaTeX_Prep_LinkInternal(reader.Value) + "}{" + LaTeX_Prep_String(reader.Value, "");
								break;
							case "link:internal:list":
								contents = contents + LaTeX_Prep_LinkInternal(reader.Value) + "}{" + LaTeX_Prep_String(reader.Value, "1");
								break;
							case "title":
								page_title = reader.Value;
								break;
							case "last-change-date":
								page_date = reader.Value;
								break;
							case "tag":
								page_tag = reader.Value;
								break;
							case "list":
								contents = contents + LaTeX_Prep_String(reader.Value, "1");
								break;
						
						// The ones I want to ignore
							case "x":
								break;
							case "y":
								break;
							case "width":
								break;
							case "height":
								break;
							case "open-on-startup":
								break;
							case "cursor-position":
								break;
							case "last-metadata-change-date":
								break;
							case "create-date":
								break;
						
						// And for all normal text
							default:
								// For LaTeX output, special characters must be dealt with
								// 		# $ % & ~ _ ^ \ { }
								contents = contents + LaTeX_Prep_String(reader.Value, "");
								break;
							}
                        break;
                    case XmlNodeType.Element: // The start of an element.
						switch (reader.Name) {
							case "link:internal":
								contents = contents + "\\hyperlink{";
								if (element_catch == "list") {
									element_catch = "link:internal:list";
								} else {
									element_catch = reader.Name;
								}
								break;
							case "bold":
								contents = contents + "\\textbf{";
								break;
							case "italic":
								contents = contents + "\\textit{";
								break;
							case "list":
								contents = contents + "\\begin{itemize}\n";
								element_catch = reader.Name;
								break;
							case "list-item":
								contents = contents + "\\item ";
								element_catch = "list";
								break;
							default:
								element_catch = reader.Name;
								break;
							}
                        break;
                    case XmlNodeType.EndElement: // The end of an element.
						switch (reader.Name) {
							case "link:internal":
								element_catch = "";
								contents = contents + "}";
								break;
							case "bold":
								contents = contents + "}";
								break;
							case "italic":
								contents = contents + "}";
								break;
							case "list":
								contents = contents + "\\end{itemize}\n";
								element_catch = "";
								break;
							case "list-item":
								contents = contents + "\n";
								element_catch = "list";
								break;
							default:
								//element_catch = "";
								break;
							}
                        break;
                }
		}
		
		reader.Close();
		
		// Open an output file
		//FileInfo t = new FileInfo("tomboy_notes.tex");
		//StreamWriter Tex = t.AppendText();
		StreamWriter Tex = new StreamWriter(OutputFileName, true, System.Text.Encoding.UTF8);

		Tex.WriteLine("\\hypertarget{" + LaTeX_Prep_LinkInternal(page_title) + "}{\\section {" + page_title + "}}");
		//Tex.WriteLine("Last edited on: " + page_date);
		//Tex.WriteLine("Member of this group: " + page_tag);
		Tex.WriteLine(contents);
		
		Tex.Close();		
			
		} catch {
			Console.WriteLine ("Unable to process file: " + FileName);
		
			return;
		}
			


	}
	public static void FinalizeTeX (string OutputFileName) {
		/* Write an ending to the LaTeX file */

		Console.WriteLine("Finalizing LaTeX file...");

		try {
			// Open the output file
			//FileInfo t = new FileInfo("tomboy_notes.tex");
			//StreamWriter Tex = t.AppendText();
			StreamWriter Tex = new StreamWriter(OutputFileName, true, System.Text.Encoding.UTF8);

			// Generate a footer
			string LaTeX_End =
				"\\end{document}";
			
			Tex.WriteLine(LaTeX_End);	
			Tex.Close();		
				
		} catch {
			Console.WriteLine ("Unable to write to output file");
		
			return;
		}
	
	}
}

public class MyNotes {

		public string filename;
		public string title;
		public string date;
		public string notegroup;

		public MyNotes(string filename, string title, string date, string notegroup)

		{
			this.filename = filename;
			this.title = title;
			this.date = date;
			this.notegroup = notegroup;
		}
}
}