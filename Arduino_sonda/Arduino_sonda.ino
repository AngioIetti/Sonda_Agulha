#include <SPI.h> //necessário para o ADS1248
#include <math.h>

/*********** definições de constantes do programa *******************/
#define ADS1248_NCANAIS 4

int ADS_INTERVALO = 250; // minimo 
const uint8_t LED_STATUS = 13; //led da própria placa
const uint8_t ADS_SLAVE_SELECT = 8;// 10: UNO, 53=DUE saulo
//Instead of one Chip Select (SS) pin, the Due supports three.  These are hardware Digital Pin 10 (SPI-CS0), Digital Pin 4 (SPI-CS1) and Digital 52 (SPI-CS2).  
//These pin values are used in the Due SPI library calls to determine which hardware SPI device you are addressing (up to three at the same time).


bool send_data = false;
String received_data;

long long tempo_ms = 0;
long long tempo_ms_anterior = 0;

//variaveis do ads1248 
double ads1248_ganhoCanal[4]; //vetor de configuração de ganho por canal
float ADS_V0, ADS_V1, ADS_V2, ADS_V3; //4 canais com 8 entradas diferenciais


typedef enum {

  AJUSTE_GANHO_CH0 = 0,
  LEITURA_CH0,
  AJUSTE_GANHO_CH1,
  LEITURA_CH1,
  AJUSTE_GANHO_CH2,
  LEITURA_CH2,
  AJUSTE_GANHO_CH3,
  LEITURA_CH3,
  MOSTRA_DADOS
} ADS_STATES;

ADS_STATES estado_atual = AJUSTE_GANHO_CH0;

void SPI_init();
void ads1248_clear();
void ads1248_ajuste_ganho(uint8_t canal);
double ads1248_ler_amplificador(uint8_t canal);
bool ads1248_ajustar_canal_ganho(int MUX_SP, int MUX_SN, int PGA);
double ads1248_ler(int ganho);
//====================================================================================================
void setup() 
{

  //Serial que se comunica com o software no PC
  Serial.begin(9600);

  pinMode(LED_STATUS, OUTPUT);
  digitalWrite(LED_STATUS, LOW);

  ads1248_ganhoCanal[0] = 4;  //2 elev 4 = 16 (datasheet)
  ads1248_ganhoCanal[1] = 4;
  ads1248_ganhoCanal[2] = 4;
  ads1248_ganhoCanal[3] = 4;

  //SPI
  pinMode(ADS_SLAVE_SELECT, OUTPUT);
  digitalWrite(ADS_SLAVE_SELECT, HIGH);
  
  SPI_init();
  ads1248_clear();
  

}

//====================================================================================================

void loop() 
{
  if(Serial.available()){
      received_data = Serial.readString(); //Lê a string recebida
      
      if(received_data == "Connect\n"){
        Serial.println("Connected");
      }
      else if(received_data == "Start\n"){
        send_data = true;
      }
      else if(received_data == "Stop\n"){
        send_data = false;
      }
  }

  
  tempo_ms = millis();

  if ((tempo_ms - tempo_ms_anterior) >= ADS_INTERVALO)
  {
       tempo_ms_anterior = tempo_ms;
       
       switch (estado_atual) 
       {

        case AJUSTE_GANHO_CH0:
        //delay(1000);
        ads1248_ajuste_ganho(0); 
       // Serial.print("1");
        estado_atual = LEITURA_CH0;
        break;

        case LEITURA_CH0:
        ADS_V0 = ads1248_ler_amplificador(0);
        if(send_data){Serial.println(ADS_V0);}
        //Serial.print("2");
        //estado_atual = AJUSTE_GANHO_CH1;
        estado_atual = AJUSTE_GANHO_CH0;
        break;

        case AJUSTE_GANHO_CH1:
        ads1248_ajuste_ganho(1); 
        //Serial.print("3");
        estado_atual = LEITURA_CH1;
        break;

        case LEITURA_CH1:
        ADS_V1 = ads1248_ler_amplificador(1);
        //Serial.print("4");
        estado_atual = AJUSTE_GANHO_CH2;
        break;

        case AJUSTE_GANHO_CH2:
        ads1248_ajuste_ganho(2); 
        //Serial.print("5");
        estado_atual = LEITURA_CH2;
        break;

        case LEITURA_CH2:
        ADS_V2 = ads1248_ler_amplificador(2);
       // Serial.print("6");
        estado_atual = AJUSTE_GANHO_CH3;
        break;

        case AJUSTE_GANHO_CH3:
        ads1248_ajuste_ganho(3); 
        //Serial.print("7");
        estado_atual = LEITURA_CH3;
        break;

        case LEITURA_CH3:
        ADS_V3 = ads1248_ler_amplificador(3);
        //Serial.print("8");
        estado_atual = MOSTRA_DADOS;
        break;


       case MOSTRA_DADOS:

        Serial.print("Ch0: ");
        Serial.print(ADS_V0);
        Serial.print("     Ch1: ");
        Serial.print(ADS_V1);
        Serial.print("     Ch2: ");
        Serial.print(ADS_V2);
        Serial.print("     Ch3: ");
        Serial.println(ADS_V3);

        estado_atual = AJUSTE_GANHO_CH0;
        break;

      default:
      break;
      }

  }
}

