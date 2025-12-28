using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MemoHanzi
{
    public partial class fMain : Form
    {
        private readonly string rootPath = Path.Combine(Application.StartupPath, "Data");

        private ContextMenuStrip ctxMenu;

        public fMain()
        {
            InitializeComponent();

            LoadTreeView();
            InitializeContextMenu();
        }

        private void LoadTreeView()
        {
            treeView1.Nodes.Clear();
            DirectoryInfo rootDir = new DirectoryInfo(rootPath);
            TreeNode rootNode = new TreeNode("Data");
            rootNode.Tag = rootDir.FullName;
            treeView1.Nodes.Add(rootNode);

            LoadDirectory(rootDir, rootNode);
            rootNode.Expand();
        }

        private void LoadDirectory(DirectoryInfo dir, TreeNode parentNode)
        {
            try
            {
                foreach (DirectoryInfo subDir in dir.GetDirectories())
                {
                    TreeNode dirNode = new TreeNode(subDir.Name);
                    dirNode.Tag = subDir.FullName;
                    parentNode.Nodes.Add(dirNode);
                    LoadDirectory(subDir, dirNode);
                }

                foreach (FileInfo file in dir.GetFiles("*.txt"))
                {
                    TreeNode fileNode = new TreeNode(file.Name);
                    fileNode.Tag = file.FullName;
                    parentNode.Nodes.Add(fileNode);
                }
            }
            catch { }
        }

        private void InitializeContextMenu()
        {
            ctxMenu = new ContextMenuStrip();

            ToolStripMenuItem itemAddFile = new ToolStripMenuItem("Thêm File mới");
            ToolStripMenuItem itemAddFolder = new ToolStripMenuItem("Thêm Thư mục");
            ToolStripMenuItem itemDelete = new ToolStripMenuItem("Xoá");
            ToolStripMenuItem itemRename = new ToolStripMenuItem("Đổi tên");

            itemAddFile.Click += MenuAddFile_Click;
            itemAddFolder.Click += MenuAddFolder_Click;
            itemDelete.Click += MenuDelete_Click;
            itemRename.Click += MenuRename_Click;

            ctxMenu.Items.AddRange(new ToolStripItem[] {
                itemAddFile,
                itemAddFolder,
                itemRename,
                itemDelete
            });

            treeView1.ContextMenuStrip = ctxMenu;
        }

        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                treeView1.SelectedNode = e.Node;

                string path = e.Node.Tag?.ToString();

                if (path != null && File.Exists(path))
                {
                    ctxMenu.Items[0].Visible = false;
                    ctxMenu.Items[1].Visible = false;
                    ctxMenu.Items[2].Visible = true;
                    ctxMenu.Items[3].Visible = true;
                }
                else
                {
                    if (e.Node.Parent == null)
                    {
                        ctxMenu.Items[0].Visible = true;
                        ctxMenu.Items[1].Visible = true;

                        ctxMenu.Items[2].Visible = false;
                        ctxMenu.Items[3].Visible = false;
                    }
                    else
                    {
                        ctxMenu.Items[0].Visible = true;
                        ctxMenu.Items[1].Visible = true;
                        ctxMenu.Items[2].Visible = true;
                        ctxMenu.Items[3].Visible = true;
                    }
                }
            }
        }

        private void MenuAddFile_Click(object sender, EventArgs e)
        {
            TreeNode selectedNode = treeView1.SelectedNode;
            if (selectedNode == null) return;

            string parentPath = selectedNode.Tag.ToString();

            if (File.Exists(parentPath)) parentPath = Path.GetDirectoryName(parentPath);

            string baseName = "NewFile";
            string newFilePath = Path.Combine(parentPath, baseName + ".txt");
            int counter = 1;
            while (File.Exists(newFilePath))
            {
                newFilePath = Path.Combine(parentPath, $"{baseName}_{counter}.txt");
                counter++;
            }

            File.WriteAllText(newFilePath, "");

            TreeNode newNode = new TreeNode(Path.GetFileName(newFilePath));
            newNode.Tag = newFilePath;
            selectedNode.Nodes.Add(newNode);
            if (!selectedNode.IsExpanded) selectedNode.Expand();
        }

        private void MenuAddFolder_Click(object sender, EventArgs e)
        {
            TreeNode selectedNode = treeView1.SelectedNode;
            if (selectedNode == null) return;

            string parentPath = selectedNode.Tag.ToString();

            if (File.Exists(parentPath)) parentPath = Path.GetDirectoryName(parentPath);

            string baseName = "NewFolder";
            string newFolderPath = Path.Combine(parentPath, baseName);
            int counter = 1;
            while (Directory.Exists(newFolderPath))
            {
                newFolderPath = Path.Combine(parentPath, $"{baseName}_{counter}");
                counter++;
            }

            Directory.CreateDirectory(newFolderPath);

            TreeNode newNode = new TreeNode(Path.GetFileName(newFolderPath));
            newNode.Tag = newFolderPath;
            selectedNode.Nodes.Add(newNode);
            if (!selectedNode.IsExpanded) selectedNode.Expand();
        }

        private void MenuDelete_Click(object sender, EventArgs e)
        {
            TreeNode selectedNode = treeView1.SelectedNode;
            if (selectedNode == null) return;

            if (selectedNode.Parent == null)
            {
                MessageBox.Show("Không được phép xoá thư mục gốc!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string path = selectedNode.Tag.ToString();
            var confirmResult = MessageBox.Show($"Bạn có chắc muốn xoá '{selectedNode.Text}'?",
                                     "Xác nhận xoá",
                                     MessageBoxButtons.YesNo);

            if (confirmResult == DialogResult.Yes)
            {
                try
                {
                    if (File.Exists(path)) File.Delete(path);
                    else if (Directory.Exists(path)) Directory.Delete(path, true);

                    selectedNode.Remove();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi không xoá được: " + ex.Message);
                }
            }
        }

        private void MenuRename_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode != null)
            {
                if (treeView1.SelectedNode.Parent == null)
                {
                    MessageBox.Show("Không được phép đổi tên thư mục gốc!");
                    return;
                }

                treeView1.LabelEdit = true;
                treeView1.SelectedNode.BeginEdit();
            }
        }

        private void treeView1_AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            if (e.Node.Parent == null)
            {
                e.CancelEdit = true;
                return;
            }

            if (e.Label == null || e.Label.Trim() == "")
            {
                e.CancelEdit = true;
                return;
            }

            TreeNode node = e.Node;
            string oldPath = node.Tag.ToString();
            string parentDir = Path.GetDirectoryName(oldPath);
            string newName = e.Label;
            string newPath = Path.Combine(parentDir, newName);

            try
            {
                if (File.Exists(oldPath))
                {
                    if (!newPath.EndsWith(".txt")) newPath += ".txt";

                    File.Move(oldPath, newPath);
                    node.Tag = newPath;
                }
                else if (Directory.Exists(oldPath))
                {
                    Directory.Move(oldPath, newPath);
                    node.Tag = newPath;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi đổi tên: " + ex.Message);
                e.CancelEdit = true;
            }
        }
    }
}