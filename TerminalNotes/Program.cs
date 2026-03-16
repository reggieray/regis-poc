using System.Text.Json;
using Terminal.Gui;

namespace TerminalNotes
{
    class Note
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public override string ToString() => Title;
    }

    class Program
    {
        static string filePath = "notes.json";
        static List<Note> allNotes = new List<Note>();
        static List<Note> filteredNotes = new List<Note>();

        // UI Components
        static ListView noteListView;
        static TextView editor;
        static TextField searchField;
        static Note selectedNote;
        static FrameView editorFrame;

        // Custom Themes

        static ColorScheme Obsidian = new ColorScheme()
        {
            Normal = Terminal.Gui.Attribute.Make(Color.White, Color.Black),
            Focus = Terminal.Gui.Attribute.Make(Color.Black, Color.White),
            HotNormal = Terminal.Gui.Attribute.Make(Color.White, Color.Black),
            HotFocus = Terminal.Gui.Attribute.Make(Color.Black, Color.White)
        };

        static ColorScheme ClassicBlue = new ColorScheme
        {
            Normal = Colors.Base.Normal,
            Focus = Colors.Base.Focus,
            HotNormal = Colors.Base.HotNormal,
            HotFocus = Colors.Base.HotFocus
        };

        static ColorScheme DarkMatrix = new ColorScheme()
        {
            Normal = Terminal.Gui.Attribute.Make(Color.BrightGreen, Color.Black),
            Focus = Terminal.Gui.Attribute.Make(Color.White, Color.DarkGray),
            HotNormal = Terminal.Gui.Attribute.Make(Color.Green, Color.Black),
            HotFocus = Terminal.Gui.Attribute.Make(Color.BrightYellow, Color.DarkGray)
        };

        static ColorScheme HighContrast = new ColorScheme()
        {
            Normal = Terminal.Gui.Attribute.Make(Color.White, Color.Black),
            Focus = Terminal.Gui.Attribute.Make(Color.Black, Color.White),
            HotNormal = Terminal.Gui.Attribute.Make(Color.BrightYellow, Color.Black),
            HotFocus = Terminal.Gui.Attribute.Make(Color.BrightYellow, Color.White)
        };

        static ColorScheme SolarizedDark = new ColorScheme()
        {
            Normal = Terminal.Gui.Attribute.Make(Color.Blue, Color.Black),
            Focus = Terminal.Gui.Attribute.Make(Color.Cyan, Color.DarkGray),
            HotNormal = Terminal.Gui.Attribute.Make(Color.BrightMagenta, Color.Black),
            HotFocus = Terminal.Gui.Attribute.Make(Color.BrightMagenta, Color.DarkGray)
        };

        // 2. Midnight (Deep purples and blues)
        static ColorScheme Midnight = new ColorScheme()
        {
            Normal = Terminal.Gui.Attribute.Make(Color.BrightBlue, Color.Black),
            Focus = Terminal.Gui.Attribute.Make(Color.White, Color.Magenta),
            HotNormal = Terminal.Gui.Attribute.Make(Color.Cyan, Color.Black),
            HotFocus = Terminal.Gui.Attribute.Make(Color.BrightCyan, Color.Magenta)
        };

        // 3. Retro Hacker (The classic terminal look)
        static ColorScheme RetroHacker = new ColorScheme()
        {
            Normal = Terminal.Gui.Attribute.Make(Color.BrightGreen, Color.Black),
            Focus = Terminal.Gui.Attribute.Make(Color.Black, Color.BrightGreen),
            HotNormal = Terminal.Gui.Attribute.Make(Color.Green, Color.Black),
            HotFocus = Terminal.Gui.Attribute.Make(Color.DarkGray, Color.BrightGreen)
        };

        // 4. Paper (Light mode for those who prefer high contrast)
        static ColorScheme PaperMode = new ColorScheme()
        {
            Normal = Terminal.Gui.Attribute.Make(Color.Black, Color.White),
            Focus = Terminal.Gui.Attribute.Make(Color.White, Color.Blue),
            HotNormal = Terminal.Gui.Attribute.Make(Color.DarkGray, Color.White),
            HotFocus = Terminal.Gui.Attribute.Make(Color.BrightYellow, Color.Blue)
        };

        static void Main()
        {
            Application.Init();
            var top = Application.Top;

            // Set initial theme
            Colors.Base = Obsidian;

            var win = new Window("Notes")
            {
                X = 0,
                Y = 1,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };

            var searchLabel = new Label("Search: ") { X = 1, Y = 1 };
            searchField = new TextField("")
            {
                X = Pos.Right(searchLabel),
                Y = 1,
                Width = Dim.Fill(1)
            };

            var listFrame = new FrameView("Notes")
            {
                X = 0,
                Y = 3,
                Width = 30,
                Height = Dim.Fill()
            };
            noteListView = new ListView(filteredNotes)
            {
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                CanFocus = true
            };
            listFrame.Add(noteListView);

            editorFrame = new FrameView("Editor (No Note Selected)")
            {
                X = 31,
                Y = 3,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            editor = new TextView()
            {
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                ReadOnly = true
            };
            editorFrame.Add(editor);

            // --- Event Handlers ---
            searchField.TextChanged += (prev) => UpdateListView();

            noteListView.SelectedItemChanged += (e) => {
                SaveCurrentProgress(); // Captures text from editor before swapping
                LoadSelectedNote();
            };

            win.KeyPress += (e) => {
                if (e.KeyEvent.IsCtrl && (e.KeyEvent.Key == Key.S || e.KeyEvent.Key == (Key)115))
                {
                    SaveToDisk();
                    MessageBox.Query("Saved", "Notes saved to disk.", "Ok");
                    e.Handled = true;
                }
            };

            // --- Menu Bar ---
            var menu = new MenuBar(new MenuBarItem[] {
                new MenuBarItem("_File", new MenuItem[] {
                    new MenuItem("_New", "", NewNote),
                    new MenuItem("_Rename", "F2", RenameNote),
                    new MenuItem("_Save", "Ctrl+S", () => { SaveToDisk(); MessageBox.Query("Saved", "Changes committed.", "Ok"); }),
                    new MenuItem("_Delete", "", DeleteNote),
                    new MenuItem("_Quit", "", () => { SaveToDisk(); Application.RequestStop(); })
                }),
                new MenuBarItem("_Themes", new MenuItem[] {
                    new MenuItem("_Obsidian", "", () => SetTheme(Obsidian)),
                    new MenuItem("_Classic Blue", "", () => SetTheme(ClassicBlue)),
                    new MenuItem("_Dark Matrix", "", () => SetTheme(DarkMatrix)),
                    new MenuItem("_Solarized", "", () => SetTheme(SolarizedDark)),
                    new MenuItem("_Midnight", "", () => SetTheme(Midnight)),
                    new MenuItem("_Hacker", "", () => SetTheme(RetroHacker)),
                    new MenuItem("_Paper", "", () => SetTheme(PaperMode)),
                    new MenuItem("_High Contrast", "", () => SetTheme(HighContrast))
                })
            });

            win.Add(searchLabel, searchField, listFrame, editorFrame);
            top.Add(menu, win);

            LoadFromDisk();

            // Apply theme to the newly built UI
            SetTheme(Colors.Base);

            Application.Run();
            Application.Shutdown();
        }

        // Logic methods (SaveToDisk, LoadFromDisk, RenameNote, etc.) remain as you wrote them...
        // Ensure SaveCurrentProgress is called inside SaveToDisk as you did!

        static void SaveToDisk()
        {
            SaveCurrentProgress();
            var json = JsonSerializer.Serialize(allNotes, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        static void LoadFromDisk()
        {
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                allNotes = JsonSerializer.Deserialize<List<Note>>(json) ?? new List<Note>();
                UpdateListView();
            }
        }

        static void RenameNote()
        {
            if (selectedNote == null) { MessageBox.ErrorQuery("Error", "Select a note first!", "Ok"); return; }
            var dialog = new Dialog("Rename", 50, 7);
            var label = new Label("New Title:") { X = 1, Y = 1 };
            var input = new TextField(selectedNote.Title) { X = Pos.Right(label) + 1, Y = 1, Width = Dim.Fill(1) };
            var ok = new Button("Ok", true);
            ok.Clicked += () => {
                selectedNote.Title = input.Text.ToString();
                UpdateListView();
                editorFrame.Title = $"Editing: {selectedNote.Title}";
                Application.RequestStop();
            };
            dialog.Add(label, input, ok);
            Application.Run(dialog);
        }

        static void NewNote()
        {
            var n = new Note { Title = $"Note {allNotes.Count + 1}", Content = "" };
            allNotes.Add(n);
            UpdateListView();
            noteListView.SelectedItem = filteredNotes.Count - 1;
            LoadSelectedNote();
        }

        static void LoadSelectedNote()
        {
            if (filteredNotes.Count > 0 && noteListView.SelectedItem >= 0)
            {
                selectedNote = filteredNotes[noteListView.SelectedItem];
                editor.Text = selectedNote.Content;
                editor.ReadOnly = false;
                editorFrame.Title = $"Editing: {selectedNote.Title}";
            }
            else
            {
                selectedNote = null;
                editor.Text = "";
                editor.ReadOnly = true;
                editorFrame.Title = "No Note Selected";
            }
        }

        static void SaveCurrentProgress() { if (selectedNote != null) selectedNote.Content = editor.Text.ToString(); }

        static void DeleteNote()
        {
            if (selectedNote != null && MessageBox.Query("Delete", $"Delete '{selectedNote.Title}'?", "Yes", "No") == 0)
            {
                allNotes.Remove(selectedNote);
                SaveToDisk();
                UpdateListView();
            }
        }

        static void UpdateListView()
        {
            var search = searchField.Text.ToString();
            filteredNotes = allNotes.Where(n => n.Title.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
            noteListView.SetSource(filteredNotes);
        }

        static void SetTheme(ColorScheme scheme)
        {
            Colors.Base = scheme;
            void Apply(View view)
            {
                view.ColorScheme = scheme;
                foreach (var sub in view.Subviews) Apply(sub);
            }

            if (Application.Top != null)
            {
                Apply(Application.Top);

                Application.Refresh();
            }
        }
    }
}