using System;
using System.Text;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using Mono.Unix;
using Tomboy;

namespace Tomboy.ExportToLaTeX {
	
	public class exporterLaTeX {
		public string outputPath;
		public string notebookName;
		public bool useHyperlinking;
		public bool	useIcelandic;
		List<MyNotes> Notes = new List<MyNotes>();
		
		Encoding outputEnc = new UTF8Encoding(false); // Define a UTF8 encoding without the BOM mark which interrupts LaTeX parsers
		
		public exporterLaTeX (string outputPath, string notebookName, bool useHyperlinking, bool useIcelandic, List<MyNotes> Notes ) {
			this.outputPath = outputPath;
			this.notebookName = notebookName;
			this.useHyperlinking = useHyperlinking;
			this.useIcelandic = useIcelandic;
			this.Notes = Notes;
		}
		
		public void beginExport () {
			// Start the process...
			
			InitiateTeX(); // Write the "LaTeX header"
				
			// Go through all the notebooks and process those that belong to the specified group
			
			if (this.notebookName == "all") {
				// Export all notebooks
				this.Notes.ForEach(delegate(MyNotes p)
				{
					Logger.Info ("Tomboy Exporter: => processing note '{0}'", p.title);
					GenerateTeX(System.IO.Path.Combine(Tomboy.DefaultNoteManager.NoteDirectoryPath, p.filename));
				});								
			} else {
				// Export only the specified notebook
				this.Notes.ForEach(delegate(MyNotes p)
				{
					if(p.notegroup == this.notebookName) {
						Logger.Info ("Tomboy Exporter: => processing note '{0}'", p.title);
						GenerateTeX(System.IO.Path.Combine(Tomboy.DefaultNoteManager.NoteDirectoryPath, p.filename));						
					} 
				});
			}

			FinalizeTeX(); // Write the "LaTeX footer"
			
			Logger.Info ("Tomboy Exporter: The operation has completed");
		}
		
