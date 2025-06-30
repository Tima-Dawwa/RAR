using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RAR.UI
{
    public class DragWindowLogic
    {
        private bool isDragging = false;
        private Point lastCursor;
        private Point lastForm;

        public void MainForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                lastCursor = Cursor.Position;
                lastForm = (sender as Control).FindForm().Location;
            }
        }

        public void MainForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                Point diff = Point.Subtract(Cursor.Position, new Size(lastCursor));
                (sender as Control).FindForm().Location = Point.Add(lastForm, new Size(diff));
            }
        }

        public void MainForm_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
        }
    }

}