//====================================================================================================

void SPI_init()   //* Init SPI interface*/
{
//  SPI.begin();
//  SPI.setDataMode(SPI_MODE1);
//  SPI.setClockDivider(SPI_CLOCK_DIV16); //84
//  //SPI.beginTransaction(SPISettings(1000000, MSBFIRST, SPI_MODE1));
    SPI.begin();
    
    SPI.setDataMode(SPI_MODE1);
    SPI.setClockDivider(84);
}

//====================================================================================================

void ads1248_clear() /* clear ads1248 registers */
{
  digitalWrite(ADS_SLAVE_SELECT, LOW);
  SPI.transfer(0x06);
  delay(2); //2  ssssssssssssssssss
  digitalWrite(ADS_SLAVE_SELECT, HIGH);
  digitalWrite(ADS_SLAVE_SELECT, LOW);
  SPI.transfer(0x41);
  SPI.transfer(0X00);
  SPI.transfer(0xAA);  //??
  digitalWrite(ADS_SLAVE_SELECT, HIGH);
  digitalWrite(ADS_SLAVE_SELECT, LOW);
  SPI.transfer(0x42);
  SPI.transfer(0X00);
  SPI.transfer(0x30); //??
  digitalWrite(ADS_SLAVE_SELECT, HIGH);
}

//====================================================================================================

void ads1248_ajuste_ganho(uint8_t canal) /* clear ads1248 registers */
{
  int canal_positivo_ads1248, canal_negativo_ads1248;
  bool scratch_ads1248; //aparentemente eh uma variavel sem uso


  canal_positivo_ads1248 = canal * 2;
  canal_negativo_ads1248 = (canal * 2) + 1;
 
  scratch_ads1248 = ads1248_ajustar_canal_ganho(canal_positivo_ads1248, canal_negativo_ads1248, ads1248_ganhoCanal[canal]);
    
  //return temp_ADS_V;
  //return;
}


//====================================================================================================

double ads1248_ler_amplificador(uint8_t canal) /* clear ads1248 registers */
{
  double temp_ads1248_ad  = 0;
  temp_ads1248_ad = (ads1248_ler(ads1248_ganhoCanal[canal]) * 1000000); //converte uV para numeros legíveis (1e6)

  return temp_ads1248_ad;
}

