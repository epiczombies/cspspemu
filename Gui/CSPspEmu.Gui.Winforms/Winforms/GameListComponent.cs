﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using BrightIdeasSoftware;
using CSharpUtils;

namespace CSPspEmu.Gui.Winforms
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="http://www.codeproject.com/Articles/16009/A-Much-Easier-to-Use-ListView"/>
    public partial class GameListComponent : UserControl
    {
        public GameListComponent()
        {
            InitializeComponent();
        }

        public class Entry
        {
            public string Title;
            public string Title2;

            public string GetTitle()
            {
                return Title;
            }
        }

        TypedObjectListView<GameList.GameEntry> TypedObjectListViewEntry;

        OLVColumn BannerColumn = new OLVColumn("Banner", "");
        OLVColumn DiscIdColumn = new OLVColumn("Id", "");
        OLVColumn TitleColumn = new OLVColumn("Title", "");
        OLVColumn FirmwareColumn = new OLVColumn("Firmware", "");
        OLVColumn PathColumn = new OLVColumn("Path", "");
        OLVColumn RegionColumn = new OLVColumn("Region", "");
        OLVColumn MediaTypeColumn = new OLVColumn("Media", "");
        OLVColumn LicenseTypeColumn = new OLVColumn("License", "");
        OLVColumn ReleaseTypeColumn = new OLVColumn("Release", "");

        /*
        class MyFilter : IModelFilter
        {
            public bool Filter(object modelObject)
            {
                var Entry = (GameList.GameEntry)modelObject;
                return true;
            }
        }
        */

        Font Font2 = new Font("MS Gothic Normal", 13);
        Font Font3 = new Font("MS Gothic Normal", 13, FontStyle.Strikeout);

        private void GameListForm_Load(object sender, EventArgs e)
        {
            TypedObjectListViewEntry = new TypedObjectListView<GameList.GameEntry>(GameListView);
            GameListView.ShowGroups = false;
            GameListView.FullRowSelect = true;
            DiscIdColumn.TextAlign = HorizontalAlignment.Center;
            FirmwareColumn.TextAlign = HorizontalAlignment.Center;

            GameListView.StateImageList = new ImageList();
            var img = new Bitmap(640, 120);
            var g = Graphics.FromImage(img);
            g.DrawLine(new Pen(new SolidBrush(Color.Red)), new Point(0, 0), new Point(100, 100));
            GameListView.StateImageList.Images.Add("test", img);
            GameListView.OwnerDraw = true;

            //var IconSize = new Size(144, 80);
            var IconSize = new Size(108, 60);

            GameListView.RowHeight = IconSize.Height;
            //objectListView1.AllowColumnReorder = true;
            //objectListView1.AutoResizeColumns();

            GameListView.Resize += objectListView1_Resize;
            ResetColumns();
            GameListView.GridLines = true;

            GameListView.Sort(TitleColumn, SortOrder.Ascending);

            //TitleColumn.HeaderFont = new Font("MS Gothic Normal", 16);
            TitleColumn.RendererDelegate = (ee, gg, rr, oo) =>
            {
                try
                {
                    var Entry = ((GameList.GameEntry) oo);
                    var Selected = (GameListView.SelectedObjects.Contains((object) Entry));
                    gg.FillRectangle(new SolidBrush(!Selected ? SystemColors.Window : SystemColors.Highlight),
                        new Rectangle(rr.Left - 1, rr.Top - 1, rr.Width + 1, rr.Height + 1));

                    var Text = Entry.TITLE;
                    Font Font;

                    if (Entry.PatchedWithPrometheus)
                    {
                        Text = Text + " *PATCHED*";
                        Font = Font3;
                    }
                    else
                    {
                        Font = Font2;
                    }

                    var Measure = gg.MeasureString(Text, Font, new Size(rr.Width, rr.Height));
                    gg.Clip = new System.Drawing.Region(rr);
                    gg.DrawString(
                        Text,
                        Font,
                        new SolidBrush(!Selected ? SystemColors.WindowText : SystemColors.HighlightText),
                        new Rectangle(
                            new Point(rr.Left + 8, (int) (rr.Top + rr.Height / 2 - Measure.Height / 2)),
                            new Size(rr.Width, rr.Height)
                        )
                    );
                    //gg.FillRectangle(new SolidBrush(Color.White), rr);
                    //gg.DrawImageUnscaled(Entry.CachedBitmap, new Point(rr.Left, rr.Top));
                }
                catch
                {
                }
                return true;
            };

            BannerColumn.MaximumWidth = BannerColumn.Width = BannerColumn.MinimumWidth = IconSize.Width;
            //BannerColumn.AspectGetter = delegate(object _entry) { return null; };
            BannerColumn.RendererDelegate = (ee, gg, rr, oo) =>
            {
                try
                {
                    var Entry = ((GameList.GameEntry) oo);
                    var Data = Entry.Icon0Png;
                    if (Entry.CachedBitmap == null)
                    {
                        Entry.CachedBitmap = new Bitmap(IconSize.Width, IconSize.Height);
                        using (var gg2 = Graphics.FromImage(Entry.CachedBitmap))
                        {
                            var IconToBlit = Image.FromStream(new MemoryStream(Data));

                            var TempBuffer = new Bitmap(144, 80);
                            using (var gg3 = Graphics.FromImage(TempBuffer))
                            {
                                gg3.CompositingQuality = CompositingQuality.HighQuality;
                                gg3.Clear(Color.Transparent);
                                gg3.DrawImage(IconToBlit,
                                    new Rectangle(TempBuffer.Width / 2 - IconToBlit.Width / 2, 0, IconToBlit.Width,
                                        IconToBlit.Height));
                            }

                            //Console.WriteLine("{0}x{1}", IconToBlit.Width, IconToBlit.Height);

                            gg2.CompositingQuality = CompositingQuality.HighQuality;
                            if (Entry.PatchedWithPrometheus)
                            {
                                gg2.Clear(Color.Red);
                            }
                            else
                            {
                                gg2.Clear(Color.White);
                            }
                            gg2.DrawImage(TempBuffer, new Rectangle(0, 0, IconSize.Width, IconSize.Height));
                            if (Entry.PatchedWithPrometheus)
                            {
                                gg2.DrawLine(new Pen(Color.Red), new Point(0, 0),
                                    new Point(IconSize.Width, IconSize.Height));
                                gg2.DrawLine(new Pen(Color.Red), new Point(IconSize.Width, 0),
                                    new Point(0, IconSize.Height));
                            }
                        }
                    }

                    gg.FillRectangle(new SolidBrush(Entry.PatchedWithPrometheus ? Color.Red : Color.White),
                        new Rectangle(rr.Left - 1, rr.Top - 1, rr.Width + 1, rr.Height + 1));
                    gg.DrawImageUnscaled(Entry.CachedBitmap, new Point(rr.Left - 1, rr.Top - 1));
                }
                catch
                {
                }

                return true;
            };

            TitleColumn.AspectGetter = delegate(object _entry)
            {
                try
                {
                    return ((GameList.GameEntry) _entry).TITLE;
                }
                catch
                {
                    return "*ERROR*";
                }
            };
            DiscIdColumn.AspectGetter = delegate(object _entry)
            {
                try
                {
                    return ((GameList.GameEntry) _entry).DiscId0;
                }
                catch
                {
                    return "*ERROR*";
                }
            };
            PathColumn.AspectGetter = delegate(object _entry)
            {
                try
                {
                    return Path.GetFileName(((GameList.GameEntry) _entry).IsoFile);
                }
                catch
                {
                    return "*ERROR*";
                }
            };
            MediaTypeColumn.AspectGetter = delegate(object _entry)
            {
                try
                {
                    var Entry = ((GameList.GameEntry) _entry);

                    var DISC_ID = Entry.DISC_ID;
                    switch (DISC_ID[0])
                    {
                        case 'S': return "CD/DVD";
                        case 'U': return "UMD";
                        case 'B': return "BluRay";
                        case 'N': return "PSN";
                        default: return "Unknown";
                    }
                }
                catch
                {
                    return "*ERROR*";
                }
            };
            LicenseTypeColumn.AspectGetter = delegate(object _entry)
            {
                try
                {
                    var Entry = ((GameList.GameEntry) _entry);

                    var DISC_ID = Entry.DISC_ID;
                    switch (DISC_ID[1])
                    {
                        case 'C': return "Sony";
                        case 'P': return "PSN";
                        case 'L': return "Other";
                        default: return "Unknown";
                    }
                }
                catch
                {
                    return "*ERROR*";
                }
            };
            RegionColumn.AspectGetter = delegate(object _entry)
            {
                try
                {
                    var Entry = ((GameList.GameEntry) _entry);

                    var DISC_ID = Entry.DISC_ID;
                    switch (DISC_ID[2])
                    {
                        case 'P':
                        case 'J': return "Japan";
                        case 'E': return "Europe";
                        case 'K': return "Korea";
                        case 'U': return "USA";
                        case 'A': return "Asia";
                        default: return "Unknown";
                    }
                }
                catch
                {
                    return "*ERROR*";
                }
            };
            ReleaseTypeColumn.AspectGetter = delegate(object _entry)
            {
                try
                {
                    var Entry = ((GameList.GameEntry) _entry);

                    var DISC_ID = Entry.DISC_ID;
                    switch (DISC_ID[3])
                    {
                        case 'D': return "Demo";
                        case 'M': return "Malaysian";
                        case 'S': return "Retail";
                        default: return "Unknown";
                    }
                }
                catch
                {
                    return "*ERROR*";
                }
            };

            FirmwareColumn.AspectGetter = delegate(object _entry)
            {
                try
                {
                    return ((GameList.GameEntry) _entry).PSP_SYSTEM_VER;
                }
                catch
                {
                    return "*ERROR*";
                }
            };
        }

        public int GetColumnsWidth(params ColumnSize[] Except)
        {
            int Width = 0;

            var Columns = new[]
            {
                BannerColumnSize, DiscIdColumnSize, TitleColumnSize, FirmwareColumnSize, RegionColumnSize,
                PathColumnSize
            };

            foreach (var Column in Columns)
            {
                if (!Except.Contains(Column))
                {
                    Width += Column.Width;
                }
            }
            return Width;
        }

        void ResetColumns()
        {
            ResetColumnsWidths();
            UpdateColumnsWidths();

            GameListView.Columns.Clear();
            GameListView.Columns.Add(BannerColumn);
            GameListView.Columns.Add(DiscIdColumn);
            GameListView.Columns.Add(TitleColumn);
            GameListView.Columns.Add(FirmwareColumn);
            GameListView.Columns.Add(RegionColumn);
            GameListView.Columns.Add(MediaTypeColumn);
            //objectListView1.Columns.Add(LicenseTypeColumn);
            //objectListView1.Columns.Add(ReleaseTypeColumn);
            GameListView.Columns.Add(PathColumn);
        }

        ColumnSize BannerColumnSize = new ColumnSize();
        ColumnSize DiscIdColumnSize = new ColumnSize();
        ColumnSize TitleColumnSize = new ColumnSize();
        ColumnSize FirmwareColumnSize = new ColumnSize();
        ColumnSize RegionColumnSize = new ColumnSize();
        ColumnSize PathColumnSize = new ColumnSize();

        public class ColumnSize
        {
            public int MinimumWidth = 0;
            public int MaximumWidth = 1024;
            private int _Width = 120;

            public int Width
            {
                get => _Width;
                set => _Width = MathUtils.Clamp(value, MinimumWidth, MaximumWidth);
            }
        }

        void ResetColumnsWidths()
        {
            DiscIdColumnSize.MaximumWidth = DiscIdColumnSize.MinimumWidth = DiscIdColumnSize.Width = 80;
            FirmwareColumnSize.MaximumWidth = FirmwareColumnSize.MinimumWidth = FirmwareColumnSize.Width = 60;
            PathColumnSize.Width = 120;
            TitleColumnSize.Width = 400;
            TitleColumnSize.MaximumWidth = 420;
            TitleColumnSize.MinimumWidth = 120;
            RegionColumnSize.Width = 60;
        }

        static void UpdateColumnWidths(ColumnSize ColumnSize, OLVColumn Column)
        {
            if (Column.MinimumWidth != ColumnSize.MinimumWidth) Column.MinimumWidth = ColumnSize.MinimumWidth;
            if (Column.MaximumWidth != ColumnSize.MaximumWidth) Column.MaximumWidth = ColumnSize.MaximumWidth;
            if (Column.Width != ColumnSize.Width) Column.Width = ColumnSize.Width;
        }

        void UpdateColumnsWidths()
        {
            UpdateColumnWidths(BannerColumnSize, BannerColumn);
            UpdateColumnWidths(DiscIdColumnSize, DiscIdColumn);
            UpdateColumnWidths(TitleColumnSize, TitleColumn);
            UpdateColumnWidths(FirmwareColumnSize, FirmwareColumn);
            UpdateColumnWidths(RegionColumnSize, RegionColumn);
            UpdateColumnWidths(PathColumnSize, PathColumn);
        }

        void objectListView1_Resize(object sender, EventArgs e)
        {
            ResetColumnsWidths();

            bool Updated = false;

            //ResetColumns();
            var InitialWidth = GameListView.Width - 32;
            var RestColumnWidth = GetColumnsWidth(TitleColumnSize);
            TitleColumnSize.Width = InitialWidth - RestColumnWidth;

            /*
            if (GetColumnsWidth() > InitialWidth)
            {
                PathColumn.Width = PathColumn.MinimumWidth = 0;
            }
            */

            UpdateColumnsWidths();
        }

        public void Init(string IsosPath, string CachePath)
        {
            if (IsosPath == null)
            {
                return;
            }
            var ProgressForm = new ProgressForm();
            try
            {
                ThreadPool.QueueUserWorkItem((state) =>
                {
                    var GameList = new GameList();
                    Console.WriteLine("Reading ISOs...");
                    GameList.Progress += (Title, Current, Total) =>
                    {
                        //Console.WriteLine("Progress: {0}, {1}/{2}", Title, Current, Total);
                        ProgressForm.SetProgress(Title, Current, Total);
                    };

                    var List = new List<GameList.GameEntry>();

                    GameList.EntryAdded += (Entry, Cached) =>
                    {
                        //Console.WriteLine("aaaaaaaa");
                        List.Add(Entry);
                        if (!Cached)
                        {
                            GameListView.AddObject(Entry);
                        }
                    };

                    GameList.ScanPath(IsosPath, CachePath);

                    this.Invoke(new Action(() =>
                    {
                        GameListView.SetObjects(List);
                        GameListView.Sort(TitleColumn, SortOrder.Ascending);
                    }));

                    Console.WriteLine("Done");
                    /*
                    foreach (var Entry in GameList.Entries)
                    {
                        Console.WriteLine(Entry.TITLE);
                    }
                    */

                    ProgressForm.End();
                }, null);

                this.Invoke(new Action(() => { ProgressForm.ShowDialog(); }));
            }
            finally
            {
                ProgressForm.End();
                this.Invoke(new Action(() => { FilterTextBox.Focus(); }));
            }
        }

        private void textBox1_Enter(object sender, EventArgs e)
        {
            //Console.WriteLine("aaaaaaaaaa");
            FilterTextBox.SelectAll();
        }

        public event Action<string> SelectedItem;

        private void SelectSelectedItem()
        {
            var Entry = (GameList.GameEntry) GameListView.SelectedObject;
            SelectedItem(Entry.IsoFile);
        }

        private void objectListView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            SelectSelectedItem();
        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {
        }

        private void objectListView1_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            var Text = FilterTextBox.Text;
            if (Text.Length > 0)
            {
                TextMatchFilter filter = new TextMatchFilter(GameListView, Text);
                GameListView.UseFiltering = true;
                GameListView.ModelFilter = filter;
                //objectListView1.DefaultRenderer = new HighlightTextRenderer(filter);
            }
            else
            {
                GameListView.UseFiltering = false;
                GameListView.ModelFilter = null;
                //objectListView1.DefaultRenderer = new BaseRenderer();
            }
            GameListView.FullRowSelect = true;
            GameListView.SelectedIndex = MathUtils.Clamp(GameListView.SelectedIndex, 0, GameListView.Items.Count - 1);
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            //Console.WriteLine("KeyPress: {0}", e);
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            //Console.WriteLine("KeyDown: {0}", e);
            int SelectedIndex = GameListView.SelectedIndex;
            switch (e.KeyCode)
            {
                case Keys.Up:
                case Keys.Down:
                case Keys.Return:
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    switch (e.KeyCode)
                    {
                        case Keys.Up:
                            SelectedIndex--;
                            break;
                        case Keys.Down:
                            SelectedIndex++;
                            break;
                    }
                    break;
            }
            GameListView.FullRowSelect = true;
            GameListView.SelectedIndex = MathUtils.Clamp(SelectedIndex, 0, GameListView.Items.Count - 1);
            if (e.KeyCode == Keys.Return)
            {
                SelectSelectedItem();
            }
        }

        private void objectListView1_KeyPress(object sender, KeyPressEventArgs e)
        {
        }

        private void objectListView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Return)
            {
                SelectSelectedItem();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }
    }
}