		// <summary>
		// This function writes the LaTeX header to the output file
		// </summary>
		public void InitiateTeX () {
	
			string latexIcelandicCode = "";
			string latexHyperl_a = "";
			string latexHyperl_b = "";
			
			Logger.Info ("Tomboy Exporter: Preparing LaTeX file...");
	
			try {
				// Open an output file
				//StreamWriter Tex = new StreamWriter(this.outputPath, true, System.Text.Encoding.UTF8);
				StreamWriter Tex = new StreamWriter(this.outputPath, true, outputEnc);
	
				// Generate a header
				if (this.useIcelandic) {
					latexIcelandicCode = "%The following two lines are for parsing special Icelandic letters \n" +
												"\\usepackage[icelandic]{babel} \n" +
												"\\usepackage[T1]{fontenc} \n\n";}
				
				if (this.useHyperlinking) {
					latexHyperl_a = "\\usepackage[colorlinks, linkcolor=blue]{hyperref} \n\n";
					latexHyperl_b = "\\hypersetup{linkcolor=black} \n";
				}
				
				string LaTeX_Header =
					"%begin LaTeX Document \n" +
					"%Automatically generated by Tomboy Exporter \n" +
					"\\documentclass[10pt,a4paper]{article} \n\n" +

					"\\usepackage{ucs} \n" +
					"\\usepackage[utf8x]{inputenc} \n\n" +

					latexIcelandicCode +

					"%Set margins, delete or comment out the following four lines if you want to use the defaults \n" +
					"\\setlength{\\topmargin}{-.5in} \n" +
					"\\setlength{\\textheight}{9in} \n" +
					"\\setlength{\\oddsidemargin}{.125in} \n" +
					"\\setlength{\\textwidth}{6.25in} \n\n" +

					"%Needed for formulas and hyperlinking\n" +
					"\\usepackage{amsmath} \n" +
					"\\usepackage{amsfonts} \n" +
					"\\usepackage{amssymb} \n" +
					latexHyperl_a + "\n" +

					"%Needed for proper display of underline and strikethrough text \n" +
					"\\usepackage{ulem} \n" +
					"\\normalem \n\n" +

					"\\begin{document} \n" +
					"\\author{Tomboy Exporter} \n" +
					"\\begingroup \n" +
					latexHyperl_b +
					"\\tableofcontents \n" +
					"\\endgroup \n" +
					"\\newpage \n";
				
				Tex.WriteLine(LaTeX_Header);	
				Tex.Close();		
					
			} catch {
				Logger.Error ("Tomboy Exporter: Unable to write to output file '{0}'", this.outputPath);
			
				return;
			}
		}
		// <summary>
		// This function does the actual parsing of every note XML file
		// </summary>
		public void GenerateTeX (string FileName) {
	
			// Define all the variables
			string contents = ""; 
			string element_catch = "";
			string page_title = "unknown";
			string page_date = "NO_DATE";
			string page_tag = "NO-TAG";
			
			bool   title_cought = false; // The title comes twice, first as a title and then as the first text in the note; do not display twice

			// Open the XML file
	        try {
				XmlTextReader reader = new XmlTextReader(FileName);
				reader.WhitespaceHandling = WhitespaceHandling.None;
			
				// Start reading the XML file
		        while (reader.Read()) {
	
	                switch (reader.NodeType) 
	                {
	                    case XmlNodeType.Text: // The text inside each element.
							switch (element_catch) {
							// The ones I have special interest in
								case "link:internal":
									if (this.useHyperlinking) {contents += LaTeX_Prep_LinkInternal(reader.Value) + "}{" + LaTeX_Prep_String(reader.Value, "");}
													     else {contents += LaTeX_Prep_String(reader.Value, "");}
									break;
								case "link:internal:list":
									if (this.useHyperlinking) {contents += LaTeX_Prep_LinkInternal(reader.Value) + "}{" + LaTeX_Prep_String(reader.Value, "1");}
													     else {contents += LaTeX_Prep_String(reader.Value, "1");}
									break;
							
								case "title":			 page_title = reader.Value;	break;
								case "last-change-date": page_date  = reader.Value;	break;
								case "tag":				 page_tag   = reader.Value;	break;
							
								case "list":			 contents  += LaTeX_Prep_String(reader.Value, "1");	break;
							
							// The ones I want to ignore -> the contents of these elements do not concern me
								case "x": 							break;
								case "y": 							break;
								case "width":						break;
								case "height":						break;
								case "open-on-startup":				break;
								case "cursor-position":				break;
								case "last-metadata-change-date":	break;
								case "create-date": 				break;
							
							// And for all normal text
								default: if ((reader.Value.StartsWith(page_title)) && (!title_cought)) {title_cought=true;}
										 // For LaTeX output, special characters must be dealt with,   # $ % & ~ _ ^ \ { }
										 else {contents += LaTeX_Prep_String(reader.Value, "");}
									     break;
							}
	                        break;
	                    case XmlNodeType.Element: // The start of an element.
							switch (reader.Name) {
								case "link:internal":	if (this.useHyperlinking)    {contents += "\\hyperlink{"; }
														if (element_catch == "list") { element_catch = "link:internal:list";}
																				else { element_catch = reader.Name;} break;
								case "bold":			contents += "\\textbf{";	break;
								case "italic":			contents += "\\textit{";	break;
								case "strikethrough":	contents += "\\sout{";		break;
								case "underline":		contents += "\\uline{";		break;
								case "monospace":		contents += "\\texttt{";	break;
								case "size:small":		contents += "{\\small ";	break;
								case "size:large":		contents += "{\\large ";	break;
								case "size:huge":		contents += "{\\Large ";	break;
								case "list":			if (!contents.EndsWith("\n")) {contents += "\n";}
														contents += "\\begin{itemize}\n"; element_catch = reader.Name; break;
								case "list-item":		contents += "\\item "; element_catch = "list"; break;
								default:	element_catch = reader.Name; break;
							}
	                        break;
	                    case XmlNodeType.EndElement: // The end of an element.
							switch (reader.Name) {
								case "link:internal":	if (this.useHyperlinking) {contents += "}"; } element_catch = ""; break;
								case "bold":			contents += "}"; 			break;
								case "italic":			contents += "}"; 			break;
								case "strikethrough":	contents += "}"; 			break;
								case "underline":		contents += "}"; 			break;
								case "monospace":		contents += "}"; 			break;
								case "size:small":		contents += "}"; 			break;
								case "size:large":		contents += "}"; 			break;
								case "size:huge":		contents += "}";			break;
								case "list":			contents += "\\end{itemize}\n"; element_catch = "";	break;
								case "list-item":		contents += "\n"; element_catch = "list"; break;
								default: break;
							}
	                        break;
	                }
				}
			
				reader.Close(); // Close the XML note file
				
				// Open an output file and write this note to it
				//StreamWriter Tex = new StreamWriter(this.outputPath, true, System.Text.Encoding.UTF8);
				StreamWriter Tex = new StreamWriter(this.outputPath, true, outputEnc);
				
					if (this.useHyperlinking) { // Add a title for this note / section
						Tex.WriteLine("\\hypertarget{" + LaTeX_Prep_LinkInternal(page_title) + "}{\\section {" + page_title + "}}");
					} else {
						Tex.WriteLine("\\section {" + page_title + "}");
					}
					Tex.WriteLine(contents); 	// Output note contents
				
				Tex.Close();
				
			} catch {
				Logger.Error ("Tomboy Exporter: Unable to process note '{0}' stored in '{1}'", page_title, FileName);
				return;
			}

		}
		// <summary>
		// This function writes the ending to a LaTeX file
		// </summary>
		public void FinalizeTeX () {
			/* Write an ending to the LaTeX file */
	
			try {
				// Open the output file
				//StreamWriter Tex = new StreamWriter(this.outputPath, true, System.Text.Encoding.UTF8);
				StreamWriter Tex = new StreamWriter(this.outputPath, true, outputEnc);
	
				// Generate a footer
				string LaTeX_End =
					"\\end{document}";
				
				Tex.WriteLine(LaTeX_End);	
				Tex.Close();		
					
			} catch {
				return;
			}
		
		}

		
		// <summary>
		// Helper functions used to generate the LaTeX code
		// </summary>
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
				CleanString = CleanString.Replace("ϵ", "$\\epsilon$");
				CleanString = CleanString.Replace("θ", "$\\theta$");
				CleanString = CleanString.Replace("Θ", "$\\Theta$");
				CleanString = CleanString.Replace("ϑ", "$\\vartheta$");
				CleanString = CleanString.Replace("σ", "$\\sigma$");
				CleanString = CleanString.Replace("Σ", "$\\Sigma$");
				CleanString = CleanString.Replace("ς", "$\\varsigma$");
				CleanString = CleanString.Replace("λ", "$\\lambda$");
				CleanString = CleanString.Replace("Λ", "$\\Lambda$");
				CleanString = CleanString.Replace("π", "$\\pi$");
				CleanString = CleanString.Replace("Π", "$\\Pi$");
				CleanString = CleanString.Replace("ϖ", "$\\varpi$");
				CleanString = CleanString.Replace("ρ", "$\\rho$");
				CleanString = CleanString.Replace("ϱ", "$\\varrho$");
				CleanString = CleanString.Replace("ϕ", "$\\phi$");
				CleanString = CleanString.Replace("Φ", "$\\Phi$");
				CleanString = CleanString.Replace("φ", "$\\varphi$");
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
	
				// Step 4
				CleanString = CleanString.Replace("¹", "$^{1}$");
				CleanString = CleanString.Replace("²", "$^{2}$");
				CleanString = CleanString.Replace("³", "$^{3}$");
				CleanString = CleanString.Replace("⁴", "$^{4}$");
				CleanString = CleanString.Replace("⁵", "$^{5}$");
			
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
				// parsing problems per se but it does confuse some latex guis -> remove all "$$"
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
		public static string LaTeX_CleanOutputFile () {
			/* Take the automatically generated LaTeX file and attempt to fix errors if needed
				- Remove "\\" at the end of list items
				- Remove "\\" at the end of lines that preceede lists
				- Remove all \\ \n \[ , that is line breaks before formulas
				- Remove all \]\\ , that is line breaks after formulas				
			*/
			return null;
	
		}
		
	}
}