//====================================================================================================
bool ads1248_ajustar_canal_ganho(int MUX_SP, int MUX_SN, int PGA) /* clear ads1248 registers */
{
  int BCS, DOR;
  int MUX0_word_write, SYS0_word_write, MUX0_word_read, SYS0_word_read;
  int contador_tentativas;
  bool registro_MUX0_ok, registro_SYS0_ok;
  registro_MUX0_ok = false;
  registro_SYS0_ok = false;
  contador_tentativas = 0;
  while (registro_MUX0_ok == false && contador_tentativas < 10) 
  {
    BCS = 0;
    MUX0_word_write = (BCS << 6) | (MUX_SP << 3) | MUX_SN;
    digitalWrite(ADS_SLAVE_SELECT, LOW);
    SPI.transfer(0x40);
    SPI.transfer(0x00);
    SPI.transfer(MUX0_word_write);
    digitalWrite(ADS_SLAVE_SELECT, HIGH);
    digitalWrite(ADS_SLAVE_SELECT, LOW);
    SPI.transfer(0x20);
    SPI.transfer(0x00);
    MUX0_word_read = SPI.transfer(0xFF);
    digitalWrite(ADS_SLAVE_SELECT, HIGH);
    if (MUX0_word_write == MUX0_word_read)      registro_MUX0_ok = true;
    contador_tentativas++;
  }
  contador_tentativas = 0;
  while (registro_SYS0_ok == false && contador_tentativas < 10) 
  {
    DOR = 0;
    SYS0_word_write = (PGA << 4) | DOR;
    digitalWrite(ADS_SLAVE_SELECT, LOW);
    SPI.transfer(0x43);
    SPI.transfer(0X00);
    SPI.transfer(SYS0_word_write);
    digitalWrite(ADS_SLAVE_SELECT, HIGH);
    digitalWrite(ADS_SLAVE_SELECT, LOW);
    SPI.transfer(0x23);
    SPI.transfer(0x00);
    SYS0_word_read = SPI.transfer(0xFF);
    digitalWrite(ADS_SLAVE_SELECT, HIGH);
    if (SYS0_word_write == SYS0_word_read)      registro_SYS0_ok = true;
    contador_tentativas++;
  }
  if (registro_MUX0_ok && registro_SYS0_ok) 
  {
    return true;
  }
  else {
    return false;
  }
}

//====================================================================================================

double ads1248_ler(int ganho) 
{
  //int MSB, midSB, LSB;
  //unsigned long int palavra;
  long long MSB, midSB, LSB;
  unsigned long int palavra;
  double tensao_ads1248[200];
  double media_tensao_ads1248, media_tensao_ads1248_corrigida;
  int contador_tensoes_ok, numero_leitura_media;
  numero_leitura_media = 10;
  media_tensao_ads1248 = 0;
  media_tensao_ads1248_corrigida = 0;
  contador_tensoes_ok = 0;
  for (int leituras = 1; leituras <= numero_leitura_media; leituras++) {
    digitalWrite(ADS_SLAVE_SELECT, LOW);
    SPI.transfer(0x12);
    MSB = SPI.transfer(0xFF);
    midSB = SPI.transfer(0xFF);
    LSB = SPI.transfer(0xFF);
    digitalWrite(ADS_SLAVE_SELECT, HIGH);
    palavra = (MSB << 16) | (midSB << 8) | LSB;
    palavra = palavra & 0x7FFFFF;
    tensao_ads1248[leituras] = palavra;
    tensao_ads1248[leituras] = tensao_ads1248[leituras] * 2.048/8388607;
    if ((MSB >> 7) == 1)      tensao_ads1248[leituras] = (tensao_ads1248[leituras] - 2.048) / pow(2, ganho);
    if ((MSB >> 7) == 0)      tensao_ads1248[leituras] = (tensao_ads1248[leituras]) / pow(2, ganho);
    media_tensao_ads1248 = media_tensao_ads1248 + tensao_ads1248[leituras];
  }
  ///media_tensao_ads1248_corrigida = media_tensao_ads1248; /// ALTERADO
  media_tensao_ads1248 = media_tensao_ads1248 / numero_leitura_media;
  for ( int leituras = 1; leituras <= numero_leitura_media; leituras++)
  {
    if ( tensao_ads1248[leituras] >= (media_tensao_ads1248 - media_tensao_ads1248*0.1) && tensao_ads1248[leituras] <= (media_tensao_ads1248 + media_tensao_ads1248*0.1))
    {
      media_tensao_ads1248_corrigida = media_tensao_ads1248_corrigida + tensao_ads1248[leituras];
      contador_tensoes_ok++;
    }
  }
  if ( contador_tensoes_ok != 0)
  {
    media_tensao_ads1248_corrigida = media_tensao_ads1248_corrigida/contador_tensoes_ok;
    return media_tensao_ads1248_corrigida;
  }
  else
  {
    return media_tensao_ads1248;
  }
}
