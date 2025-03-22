using System.Windows.Forms.DataVisualization.Charting;

namespace Osciloscopio
{
    partial class Form1
    {
        private System.Windows.Forms.Button btnFecharPorta;
        private System.Windows.Forms.Button btnAbrirPorta;
        private System.Windows.Forms.Button btnDisparo;
        private System.Windows.Forms.Label lblFreq;
        private System.Windows.Forms.Label lblMaxVolts;
        private System.Windows.Forms.Label lblTempoVarredura;
        private System.Windows.Forms.Label lblEscalaVertical;
        private System.Windows.Forms.TrackBar trkTempoVarredura;
        private System.Windows.Forms.TrackBar trkEscalaVertical;
        private System.Windows.Forms.TextBox txtAmostras;
        private System.Windows.Forms.TextBox txtVref;
        private System.ComponentModel.IContainer components;
        private System.Windows.Forms.CheckBox chkAltaVelocidade;
        private System.Windows.Forms.CheckBox chkDebug;
        private System.Windows.Forms.CheckBox chkMarcadores;
        private System.Windows.Forms.Button btnSair;
        private System.Windows.Forms.TextBox txtCOM;
        private System.Windows.Forms.TextBox txtDadosSerial;
        private Chart chart1;
        public System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Label lblMediaVolts;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.RadioButton radioButton1;
        private System.Windows.Forms.RadioButton rdoAcoplamentoDC;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button btnTrigger;
        private System.Windows.Forms.Button btnFFT;
    }
}
