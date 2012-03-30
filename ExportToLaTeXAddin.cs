using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using Mono.Unix;

using Tomboy;

namespace Tomboy.ExportToLaTeX {

	public class ExportToLaTeXAddin : NoteAddin	{
		// Add "Export to LaTeX" to the "Tools" menu
		//Gtk.ImageMenuItem item; -- can be deleted
		
		static Gtk.ActionGroup action_group;
		static uint tools_menu_ui = 0;
		
		public override void Initialize () {
				// Add "Export notebook(s)" to the "Tools" menu in the main window
                
				// This code added the option to the Note window, -- can be deleted or used to make
				// the option to export a single note
					//item = new Gtk.ImageMenuItem (Catalog.GetString ("Export notebook(s)"));
					//item.Image = new Gtk.Image (Gtk.Stock.Convert, Gtk.IconSize.Menu);
	                //item.Activated += ExportToLaTeXClicked;
	                //item.Show ();
	                //AddPluginMenuItem (item);
			        //void ExportToLaTeXClicked (object sender, EventArgs args)
			        //{
						// User clicks "Export...", let something happen
						// Now ask the user for the required information
						//OnExportNotebookAction ();
			        //}			
		
				action_group = new Gtk.ActionGroup ("Export");
				action_group.Add (new Gtk.ActionEntry [] {
					new Gtk.ActionEntry ("ToolsMenuAction", null, Catalog.GetString ("_Tools"), null, null,
				        null),
					new Gtk.ActionEntry ("ExportNotebookAction", null, Catalog.GetString ("Export notebook(s)"), null, null,
				        delegate { OnExportNotebookAction ();})
				});
	
				tools_menu_ui = Tomboy.ActionManager.UI.AddUiFromString (@"
				                <ui>
				                  <menubar name='MainWindowMenubar'>
				                    <placeholder name='MainWindowMenuPlaceholder'>
				                      <menu name='ToolsMenu' action='ToolsMenuAction'>
				                        <menuitem name='ExportNotebook' action='ExportNotebookAction' />
				                      </menu>
				                    </placeholder>
				                  </menubar>
				                </ui>
				                ");
	
				Tomboy.ActionManager.UI.InsertActionGroup (action_group, 0);			

        }

        public override void Shutdown ()
        {
			// Remove Tomboy Exporter from the menu
			try {
				Tomboy.ActionManager.UI.RemoveActionGroup (action_group);
			} catch {}
			try {
				Tomboy.ActionManager.UI.RemoveUi (tools_menu_ui);
			} catch {}			
            //item.Activated -= ExportToLaTeXClicked;
        }

		public override void OnNoteOpened ()
        {
		}
		
		// <summary>
		// This function is called whenever the user selects the Tomboy Exporter
		// from the Tools menu
		// </summary>
		private void OnExportNotebookAction ()
		{
			try {
				ExportToLaTeXDialog dialog = new ExportToLaTeXDialog ();	// Open a dialog

				int response = dialog.Run();

				if (response != (int) Gtk.ResponseType.Yes) {
					dialog.Destroy (); 										// User canceled, terminate
					return;
				}
			} catch {
				Logger.Error ("Tomboy Exporter: Unknown error occurred while initializing Tomboy Exporter dialog");
				//Console.WriteLine("Unknown error occurred while initializing Tomboy Exporter dialog");
			}						
		}
		
	}
}