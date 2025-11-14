using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;

// Asegúrate de que este 'namespace' coincide con el tuyo
namespace GeneracionInterfazXML
{
    public partial class Form1 : Form
    {
        private DataGridViewRow FilaEnEdicion = null;

        public Form1()
        {
            InitializeComponent();
            this.Load += new EventHandler(this.Form1_Load);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.BackColor = Color.FromArgb(220, 220, 220);
            this.Text = "Gestor de Datos Dinámico";

            // Redimensionamos el formulario para que quepa el buscador
            this.Size = new Size(320, 420); // Ancho, Alto

            GenerarControlesDesdeXML();
            CargarDatosExternos();
        }

        private void GenerarControlesDesdeXML()
        {
            try
            {
                string rutaXml = Path.Combine(Application.StartupPath, "Interfaz.xml");
                if (!File.Exists(rutaXml))
                {
                    MessageBox.Show("Error: No se encontró el archivo Interfaz.xml...", "Error de Carga", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                XDocument doc = XDocument.Load(rutaXml);

                var controlesRaiz = doc.Root.Element("Controles").Elements("Control");
                foreach (var nodoControl in controlesRaiz)
                {
                    ProcesarControlRecursivo(nodoControl, this);
                }

                var menuNode = doc.Root.Element("Menu");
                if (menuNode != null)
                {
                    MenuStrip menuPrincipal = new MenuStrip();
                    menuPrincipal.Dock = DockStyle.Top;
                    menuPrincipal.BackColor = Color.WhiteSmoke;
                    menuPrincipal.ForeColor = Color.Black;

                    foreach (var itemNode in menuNode.Elements("MenuItem"))
                    {
                        ProcesarMenuItem(itemNode, menuPrincipal.Items);
                    }
                    this.Controls.Add(menuPrincipal);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al procesar Interfaz.xml: " + ex.Message, "Error de Parseo", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Procesa un control y todos sus hijos de forma recursiva.
        /// (AHORA INCLUYE LÓGICA DE ID READONLY y BUSCADOR)
        /// </summary>
        private void ProcesarControlRecursivo(XElement nodoControl, Control contenedorPadre)
        {
            string tipo = nodoControl.Attribute("tipo").Value;
            string nombre = nodoControl.Attribute("nombre").Value;
            string texto = nodoControl.Attribute("texto")?.Value;
            string[] pos = nodoControl.Attribute("posicion").Value.Split(',');
            string[] tam = nodoControl.Attribute("tamano").Value.Split(',');

            Point posicion = new Point(int.Parse(pos[0]), int.Parse(pos[1]));
            Size tamano = new Size(int.Parse(tam[0]), int.Parse(tam[1]));

            Control controlGenerado = null;

            switch (tipo)
            {
                case "Label":
                    Label lbl = new Label();
                    lbl.Text = texto;
                    lbl.ForeColor = Color.Black;
                    lbl.BackColor = Color.Transparent;
                    controlGenerado = lbl;
                    break;

                case "Button":
                    Button btn = new Button();
                    btn.Text = texto;
                    btn.Click += new EventHandler(DynamicButton_Click);
                    btn.BackColor = Color.Firebrick;
                    btn.ForeColor = Color.White;
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.FlatAppearance.BorderSize = 0;
                    controlGenerado = btn;
                    break;

                case "TextBox":
                    TextBox txt = new TextBox();
                    txt.Text = texto;
                    txt.BackColor = Color.White;
                    txt.ForeColor = Color.Black;
                    txt.BorderStyle = BorderStyle.FixedSingle;
                    controlGenerado = txt;

                    // --- ¡NUEVO! Conectar evento para el buscador ---
                    if (nombre == "txtBuscar")
                    {
                        txt.TextChanged += new EventHandler(this.DynamicSearch_TextChanged);
                    }
                    break;

                case "GroupBox":
                    GroupBox gb = new GroupBox();
                    gb.Text = texto;
                    gb.ForeColor = Color.Black;
                    gb.BackColor = Color.Transparent;
                    controlGenerado = gb;
                    break;

                case "DataGridView":
                    DataGridView dgv = new DataGridView();
                    dgv.AllowUserToAddRows = false;
                    dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                    dgv.CellDoubleClick += new DataGridViewCellEventHandler(this.DynamicGrid_DoubleClick);
                    dgv.BackgroundColor = Color.White;
                    dgv.GridColor = Color.LightGray;
                    dgv.BorderStyle = BorderStyle.None;
                    dgv.EnableHeadersVisualStyles = false;
                    dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(64, 64, 64);
                    dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                    dgv.ColumnHeadersDefaultCellStyle.Font = new Font(dgv.Font, FontStyle.Bold);
                    dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
                    dgv.DefaultCellStyle.BackColor = Color.White;
                    dgv.DefaultCellStyle.ForeColor = Color.Black;
                    dgv.DefaultCellStyle.SelectionBackColor = Color.Firebrick;
                    dgv.DefaultCellStyle.SelectionForeColor = Color.White;
                    dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);

                    var columnas = nodoControl.Element("Columnas")?.Elements("Columna");
                    if (columnas != null)
                    {
                        foreach (var colNode in columnas)
                        {
                            string colNombre = colNode.Attribute("nombre").Value;
                            string colHeader = colNode.Attribute("header").Value;
                            dgv.Columns.Add(colNombre, colHeader);

                            if (int.TryParse(colNode.Attribute("ancho")?.Value, out int ancho))
                            {
                                dgv.Columns[colNombre].Width = ancho;
                            }

                            // --- ¡NUEVO! Hacer columna ID no editable ---
                            if (colNombre == "colId")
                            {
                                dgv.Columns[colNombre].ReadOnly = true;
                                // Estilo visual para que parezca "deshabilitada"
                                dgv.Columns[colNombre].DefaultCellStyle.BackColor = Color.FromArgb(235, 235, 235);
                                dgv.Columns[colNombre].DefaultCellStyle.ForeColor = Color.Gray;
                            }
                        }
                    }
                    controlGenerado = dgv;
                    break;
            }

            if (controlGenerado != null)
            {
                controlGenerado.Name = nombre;
                controlGenerado.Location = posicion;
                controlGenerado.Size = tamano;

                contenedorPadre.Controls.Add(controlGenerado);

                var controlesHijos = nodoControl.Elements("Control");
                foreach (var nodoHijo in controlesHijos)
                {
                    ProcesarControlRecursivo(nodoHijo, controlGenerado);
                }
            }
        }

        /// <summary>
        /// Lee Datos.xml y rellena los controles existentes.
        /// </summary>
        private void CargarDatosExternos()
        {
            try
            {
                string rutaDatos = Path.Combine(Application.StartupPath, "Datos.xml");
                if (!File.Exists(rutaDatos)) return;

                Control[] gridArr = this.Controls.Find("gridDemo", true);
                if (gridArr.Length == 0) return;

                DataGridView dgv = gridArr[0] as DataGridView;
                if (dgv == null) return;

                XDocument docDatos = XDocument.Load(rutaDatos);

                var filas = docDatos.Root.Elements("Fila");
                foreach (var filaNode in filas)
                {
                    object[] celdas = filaNode.Elements("Celda").Select(c => (object)c.Value).ToArray();
                    dgv.Rows.Add(celdas);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar Datos.xml: " + ex.Message, "Error de Datos", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Procesa recursivamente los items del menú.
        /// </summary>
        private void ProcesarMenuItem(XElement itemNode, ToolStripItemCollection parentCollection)
        {
            string texto = itemNode.Attribute("texto").Value;

            if (texto == "-")
            {
                parentCollection.Add(new ToolStripSeparator());
                return;
            }

            ToolStripMenuItem menuItem = new ToolStripMenuItem(texto);
            menuItem.Name = itemNode.Attribute("nombre")?.Value;

            if (menuItem.Name != null)
            {
                menuItem.Click += new EventHandler(DynamicMenuItem_Click);
            }

            parentCollection.Add(menuItem);

            foreach (var subItemNode in itemNode.Elements("MenuItem"))
            {
                ProcesarMenuItem(subItemNode, menuItem.DropDownItems);
            }
        }

        // ==================================================================
        // ===== NUEVAS FUNCIONES PARA EL BUSCADOR =====
        // ==================================================================

        /// <summary>
        /// Se dispara CADA VEZ que el texto en 'txtBuscar' cambia.
        /// </summary>
        private void DynamicSearch_TextChanged(object sender, EventArgs e)
        {
            TextBox txtBuscar = sender as TextBox;
            if (txtBuscar == null) return;

            Control[] gridArr = this.Controls.Find("gridDemo", true);
            if (gridArr.Length == 0) return;

            DataGridView dgv = gridArr[0] as DataGridView;
            if (dgv == null) return;

            // Llamamos a la función que hace la magia de filtrar
            FiltrarTabla(dgv, txtBuscar.Text);
        }

        /// <summary>
        /// Oculta o muestra filas en el DataGridView según un término de búsqueda.
        /// </summary>
        private void FiltrarTabla(DataGridView dgv, string termino)
        {
            string terminoBusqueda = termino.ToLower().Trim();

            // Asegurarse de que no se modifiquen las filas mientras se filtran
            dgv.SuspendLayout();

            foreach (DataGridViewRow row in dgv.Rows)
            {
                // Ignorar la fila "nueva" al final de la tabla
                if (row.IsNewRow) continue;

                // Si no hay término de búsqueda, mostrar todas las filas
                if (string.IsNullOrWhiteSpace(terminoBusqueda))
                {
                    row.Visible = true;
                    continue;
                }

                bool encontrado = false;
                // Recorremos las celdas (ignorando la celda 0, que es el ID)
                for (int i = 1; i < row.Cells.Count; i++)
                {
                    if (row.Cells[i].Value != null &&
                        row.Cells[i].Value.ToString().ToLower().Contains(terminoBusqueda))
                    {
                        encontrado = true;
                        break; // Si se encuentra, no hace falta seguir buscando en esta fila
                    }
                }

                // Si 'encontrado' es true, la fila es visible. Si es false, se oculta.
                row.Visible = encontrado;
            }

            dgv.ResumeLayout(); // Volver a dibujar la tabla
        }


        // ==================================================================
        // ===== LÓGICA DE BOTONES Y DOBLE CLIC (Igual que antes) =====
        // ==================================================================

        /// <summary>
        /// Se dispara al hacer doble clic en una celda de la tabla.
        /// </summary>
        private void DynamicGrid_DoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            Control[] txtNombreArr = this.Controls.Find("txtNombre", true);
            Control[] txtEmailArr = this.Controls.Find("txtEmail", true);
            if (txtNombreArr.Length == 0 || txtEmailArr.Length == 0) return;

            TextBox txtNombre = txtNombreArr[0] as TextBox;
            TextBox txtEmail = txtEmailArr[0] as TextBox;
            DataGridView dgv = sender as DataGridView;

            FilaEnEdicion = dgv.Rows[e.RowIndex];
            txtNombre.Text = FilaEnEdicion.Cells[1].Value.ToString();
            txtEmail.Text = FilaEnEdicion.Cells[2].Value.ToString();

            Control[] btnAnadirArr = this.Controls.Find("btnAnadir", true);
            if (btnAnadirArr.Length > 0)
            {
                Button btnAnadir = btnAnadirArr[0] as Button;
                btnAnadir.Text = "Actualizar";
                btnAnadir.Name = "btnActualizar";
                btnAnadir.BackColor = Color.DarkGreen;
            }
        }

        /// <summary>
        /// Manejador de eventos para TODOS los botones (CON FUNCIONALIDAD).
        /// </summary>
        private void DynamicButton_Click(object sender, EventArgs e)
        {
            Button botonPresionado = sender as Button;
            if (botonPresionado == null) return;

            Control[] txtNombreArr = this.Controls.Find("txtNombre", true);
            Control[] txtEmailArr = this.Controls.Find("txtEmail", true);
            Control[] gridArr = this.Controls.Find("gridDemo", true);

            TextBox txtNombre = (txtNombreArr.Length > 0) ? txtNombreArr[0] as TextBox : null;
            TextBox txtEmail = (txtEmailArr.Length > 0) ? txtEmailArr[0] as TextBox : null;
            DataGridView dgv = (gridArr.Length > 0) ? gridArr[0] as DataGridView : null;

            switch (botonPresionado.Name)
            {
                case "btnAnadir":
                    if (txtNombre != null && txtEmail != null && dgv != null)
                    {
                        if (string.IsNullOrWhiteSpace(txtNombre.Text))
                        {
                            MessageBox.Show("El campo 'Nombre' no puede estar vacío.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        int nuevoId = dgv.Rows.Count + 1;
                        dgv.Rows.Add(nuevoId.ToString(), txtNombre.Text, txtEmail.Text);

                        txtNombre.Text = "";
                        txtEmail.Text = "";
                        MessageBox.Show("Registro añadido con éxito.", "Añadir", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    break;

                case "btnActualizar":
                    if (txtNombre != null && txtEmail != null && FilaEnEdicion != null)
                    {
                        FilaEnEdicion.Cells[1].Value = txtNombre.Text;
                        FilaEnEdicion.Cells[2].Value = txtEmail.Text;
                        txtNombre.Text = "";
                        txtEmail.Text = "";
                        botonPresionado.Text = "Añadir";
                        botonPresionado.Name = "btnAnadir";
                        botonPresionado.BackColor = Color.Firebrick;
                        FilaEnEdicion = null;
                        MessageBox.Show("Registro actualizado con éxito.", "Actualizar", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    break;

                case "btnEditar":
                    MessageBox.Show(
                        "Para editar un registro:\n\n1. Haz doble clic sobre él en la tabla.\n2. Los datos se cargarán en los campos de arriba.\n3. Modifica los datos.\n4. Pulsa el botón 'Actualizar' (reemplaza a 'Añadir').",
                        "Guía de Edición",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    break;

                case "btnEliminar":
                    if (dgv != null)
                    {
                        if (dgv.SelectedRows.Count > 0)
                        {
                            var confirmacion = MessageBox.Show(
                                "¿Estás seguro de que deseas eliminar la fila seleccionada?",
                                "Confirmar Eliminación",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Warning);

                            if (confirmacion == DialogResult.Yes)
                            {
                                dgv.Rows.Remove(dgv.SelectedRows[0]);
                                MessageBox.Show("Registro eliminado.", "Eliminar", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                        else
                        {
                            MessageBox.Show("Por favor, selecciona una fila completa para eliminar.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Manejador de eventos para TODOS los items del menú.
        /// </summary>
        private void DynamicMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;
            if (item != null)
            {
                MessageBox.Show($"Has hecho clic en el menú: {item.Text}");

                if (item.Name == "menuSalir")
                {
                    this.Close();
                }
            }
        }
    }
}
