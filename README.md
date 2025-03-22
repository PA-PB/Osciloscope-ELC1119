# 🧪 Osciloscópio com Arduino Mega 2560 – SLP

Este projeto foi desenvolvido como parte da disciplina de **Sistemas Lógicos Programáveis (SLP)**, com o objetivo de criar um osciloscópio funcional utilizando um **Arduino Mega 2560** e uma interface em C# para visualização de dados no PC.

---

## 🧩 O Problema

Os plotters gráficos integrados ao Arduino IDE são bastante limitados, dificultando medições mais precisas e funcionalidades essenciais como análise de frequência e visualização detalhada da forma de onda.

---

## 🛠️ A Solução

O projeto consiste em simular um **osciloscópio digital**, com os seguintes componentes principais:

1. **Aquisição de sinais analógicos**
   - Usando o conversor AD do ATMega2560 (ADC)
   - Definição de amostragem com base no Teorema de Nyquist

2. **Transmissão de dados**
   - Via comunicação serial USART
   - Otimizada para reduzir tempo de envio com uso de buffers

3. **Temporização**
   - Utilização do Timer1 para geração de delays precisos

4. **Interface com o PC**
   - Desenvolvida em C# (Windows Forms)
   - Permite envio de comandos, visualização gráfica, exportação `.csv` e cálculo de métricas

---

## 🔍 Resultados

- Foi possível capturar sinais e visualizá-los no computador de maneira funcional.
- O sistema apresenta limitações quanto à frequência máxima amostrada (~5 kHz).
- Alguns erros de medição foram notados, especialmente relacionados à ausência de trigger e precisão do tempo.
- Mesmo assim, o sistema se mostrou **viável, acessível e funcional para fins educacionais e hobbystas**.

---

## 📄 Relatório Completo

[📥 Clique aqui para baixar o relatório em PDF](./Relatório_TrabalhoSLP.pdf)
