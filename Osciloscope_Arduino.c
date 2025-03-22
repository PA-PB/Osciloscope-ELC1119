#include <avr/io.h>
#include <util/delay.h>
#include <stdlib.h>
#include <string.h>
#include <stdio.h>
#include <avr/interrupt.h>

#define F_CPU 16000000UL
#define BAUD 2000000
#define MYUBRR F_CPU/8/BAUD-1

#define cbi(sfr, bit) (_SFR_BYTE(sfr) &= ~_BV(bit))
#define sbi(sfr, bit) (_SFR_BYTE(sfr) |= _BV(bit))

volatile unsigned int numBurstSamples = 500;
volatile unsigned long burstDurationmSec = 0;
volatile unsigned long microsCount = 0;
volatile uint8_t delay_flag = 0;

void uart_init(unsigned int ubrr) {
   
    UBRR0H = (unsigned char)(ubrr >> 8);
    UBRR0L = (unsigned char)ubrr;
  
    UCSR0A = (1 << U2X0);
    
    UCSR0B = (1 << RXEN0) | (1 << TXEN0);
    
    UCSR0C = (1 << UCSZ01) | (1 << UCSZ00);
}

void uart_transmit(unsigned char data) {

    while (!(UCSR0A & (1 << UDRE0)));
    UDR0 = data;
}

unsigned char uart_receive(void) {
    while (!(UCSR0A & (1 << RXC0)));
    return UDR0;
}

void uart_print(const char *str) {
    while (*str) {
        uart_transmit(*str++);
    }
}

void uart_println(const char *str) {
    uart_print(str);
    uart_transmit('\n');
}

void analog_init() {
    ADCSRA |= (1 << ADPS2) | (1 << ADPS1) | (1 << ADPS0);
    ADMUX = (1 << REFS0);
    ADCSRA |= (1 << ADEN);
}

unsigned int analog_read(uint8_t pin) {
    ADMUX = (ADMUX & 0xF0) | (pin & 0x0F);
    sbi(ADCSRA, ADSC);
    while (ADCSRA & (1 << ADSC));
    return ADC;
}

void timer1_init() {
    TCCR1A = 0;                      
    TCCR1B = (1 << CS11);         
    TIMSK1 = (1 << OCIE1A);         
    TCNT1 = 0;                        
    sei();                             
}

ISR(TIMER1_COMPA_vect) {
    delay_flag = 1;
}

void delayuseconds(uint16_t us) {
    cli();                             
    delay_flag = 0;                   
    TCNT1 = 0;                         
    OCR1A = us * 2;                    
    sei();                            
    while (!delay_flag);               
}


void GrabBurstandSend() {
    unsigned int val[numBurstSamples];
    unsigned long burstSampleDelayuSec = 0;
    char buffer[5];

    burstSampleDelayuSec = ((burstDurationmSec * 1000UL) / numBurstSamples)-100;

    for (int i = 0; i < numBurstSamples; i++) {
        val[i] = analog_read(0); 
        delayuseconds(burstSampleDelayuSec);
    }


    for (int i = 0; i < numBurstSamples; i++) {
        snprintf(buffer, sizeof(buffer), "%u", val[i]);
        uart_println(buffer);
    }
    uart_println("FIM");

}

int main(void) {
    char receiveString[100];
    int receiveIndex = 0;
    uart_init(MYUBRR);
    analog_init();
    timer1_init();  
    sei();  

    while (1) {
        if (UCSR0A & (1 << RXC0)) {
            char c = uart_receive();
            if (c == '\n') {
                receiveString[receiveIndex] = '\0';
                if (receiveString[0] == 'S') {
                    numBurstSamples = atoi(&receiveString[1]);
                } else if (receiveString[0] == 'B') {
                    burstDurationmSec = atol(&receiveString[1]);
                    GrabBurstandSend();
                }
                receiveIndex = 0;
            } else {
                receiveString[receiveIndex++] = c;
                if (receiveIndex >= sizeof(receiveString) - 1) {
                    receiveIndex = sizeof(receiveString) - 1;
                }
            }
        }
    }

    return 0;
}