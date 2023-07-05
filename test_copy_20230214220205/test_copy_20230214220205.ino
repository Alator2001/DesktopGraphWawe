uint32_t intcounter =0;

void OnTimerInterrupt() 
{
  intcounter++;
}

void setup()
{
pinMode(PC13, OUTPUT);
Serial.begin(9600);
pinMode(PA6, INPUT);

Timer3.pause();
Timer3.setPrescaleFactor(72);
Timer3.setInputCaptureMode(TIMER_CH1, TIMER_IC_INPUT_DEFAULT);
Timer3.setInputCaptureMode(TIMER_CH2, TIMER_IC_INPUT_SWITCH);
Timer3.setPolarity(TIMER_CH2, 1);
Timer3.setSlaveFlags(TIMER_SMCR_TS_TI1FP1 | TIMER_SMCR_SMS_RESET);
Timer3.refresh();
Timer3.resume();
//Timer3.attachInterrupt(TIMER_CH1, OnTimerInterrupt);
}

void loop()
{
  if(Timer3.getInputCaptureFlag(TIMER_CH2))
  { 
    Serial.println(Timer3.getCompare(TIMER_CH2));
    intcounter = 0;   
    Timer3.refresh();
  }
}

      

