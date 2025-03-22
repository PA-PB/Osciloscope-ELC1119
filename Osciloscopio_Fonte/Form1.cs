using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace Osciloscopio
{
    public partial class Form1 : Form
    {
        #region INICIALIZAÇÃO

        public SerialPort PortaArduino;
        public double vref;
        public int taxaBaud = 2000000;
        public int numAmostras = 500;
        public int tempoBufferVarreduraMSec = 50;
        public int TensaoTrigger;
        public bool toggleState = false;

        public double yAxisMax;
        public bool acoplamentoDC;
        public bool clicadoUmaVez = false;

        public bool fimDosDados = false;
        public int duracaoBurstMSec;
        public List<string> listaStringEntradaBruta = new List<string>();
        public double mSecPorAmostra = 0.1;

        public bool debug = false;

        Stopwatch cronometroBurstArduino = new Stopwatch();

        public ProcessadorDadosEntrada processador;

        List<double> valoresX = new List<double>() { 10.0 };
        List<double> valoresY = new List<double>() { 0.0 };

        #endregion
        public Form1()
        {
            InitializeComponent();

            ConfiguracaoUI();

            ConfiguracaoGrafico();

            ConfiguracaoPortaUI();
        }
        #region MÉTODOS

        private void ConfiguracaoUI()
        {
            txtDadosSerial.Text = "8";
            txtVref.Text = "5.2";
            txtAmostras.Text = "500";
            rdoAcoplamentoDC.Checked = true;

            trkTempoVarredura.Value = 10;
            lblTempoVarredura.Text = Convert.ToString(0.05 * Convert.ToDouble(trkTempoVarredura.Value)) + " Seg.";

            trkEscalaVertical.Value = 10;
            lblEscalaVertical.Text = "Escala X" + Convert.ToString(0.1 * Convert.ToDouble(trkEscalaVertical.Value));

            btnDisparo.Enabled = false;
            btnDisparo.Visible = false;
            btnFecharPorta.Enabled = false;
            btnFecharPorta.Visible = false;
        }

        private void ConfiguracaoGrafico()
        {
            chart1.Series["Serie1"].ChartType = SeriesChartType.Line;

            chart1.Series["Serie1"].BorderWidth = 2;
            chart1.Series["Serie1"].Color = Color.Yellow;
            chart1.ChartAreas["AreaGrafico1"].AxisX.Minimum = 0;
            chart1.ChartAreas["AreaGrafico1"].AxisY.Minimum = -5.0;
            chart1.ChartAreas["AreaGrafico1"].AxisY.Interval = 1.0;
            chart1.ChartAreas["AreaGrafico1"].AxisY.Maximum = yAxisMax;
            chart1.ChartAreas["AreaGrafico1"].AxisX.RoundAxisValues();
            chart1.ChartAreas["AreaGrafico1"].AxisX.Title = "mSeg";
            chart1.ChartAreas["AreaGrafico1"].AxisX.TitleForeColor = Color.Aquamarine;
            chart1.ChartAreas["AreaGrafico1"].AxisX.LineColor = Color.Gray;
            chart1.ChartAreas["AreaGrafico1"].AxisY.LineColor = Color.Gray;
            chart1.ChartAreas["AreaGrafico1"].AxisX.MajorGrid.LineColor = Color.Gray;
            chart1.ChartAreas["AreaGrafico1"].AxisX.MajorGrid.LineWidth = 1;
            chart1.ChartAreas["AreaGrafico1"].AxisY.MajorGrid.LineWidth = 1;
            chart1.ChartAreas["AreaGrafico1"].AxisY.MajorGrid.LineColor = Color.Gray;
            chart1.ChartAreas["AreaGrafico1"].AxisX.LabelStyle.ForeColor = Color.Aquamarine;
            chart1.ChartAreas["AreaGrafico1"].AxisY.LabelStyle.ForeColor = Color.Aquamarine;
            chart1.ChartAreas["AreaGrafico1"].BackColor = Color.Black;
            chart1.Legends.Clear();

            chart1.Series["Serie1"].Points.AddXY(0.0, 0.0);

            chart1.Series["Serie1"].Points.DataBindXY(valoresX, valoresY);

        }
        private void ConfiguracaoPortaUI()
        {
            txtCOM.AppendText("As seguintes portas COM ativas foram encontradas:" + Environment.NewLine);
            List<string> listaPortas = ObterPortas();
            foreach (string s in listaPortas)
            {
                txtCOM.AppendText(s + Environment.NewLine);
            }
        }
        private List<string> ObterPortas()
        {
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Caption like '%(COM%'"))
            {
                string[] nomesPortas = SerialPort.GetPortNames();

                var portas = searcher.Get().Cast<ManagementBaseObject>().Select(p => p["Caption"].ToString()).ToList();

                List<string> listaPortas = nomesPortas
                    .Select(n => n + " - " + portas.FirstOrDefault(s => s.Contains(n)) ?? "Desconhecido")
                    .ToList();

                return listaPortas;
            }
        }
        private void AbrirPorta()
        {
            PortaArduino = new SerialPort();
            PortaArduino.BaudRate = taxaBaud;
            PortaArduino.PortName = "COM" + txtDadosSerial.Text;

            try
            {
                PortaArduino.Open();
            }
            catch (Exception)
            {
                MessageBox.Show("Você precisa conectar o dispositivo primeiro");
                return;
            }
            if (PortaArduino.IsOpen)
            {
                btnAbrirPorta.Text = "PORTA ABERTA";
                btnAbrirPorta.ForeColor = Color.Red;
                btnAbrirPorta.BackColor = Color.LightYellow;

                btnAbrirPorta.Enabled = false;
                btnFecharPorta.Enabled = true;
                btnFecharPorta.Visible = true;
                btnDisparo.Enabled = true;
                btnDisparo.Visible = true;
            }
        }
        private void LerBurstArduino()
        {
            int contadorStringsLidas = 1;

            while (fimDosDados == false)
            {
                try
                {
                    string stringEntrada = PortaArduino.ReadLine();

                    if (stringEntrada.TrimEnd('\r', '\n') == "FIM")
                    {
                        fimDosDados = true;
                    }
                    else
                    {
                        listaStringEntradaBruta.Add(stringEntrada);
                    }
                }
                catch
                {
                    MessageBox.Show("Não foi possível ler dados do Arduino");
                }
                if (!fimDosDados) { contadorStringsLidas++; }
            }
            processador = new ProcessadorDadosEntrada(listaStringEntradaBruta);
            ProcessarBurstDados();
        }

        private void ProcessarBurstDados()
        {
            processador.ParsarCSVBurst();

            double intervaloAmostragemMSec =
                Convert.ToDouble(duracaoBurstMSec) / Convert.ToDouble(numAmostras) - 0.0025;

            processador.ZerarTemposBurst(intervaloAmostragemMSec);
            processador.EscalarValoresBurst(1023, vref);

            if (acoplamentoDC == false)
            {
                processador.ObterAcoplamentoCA();
            }

            double maxVolts = processador.ObterMax();
            lblMaxVolts.BringToFront();
            lblMaxVolts.BackColor = Color.White;
            lblMaxVolts.Text = string.Format("Tensão Máx. = {0:0.00}", maxVolts);

            double mediaVolts = processador.ObterMedia();
            lblMediaVolts.BringToFront();
            lblMediaVolts.BackColor = Color.White;
            lblMediaVolts.Text = string.Format("Tensão Média = {0:0.00}", mediaVolts);

            double freq = processador.ObterFreq();
            lblFreq.BringToFront();
            lblFreq.BackColor = Color.White;
            lblFreq.Text = string.Format("Freq. Média = {0:0.00} Hz", freq);

            PlotarBurstDados(processador.arrayDoubleProcessada.Length / 2);

            fimDosDados = false;
            listaStringEntradaBruta.Clear();
        }

        private void PlotarBurstDados(int numAmostras)
        {
            valoresX.Clear();
            valoresY.Clear();

            for (int i = 0; i < numAmostras; i++)
            {
                valoresX.Add(processador.arrayDoubleProcessada[i, 0]);
                valoresY.Add(processador.arrayDoubleProcessada[i, 1]);
            }

            chart1.Invoke(new MethodInvoker(
                delegate
                {
                    chart1.Series["Serie1"].Points.DataBindXY(valoresX, valoresY);
                }
                ));
        }

        private void DrawHorizontalLine(Chart chart, ChartArea chartArea, double yValue)
        {
            // Clear any existing horizontal lines with the specified name
            foreach (StripLine stripLine in chartArea.AxisY.StripLines)
            {
                if (stripLine.Tag?.ToString() == "HorizontalLine")
                {
                    chartArea.AxisY.StripLines.Remove(stripLine);
                    break; // Assuming there's only one strip line with this name
                }
            }

            // Create a new horizontal line
            StripLine horizontalLine = new StripLine();
            horizontalLine.Interval = 0; // Position along the axis
            horizontalLine.StripWidth = 0; // Thickness of the line
            horizontalLine.BorderWidth = 1;
            horizontalLine.BorderColor = Color.Red; // Color of the line
            horizontalLine.BorderDashStyle = ChartDashStyle.Solid; // Style of the line
            horizontalLine.IntervalOffset = yValue; // Y value where the line will be drawn
            horizontalLine.Tag = "HorizontalLine"; // Tag to identify this line

            // Add the line to the chart
            chartArea.AxisY.StripLines.Add(horizontalLine);
        }

        #endregion

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.btnFecharPorta = new System.Windows.Forms.Button();
            this.btnAbrirPorta = new System.Windows.Forms.Button();
            this.btnDisparo = new System.Windows.Forms.Button();
            this.chart1 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.lblFreq = new System.Windows.Forms.Label();
            this.lblMaxVolts = new System.Windows.Forms.Label();
            this.lblTempoVarredura = new System.Windows.Forms.Label();
            this.lblEscalaVertical = new System.Windows.Forms.Label();
            this.trkTempoVarredura = new System.Windows.Forms.TrackBar();
            this.trkEscalaVertical = new System.Windows.Forms.TrackBar();
            this.txtAmostras = new System.Windows.Forms.TextBox();
            this.txtVref = new System.Windows.Forms.TextBox();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.chkAltaVelocidade = new System.Windows.Forms.CheckBox();
            this.chkDebug = new System.Windows.Forms.CheckBox();
            this.chkMarcadores = new System.Windows.Forms.CheckBox();
            this.btnSair = new System.Windows.Forms.Button();
            this.txtCOM = new System.Windows.Forms.TextBox();
            this.txtDadosSerial = new System.Windows.Forms.TextBox();
            this.lblMediaVolts = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.button2 = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.radioButton1 = new System.Windows.Forms.RadioButton();
            this.rdoAcoplamentoDC = new System.Windows.Forms.RadioButton();
            this.btnTrigger = new System.Windows.Forms.Button();
            this.btnFFT = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trkTempoVarredura)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trkEscalaVertical)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnFecharPorta
            // 
            this.btnFecharPorta.Location = new System.Drawing.Point(9, 174);
            this.btnFecharPorta.Name = "btnFecharPorta";
            this.btnFecharPorta.Size = new System.Drawing.Size(107, 23);
            this.btnFecharPorta.TabIndex = 0;
            this.btnFecharPorta.Text = "FECHAR PORTA";
            this.btnFecharPorta.UseVisualStyleBackColor = true;
            this.btnFecharPorta.Click += new System.EventHandler(this.btnFecharPorta_Click_1);
            // 
            // btnAbrirPorta
            // 
            this.btnAbrirPorta.Location = new System.Drawing.Point(9, 145);
            this.btnAbrirPorta.Name = "btnAbrirPorta";
            this.btnAbrirPorta.Size = new System.Drawing.Size(107, 23);
            this.btnAbrirPorta.TabIndex = 1;
            this.btnAbrirPorta.Text = "ABRIR PORTA";
            this.btnAbrirPorta.UseVisualStyleBackColor = true;
            this.btnAbrirPorta.Click += new System.EventHandler(this.button1_Click);
            // 
            // btnDisparo
            // 
            this.btnDisparo.Location = new System.Drawing.Point(190, 174);
            this.btnDisparo.Name = "btnDisparo";
            this.btnDisparo.Size = new System.Drawing.Size(107, 23);
            this.btnDisparo.TabIndex = 2;
            this.btnDisparo.Text = "DISPARO";
            this.btnDisparo.UseVisualStyleBackColor = true;
            this.btnDisparo.Click += new System.EventHandler(this.btnDisparo_Click);
            // 
            // chart1
            // 
            chartArea1.Name = "AreaGrafico1";
            this.chart1.ChartAreas.Add(chartArea1);
            this.chart1.Cursor = System.Windows.Forms.Cursors.Cross;
            this.chart1.IsSoftShadows = false;
            legend1.Name = "Legenda1";
            this.chart1.Legends.Add(legend1);
            this.chart1.Location = new System.Drawing.Point(381, 54);
            this.chart1.Name = "chart1";
            series1.ChartArea = "AreaGrafico1";
            series1.Legend = "Legenda1";
            series1.Name = "Serie1";
            this.chart1.Series.Add(series1);
            this.chart1.Size = new System.Drawing.Size(919, 404);
            this.chart1.TabIndex = 3;
            this.chart1.Text = "chart1";
            this.chart1.TextAntiAliasingQuality = System.Windows.Forms.DataVisualization.Charting.TextAntiAliasingQuality.Normal;
            this.chart1.Click += new System.EventHandler(this.chart1_Click);
            this.chart1.MouseClick += new System.Windows.Forms.MouseEventHandler(this.chart1_MouseClick);
            // 
            // lblFreq
            // 
            this.lblFreq.AutoSize = true;
            this.lblFreq.Location = new System.Drawing.Point(511, 9);
            this.lblFreq.Name = "lblFreq";
            this.lblFreq.Size = new System.Drawing.Size(76, 13);
            this.lblFreq.TabIndex = 5;
            this.lblFreq.Text = "FREQUÊNCIA";
            this.lblFreq.Click += new System.EventHandler(this.lblFreq_Click);
            // 
            // lblMaxVolts
            // 
            this.lblMaxVolts.AutoSize = true;
            this.lblMaxVolts.Location = new System.Drawing.Point(755, 9);
            this.lblMaxVolts.Name = "lblMaxVolts";
            this.lblMaxVolts.Size = new System.Drawing.Size(80, 13);
            this.lblMaxVolts.TabIndex = 6;
            this.lblMaxVolts.Text = "TENSÃO MÁX.";
            this.lblMaxVolts.Click += new System.EventHandler(this.lblMaxVolts_Click);
            // 
            // lblTempoVarredura
            // 
            this.lblTempoVarredura.AutoSize = true;
            this.lblTempoVarredura.Location = new System.Drawing.Point(769, 512);
            this.lblTempoVarredura.Name = "lblTempoVarredura";
            this.lblTempoVarredura.Size = new System.Drawing.Size(96, 13);
            this.lblTempoVarredura.TabIndex = 7;
            this.lblTempoVarredura.Text = "TEMPO VARRED.";
            this.lblTempoVarredura.Click += new System.EventHandler(this.lblTempoVarredura_Click);
            // 
            // lblEscalaVertical
            // 
            this.lblEscalaVertical.AutoSize = true;
            this.lblEscalaVertical.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblEscalaVertical.Location = new System.Drawing.Point(239, 411);
            this.lblEscalaVertical.Name = "lblEscalaVertical";
            this.lblEscalaVertical.Size = new System.Drawing.Size(85, 15);
            this.lblEscalaVertical.TabIndex = 8;
            this.lblEscalaVertical.Text = "ESCALA VERT.";
            this.lblEscalaVertical.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblEscalaVertical.Click += new System.EventHandler(this.label1_Click);
            // 
            // trkTempoVarredura
            // 
            this.trkTempoVarredura.Location = new System.Drawing.Point(381, 464);
            this.trkTempoVarredura.Maximum = 20;
            this.trkTempoVarredura.Minimum = 1;
            this.trkTempoVarredura.Name = "trkTempoVarredura";
            this.trkTempoVarredura.Size = new System.Drawing.Size(919, 45);
            this.trkTempoVarredura.TabIndex = 10;
            this.trkTempoVarredura.Value = 1;
            this.trkTempoVarredura.Scroll += new System.EventHandler(this.trkTempoVarredura_Scroll);
            // 
            // trkEscalaVertical
            // 
            this.trkEscalaVertical.Location = new System.Drawing.Point(330, 54);
            this.trkEscalaVertical.Minimum = 1;
            this.trkEscalaVertical.Name = "trkEscalaVertical";
            this.trkEscalaVertical.Orientation = System.Windows.Forms.Orientation.Vertical;
            this.trkEscalaVertical.Size = new System.Drawing.Size(45, 383);
            this.trkEscalaVertical.TabIndex = 11;
            this.trkEscalaVertical.Value = 1;
            this.trkEscalaVertical.Scroll += new System.EventHandler(this.trkEscalaVertical_Scroll);
            // 
            // txtAmostras
            // 
            this.txtAmostras.Location = new System.Drawing.Point(114, 79);
            this.txtAmostras.Name = "txtAmostras";
            this.txtAmostras.Size = new System.Drawing.Size(100, 20);
            this.txtAmostras.TabIndex = 13;
            // 
            // txtVref
            // 
            this.txtVref.Location = new System.Drawing.Point(114, 105);
            this.txtVref.Name = "txtVref";
            this.txtVref.Size = new System.Drawing.Size(100, 20);
            this.txtVref.TabIndex = 15;
            // 
            // timer1
            // 
            this.timer1.Interval = 1;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // chkAltaVelocidade
            // 
            this.chkAltaVelocidade.AutoSize = true;
            this.chkAltaVelocidade.Location = new System.Drawing.Point(126, 242);
            this.chkAltaVelocidade.Name = "chkAltaVelocidade";
            this.chkAltaVelocidade.Size = new System.Drawing.Size(100, 17);
            this.chkAltaVelocidade.TabIndex = 17;
            this.chkAltaVelocidade.Text = "Alta Velocidade";
            this.chkAltaVelocidade.UseVisualStyleBackColor = true;
            this.chkAltaVelocidade.CheckedChanged += new System.EventHandler(this.chkAltaVelocidade_CheckedChanged);
            // 
            // chkDebug
            // 
            this.chkDebug.AutoSize = true;
            this.chkDebug.Location = new System.Drawing.Point(12, 318);
            this.chkDebug.Name = "chkDebug";
            this.chkDebug.Size = new System.Drawing.Size(64, 17);
            this.chkDebug.TabIndex = 18;
            this.chkDebug.Text = "DEBUG";
            this.chkDebug.UseVisualStyleBackColor = true;
            this.chkDebug.CheckedChanged += new System.EventHandler(this.chkDebug_CheckedChanged);
            // 
            // chkMarcadores
            // 
            this.chkMarcadores.AutoSize = true;
            this.chkMarcadores.Location = new System.Drawing.Point(220, 81);
            this.chkMarcadores.Name = "chkMarcadores";
            this.chkMarcadores.Size = new System.Drawing.Size(82, 17);
            this.chkMarcadores.TabIndex = 19;
            this.chkMarcadores.Text = "Marcadores";
            this.chkMarcadores.UseVisualStyleBackColor = true;
            this.chkMarcadores.CheckedChanged += new System.EventHandler(this.chkMarcadores_CheckedChanged);
            // 
            // btnSair
            // 
            this.btnSair.Location = new System.Drawing.Point(1291, 610);
            this.btnSair.Name = "btnSair";
            this.btnSair.Size = new System.Drawing.Size(75, 23);
            this.btnSair.TabIndex = 20;
            this.btnSair.Text = "SAIR";
            this.btnSair.UseVisualStyleBackColor = true;
            this.btnSair.Click += new System.EventHandler(this.btnSair_Click_1);
            // 
            // txtCOM
            // 
            this.txtCOM.Location = new System.Drawing.Point(12, 341);
            this.txtCOM.Multiline = true;
            this.txtCOM.Name = "txtCOM";
            this.txtCOM.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtCOM.Size = new System.Drawing.Size(224, 144);
            this.txtCOM.TabIndex = 21;
            // 
            // txtDadosSerial
            // 
            this.txtDadosSerial.Location = new System.Drawing.Point(114, 53);
            this.txtDadosSerial.Name = "txtDadosSerial";
            this.txtDadosSerial.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtDadosSerial.Size = new System.Drawing.Size(100, 20);
            this.txtDadosSerial.TabIndex = 22;
            this.txtDadosSerial.TextChanged += new System.EventHandler(this.txtDadosSerial_TextChanged);
            // 
            // lblMediaVolts
            // 
            this.lblMediaVolts.AutoSize = true;
            this.lblMediaVolts.Location = new System.Drawing.Point(980, 9);
            this.lblMediaVolts.Name = "lblMediaVolts";
            this.lblMediaVolts.Size = new System.Drawing.Size(88, 13);
            this.lblMediaVolts.TabIndex = 23;
            this.lblMediaVolts.Text = "TENSÃO MÉDIA";
            // 
            // groupBox1
            // 
            this.groupBox1.BackColor = System.Drawing.SystemColors.AppWorkspace;
            this.groupBox1.Controls.Add(this.button2);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.btnFecharPorta);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.chkMarcadores);
            this.groupBox1.Controls.Add(this.txtDadosSerial);
            this.groupBox1.Controls.Add(this.txtAmostras);
            this.groupBox1.Controls.Add(this.txtVref);
            this.groupBox1.Controls.Add(this.btnAbrirPorta);
            this.groupBox1.Controls.Add(this.btnDisparo);
            this.groupBox1.Location = new System.Drawing.Point(12, 30);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(303, 203);
            this.groupBox1.TabIndex = 24;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "CONFIGURAÇÃO DA PORTA";
            this.groupBox1.Enter += new System.EventHandler(this.groupBox1_Enter);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(190, 145);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(107, 23);
            this.button2.TabIndex = 26;
            this.button2.Text = "Exportar CSV";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 108);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(81, 13);
            this.label3.TabIndex = 25;
            this.label3.Text = "Tensão de Ref.";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 82);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(105, 13);
            this.label2.TabIndex = 24;
            this.label2.Text = "Número de Amostras";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 56);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(91, 13);
            this.label1.TabIndex = 23;
            this.label1.Text = "Selecione a Porta";
            this.label1.Click += new System.EventHandler(this.label1_Click_1);
            // 
            // groupBox2
            // 
            this.groupBox2.BackColor = System.Drawing.SystemColors.ActiveBorder;
            this.groupBox2.Controls.Add(this.radioButton1);
            this.groupBox2.Controls.Add(this.rdoAcoplamentoDC);
            this.groupBox2.Location = new System.Drawing.Point(12, 239);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(108, 49);
            this.groupBox2.TabIndex = 25;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Acoplamento";
            this.groupBox2.UseCompatibleTextRendering = true;
            this.groupBox2.Enter += new System.EventHandler(this.groupBox2_Enter);
            // 
            // radioButton1
            // 
            this.radioButton1.AutoSize = true;
            this.radioButton1.Location = new System.Drawing.Point(55, 19);
            this.radioButton1.Name = "radioButton1";
            this.radioButton1.Size = new System.Drawing.Size(39, 17);
            this.radioButton1.TabIndex = 27;
            this.radioButton1.TabStop = true;
            this.radioButton1.Text = "AC";
            this.radioButton1.UseVisualStyleBackColor = true;
            this.radioButton1.CheckedChanged += new System.EventHandler(this.radioButton1_CheckedChanged);
            // 
            // rdoAcoplamentoDC
            // 
            this.rdoAcoplamentoDC.AutoSize = true;
            this.rdoAcoplamentoDC.Location = new System.Drawing.Point(9, 19);
            this.rdoAcoplamentoDC.Name = "rdoAcoplamentoDC";
            this.rdoAcoplamentoDC.Size = new System.Drawing.Size(40, 17);
            this.rdoAcoplamentoDC.TabIndex = 26;
            this.rdoAcoplamentoDC.TabStop = true;
            this.rdoAcoplamentoDC.Text = "DC";
            this.rdoAcoplamentoDC.UseVisualStyleBackColor = true;
            this.rdoAcoplamentoDC.CheckedChanged += new System.EventHandler(this.rdoAcoplamentoDC_CheckedChanged);
            // 
            // btnTrigger
            // 
            this.btnTrigger.Enabled = false;
            this.btnTrigger.Location = new System.Drawing.Point(126, 265);
            this.btnTrigger.Name = "btnTrigger";
            this.btnTrigger.Size = new System.Drawing.Size(100, 23);
            this.btnTrigger.TabIndex = 26;
            this.btnTrigger.Text = "TRIGGER";
            this.btnTrigger.UseVisualStyleBackColor = true;
            // 
            // btnFFT
            // 
            this.btnFFT.Enabled = false;
            this.btnFFT.Location = new System.Drawing.Point(126, 295);
            this.btnFFT.Name = "btnFFT";
            this.btnFFT.Size = new System.Drawing.Size(75, 23);
            this.btnFFT.TabIndex = 27;
            this.btnFFT.Text = "FFT";
            this.btnFFT.UseVisualStyleBackColor = true;
            this.btnFFT.Click += new System.EventHandler(this.button3_Click);
            // 
            // Form1
            // 
            this.ClientSize = new System.Drawing.Size(1378, 645);
            this.Controls.Add(this.btnFFT);
            this.Controls.Add(this.btnTrigger);
            this.Controls.Add(this.lblMediaVolts);
            this.Controls.Add(this.txtCOM);
            this.Controls.Add(this.btnSair);
            this.Controls.Add(this.chkDebug);
            this.Controls.Add(this.chkAltaVelocidade);
            this.Controls.Add(this.trkEscalaVertical);
            this.Controls.Add(this.trkTempoVarredura);
            this.Controls.Add(this.lblEscalaVertical);
            this.Controls.Add(this.lblTempoVarredura);
            this.Controls.Add(this.lblMaxVolts);
            this.Controls.Add(this.lblFreq);
            this.Controls.Add(this.chart1);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.groupBox2);
            this.Name = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trkTempoVarredura)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trkEscalaVertical)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #region MANIPULADORES DE EVENTOS
        private void button1_Click(object sender, EventArgs e)
        {
            AbrirPorta();
        }

        private void btnDisparo_Click(object sender, EventArgs e)
        {
            btnFFT.Enabled = true;
            btnTrigger.Enabled = true;

            numAmostras = Convert.ToInt32(txtAmostras.Text);

            if (chkAltaVelocidade.Checked)
            {
                mSecPorAmostra = 0.02;
            }
            else
            {
                mSecPorAmostra = 0.1;
            }

            PortaArduino.WriteLine("S" + numAmostras.ToString());

            lblTempoVarredura.Text = Convert.ToString(Convert.ToInt32(numAmostras * mSecPorAmostra *
                Convert.ToDouble(trkTempoVarredura.Value))) + " mSeg.";
            yAxisMax = 0.5 * Convert.ToDouble(trkEscalaVertical.Value);
            lblEscalaVertical.Text = "Escala X" + 0.2 * yAxisMax;
            chart1.ChartAreas["AreaGrafico1"].AxisY.Maximum = yAxisMax;

            duracaoBurstMSec = Convert.ToInt32(numAmostras * mSecPorAmostra *
                Convert.ToDouble(trkTempoVarredura.Value));
            vref = Convert.ToDouble(txtVref.Text);

            PortaArduino.WriteLine("B" + duracaoBurstMSec.ToString());

            LerBurstArduino();

            timer1.Enabled = true;
            timer1.Interval = duracaoBurstMSec + tempoBufferVarreduraMSec;
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (chkMarcadores.Checked)
            {
                chart1.Series["Serie1"].MarkerStyle = MarkerStyle.Circle;
                chart1.Series["Serie1"].MarkerColor = Color.Red;
            }
            else
            {
                chart1.Series["Serie1"].MarkerStyle = MarkerStyle.None;
            }

            yAxisMax = 0.5 * Convert.ToDouble(trkEscalaVertical.Value);
            lblEscalaVertical.Text = "Escala X" + 0.2 * yAxisMax;
            chart1.ChartAreas["AreaGrafico1"].AxisY.Maximum = yAxisMax;
            chart1.ChartAreas["AreaGrafico1"].AxisY.Minimum = -yAxisMax;

            lblTempoVarredura.Text = Convert.ToString(Convert.ToInt32(numAmostras * mSecPorAmostra *
                Convert.ToDouble(trkTempoVarredura.Value))) + " mSeg.";

            PortaArduino.WriteLine("B" + duracaoBurstMSec.ToString());

            cronometroBurstArduino.Restart();
            LerBurstArduino();
            cronometroBurstArduino.Stop();

            var tempoBurst = cronometroBurstArduino.ElapsedMilliseconds;

            timer1.Interval = Convert.ToInt16(tempoBurst) + tempoBufferVarreduraMSec;

            if (chkDebug.Checked == true)
            {
                txtCOM.Invoke(new MethodInvoker(
                    delegate
                    {

                        txtCOM.Text = "Tempo total de varredura + plotagem = " +
                        tempoBurst + " mSeg" + Environment.NewLine +
                        "Tempo total do buffer = " + tempoBufferVarreduraMSec + " mSeg" +
                        Environment.NewLine + "Intervalo do temporizador = " + timer1.Interval + " mSeg" + Environment.NewLine
                        + processador.arrayDoubleProcessada.Length;
                    }
                    ));
            }
        }

        private void btnSair_Click_1(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void btnFecharPorta_Click_1(object sender, EventArgs e)
        {
            PortaArduino.Close();
            btnAbrirPorta.Enabled = true;
            btnFecharPorta.Enabled = true;
            btnFecharPorta.Visible = true;
            btnDisparo.Enabled = true;
            btnDisparo.Visible = true;
        }

        private void trkTempoVarredura_Scroll(object sender, EventArgs e)
        {
            duracaoBurstMSec = Convert.ToInt32(numAmostras * mSecPorAmostra *
                Convert.ToDouble(trkTempoVarredura.Value));
            timer1.Interval = duracaoBurstMSec + tempoBufferVarreduraMSec;
        }

        private void chkAltaVelocidade_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void chkDebug_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void rdoAcoplamentoDC_CheckedChanged(object sender, EventArgs e)
        {
            acoplamentoDC = true;
        }

        private void lblTempoVarredura_Click(object sender, EventArgs e)
        {

        }

        private void lblMaxVolts_Click(object sender, EventArgs e)
        {

        }

        private void lblFreq_Click(object sender, EventArgs e)
        {

        }

        private void lblMediaVolts_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void chkMarcadores_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void trkEscalaVertical_Scroll(object sender, EventArgs e)
        {

        }

        private void chart1_Click(object sender, EventArgs e)
        {

        }

        private void txtDadosSerial_TextChanged(object sender, EventArgs e)
        {

        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void label1_Click_1(object sender, EventArgs e)
        {

        }

        private void groupBox2_Enter(object sender, EventArgs e)
        {

        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            acoplamentoDC = false;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            processador.ObterSaidaCSV();
        }

        private void chart1_MouseClick(object sender, MouseEventArgs e)
        {
            Chart chart = (Chart)sender;
            ChartArea chartArea = chart.ChartAreas["AreaGrafico1"];

 
            double yValue = chartArea.AxisY.PixelPositionToValue(e.Y);

    
            DrawHorizontalLine(chart, chartArea, yValue);

          
            TensaoTrigger = Convert.ToInt32(Math.Round(Math.Abs(yValue) * 1023 / vref));
            if (acoplamentoDC == true)
            {
                PortaArduino.WriteLine("T" + (TensaoTrigger).ToString());
            }
            else
            {
                PortaArduino.WriteLine("T" + (TensaoTrigger + Convert.ToInt32(processador.ObterMedia())).ToString());
            }

        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                string csvFilePath = processador.ObterSaidaCSV();

                string fftAnalysisExecutable = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fft_analysis.exe");

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = fftAnalysisExecutable,
                    Arguments = csvFilePath,
                    UseShellExecute = true,
                    CreateNoWindow = false
                };


                Process.Start(startInfo);


                MessageBox.Show("FFT analise realizada com sucesso.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {

                MessageBox.Show($"Ocorreu um erro: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


    }
}


#endregion

