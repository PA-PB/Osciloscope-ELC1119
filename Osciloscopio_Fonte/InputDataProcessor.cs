using System;
using System.Collections.Generic;
using System.IO;

namespace Osciloscopio
{
    public class ProcessadorDadosEntrada
    {
        #region Propriedades
        public List<string> listaStringEntrada { get; set; }

        public int[] arrayIntEntrada { get; set; }

        public double[,] arrayDoubleProcessada { get; set; }

        public string[] arrayStringProcessada { get; set; }

        public double mediaBurst;

        public double tempoInicio;
        public double tempoFim;
        public double freq;
        #endregion

        #region CONSTRUTOR
        public ProcessadorDadosEntrada(List<string> ListaStringEntrada)
        {
            this.arrayIntEntrada = new int[ListaStringEntrada.Count];
            this.arrayDoubleProcessada = new double[ListaStringEntrada.Count, 2];
            this.arrayStringProcessada = new string[ListaStringEntrada.Count];
            this.listaStringEntrada = ListaStringEntrada;
        }
        #endregion

        #region MÉTODOS
        public void ParsarCSVBurst()
        {
            int i = 0;

            foreach (string s in this.listaStringEntrada)
            {
                string valString = s.TrimEnd('\r', '\n');

                if (int.TryParse(valString, out this.arrayIntEntrada[i])) { }
                else
                {
                    this.arrayIntEntrada[i] = 0;
                }

                i++;
            }
        }
        public void EscalarValoresBurst(int intervaloEscala, double Vref)
        {
            double intervaloDouble = Convert.ToDouble(intervaloEscala);

            for (int i = 0; i < this.arrayIntEntrada.Length; i++)
            {
                double amostraDouble = Convert.ToDouble(this.arrayIntEntrada[i]);
                this.arrayDoubleProcessada[i, 1] = Vref * amostraDouble / intervaloDouble;
            }
        }
        public void ZerarTemposBurst(double intervaloAmostraMSec)
        {
            double tempoAmostra = 0.0;

            for (int i = 0; i < this.arrayIntEntrada.Length; i++)
            {
                this.arrayDoubleProcessada[i, 0] = tempoAmostra;
                tempoAmostra += intervaloAmostraMSec;
            }
        }

        public double ObterMax()
        {
            double maxVolts = 0.0;

            for (int i = 0; i < this.arrayIntEntrada.Length; i++)
            {
                if (this.arrayDoubleProcessada[i, 1] > maxVolts)
                {
                    maxVolts = this.arrayDoubleProcessada[i, 1];
                }
            }
            return maxVolts;
        }
        public double ObterMedia()
        {
            double somaArray = 0.0;

            for (int i = 0; i < this.arrayIntEntrada.Length; i++)
            {
                somaArray += arrayDoubleProcessada[i, 1];
            }

            return mediaBurst = somaArray / Convert.ToDouble(arrayIntEntrada.Length);
        }
        public void ObterAcoplamentoCA()
        {
            ObterMedia();

            for (int i = 0; i < this.arrayIntEntrada.Length; i++)
            {
                arrayDoubleProcessada[i, 1] = arrayDoubleProcessada[i, 1] - mediaBurst;
            }
        }
        public double ObterFreq()
        {
            bool inicioDetectado = false;
            tempoInicio = 0.0;
            tempoFim = 0.0;
            freq = 0.0; // Initialize freq to 0.0

            for (int i = 1; i < this.arrayIntEntrada.Length - 1; i++) // Start from i = 1 to avoid out-of-bounds error
            {
                if (arrayDoubleProcessada[i - 1, 1] < 0 && arrayDoubleProcessada[i, 1] > 0)
                {
                    if (!inicioDetectado)
                    {
                        tempoInicio = arrayDoubleProcessada[i, 0];
                        inicioDetectado = true;
                    }
                    else
                    {
                        tempoFim = arrayDoubleProcessada[i, 0];
                        freq = 1000.0 / (tempoFim - tempoInicio);
                        return freq;
                    }
                }
            }

            return freq; // Return 0.0 if the frequency is not detected
        }
        public string ObterSaidaCSV()

        {
            string fileName = $"output_{DateTime.Now:yyyyMMddHHmmssfff}.csv";
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);



            string[] headers = { "Time", "Voltage" };

            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.WriteLine(string.Join(",", headers));

                for (int i = 0; i < arrayDoubleProcessada.GetLength(0); i++)
                {
                    writer.WriteLine($"{arrayDoubleProcessada[i, 0]},{arrayDoubleProcessada[i, 1]}");
                }
            }
            return filePath;
        }
        #endregion
    }
}
